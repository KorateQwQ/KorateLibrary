namespace KL.AttributeSystem;

public static class CharacterAttributes
{
    /// <summary>
    /// League-style ability haste. 100 haste gives 50% cooldown reduction;
    /// the final value is capped at 500.
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
            : GetCooldownSpeedMultiplier(attributes.GetFinalValue(CooldownHaste));
    }
}
