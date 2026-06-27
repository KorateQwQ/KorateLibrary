using KL.DamageSystem;
using Terraria.UI.Chat;

namespace KL.Drawing.Snippets;

public sealed class KLSnippetRegisterSystem : ModSystem
{
    public override void Load()
    {
        if (Main.dedServ)
            return;

        ChatManager.Register<KLTextureTagHandler>("klicon");
    }
}
public static class SnippetsExtensions
{
    public static string GetIcon(this ElementType type, int size = 24, int offsetY = 2)
    {
        string texturePath = type switch
        {
            ElementType.Fire => "FireIcon",
            ElementType.Ice => "IceIcon",
            ElementType.Lightning => "LightningIcon",
            ElementType.Wind => "WindIcon",
            ElementType.Water => "WaterIcon",
            _ => string.Empty,
        };

        if (string.IsNullOrEmpty(texturePath))
            return string.Empty;

        return $"[klicon/{size},{offsetY}:KL/Drawing/Snippets/Icons/{texturePath}]";
    }
}