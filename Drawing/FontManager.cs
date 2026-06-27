using ReLogic.Graphics;
using Terraria.Initializers;

namespace KL.Drawing;

public class FontManager : ModSystem
{
    public static Asset<DynamicSpriteFont> LoliFont;
    public static Asset<DynamicSpriteFont> HarmonyOS_Sans_SC;

    public override void Load()
    {
        LoliFont = ModContent.Request<DynamicSpriteFont>("KL/Fonts/LoliFont", AssetRequestMode.ImmediateLoad);
        HarmonyOS_Sans_SC = ModContent.Request<DynamicSpriteFont>("KL/Fonts/HarmonyOS_Sans_SC", AssetRequestMode.ImmediateLoad);
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
}
