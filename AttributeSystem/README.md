# 属性系统入门

KL 的 `AttributeSystem` 负责保存属性、合并加成、发布结果和通知变化。
Mod 作者负责决定数值来源、资源的恢复与消耗，以及如何将最终属性用于玩法。
下文用“力量”和“体力”演示两类属性，可以替换为你自己的属性名称。

## 1. 选择类型、定义属性

| 类型 | 适用数据 | 每帧重置 | 修改何时发布 | 固定、百分比加成 |
| --- | --- | --- | --- | --- |
| `Rebuilt`，默认类型 | 力量、抗性、速度、体力上限 | 回到默认白值，清空额外加成 | `Commit()` 时统一发布 | 支持 |
| `Resource` | 当前体力、当前护盾、能量 | 保留当前持有量 | `SetBase` / `AddBase` 写入时立即发布 | 不支持，应修改对应上限或恢复量 |

“最大体力”通常随装备重算，“当前体力”记录已经消耗或恢复了多少。
只有需要独立加成或监听时，才需要把上限也定义为属性；简单上限可以由业务直接计算。

```csharp
using KL.AttributeSystem;

public static class MyAttributes
{
    public static readonly AttributeDefinition Power =
        new(defaultBaseValue: 100f, minValue: 0f);

    public static readonly AttributeDefinition MaxStamina =
        new(defaultBaseValue: 100f, minValue: 0f);

    public static readonly AttributeDefinition Stamina =
        new(defaultBaseValue: 0f, minValue: 0f, kind: AttributeKind.Resource);
}
```

定义必须保存为 `static readonly` 字段。属性按定义对象引用区分，不需要字符串 ID。
不要在 getter、更新循环或每个玩家实例中重新 `new` 同一个业务属性的定义。
`static readonly` 是使用约定，目前没有编译分析器强制检查。
异常日志自动取得声明字段名，构造函数的 `declarationName` 参数应省略。
通过工厂构造时该名称可能是工厂成员名，不能当成稳定协议标识。

## 2. 每个角色持有一个组件

只需要属性基础设施时继承 `AttributeModPlayer`；需要 KL 通用 RPG 属性时继承
`RPGAttributeModPlayer`，用法相同。具体角色可以向同一组件添加自己的专属属性。
两个基类都是抽象类，不会沿继承链给同一个角色创建多份组件。

```csharp
using KL.AttributeSystem;

public partial class MyAttributePlayer : AttributeModPlayer
{
    // 每次读取组件，不另存一份最终力量。
    public float Power => GetAttributeValue(MyAttributes.Power);
}
```

这里用 `partial`，方便后面的资源示例继续扩展同一个玩家类。
`AttributeModPlayer` 已在 `ResetEffects` 调用 `ResetForTick()`，在 `PostUpdate` 调用 `Commit()`。
派生类覆写这些方法时必须调用基类。装备、Buff 和技能只引用这个组件，不再次重置或提交。
只有属性定义是静态的，各玩家的组件和数值仍然独立。

## 3. 普通属性：装备、Buff、增益和减益

### 白值、固定绿值、百分比绿值

```text
BaseValue = 定义默认白值 + 本帧 AddBase 总和（或由 SetBase 覆盖）
ExtraValue = ExtraFlatValue + BaseValue × ExtraPercentValue
FinalValue = Clamp(BaseValue + ExtraValue, MinValue, MaxValue)
```

| 业务意图 | 调用 | 计算含义 |
| --- | --- | --- |
| 基础力量 +20 | `AddBase(Power, 20f)` | 加入白值，会受百分比绿值影响 |
| 额外固定力量 +15 | `AddExtraFlat(Power, 15f)` | 加入固定绿值，不受百分比绿值影响 |
| 力量加成 +25% | `AddExtraPercent(Power, 0.25f)` | 增加白值的 25% |
| 额外固定力量 -10 | `AddExtraFlat(Power, -10f)` | 减少固定绿值 |
| 力量加成 -10% | `AddExtraPercent(Power, -0.10f)` | 减少白值的 10% |

