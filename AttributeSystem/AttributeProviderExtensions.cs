namespace KL.AttributeSystem;

/// <summary>
/// Convenience accessors for ModPlayers that expose an AttributeComponent.
/// </summary>
public static class AttributeProviderExtensions
{
    public static AttributeSnapshot GetAttribute(this IAttributeProvider provider,
        AttributeDefinition definition)
    {
        return GetComponent(provider).Get(definition);
    }

    public static float GetAttributeValue(this IAttributeProvider provider,
        AttributeDefinition definition)
    {
        return GetComponent(provider).GetFinalValue(definition);
    }

    private static AttributeComponent GetComponent(IAttributeProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return provider.Attributes ?? throw new InvalidOperationException(
            $"{provider.GetType().FullName} does not have an attribute component bound.");
    }
}
