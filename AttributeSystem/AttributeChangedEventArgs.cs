namespace KL.AttributeSystem;

[Flags]
public enum AttributeChangeKind
{
    None = 0,
    BaseValue = 1 << 0,
    ExtraFlatValue = 1 << 1,
    ExtraPercentValue = 1 << 2,
    ExtraValue = 1 << 3,
    FinalValue = 1 << 4,
}

public readonly struct AttributeSnapshot
{
    public AttributeSnapshot(float baseValue, float extraValue, float extraFlatValue, float extraPercentValue,
        float finalValue)
    {
        BaseValue = baseValue;
        ExtraValue = extraValue;
        ExtraFlatValue = extraFlatValue;
        ExtraPercentValue = extraPercentValue;
        FinalValue = finalValue;
    }

    public float BaseValue { get; }

    public float ExtraValue { get; }

    public float ExtraFlatValue { get; }

    public float ExtraPercentValue { get; }

    public float FinalValue { get; }
}

public sealed class AttributeChangedEventArgs : EventArgs
{
    public AttributeChangedEventArgs(AttributeDefinition definition, AttributeSnapshot previous,
        AttributeSnapshot current)
    {
        Definition = definition;
        Previous = previous;
        Current = current;
        ChangeKind = GetChangeKind(previous, current);
    }

    public AttributeDefinition Definition { get; }

    public AttributeSnapshot Previous { get; }

    public AttributeSnapshot Current { get; }

    public AttributeChangeKind ChangeKind { get; }

    public bool BaseValueChanged => ChangeKind.HasFlag(AttributeChangeKind.BaseValue);

    public bool ExtraValueChanged => ChangeKind.HasFlag(AttributeChangeKind.ExtraValue);

    public bool FinalValueChanged => ChangeKind.HasFlag(AttributeChangeKind.FinalValue);

    internal static AttributeChangeKind GetChangeKind(AttributeSnapshot previous,
        AttributeSnapshot current)
    {
        AttributeChangeKind result = AttributeChangeKind.None;
        AddIfChanged(ref result, AttributeChangeKind.BaseValue, previous.BaseValue, current.BaseValue);
        AddIfChanged(ref result, AttributeChangeKind.ExtraFlatValue,
            previous.ExtraFlatValue, current.ExtraFlatValue);
        AddIfChanged(ref result, AttributeChangeKind.ExtraPercentValue,
            previous.ExtraPercentValue, current.ExtraPercentValue);
        AddIfChanged(ref result, AttributeChangeKind.ExtraValue, previous.ExtraValue, current.ExtraValue);
        AddIfChanged(ref result, AttributeChangeKind.FinalValue, previous.FinalValue, current.FinalValue);
        return result;
    }

    private static void AddIfChanged(ref AttributeChangeKind result, AttributeChangeKind kind,
        float previous, float current)
    {
        if (MathF.Abs(previous - current) > AttributeComponent.ChangeEpsilon)
        {
            result |= kind;
        }
    }
}

/// <summary>
/// 属性发布前的可修改参数。处理器可以修正白值和绿值，或实现业务自定义的裁剪规则。
/// </summary>
public sealed class PreAttributeChangeEventArgs : EventArgs
{
    internal PreAttributeChangeEventArgs(AttributeDefinition definition, bool hasPreviousValue,
        AttributeSnapshot previous, float baseValue, float extraFlatValue, float extraPercentValue)
    {
        Definition = definition;
        HasPreviousValue = hasPreviousValue;
        Previous = previous;
        BaseValue = baseValue;
        ExtraFlatValue = extraFlatValue;
        ExtraPercentValue = extraPercentValue;
    }

    public AttributeDefinition Definition { get; }

    public bool HasPreviousValue { get; }

    public AttributeSnapshot Previous { get; }

    public float BaseValue { get; set; }

    public float ExtraFlatValue { get; set; }

    public float ExtraPercentValue { get; set; }

    public AttributeSnapshot Proposed
    {
        get
        {
            if (Definition.Kind == AttributeKind.Resource)
            {
                float value = Definition.ClampFinalValue(BaseValue);
                return new AttributeSnapshot(value, 0f, 0f, 0f, value);
            }
            float extraValue = ExtraFlatValue + BaseValue * ExtraPercentValue;
            float finalValue = Definition.ClampFinalValue(BaseValue + extraValue);
            return new AttributeSnapshot(BaseValue, extraValue, ExtraFlatValue,
                ExtraPercentValue, finalValue);
        }
    }
}

/// <summary>
/// 属性首次发布后的初始化通知。没有旧值，不表示发生了一次属性变化。
/// </summary>
public sealed class AttributeInitializedEventArgs : EventArgs
{
    internal AttributeInitializedEventArgs(AttributeDefinition definition, AttributeSnapshot current)
    {
        Definition = definition;
        Current = current;
    }

    public AttributeDefinition Definition { get; }

    public AttributeSnapshot Current { get; }
}
