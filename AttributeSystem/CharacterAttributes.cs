namespace KL.AttributeSystem;

/// <summary>通用 RPG 属性定义及换算规则；每个角色的实际数值存放在其 AttributeComponent 中。</summary>
public static class CharacterAttributes
{
    /// <summary>
    /// 技能急速，100时对应50%冷却时间减少
    /// 最大值为500
    /// </summary>
    public static readonly AttributeDefinition CooldownHaste =
        new("KL.CooldownHaste", 0f, 0f, 500f);

    public static float GetCooldownReductionPercent(float cooldownHaste)
    {
        cooldownHaste = CooldownHaste.ClampFinalValue(cooldownHaste);
        return cooldownHaste / (100f + cooldownHaste);
    }

    public static float GetCooldownSpeedMultiplier(float cooldownHaste)
    {
        cooldownHaste = CooldownHaste.ClampFinalValue(cooldownHaste);
        return 1f + cooldownHaste / 100f;
    }

    public static float GetCooldownSpeedMultiplier(AttributeComponent attributes)
    {
        return attributes == null
            ? 1f
            : GetCooldownSpeedMultiplier(attributes.GetPublishedFinalValue(CooldownHaste));
    }
}
