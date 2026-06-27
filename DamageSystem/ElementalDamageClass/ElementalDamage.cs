namespace KL.DamageSystem.ElementalDamageClass;

/// <summary>
/// 元素伤害类属于魔法伤害的子类
/// </summary>
public abstract class ElementalDamage : DamageClass
{
    public override StatInheritanceData GetModifierInheritance(DamageClass damageClass)
    {
        if (damageClass == DamageClass.Generic)
            return StatInheritanceData.Full;
        
        if (damageClass == DamageClass.Magic)
            return StatInheritanceData.Full;

        // 对于其他伤害类，不继承任何加成
        return StatInheritanceData.None;
    }

    /// <summary>
    /// 获取元素伤害类的效果继承，元素伤害继承自魔法伤害
    /// </summary>
    /// <param name="damageClass"></param>
    /// <returns></returns>
    public override bool GetEffectInheritance(DamageClass damageClass)
    {
        return damageClass == Magic;
    }
    
    public override void SetDefaultStats(Player player)
    {
        base.SetDefaultStats(player);
    }
    
    // 使用标准的暴击计算
    public override bool UseStandardCritCalcs => true;

    public override bool ShowStatTooltipLine(Player player, string lineName)
    {
        // 你可以使用的四个行名称是 "Damage"、"CritChance"、"Speed" 和 "Knockback"。所有四种情况默认为 true，因此将显示。
        // 显示所有标准的工具提示行
        return base.ShowStatTooltipLine(player, lineName);
    }
}