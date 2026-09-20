using ReLogic.Graphics;
using Terraria.Initializers;

namespace KL.Drawing;

public class FontManager : ModSystem
{
    public static Asset<DynamicSpriteFont> LoliFont;
    public static Asset<DynamicSpriteFont> HarmonyOS_Sans_SC;
    /// <summary>Noto Serif SC Regular (400), baked at 36pt / 48px for titles and skill names.</summary>
    public static Asset<DynamicSpriteFont> NotoSerifSC;
    /// <summary>Gelasio Regular (400), 36pt / 48px, printable ASCII for Georgia-style labels.</summary>
    public static Asset<DynamicSpriteFont> Gelasio;

    public override void Load()
    {
        LoliFont = ModContent.Request<DynamicSpriteFont>("KL/Fonts/LoliFont", AssetRequestMode.ImmediateLoad);
        HarmonyOS_Sans_SC = ModContent.Request<DynamicSpriteFont>("KL/Fonts/HarmonyOS_Sans_SC", AssetRequestMode.ImmediateLoad);
        if (!Main.dedServ)
        {
            NotoSerifSC = ModContent.Request<DynamicSpriteFont>("KL/Fonts/NotoSerifSC", AssetRequestMode.ImmediateLoad);
            Gelasio = ModContent.Request<DynamicSpriteFont>("KL/Fonts/Gelasio", AssetRequestMode.ImmediateLoad);
        }
        base.Load();
    }

    public override void PostUpdateEverything()
    {
        /*if (Main.mouseLeft && Main.mouseLeftRelease)
        {
            string text = "Fonts";
            foreach (var VARIABLE in KL.KLInstance.RootContentSource.GetAllAssetsStartingWith(text))
            {
                PrintText(VARIABLE);   
            }
        }*/
        base.PostUpdateEverything();
    }

    public override void Unload()
    {
        NotoSerifSC = null;
        Gelasio = null;
        base.Unload();
    }
}