百分比使用小数：`0.25f` 是 25%，`25f` 是 2500%。
百分比贡献直接相加：+25% 和 -10% 得到 +15%，不会分别乘以 1.25 和 0.9。
负百分比也不会削弱固定绿值。如果需要“所有加成结算后再降低总值”，应单独表达该业务规则，
不能把当前百分比接口理解为最终乘区。

白值为 0 时，单独添加百分比不会产生数值。先收集百分比、后收集白值也能得到相同结果，
前提是使用加法贡献，且没有后续 `SetBase` 覆盖白值。

### 装备贡献

以下是饰品的属性相关实现，物品纹理由 Mod 自行提供。

```csharp
using KL.AttributeSystem;
using Terraria;
using Terraria.ModLoader;

public class PowerCharm : ModItem
{
    public override void SetDefaults()
    {
        Item.width = 20;
        Item.height = 20;
        Item.accessory = true;
    }

    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        AttributeComponent attributes = player.GetModPlayer<MyAttributePlayer>().Attributes;
        attributes.AddBase(MyAttributes.Power, 20f);
        attributes.AddExtraFlat(MyAttributes.Power, 15f);
    }
}
```

### Buff 增幅与 Debuff 削弱

以下两个类各自作为一个 Buff 内容类，图标由 Mod 自行提供。

```csharp
using KL.AttributeSystem;
using Terraria;
using Terraria.ModLoader;

public class PowerBoost : ModBuff
{
    public override void Update(Player player, ref int buffIndex)
    {
        player.GetModPlayer<MyAttributePlayer>().Attributes
            .AddExtraPercent(MyAttributes.Power, 0.25f);
    }
}

public class PowerWeakness : ModBuff
{
    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
    }

    public override void Update(Player player, ref int buffIndex)
    {
        AttributeComponent attributes = player.GetModPlayer<MyAttributePlayer>().Attributes;
        attributes.AddExtraFlat(MyAttributes.Power, -10f);
        attributes.AddExtraPercent(MyAttributes.Power, -0.10f);
    }
}
```

业务可通过 `player.AddBuff(ModContent.BuffType<PowerBoost>(), 600)` 施加持续 600 tick 的增益。
装备和 Buff 的更新钩子每帧添加候选贡献，不调用 `Commit()`。
卸下装备或 Buff 到期后，该来源下一帧不再参与重建，不需要手动减回旧加成。

### 结算结果

按默认力量 100 和上述来源计算：

| 当前来源 | 白值 | 固定绿值 | 百分比绿值 | 发布的最终力量 |
| --- | ---: | ---: | ---: | ---: |
| 无加成 | 100 | 0 | 0% | 100 |
| 饰品 | 120 | 15 | 0% | 135 |
| 饰品 + 增益 | 120 | 15 | 25% | 165 |
| 饰品 + 增益 + 减益 | 120 | 5 | 15% | 143 |
| 卸下饰品，两个 Buff 仍在 | 100 | -10 | 15% | 105 |
| 所有临时效果结束 | 100 | 0 | 0% | 100 |

第三行是 `120 × 1.25 + 15 = 165`，固定绿值不参与百分比放大。
上下限仅裁剪普通属性的最终值，不裁剪白值和绿值各分量。
达到上限时，面板上的“白值 + 绿值”可能大于最终值。

### 等级成长与覆盖白值

等级、天赋等持久来源应保存来源数据，然后每帧重新贡献白值。
例如在自己的 `UpdateEquips` 中调用 `AddBase(MyAttributes.Power, levelBonus)`。
不要仅在升级时调用一次 `AddBase`：普通属性下一帧会重置，这次增加不会永久保留。

`SetBase` 会覆盖已收集的白值。如果默认值是 100、饰品已 `AddBase(20)`，
再调用 `SetBase(100)` 就会丢掉饰品的 20 点白值，但保留固定和百分比绿值。
基础值确实需要覆盖时，应由唯一负责人在收集前设置，例如在自己的 `ResetEffects`
调用 `base.ResetEffects()` 之后设置。装备、Buff 等独立来源优先使用加法接口。
在其他玩家类的 `ResetEffects` 中给这个组件加成，可能被持有者后续的重置清掉。

## 4. 发布属性与读取结果

```text
ResetForTick → 收集基础值和加成 → 前置修正 → 全部发布 → 后置通知 → 业务消费
```

