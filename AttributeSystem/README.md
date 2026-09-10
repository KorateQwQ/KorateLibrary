# 角色属性系统

`AttributeSystem` 提供一个可独立使用、也可以挂载到 `ModPlayer` 上的角色属性组件。
设计思路参考 UE GAS 的属性聚合与变化钩子。普通属性遵循 Terraria/tModLoader 常见的
`ResetEffects` 每帧重建模式，资源属性保留当前值并在写入时立即生效。

## 核心概念

属性分为两种：默认的 `AttributeKind.Rebuilt` 每帧重建白值和绿值；
`AttributeKind.Resource` 在组件内保留当前白值，绿值固定为零。
两类属性统一通过 `SetBase` 和 `AddBase` 修改白值，由定义的 `Kind` 决定延迟提交还是立即生效。
两者共享 `AttributeDefinition`、`AttributeSnapshot`、读取入口和变化事件。

每个属性都有三层数值：

- `BaseValue`：白值。普通属性来自等级、装备、技能等来源；资源属性表示当前持有量。
- `ExtraValue`：绿值。普通属性的额外加成，来自增益等效果；资源属性固定为零。
- `FinalValue`：最终用于游戏逻辑的数值。

普通属性的 ExtraValue 支持绝对值和百分比两种加成：

```text
ExtraValue = ExtraFlatValue + BaseValue * ExtraPercentValue
FinalValue = Clamp(BaseValue + ExtraValue)
```

百分比使用小数表示：`0.2f` 表示 20%。例如白值为 100，Buff 提供 20% 加成，
则 ExtraValue 为 20，FinalValue 为 120。

## 挂载到 ModPlayer

仅需要通用属性容器时继承 `AttributeModPlayer`；角色需要急速等通用 RPG 属性时，
继承抽象的 `RPGAttributeModPlayer`。具体角色的专属规则也写在这个派生类中。

当前 `AttributeModPlayer` 继承 `KLModPlayer`，后者继承 tModLoader 的 `ModPlayer`。
属性的重置与提交由 `AttributeModPlayer` 管理；继承网络基类不会自动同步属性数值。

```csharp
using KL.AttributeSystem;

public sealed class MyAttributePlayer : RPGAttributeModPlayer
{
    public override void UpdateEquips()
    {
        // 装备提供 100 点白值。
        Attributes.AddBase(CharacterAttributes.CooldownHaste, 100f);
        base.UpdateEquips();
    }
}
```

职责与继承结构：

```text
AttributeModPlayer                 创建组件、重置、提交、报告异常
└─ RPGAttributeModPlayer            通用 RPG 属性注册、便捷读取和通用规则
   └─ ElainaAttributeModPlayer      伊蕾娜魔力的恢复、消耗、存档和专属约束

KLSkillModPlayer                    技能管理、读取急速结算冷却
└─ ElainaSkillModPlayer             引用 ElainaAttributeModPlayer.Attributes
```

两个属性基类均为抽象类，tModLoader 只挂载具体角色类，不会为继承链创建额外的玩家组件。
`RPGAttributeModPlayer` 不创建第二个 `AttributeComponent`，也不重复执行重置或提交。
通用急速与角色专属魔力存放在同一份 `Attributes` 中，技能、装备和其他消费者引用这一份组件。

`CharacterAttributes` 保留通用属性的静态定义及换算公式，角色实际数值由组件保存。
`RPGAttributeModPlayer` 提供只读的 `CooldownHaste`、`CooldownReductionPercent` 和
`CooldownSpeedMultiplier`，全部从已发布快照计算，不另存数值字段。
`CooldownReductionPercent` 使用小数表示，例如 100 急速对应 0.5；冷却速度倍率对应 2。
通用属性在 RPG 玩家构造时注册，首次提交即可执行规则，即使角色没有技能系统或装备加成。
角色专属属性定义仍放在各自 Mod 中；添加通用属性时扩展 `CharacterAttributes` 和 RPG 层即可。

典型的每帧流程如下：

1. `ResetEffects` 中重置普通属性的白值和 ExtraValue，保留资源属性。
2. 由业务代码在装备、技能、Buff 等钩子中添加本帧加成。
3. `PostUpdate` 中执行全部前置修正；任一失败则整批普通属性不发布。
4. 统一发布全部普通属性，再执行初始化、后置变化通知和资源联动。
5. 业务消费已发布属性；需要本帧结果的逻辑必须放在提交和后置联动完成之后。

