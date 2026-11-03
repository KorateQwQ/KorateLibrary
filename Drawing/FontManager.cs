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
    /// <summary>
    /// The three fonts above baked at exact pixel sizes (clients only), for sharp small UI text.
    /// The ranges match 伊蕾娜/Tools/FontTiers/build.ps1, which generates the Name_px files.
    /// </summary>
    public static PixelFont HarmonyOS_Sans_SC_Pixels, NotoSerifSC_Pixels, Gelasio_Pixels;

    public override void Load()
    {
        LoliFont = ModContent.Request<DynamicSpriteFont>("KL/Fonts/LoliFont", AssetRequestMode.ImmediateLoad);
        HarmonyOS_Sans_SC = ModContent.Request<DynamicSpriteFont>("KL/Fonts/HarmonyOS_Sans_SC", AssetRequestMode.ImmediateLoad);
        if (!Main.dedServ)
        {
            NotoSerifSC = ModContent.Request<DynamicSpriteFont>("KL/Fonts/NotoSerifSC", AssetRequestMode.ImmediateLoad);
            Gelasio = ModContent.Request<DynamicSpriteFont>("KL/Fonts/Gelasio", AssetRequestMode.ImmediateLoad);
            HarmonyOS_Sans_SC_Pixels = Pixels("KL/Fonts/HarmonyOS_Sans_SC", HarmonyOS_Sans_SC, 9, 28);
            NotoSerifSC_Pixels = Pixels("KL/Fonts/NotoSerifSC", NotoSerifSC, 9, 28);
            Gelasio_Pixels = Pixels("KL/Fonts/Gelasio", Gelasio, 8, 26);
        }
        base.Load();
    }

    private static PixelFont Pixels(string path, Asset<DynamicSpriteFont> master, int min, int max)
    {
        var sizes = new Dictionary<int, Asset<DynamicSpriteFont>>();
        for (int pixels = min; pixels <= max; pixels++)
            if (ModContent.RequestIfExists<DynamicSpriteFont>($"{path}_{pixels}", out var size, AssetRequestMode.AsyncLoad))
                sizes[pixels] = size;
        return new(master, sizes);
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
        HarmonyOS_Sans_SC_Pixels = NotoSerifSC_Pixels = Gelasio_Pixels = null;
        base.Unload();
    }
}