“发布”是把候选结果写入可供业务读取的快照，不是发送网络包。
普通属性的加成接口只收集候选值，不立即更新已发布值或发送变化通知。

| 读取接口 | 返回什么 | 用途 |
| --- | --- | --- |
| `Get` / `GetFinalValue` | 最近一次已发布快照 / 最终值 | UI、伤害、冷却等正常消费 |
| `GetPublished` / `GetPublishedFinalValue` | 与上一行相同 | 显式表达读取发布结果 |
| `GetPending` / `GetPendingFinalValue` | 当前收集中的候选结果，未经过本次前置修正 | 计算调试、明确需要候选值的规则 |

玩家的便捷接口是 `GetAttribute` / `GetAttributeValue`，以及
`GetPendingAttribute` / `GetPendingAttributeValue`。
首次发布前，普通读取返回定义默认值形成的快照；读取不会自动提交。

例如上帧力量为 165，本帧刚重置时普通读取仍返回 165，直到本帧提交完成。
需要本帧结果时，在自己的玩家类中先提交再读取：

```csharp
public override void PostUpdate()
{
    base.PostUpdate(); // 基类统一提交，完成前置修正、发布和后置通知。
    float power = GetAttributeValue(MyAttributes.Power);
    // 在此将 power 用于自己的业务。
}
```

自定义属性不会自动变成 Terraria 的伤害或防御，需由业务明确应用。
如果还要叠加原版伤害倍率，应决定计算顺序，避免同一加成在 KL 与原版各算一次。

不同 `ModPlayer` 的 `PostUpdate` 有先后，不能认为另一个玩家类已完成提交。
跨系统需要同帧结果时，由负责人提交后显式调用消费者，或放到所有玩家更新完成后的阶段。
KL 技能系统已在 `PostUpdatePlayers` 处理登记的冷却更新，专用规则见
[技能冷却与属性接入](../SkillSystem/冷却与属性.md)。

## 5. 资源属性：初始化、恢复、消耗与动态上限

资源只存当前持有量，白值与最终值相同、绿值为零。`ResetForTick` 保留资源，
`Commit` 不发布资源；显式写入立即完成前置修正、裁剪、保存和通知。
不要对资源调用 `AddExtraFlat` 或 `AddExtraPercent`，组件会拒绝。

### 先初始化，再恢复或消耗

固定上限可直接写在资源定义的 `maxValue` 中。上限需要装备增幅时，使用前面的
`MaxStamina` 普通属性和 `Stamina` 资源属性，由业务表达两者关系。

下面是 `MyAttributePlayer` 的资源部分，可与前面的 `partial` 类合并。
示例选择“首次发布上限时填满、上限增长不赠送体力、上限下降截去超出量”。
这些行为由业务选择，不是组件自动规定的。

```csharp
using System;
using KL.AttributeSystem;

public partial class MyAttributePlayer
{
    public float Stamina => GetAttributeValue(MyAttributes.Stamina);
    public float MaxStamina => GetAttributeValue(MyAttributes.MaxStamina);

    public override void Initialize()
    {
        base.Initialize();

        Attributes.GetAttributeChangeEvent(MyAttributes.Stamina).PreAttributeChange +=
            (_, args) => args.BaseValue = Math.Clamp(args.BaseValue, 0f, MaxStamina);

        AttributeChangeEvent maximum = Attributes.GetAttributeChangeEvent(MyAttributes.MaxStamina);
        maximum.AttributeInitialized += (_, args) =>
            Attributes.SetBase(MyAttributes.Stamina, args.Current.FinalValue);
        maximum.PostAttributeChange += (_, args) =>
        {
            if (Stamina > args.Current.FinalValue)
                Attributes.SetBase(MyAttributes.Stamina, args.Current.FinalValue);
        };
    }

    public float RestoreStamina(float amount)
    {
        return amount > 0f ? Attributes.AddBase(MyAttributes.Stamina, amount) : 0f;
    }

    // 尽可能消耗：适合护盾吸收等允许部分支付的业务。
    public float DrainStamina(float amount)
    {
        return amount > 0f ? -Attributes.AddBase(MyAttributes.Stamina, -amount) : 0f;
    }

    // 本示例只有上下限裁剪规则；不足时不消耗。
    public bool TrySpendStamina(float cost)
    {
        if (cost <= 0f)
            return true;
        if (Stamina < cost)
            return false;

        float spent = DrainStamina(cost);
        return spent >= cost;
    }
}
```