整体顺序为：**重置 → 收集来源 → 前置修正 → 全部发布 → 后置联动 → 业务消费**。
每个组件只有一个生命周期负责人。继承 `AttributeModPlayer` 时由基类负责，
其他系统即使持有该组件的引用，也不应再次重置或提交。

派生玩家类需要消费本帧结果时，先调用 `base.PostUpdate()`，再执行消费逻辑。
需要影响本帧普通属性的贡献则应在基类提交前收集。
不同 `ModPlayer` 的 `PostUpdate` 有先后，不能把某个玩家类的 `PostUpdate` 当作全局屏障。
跨玩家类读取默认使用最近一次完整快照；它可能来自上一帧，绝不使用尚未完成修正的候选值。
需要严格的同帧依赖时，应由负责人在提交返回后显式调用消费者，不能依靠玩家类的加载顺序。

如果已有的 `ModPlayer` 不能继承 `AttributeModPlayer`，可以自行实现
`IAttributeProvider`，并在自己的 `ResetEffects` 与 `PostUpdate` 中调用
`ResetForTick()` 和 `Commit()`。

继承 `AttributeModPlayer` 后，可以直接读取属性，不必重复访问 `Attributes`：

```csharp
float haste = player.GetModPlayer<MyAttributePlayer>()
    .GetAttributeValue(CharacterAttributes.CooldownHaste);

AttributeSnapshot snapshot = player.GetModPlayer<MyAttributePlayer>()
    .GetAttribute(CharacterAttributes.CooldownHaste);
```

实现 `IAttributeProvider` 的其他 `ModPlayer` 也可以使用同名扩展方法：

```csharp
float power = player.GetModPlayer<MySkillPlayer>()
    .GetAttributeValue(MyAttributes.Power);
```

如果 `IAttributeProvider.Attributes` 返回 `null`，便捷读取方法会抛出明确的
`InvalidOperationException`，提示该玩家没有绑定属性组件。

`KLSkillModPlayer` 还提供了一个默认返回 `null` 的 `Attributes` 属性。技能玩家可以
覆写它来绑定属性组件；不覆写时不会改变原有技能冷却逻辑。
技能玩家只读取最近一次已发布的急速，不再重置或提交绑定的组件。
如果旧业务仅在技能玩家内持有组件，应迁移到具体的 `RPGAttributeModPlayer` 派生类，
让技能玩家绑定其 `Attributes`。

绑定独立的属性玩家：

```csharp
public sealed class MySkillPlayer : KLSkillModPlayer
{
    public override AttributeComponent Attributes =>
        Player.GetModPlayer<MyAttributePlayer>().Attributes;
}
```

技能玩家只消费绑定的组件，不负责通用属性的注册、重置或提交。
技能的普通属性贡献放在 `ModSkill.UpdateEquips(Player)` 等收集阶段，
不要在 `ResetEffects` 中添加贡献，以免被独立属性玩家的重置覆盖。

`KLSkillModPlayer.PostUpdate` 只登记本帧冷却更新，
`KLSkillManager.PostUpdatePlayers` 等全部玩家完成 `PostUpdate` 后统一结算。
因此无论属性玩家和技能玩家的 `PostUpdate` 谁先执行，冷却都会使用本帧已经发布的急速，
并在本帧玩家的资源联动结束后调用 `ModSkill.PreUpdateCD()`。
属性前置失败时使用上一份完整快照；没有绑定组件时仍按正常速度更新。
组件持有者须在 `PostUpdate` 返回前完成提交；晚于此阶段自行提交的组件不在这个时序保证内。

旧业务若在 `base.PostUpdate()` 后依赖“冷却已经更新”，
需要把这部分逻辑迁移到 `UpdateSkillCooldowns(float deltaTime)` 覆写中，
先调用 `base.UpdateSkillCooldowns(deltaTime)`，再执行后续逻辑。
系统对同一技能玩家每帧只结算一次；未登记、失活或死亡的玩家不结算。
伊蕾娜的 `ElainaSkillModPlayer` 已显式绑定 `ElainaAttributeModPlayer.Attributes`。

## 独立使用

属性组件不依赖 `Player`，普通类也可以直接持有：

