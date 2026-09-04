namespace KL.AttributeSystem;

/// <summary>
/// Describes one character attribute and its valid final-value range.
/// </summary>
public sealed class AttributeDefinition
{
    public AttributeDefinition(string id, float defaultBaseValue = 0f, float minValue = float.NegativeInfinity,
        float maxValue = float.PositiveInfinity)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Attribute id cannot be empty.", nameof(id));
        }

        if (minValue > maxValue)
        {
            throw new ArgumentException("Attribute minimum cannot be greater than its maximum.", nameof(minValue));
        }

        Id = id;
        DefaultBaseValue = defaultBaseValue;
        MinValue = minValue;
        MaxValue = maxValue;
    }

    public string Id { get; }

    public float DefaultBaseValue { get; }

    public float MinValue { get; }

    public float MaxValue { get; }

    public float ClampFinalValue(float value)
    {
        return MathF.Min(MathF.Max(value, MinValue), MaxValue);
    }
}

public enum AttributeModifierKind
{
    Flat,
    Percent,
}
