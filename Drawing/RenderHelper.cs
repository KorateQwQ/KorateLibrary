namespace KL.Drawing;

public class RenderHelper : ModSystem
{
    public static RenderTarget2D Render;

    public static RenderTarget2D Render2;

    internal static RenderTarget2D LastTargetRender;
    internal static RenderTarget2D SaveScreenRender;
    internal static RenderTarget2D BloomRender;

    internal static RenderTarget2D[] BloomDownSample = new RenderTarget2D[7];
    internal static RenderTarget2D[] BloomUpSample = new RenderTarget2D[7];

    public override void Load()
    {
        Terraria.Graphics.Effects.On_FilterManager.EndCapture +=
            FilterManager_EndCapture;
        Main.OnResolutionChanged += Main_OnResolutionChanged; //屏幕分辨率改变时，重新设置render
        
        base.Load();
    }

    private static void CreateRender()
    {
        int width = Main.graphics.GraphicsDevice.PresentationParameters.BackBufferWidth;
        int height = Main.graphics.GraphicsDevice.PresentationParameters.BackBufferHeight;

        Render = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight,
            false, SurfaceFormat.Vector4, DepthFormat.None);

        Render2 = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight,
            false, SurfaceFormat.Vector4, DepthFormat.None);

        BloomRender = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight,
            false, SurfaceFormat.Vector4, DepthFormat.None);

        SaveScreenRender = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight,
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

    }

    private static void FilterManager_EndCapture(Terraria.Graphics.Effects.On_FilterManager.orig_EndCapture orig,
        Terraria.Graphics.Effects.FilterManager self, RenderTarget2D finalTexture, RenderTarget2D screenTarget1,
        RenderTarget2D screenTarget2, Color clearColor)
    {
        EnsureRenderTargets();
        orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
    }

    private static void Main_OnResolutionChanged(Vector2 obj) => CreateRender();

    internal static void EnsureRenderTargets()
    {
        if (Render == null || Render2 == null || BloomRender == null || SaveScreenRender == null)
        {
            CreateRender();
        }
    }

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

    /// <summary>
    /// Restores the render target captured by the most recent <see cref="SaveScreenTarget"/> call.
    /// This is a general render-target pairing and is independent of the Bloom implementation.
    /// </summary>
    public static void ReDrawScreenTarget()
    {
        if (!DrawSystem.CanUseRender || LastTargetRender == null || SaveScreenRender == null)
        {
            return;
        }

        GraphicsDevice gd = Main.instance.GraphicsDevice;
        SpriteBatch sb = Main.spriteBatch;
        EndBeginDraw(adjustToScreen: false);

        gd.SetRenderTarget(LastTargetRender);
        gd.Clear(Color.Transparent);
        sb.Draw(SaveScreenRender, Vector2.Zero, Color.White);
    }

    public static void SwitchRender(RenderTarget2D target, bool adjustScreenSize = false, int state = 0,
        bool ignoreCanUseRender = false, bool clearTarget = true)
    {
        if (!ignoreCanUseRender && !DrawSystem.CanUseRender)
        {
            return;
        }

        if (target == null && (Render == null || Render2 == null || BloomRender == null || SaveScreenRender == null))
        {
            CreateRender();
        }

        GraphicsDevice gd = Main.instance.GraphicsDevice;
        EndBeginDraw(state, 1, adjustScreenSize);
        gd.SetRenderTarget(target);

        if (clearTarget)
        {
            gd.Clear(Color.Transparent);
        }
    }
}