```csharp
private readonly AttributeComponent attributes = new();

public void Update()
{
    attributes.ResetForTick();
    attributes.AddBase(MyAttributes.Power, 100f);
    attributes.AddExtraPercent(MyAttributes.Power, 0.2f);
    attributes.Commit();
}
```

独立使用时需要由持有者自己负责每次更新的重置和提交。

## 定义新属性

属性定义通常声明为静态字段，保证整个项目始终使用同一个定义对象：

```csharp
public static class MyAttributes
{
    public static readonly AttributeDefinition Power =
        new("MyMod.Power", defaultBaseValue: 0f, minValue: 0f, maxValue: 100000f);
}
```

建议使用包含 Mod 名称的唯一 ID，方便调试和避免命名冲突。

## 设置白值与添加加成

| 方法 | 含义 |
| --- | --- |
| `SetBase(definition, value)` | 覆盖白值，返回本次写入后的白值；保留已有绿值贡献 |
| `AddBase(definition, value)` | 增加或减少白值，返回本次写入造成的白值差额 |
| `AddExtraFlat(definition, value)` | 累积固定绿值，仅用于普通属性 |
| `AddExtraPercent(definition, value)` | 累积百分比绿值，仅用于普通属性 |

普通属性的白值操作只修改本帧候选值，在 `Commit()` 时统一执行前置钩子和发布。
资源属性通过相同白值接口立即执行前置钩子、裁剪、写入及初始化或变化通知。
`AddBase` 以白值为累加基准，不把包含绿值的最终值再次作为白值叠加。
普通属性方法的返回值尚未经过提交时的修正，不代表最终值；最终结果应从发布后的快照或事件读取。

```csharp
Attributes.SetBase(MyAttributes.Power, 100f);
Attributes.AddBase(MyAttributes.Power, 50f);
Attributes.AddExtraFlat(MyAttributes.Power, 10f);
Attributes.AddExtraPercent(MyAttributes.Power, 0.25f);
```

也可以使用统一接口：

```csharp
Attributes.AddExtra(
    MyAttributes.Power,
    0.25f,
    AttributeModifierKind.Percent);
```

普通属性不应在组件外另存一份最终值作为权威状态。装备卸下、增益消失或技能关闭后，
下一帧重置并重新收集来源，在 `Commit()` 时发布新结果。

`Get()` / `GetFinalValue()` 默认读取最近一次已发布的快照，
与 `GetPublished()` / `GetPublishedFinalValue()` 含义相同。
即使刚执行 `ResetForTick()` 或还在收集加成，也不会读到中间状态。
首次发布前返回定义默认值形成的快照（最终值经过静态范围裁剪），不运行前置钩子，也不发送初始化通知。

计算阶段确实需要查看候选值时，显式使用 `GetPending()` / `GetPendingFinalValue()`；
候选值尚未经过本次提交的前置修正，不应直接用于冷却、伤害等消费逻辑。
`AttributeModPlayer` 和 `IAttributeProvider` 对应提供
`GetPendingAttribute()` / `GetPendingAttributeValue()`。
资源的成功写入立即完成修正与发布，两组读取接口都能取得最新存储结果。

**读取语义迁移：** 旧代码若先 `AddBase` 再 `GetFinalValue` 来计算其他贡献，
需要显式改为候选读取；普通业务读取保留原方法即可使用完整快照。

## 属性变化钩子与首次初始化

`Commit()` 按“本批次全部前置修正 → 全部发布 → 首次初始化或实际变化通知”的顺序执行。
发布后回调读取其他本批次普通属性的 `GetPublishedFinalValue()` 时，得到的都是本批次完整结果，
不会因属性注册顺序而混入上一帧数值。组件级事件会收到所有属性，
也可以通过 `GetAttributeChangeEvent` 只监听一个属性：

| 接口 | 触发条件 |
| --- | --- |
| `PreAttributeChange` | 普通属性在 `Commit()` 内执行，资源在 `SetBase` / `AddBase` 内立即执行；不保证最终产生变化 |
| `PostAttributeChange` | 最终值相对上次发布确实变化后执行 |
| `PostAttributeBaseChange` | 白值相对上次发布确实变化后执行 |
| `AttributeInitialized` | 属性首次发布后只执行一次，没有旧值，不触发后置变化钩子 |