装备增加上限仍用普通属性接口，在饰品的 `UpdateAccessory` 中添加：

```csharp
AttributeComponent attributes = player.GetModPlayer<MyAttributePlayer>().Attributes;
attributes.AddBase(MyAttributes.MaxStamina, 20f);
attributes.AddExtraPercent(MyAttributes.MaxStamina, 0.25f);
```

发布的上限为 `(100 + 20) × 1.25 = 150`。首次发布时填满到 150，之后装备增减只改变上限。
当前体力为 140 时卸下饰品，上限回到 100，后置规则立即把体力裁剪到 100。
重新装备不会返还被裁掉的 40 点。

动态裁剪读取**已发布上限**，不要读取刚重置的候选上限。
全部普通属性先发布再通知，因此上限后置回调能读取本批次的其他普通属性。
如果上限来自原版字段或普通 C# 计算属性，就没有 KL 上限变化事件；
业务应在上限计算完成后检查越界，并在每次资源写入前裁剪。

### 返回值表示实际变化

假设上限 100、当前体力 90，以下操作按顺序执行：

| 操作 | 返回值 | 操作后体力 |
| --- | ---: | ---: |
| `RestoreStamina(30)` | 10，实际恢复量 | 100 |
| `DrainStamina(130)` | 100，实际消耗量 | 0 |
| `Attributes.SetBase(MyAttributes.Stamina, 200)` | 100，写入后持有量 | 100 |

`SetBase` 返回写入后的白值，`AddBase` 返回实际白值差额。
护盾应根据实际消耗量结算，不要因为申请扣除 130 就按吸收 130 计算。
普通属性的白值写入返回值尚未经过提交钩子，不能当成最终属性变化量。

`TrySpendStamina` 在上述简单裁剪规则下可用于足额支付。
如果添加其他前置规则，可能部分扣除后仍返回失败；组件没有跨资源事务，
业务必须明确支付前检查、部分支付与补偿策略。不要调用写入后无条件返回成功。
“先恢复再支付”会先被上限裁剪，涉及生命换能量等机制时应先算清支付结果。

### 持续恢复与死亡重生

持续恢复可放在资源玩家的 `PostUpdate` 中，先调用 `base.PostUpdate()`，
确认本帧上限发布后，再在存活且允许恢复时调用 `RestoreStamina(每秒恢复量 / 60f)`。
不要每帧把资源 `SetBase(最大值)`，否则所有消耗都会在下一帧被补满。

死亡清空、重生填满或保留剩余量，均由业务选择。
`AttributeInitialized` 是组件内首次发布通知，不是每次重生通知；
重生填充需要独立处理，并等待本轮上限收集完成。

## 6. 修正与监听变化

在玩家 `Initialize` 中订阅，避免每帧重复添加处理器。
普通属性首次提交前、资源首次写入前完成订阅，才能收到初始化通知；初始化不会补发。

| 事件 | 触发时机 | 用途 |
| --- | --- | --- |
| `PreAttributeChange` | 写入前修正候选值，即使最终数值不变也执行 | 动态范围、限制加成 |
| `AttributeInitialized` | 首次发布一次；不同时发送变化后置通知 | 初始资源或显示状态 |
| `PostAttributeBaseChange` | 已发布白值变化 | 基础来源联动 |
| `PostAttributeChange` | 已发布最终值变化 | 上限裁剪、刷新显示 |
| `SnapshotChanged`（单属性）/ `AttributeSnapshotChanged`（组件） | 快照任一组成分量变化 | 展示白值、绿值与最终值明细 |

变化比较使用组件现有容差。普通属性一批贡献合并通知，资源显式写入立即通知。
在自己的 `Initialize` 中加入以下代码，即可监听单个属性：

