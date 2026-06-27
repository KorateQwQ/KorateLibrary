using KL.Configs;
using Terraria.Graphics.Light;

namespace KL.Drawing;

public class RenderHelper : ModSystem
{
    public static RenderTarget2D Render;

    public static RenderTarget2D Render2;

    //用于截取屏幕颜色，但是我真的要截取吗
    internal static RenderTarget2D PreserveRender;

    internal static RenderTarget2D LastTargetRender;
    internal static RenderTarget2D SaveScreenRender;
    internal static RenderTarget2D SaveScreenRenderForBloom;
    internal static RenderTarget2D BloomRender;

    internal static RenderTarget2D[] BloomDownSample = new RenderTarget2D[7];
    internal static RenderTarget2D[] BloomUpSample = new RenderTarget2D[7];

    private static Mod lightsMod;
    
    [JITWhenModsEnabled("Lights")]
    static Type LightConfigType => typeof(Lights.LightsConfig);
    
    [JITWhenModsEnabled("Lights")]
    static Type LightModType => typeof(Lights.Lights);
    
    FieldInfo lightUseBloom;
    FieldInfo lightShadow;
    public override void Load()
    {
        On_Main.Draw += On_MainOnDraw;
        Terraria.Graphics.Effects.On_FilterManager.EndCapture +=
            FilterManager_EndCapture; //原版绘制场景的最后部分——滤镜。在这里运用render保证不会与原版冲突
        Main.OnResolutionChanged += Main_OnResolutionChanged; //屏幕分辨率改变时，重新设置render

        On_Main.InitTargets_int_int += On_MainOnInitTargets_int_int;

        if (ModLoader.TryGetMod("Lights", out Mod lights))
        {
            lightsMod = lights;
        }
        
        base.Load();
    }

    /// <inheritdoc />
    public override void ModifyLightingBrightness(ref float scale)
    {
        DisableLightsModBloom();
        base.ModifyLightingBrightness(ref scale);
    }

    void DisableLightsModBloom()
    {
        if (lightsMod != null&&DrawSystem.GetShouldBloom())
        {
            lightUseBloom ??= LightModType.GetField("useBloom",BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            lightShadow ??= LightModType.GetField("ShadowIntensity",BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (lightUseBloom != null)
            {
                lightUseBloom.SetValue(null, false);
            }
            if (lightShadow != null)
            {
                float shadowIntensity = (float)(lightShadow.GetValue(null) ?? 0);
                if(shadowIntensity>0.6) lightShadow.SetValue(null, 0.6f);
            }
            
        }
    }
    private void On_MainOnInitTargets_int_int(On_Main.orig_InitTargets_int_int orig, Main self, int width, int height)
    {
        orig(self, width, height);
        ChangeOrigRender();
    }

    private void On_MainOnDraw(On_Main.orig_Draw orig, Main self, GameTime gameTime)
    {
        if (Lighting.Mode is LightMode.Retro or LightMode.Trippy)
        {
            Lighting.Mode = LightMode.Color;
        }

        if (Main.WaveQuality < 1)
        {
            Main.WaveQuality = 1;
        }

        orig.Invoke(self, gameTime);
    }

    private static void CreateRender()
    {
        int width = Main.graphics.GraphicsDevice.PresentationParameters.BackBufferWidth;
        int height = Main.graphics.GraphicsDevice.PresentationParameters.BackBufferHeight;

        Render = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight,
            false, SurfaceFormat.Vector4, DepthFormat.None);

        Render2 = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight,
            false, SurfaceFormat.Vector4, DepthFormat.None);

        //用于截取屏幕颜色
        PreserveRender = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight,
            false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);