组件和单属性事件入口使用相同的四个名称。变化比较沿用 `0.0001f` 容差。
完整快照变化仍可通过组件的 `AttributeSnapshotChanged` 或单属性的 `SnapshotChanged` 监听。

```csharp
// 使用前面定义的普通属性 Power；资源属性的绿值不能修改。
AttributeChangeEvent powerEvent = Attributes.GetAttributeChangeEvent(MyAttributes.Power);

// 提交前修正。这里的三个来源值都可以修改，Proposed 可随时查看修正后的快照。
powerEvent.PreAttributeChange += (_, args) =>
{
    args.BaseValue = Math.Max(0f, args.BaseValue);
    args.ExtraPercentValue = Math.Max(-0.9f, args.ExtraPercentValue);
};

// 最终值真正变化后执行，不包含首次发布和数值不变的帧。
powerEvent.PostAttributeChange += (_, args) =>
{
    float oldValue = args.Previous.FinalValue;
    float newValue = args.Current.FinalValue;
};

// 首次发布单独通知，可用于建立业务初始状态。
powerEvent.AttributeInitialized += (_, args) =>
{
    float initialValue = args.Current.FinalValue;
};
```

前置钩子先调用组件级处理器，再调用单属性处理器，因此单属性处理器能看到组件级处理器修正后的值。
初始化和后置钩子先执行单属性处理器，再执行同名组件级处理器。
白值和最终值都变化时，先执行白值后置钩子，再执行最终值后置钩子，最后发送完整快照变化通知。

取得单属性事件时也会注册该属性，因此仅订阅、没有添加加成的普通属性也会发布默认值。
事件处理器中可以查询或注册其他属性；本批次开始后新注册的普通属性会从下一次 `Commit()` 开始发布。
提交前修正应通过事件参数完成；发布后通知适合更新资源、UI 等外部状态。
`AttributeInitialized`、`PostAttributeChange` 等后置回调允许立即写入资源，例如最大生命变化时修正当前生命。
提交前回调只能通过事件参数修正当前属性，不能直接调用任何属性写入接口。
在前置、初始化或后置回调中，都不能直接给普通属性调用 `SetBase`、`AddBase` 或 `AddExtra`，
也不能调用 `ResetForTick()` 或递归 `Commit()`。这些操作会抛出异常并按下述规则统一报告。
后置回调仍允许写入资源；属性查询和订阅不受限制。
同一资源写入调用链中不允许再次写入正在通知的资源，例如资源甲修改资源乙、资源乙又反过来修改资源甲，
会抛出 `InvalidOperationException`，避免无限递归及旧快照覆盖。

## 发布异常与统一报告

前置修正失败时，停止本次准备：普通属性整批不发布，保留上一份已发布快照；
资源写入不生效，保留当前资源。前置钩子应只修正参数，不承担外部副作用。
普通属性的候选贡献仍保留，可在修复处理器后重试，或由下一帧重置并重建。

初始化和后置通知逐个调用订阅者。某个处理器失败后，仍继续同一事件的其余订阅者、
组件级通知和其他属性通知。已发布属性与已经完成的资源联动不回滚。
嵌套资源写入的异常与外层操作一起收集，直到最外层 `Commit` 或资源写入结束才报告一次。
报告包含属性 ID、失败阶段以及后置处理器信息。

独立的 `new AttributeComponent()` 默认在操作结束后抛出一个 `AggregateException`。
需要统一写日志时，可以通过构造函数传入报告器：

```csharp
var attributes = new AttributeComponent(error => logger.Error("属性发布失败", error));
```

`AttributeModPlayer` 已配置 Mod 日志报告器。报告器正常返回时，调用方可以继续执行；
前置失败的资源 `SetBase` 返回保留的白值，`AddBase` 返回 0；后置失败不影响已写入的值和返回值。
错误报告不代表业务联动已全部成功；调用方可从日志定位失败处理器。
报告器在发布阶段标记释放后调用，它自身的异常直接交给调用方，不再次收集。

## 监听属性变化

`PostAttributeChange` 仅在最终值相对于上一次发布真正变化时触发。
普通属性同一帧内多次加成在提交时合并通知；资源的显式修改则立即通知：

