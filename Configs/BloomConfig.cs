using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace KL.Configs;

public class BloomConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;

    [DefaultValue(1)]
    [Range(0, 1)]
    public float BloomStrength = 1f;
    
    [DefaultValue(4)]
    [Range(2, 5)]
    public int BloomIteration = 4;
    
    [DefaultValue(true)]
    public bool ShouldBloom = true;

    public override void OnChanged()
    {
        DrawSystem.SetBloomInfo(BloomStrength, BloomIteration, Vector2.Zero, ShouldBloom);
        base.OnChanged();
    }
}