namespace KL.AttributeSystem;

/// <summary>
/// Rebuildable character-attribute container. Base values and extra values are submitted by
/// gameplay systems during the current update cycle, then published with Commit().
/// </summary>
public sealed class AttributeComponent
{
    private const float ChangeEpsilon = 0.0001f;

    private sealed class AttributeState
    {
        public AttributeState(AttributeDefinition definition)
        {
            Definition = definition;
            BaseValue = definition.DefaultBaseValue;
        }

        public AttributeDefinition Definition { get; }

        public float BaseValue { get; set; }

        public float ExtraFlatValue { get; set; }

        public float ExtraPercentValue { get; set; }

        public AttributeSnapshot PublishedValue { get; set; }

        public bool HasPublishedValue { get; set; }
    }

    private readonly Dictionary<AttributeDefinition, AttributeState> _states = new();

    public event EventHandler<AttributeChangedEventArgs> AttributeChanged;

    /// <summary>
    /// Resets this component for the next Terraria update cycle.
    /// </summary>
    public void ResetForTick()
    {
        foreach (AttributeState state in _states.Values)
        {
            state.BaseValue = state.Definition.DefaultBaseValue;
            state.ExtraFlatValue = 0f;
            state.ExtraPercentValue = 0f;
        }
    }

    public void AddBase(AttributeDefinition definition, float value)
    {
        AttributeState state = GetOrCreateState(definition);
        state.BaseValue += value;
    }

    public void AddExtra(AttributeDefinition definition, float value, AttributeModifierKind kind)
    {
        AttributeState state = GetOrCreateState(definition);
        switch (kind)
        {
            case AttributeModifierKind.Flat:
                state.ExtraFlatValue += value;
                break;
            case AttributeModifierKind.Percent:
                state.ExtraPercentValue += value;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown attribute modifier kind.");
        }
    }

    public void AddExtraFlat(AttributeDefinition definition, float value)
    {
        AddExtra(definition, value, AttributeModifierKind.Flat);
    }

    public void AddExtraPercent(AttributeDefinition definition, float value)
    {
        AddExtra(definition, value, AttributeModifierKind.Percent);
    }

    public AttributeSnapshot Get(AttributeDefinition definition)
    {
        return Calculate(GetOrCreateState(definition));
    }

    public float GetFinalValue(AttributeDefinition definition)
    {
        return Get(definition).FinalValue;
    }

    /// <summary>
    /// Publishes this cycle's values and raises one event per attribute whose final value changed.
    /// </summary>
    public void Commit()
    {
        foreach (AttributeState state in _states.Values)
        {
            AttributeSnapshot current = Calculate(state);
            if (!state.HasPublishedValue)
            {
                state.PublishedValue = current;
                state.HasPublishedValue = true;
                continue;
            }

            AttributeSnapshot previous = state.PublishedValue;
            state.PublishedValue = current;
            if (MathF.Abs(previous.FinalValue - current.FinalValue) <= ChangeEpsilon)
            {
                continue;
            }

            AttributeChanged?.Invoke(this, new AttributeChangedEventArgs(state.Definition, previous, current));
        }
    }

    private AttributeState GetOrCreateState(AttributeDefinition definition)
    {
        if (definition == null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (_states.TryGetValue(definition, out AttributeState state))
        {
            return state;
        }

        state = new AttributeState(definition);
        _states.Add(definition, state);
        return state;
    }

    private static AttributeSnapshot Calculate(AttributeState state)
    {
        float extraValue = state.ExtraFlatValue + state.BaseValue * state.ExtraPercentValue;
        float finalValue = state.Definition.ClampFinalValue(state.BaseValue + extraValue);
        return new AttributeSnapshot(state.BaseValue, extraValue, state.ExtraFlatValue,
            state.ExtraPercentValue, finalValue);
    }
}