```csharp
Attributes.PostAttributeChange += OnAttributeChanged;

private void OnAttributeChanged(
    object sender,
    AttributeChangedEventArgs args)
{
    if (args.Definition != CharacterAttributes.CooldownHaste)
        return;

    float oldValue = args.Previous.FinalValue;
    float newValue = args.Current.FinalValue;
    Main.NewText($"冷却急速: {oldValue} -> {newValue}");
}
```

如果只关心一个属性，可以直接从属性组件取得该属性的事件，不需要在回调中自行筛选：

```csharp
IDisposable subscription = Attributes
    .GetAttributeChangeEvent(CharacterAttributes.CooldownHaste)
    .Subscribe(OnCooldownHasteChanged);

private void OnCooldownHasteChanged(
    object sender,
    AttributeChangedEventArgs args)
{
    float oldValue = args.Previous.FinalValue;
    float newValue = args.Current.FinalValue;
}

// 不再需要监听时释放订阅；也可以使用事件对象的 Unsubscribe(handler)。
subscription.Dispose();
```

`GetAttributeChangeEvent` 对同一个 `AttributeDefinition` 始终返回同一个事件对象。
单属性和组件级的 `PostAttributeChange` 都只监听最终值变化，`Subscribe` 也订阅此事件。

如果需要区分白值变化、Extra 来源变化和最终值变化，请监听组件级
`AttributeSnapshotChanged` 或单属性的 `SnapshotChanged`。事件参数的 `ChangeKind` 是可组合
标记，并额外提供 `BaseValueChanged`、`ExtraValueChanged`、`FinalValueChanged` 便捷属性：

```csharp
powerEvent.SnapshotChanged += (_, args) =>
{
    if (args.BaseValueChanged)
    {
        // 白值发生了变化；即使最终值因上下限钳制而没变，也能进入这里。
    }

    if (args.FinalValueChanged)
    {
        // 游戏逻辑实际使用的最终值发生了变化。
    }
};
```

也可以直接监听组件级或单属性的 `PostAttributeBaseChange`，
无需自行筛选变化标记。基础值变化但最终值受上限限制不变时，只触发基础值事件；
只修改绿值导致最终值变化时，只触发最终值事件。以上不影响完整快照变化事件。
普通属性第一次提交、资源第一次显式写入都只触发 `AttributeInitialized`，不伪装成一次数值变化。
仅注册、读取或订阅资源不会触发初始化通知；后续写入相同数值不会重复初始化，也不会触发后置变化钩子。
初始化通知不补发，需要接收时应在第一次发布前完成订阅。

## 可消耗资源的接入

当前魔力只需要一个 `Resource` 属性。上限是否作为 KL 属性取决于业务：
伊蕾娜的上限直接由 `Player.statManaMax2 * 换算比例` 得出，使用普通 C# 计算属性即可。
它在前置钩子中裁剪写入值，并在 `PostUpdate` 中检查原版上限下降后的越界情况。

### 需要独立加成的上限示例

以下双属性示例适用于上限本身需要 KL 白值、绿值加成的项目，不是伊蕾娜当前的接入代码。
声明放在属性集合中，订阅放在玩家初始化阶段；随后展示的加成和写入用于说明调用顺序。

```csharp
public static readonly AttributeDefinition MaxMana = new("MyMod.MaxMana", 100f, 0f);
public static readonly AttributeDefinition Mana = new("MyMod.Mana", 0f, 0f,
    kind: AttributeKind.Resource);

// 在玩家初始化时订阅。具体属性之间的约束由业务表达，KL 不声明动态依赖关系。
Attributes.GetAttributeChangeEvent(Mana).PreAttributeChange += (_, args) =>
{
    args.BaseValue = Math.Clamp(args.BaseValue, 0f, Attributes.GetPublishedFinalValue(MaxMana));
};

// 上限真正变化后修正已有资源；此时所有普通属性都已经完成发布。
Attributes.GetAttributeChangeEvent(MaxMana).PostAttributeChange += (_, args) =>
{
    if (Attributes.GetFinalValue(Mana) > args.Current.FinalValue)
        Attributes.SetBase(Mana, args.Current.FinalValue);
};

// 首次上限发布时如何设置资源由业务决定。这里示例为填满，存档项目可改为恢复存档值。
Attributes.GetAttributeChangeEvent(MaxMana).AttributeInitialized += (_, args) =>
{
    Attributes.SetBase(Mana, args.Current.FinalValue);
};

// 先订阅，再执行写入，才能收到下面恢复和消耗产生的变化通知。
Attributes.GetAttributeChangeEvent(Mana).PostAttributeChange += (_, args) =>
{
    float oldMana = args.Previous.FinalValue;
    float newMana = args.Current.FinalValue;
};

// 上限仍接受普通的白值和绿值贡献。
Attributes.AddBase(MaxMana, 50f);
Attributes.AddExtraPercent(MaxMana, 0.2f);
Attributes.Commit(); // 在 AttributeModPlayer 中由基类负责调用。

// 恢复和消耗只提交目标值或增减量，共用上面的业务裁剪钩子。
Attributes.SetBase(Mana, 120f);
float gained = Attributes.AddBase(Mana, 30f);
float spent = -Attributes.AddBase(Mana, -40f);
```

