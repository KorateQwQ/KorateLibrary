namespace KL.AttributeSystem;

public enum AttributeKind
{
    /// <summary>
    /// 每帧重置的属性类型，一般为受装备buff加成的属性
    /// </summary>
    Rebuilt,
    /// <summary>
    /// 不需要重置的属性类型，一般为持续统计的资源类型
    /// </summary>
    Resource,
}

/// <summary>
/// 声明单个角色属性及其静态取值范围。
/// </summary>
public sealed class AttributeDefinition
{
    public AttributeDefinition(string id, float defaultBaseValue = 0f, float minValue = float.NegativeInfinity,
        float maxValue = float.PositiveInfinity, AttributeKind kind = AttributeKind.Rebuilt)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Attribute id cannot be empty.", nameof(id));
        }

        if (float.IsNaN(minValue) || float.IsNaN(maxValue) || minValue > maxValue)
        {
            throw new ArgumentException("Attribute minimum cannot be greater than its maximum.", nameof(minValue));
        }

        if (kind != AttributeKind.Rebuilt && kind != AttributeKind.Resource)
            throw new ArgumentOutOfRangeException(nameof(kind));
        if (!float.IsFinite(defaultBaseValue))
            throw new ArgumentOutOfRangeException(nameof(defaultBaseValue));

        Id = id;
        DefaultBaseValue = defaultBaseValue;
        MinValue = minValue;
        MaxValue = maxValue;
        Kind = kind;
    }

    public string Id { get; }

    public float DefaultBaseValue { get; }

    public float MinValue { get; }

    public float MaxValue { get; }

    public AttributeKind Kind { get; }

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