```csharp
AttributeChangeEvent powerEvent = Attributes.GetAttributeChangeEvent(MyAttributes.Power);
powerEvent.PostAttributeChange += (_, args) =>
{
    float previousPower = args.Previous.FinalValue;
    float currentPower = args.Current.FinalValue;
    // 通知自己的 UI；不要在此再次给 Power 加成。
};
```

获取事件入口也会注册属性，因此没有任何贡献的普通属性仍可在首次提交时初始化。
全组件监听使用 `Attributes.PostAttributeChange`，通过 `args.Definition` 与静态定义比较筛选。
`Subscribe(handler)` 订阅单属性最终值变化，返回 `IDisposable`；临时 UI 关闭时应释放订阅。

前置修正仅修改 `args.BaseValue`、`args.ExtraFlatValue`、`args.ExtraPercentValue`，
资源参数的两种绿值必须保持零。例如，限制总百分比减益最多降低白值的 90%：

```csharp
powerEvent.PreAttributeChange += (_, args) =>
{
    args.ExtraPercentValue = System.Math.Max(-0.9f, args.ExtraPercentValue);
};
```

发布期间不允许直接修改普通属性、`ResetForTick` 或递归 `Commit`。
前置回调不能调用组件写入方法；后置回调允许立即写入其他资源。
资源 A 的回调写 B，B 再写 A，会被重入保护拒绝。

前置顺序是组件级再单属性；初始化和各类后置通知先单属性再组件级。
同一属性先通知白值变化，再通知最终值变化，最后通知快照变化。
全部普通属性已发布不等于所有业务联动已完成：观察完整业务状态应等待提交返回。
提交过程中新增的普通属性从下一次提交起参与发布。

### 回调异常

前置失败：本批普通属性不发布，资源写入不生效，保留上一次发布结果。
后置失败：已发布值不回滚，继续其余处理器和其他属性通知。
嵌套资源写入的错误与外层操作一起收集，最外层操作结束后统一报告。

`AttributeModPlayer` 默认写 Mod 日志。独立 `new AttributeComponent()` 默认抛出
`AggregateException`，可通过构造函数传入自己的 `Action<AggregateException>` 报告器。
报告器正常返回时，前置失败的资源 `SetBase` 返回原值、`AddBase` 返回 0。
“方法正常返回”不保证所有业务回调成功，应从日志检查失败的规则。

## 7. 存档、同步与独立使用

普通属性保存真实来源，例如等级、天赋选择，加载后每帧重建贡献。
资源保存当前持有量，加载后通过 `SetBase` 恢复。
动态上限尚未计算完成时，不要立即恢复资源，以免被默认上限裁掉。
可以先在 `LoadData` 保存待恢复值，首次上限发布时用它恢复，替换资源示例中的“填满”逻辑。
不要同时执行“恢复存档”和“初始化填满”，后者会覆盖前者。

组件不自动存档或网络同步，玩家基类也不会自动同步属性。
联机业务需明确资源由哪端决定、何时同步、加入世界如何取得初始值。
收到同步值后再次发布也可能触发事件，不要在事件中无条件发包造成回传循环。
诊断字段名不是存档键或协议 ID，协议标识由业务另行约定。

已有玩家类无法继承属性基类时，可实现 `IAttributeProvider` 并持有组件，
由该玩家类在 `ResetEffects` 重置、`PostUpdate` 提交。
`Attributes` getter 不应每次创建新组件；返回 null 时便捷读取会抛异常。
装备和技能引用持有者的组件，不能各建一份表达同一角色状态。

非玩家对象也可独立持有组件，由自己的更新循环负责：

```csharp
private readonly AttributeComponent attributes = new();

public void Update()
{
    attributes.ResetForTick();
    attributes.AddBase(MyAttributes.Power, 20f);
    attributes.AddExtraFlat(MyAttributes.Power, 15f);
    attributes.AddExtraPercent(MyAttributes.Power, 0.25f);
    attributes.Commit();

    float power = attributes.GetFinalValue(MyAttributes.Power); // 165
    // 在此消费完整结果。
}
```

组件应在游戏更新线程上顺序访问，不支持后台线程同时读写。
旧版迁移：字符串 ID 已移除；普通 `Get` 改为读取发布值，计算阶段显式使用 `GetPending`。