KL 保留属性定义的静态 `minValue`、`maxValue` 范围，以及普通属性、资源属性两种存储规则。
动态裁剪通过业务的提交前和发布后钩子实现，不需要动态上限参数、依赖表或依赖排序接口。
例如伊蕾娜负责“当前魔力不得超过魔力上限”的关系，KL 负责执行钩子、存储结果和发送事件。

`ResetForTick()` 不清空资源。`Commit()` 只处理普通属性，资源只在显式写入时发布。
因此上限的发布后钩子写入当前魔力后，不会再被同一批次预先计算的旧资源快照覆盖或重复通知。
钩子中嵌套写入资源时，资源自己的初始化或变化通知立即执行。
同一普通属性的单属性后置钩子先于对应组件级后置钩子，组件级观察者可以读取前者修正后的资源。
不同普通属性的后置通知仍按注册顺序执行，不承诺其他属性的业务回调已全部完成。
资源写入将裁剪结果同时写回白值，上限恢复不会返还曾经溢出的资源。

对于资源属性，`SetBase` 返回裁剪后实际存储的白值，`AddBase` 返回实际增减量。
两者立即执行业务前置钩子、静态范围限制、发布和变化通知，不提交其他普通属性。
示例钩子使用最近一次已发布的上限，避免每帧重置的中间状态误清空资源。
资源和普通属性共用白值接口；资源不接受 `AddExtra`，前置钩子也不能为资源设置绿值。
恢复、消耗改变资源，装备和 Buff 的容量加成改变上限。

业务方仍决定首次进入世界是否填满，以及什么时候恢复、消耗、保存和加载。
伊蕾娜的魔力上限只由原版最大魔力乘以比例得出，因此直接使用普通 C# 计算属性，
只将当前魔力声明为 KL 资源属性，无需重复声明上限属性或订阅其事件。
它在原版属性计算完成后的 `PostUpdate` 中恢复存档或填满当前魔力，并检查上限下降后的越界情况；
恢复、消耗和上限下降导致的写入都通过 `SetBase` 经过前置裁剪钩子。
只有需要独立白值、绿值加成或属性监听的上限，才需要采用上面的双属性示例。
伊蕾娜通过继承 `RPGAttributeModPlayer` 接入这一流程，
急速与魔力共用继承得到的属性组件，没有独立的魔力存储字段或变化事件。

事件参数同时包含变化前后的白值、ExtraValue、Flat/Percent Extra 和最终值，适合用于
UI、日志或其他依赖属性的系统。

## 示例：冷却急速

`CharacterAttributes.CooldownHaste` 是内置通用 RPG 属性，范围为 0-500，采用 LoL 风格
技能急速算法：

```text
冷却缩减率 = 急速 / (100 + 急速)
冷却速度倍率 = 1 + 急速 / 100
```

因此：

- 100 急速 = 50% 冷却缩减。
- 500 急速 = 83.33% 冷却缩减。
- 没有属性组件时，技能冷却速度倍率为 1。

`Skill.UpdateCD` 已提供接收 `AttributeComponent` 的重载，绑定属性的
`KLSkillModPlayer` 会自动使用该倍率。

## 存档与网络

属性组件本身不实现 `SaveData`、`LoadData`、`NetSend` 或 `NetReceive`。
普通属性由等级、装备、技能和 Buff 等真实来源在每帧重建，避免临时加成过期后残留。
资源属性由业务方保存和同步当前值，并通过 KL 的 `SetBase` 恢复；内存中的跨帧保留不等于自动存档或网络同步。
