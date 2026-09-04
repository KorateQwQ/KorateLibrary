namespace KL.AttributeSystem;

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
    }

    public AttributeDefinition Definition { get; }

    public AttributeSnapshot Previous { get; }

    public AttributeSnapshot Current { get; }
}
