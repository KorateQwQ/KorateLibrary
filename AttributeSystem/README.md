# 角色属性系统

`AttributeSystem` 提供一个可独立使用、也可以挂载到 `ModPlayer` 上的角色属性组件。
设计思路参考 UE GAS 的属性聚合方式，但遵循 Terraria/tModLoader 常见的
`ResetEffects` 每帧重建模式。

## 核心概念

每个属性都有三层数值：

- `BaseValue`：白值。来自等级、装备、常驻技能等长期来源。
- `ExtraValue`：绿值。来自 Buff、临时技能和其他临时效果。
- `FinalValue`：最终用于游戏逻辑的数值。

ExtraValue 支持绝对值和百分比两种加成：

```text
ExtraValue = ExtraFlatValue + BaseValue * ExtraPercentValue
FinalValue = Clamp(BaseValue + ExtraValue)
```

百分比使用小数表示：`0.2f` 表示 20%。例如白值为 100，Buff 提供 20% 加成，
则 ExtraValue 为 20，FinalValue 为 120。

## 挂载到 ModPlayer

最简单的方式是继承 `AttributeModPlayer`：

```csharp
using KL.AttributeSystem;

public sealed class MyAttributePlayer : AttributeModPlayer
{
    public override void UpdateEquips()
    {
        // 装备提供 100 点白值。
        Attributes.AddBase(CharacterAttributes.CooldownHaste, 100f);
        base.UpdateEquips();
    }
}
```

`AttributeModPlayer` 会自动在：

1. `ResetEffects` 中恢复默认白值并清空 ExtraValue。
2. 装备、技能、Buff 等钩子提交本帧加成。
3. `PostUpdate` 中提交最终值并触发变化事件。

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

## 添加加成

```csharp
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

不要在属性组件外部直接缓存 FinalValue。装备卸下、Buff 消失或技能关闭后，下一次
`ResetForTick` 会通过来源是否重新提交来自动恢复数值。

## 监听属性变化

事件在 `Commit()` 时触发，并且只在最终值相对于上一帧真正变化时触发。同一帧内多次
修改只会产生一次事件：

```csharp
Attributes.AttributeChanged += OnAttributeChanged;

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

事件参数同时包含变化前后的白值、ExtraValue、Flat/Percent Extra 和最终值，适合用于
UI、日志或其他依赖属性的系统。

## 示例：冷却急速

`CharacterAttributes.CooldownHaste` 是内置示例属性，范围为 0-500，采用 LoL 风格
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
属性应由等级、装备、技能和 Buff 等真实来源在每帧重建。这样可以避免临时加成过期后
残留，也避免把派生值作为独立状态进行同步。
