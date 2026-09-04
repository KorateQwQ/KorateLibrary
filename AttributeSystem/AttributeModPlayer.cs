namespace KL.AttributeSystem;

public interface IAttributeProvider
{
    AttributeComponent Attributes { get; }
}

/// <summary>
/// Optional ModPlayer adapter for projects that want an attribute component attached directly
/// to a player. Existing ModPlayers can instead implement IAttributeProvider themselves.
/// </summary>
public abstract class AttributeModPlayer : ModPlayer, IAttributeProvider
{
    public AttributeComponent Attributes { get; } = new();

    /// <summary>
    /// Gets the complete current snapshot of an attribute.
    /// </summary>
    public AttributeSnapshot GetAttribute(AttributeDefinition definition)
    {
        return Attributes.Get(definition);
    }

    /// <summary>
    /// Gets the current final value of an attribute.
    /// </summary>
    public float GetAttributeValue(AttributeDefinition definition)
    {
        return Attributes.GetFinalValue(definition);
    }

    public override void ResetEffects()
    {
        Attributes.ResetForTick();
        base.ResetEffects();
    }
    

    
    public override void PostUpdateBuffs()
    {
        AttributeModPlayer attributePlayer =
            Player.GetModPlayer<AttributeModPlayer>();

        float cooldownHaste =
            attributePlayer.Attributes.GetFinalValue(
                CharacterAttributes.CooldownHaste);
        base.PostUpdateBuffs();
    }
    
    public override void PostUpdateEquips()
    {
        base.PostUpdateEquips();
    }

    public override void PostUpdateMiscEffects()
    {
        base.PostUpdateMiscEffects();
    }

    public override void PostUpdate()
    {
        Attributes.Commit();
        base.PostUpdate();
    }

}