        BloomRender = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight,
            false, SurfaceFormat.Vector4, DepthFormat.None);

        SaveScreenRender = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight,
            false, SurfaceFormat.Vector4, DepthFormat.None);
        SaveScreenRenderForBloom = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight,
            false, SurfaceFormat.Vector4, DepthFormat.None);


        for (int i = 0; i < 7; i++)
        {
            BloomDownSample[i] = new RenderTarget2D(Main.graphics.GraphicsDevice, width, height,
                false, SurfaceFormat.Vector4, DepthFormat.None);
            width /= 2;
            height /= 2;
        }

        width = Main.graphics.GraphicsDevice.PresentationParameters.BackBufferWidth * 2;
        height = Main.graphics.GraphicsDevice.PresentationParameters.BackBufferHeight * 2;
        for (int i = 6; i >= 0; i--)
        {
            BloomUpSample[i] = new RenderTarget2D(Main.graphics.GraphicsDevice, width, height,
                false, SurfaceFormat.Vector4, DepthFormat.None);
            width /= 2;
            height /= 2;
        }

        ChangeOrigRender();
    }

    static void ChangeOrigRender()
    {
        if(!DrawSystem.GetShouldBloom())return;
        if (lightsMod != null)
        {
            Main.screenTarget.Dispose();
            GraphicsDevice graphicsDevice = Main.graphics.GraphicsDevice;
            Main.screenTarget = new RenderTarget2D(graphicsDevice, 
                graphicsDevice.PresentationParameters.BackBufferWidth, 
                graphicsDevice.PresentationParameters.BackBufferHeight, false,
                SurfaceFormat.HdrBlendable, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        }
        else
        {
            Main.screenTarget.Dispose();
            Main.screenTarget = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight,
                false, SurfaceFormat.HdrBlendable, DepthFormat.Depth24);
        }


        Main.screenTargetSwap.Dispose();
        Main.screenTargetSwap = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight,
            false, SurfaceFormat.HdrBlendable, DepthFormat.Depth24);
    }

    private static void FilterManager_EndCapture(Terraria.Graphics.Effects.On_FilterManager.orig_EndCapture orig,
        Terraria.Graphics.Effects.FilterManager self, RenderTarget2D finalTexture, RenderTarget2D screenTarget1,
        RenderTarget2D screenTarget2, Color clearColor)
    {
        if (Render == null || Render2 == null || PreserveRender == null ||
            BloomRender == null || SaveScreenRender == null || SaveScreenRenderForBloom == null) CreateRender();
        orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
    }

    private static void Main_OnResolutionChanged(Vector2 obj) => CreateRender();

    //将screenTarget保存至screenTargetSwap,注意请一次性保存并重新绘制，中间不能再插入一次保存。
    public static void SaveScreenTarget(Effect shader = null)
    {
        if(!DrawSystem.CanUseRender)return;

        GraphicsDevice gd = Main.instance.GraphicsDevice;
        SpriteBatch sb = Main.spriteBatch;
        if (Main.graphics.GraphicsDevice.GetRenderTargets().Length > 0 &&
            Main.graphics.GraphicsDevice.GetRenderTargets()[0].RenderTarget != null)
        {
            Texture target = Main.graphics.GraphicsDevice.GetRenderTargets()[0].RenderTarget;
            LastTargetRender = target as RenderTarget2D;

            sb.End();
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None,
                RasterizerState.CullNone, shader);
            gd.SetRenderTarget(SaveScreenRender); //在这个上面绘制一遍原图，相当于“保存”
            gd.Clear(Color.Transparent);

            sb.Draw(LastTargetRender, Vector2.Zero, Color.White);
        }
    }

    //收集HDR主屏幕Main.screenTarget上所有大于1的像素到BloomRender上, 其余部分绘制回Main.screenTarget上
    public static void DrawOverFlowScreenTarget()
    {
        if(!DrawSystem.CanUseRender)return;

        GraphicsDevice gd = Main.instance.GraphicsDevice;
        SpriteBatch sb = Main.spriteBatch;
        if (Main.graphics.GraphicsDevice.GetRenderTargets().Length > 0 &&
            Main.graphics.GraphicsDevice.GetRenderTargets()[0].RenderTarget != null)
        {
            Texture target = Main.graphics.GraphicsDevice.GetRenderTargets()[0].RenderTarget;
            LastTargetRender = target as RenderTarget2D;

            //保存不溢出的像素
            SwitchRender(SaveScreenRender);
            EndBeginDraw(0,1,false);
            DrawUnderflowColorEffect();
            sb.Draw(LastTargetRender, new Vector2(0), Color.White);

            //保存溢出的像素
            EndBeginDraw(0, 1, false);
            DrawOverflowColorEffect();
            gd.SetRenderTarget(BloomRender);
            gd.Clear(Color.Transparent);
            DrawOverflowColorEffect();
            sb.Draw(LastTargetRender, new Vector2(0), Color.White);
        }
    }

    public static void ReDrawScreenTarget()
    {
        if(!DrawSystem.CanUseRender)return;
        if (LastTargetRender == null || SaveScreenRender == null) return;

        GraphicsDevice gd = Main.instance.GraphicsDevice;
        SpriteBatch sb = Main.spriteBatch;
        EndBeginDraw(adjustToScreen: false);

        gd.SetRenderTarget(LastTargetRender); //在这个上面绘制一遍原图，相当于“保存”
        gd.Clear(Color.Transparent);
        sb.Draw(SaveScreenRender, new Vector2(), Color.White);
    }

    public static RenderTarget2D GetLastScreenRender()
    {
        return LastTargetRender;
    }

    public static RenderTarget2D GetSaveScreenRender()
    {
        return SaveScreenRender;
    }

    public static void SwitchRender(RenderTarget2D target, bool adjustScreenSize = false, int state = 0)
    {
        if(!DrawSystem.CanUseRender)return;
        if (target == null)
        {
            CreateRender();
        }

        GraphicsDevice gd = Main.instance.GraphicsDevice;
        EndBeginDraw(state, 1, adjustScreenSize);
        gd.SetRenderTarget(target);

        gd.Clear(Color.Transparent);
    }
}