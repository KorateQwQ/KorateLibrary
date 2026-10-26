using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace KL.Configs;

public class BloomConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;

    [DefaultValue(true)]
    public bool ShouldBloom = true;

    public override void OnChanged()
    {
        DrawSystem.SetBloomInfo(1f, 3, Vector2.Zero, ShouldBloom);
        base.OnChanged();
    }
}
