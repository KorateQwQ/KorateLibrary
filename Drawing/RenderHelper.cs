namespace KL.Drawing;

public class RenderHelper : ModSystem
{
    public static RenderTarget2D Render;

    public static RenderTarget2D Render2;

    internal static RenderTarget2D LastTargetRender;
    internal static RenderTarget2D SaveScreenRender;
    internal static RenderTarget2D BloomRender;

    internal static RenderTarget2D[] BloomDownSample = new RenderTarget2D[7];
    // UpSampler ends at index 5 (full resolution). Index 6 was never sampled or drawn,
    // but allocated a target with twice the screen width AND height.
    internal static RenderTarget2D[] BloomUpSample = new RenderTarget2D[6];

    public override void Load()
    {
        if (Main.dedServ)
            return;

        Terraria.Graphics.Effects.On_FilterManager.EndCapture +=
            FilterManager_EndCapture;
        Main.OnResolutionChanged += Main_OnResolutionChanged; //屏幕分辨率改变时，重新设置render
        
        base.Load();
    }

    public override void Unload()
    {
        Terraria.Graphics.Effects.On_FilterManager.EndCapture -= FilterManager_EndCapture;
        Main.OnResolutionChanged -= Main_OnResolutionChanged;

        // Detach now: the queued disposal must never release a subsequent reload's targets.
        List<RenderTarget2D> targets = DetachRenderTargets();
        if (targets.Count > 0)
            Main.QueueMainThreadAction(() => DisposeTargets(targets));
        base.Unload();
    }

    private static void CreateRender()
    {
        long allocationStart = System.Diagnostics.Stopwatch.GetTimestamp();
        DisposeTargets(DetachRenderTargets());
        GraphicsDevice device = Main.graphics.GraphicsDevice;
        int width = Math.Max(1, Main.screenWidth);
        int height = Math.Max(1, Main.screenHeight);
        try
        {
            Render = new RenderTarget2D(device, width, height,
                false, SurfaceFormat.Vector4, DepthFormat.None);
            Render2 = new RenderTarget2D(device, width, height,
                false, SurfaceFormat.Vector4, DepthFormat.None);
            BloomRender = new RenderTarget2D(device, width, height,
                false, SurfaceFormat.Vector4, DepthFormat.None);
            SaveScreenRender = new RenderTarget2D(device, width, height,
                false, SurfaceFormat.Vector4, DepthFormat.None);

            width = Math.Max(1, device.PresentationParameters.BackBufferWidth);
            height = Math.Max(1, device.PresentationParameters.BackBufferHeight);
            for (int i = 0; i < BloomDownSample.Length; i++)
            {
                BloomDownSample[i] = new RenderTarget2D(device, Math.Max(1, width >> i), Math.Max(1, height >> i),
                    false, SurfaceFormat.Vector4, DepthFormat.None);
            }

            for (int i = 0; i < BloomUpSample.Length; i++)
            {
                int shift = BloomUpSample.Length - 1 - i;
                BloomUpSample[i] = new RenderTarget2D(device, Math.Max(1, width >> shift), Math.Max(1, height >> shift),
                    false, SurfaceFormat.Vector4, DepthFormat.None);
            }
        }
        catch
        {
            // A failed allocation must not leave an apparently usable, partial target set.
            DisposeTargets(DetachRenderTargets());
            throw;
        }
        finally
        {
            RenderPerformanceDiagnostics.RecordAllocation(allocationStart);
        }
    }

    private static List<RenderTarget2D> DetachRenderTargets()
    {
        var targets = new List<RenderTarget2D>();
        if (Render != null) targets.Add(Render);
        if (Render2 != null) targets.Add(Render2);
        if (BloomRender != null) targets.Add(BloomRender);
        if (SaveScreenRender != null) targets.Add(SaveScreenRender);
        Render = Render2 = BloomRender = SaveScreenRender = null;
        // LastTargetRender is borrowed from Terraria, never owned by this helper.
        LastTargetRender = null;
        foreach (RenderTarget2D target in BloomDownSample)
            if (target != null) targets.Add(target);
        foreach (RenderTarget2D target in BloomUpSample)
            if (target != null) targets.Add(target);
        Array.Clear(BloomDownSample, 0, BloomDownSample.Length);
        Array.Clear(BloomUpSample, 0, BloomUpSample.Length);
        return targets;
    }

    private static void DisposeTargets(List<RenderTarget2D> targets)
    {
        foreach (RenderTarget2D target in targets)
            target.Dispose();
    }

    private static void FilterManager_EndCapture(Terraria.Graphics.Effects.On_FilterManager.orig_EndCapture orig,
        Terraria.Graphics.Effects.FilterManager self, RenderTarget2D finalTexture, RenderTarget2D screenTarget1,
        RenderTarget2D screenTarget2, Color clearColor)
    {
        EnsureRenderTargets();
        orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
    }

    private static void Main_OnResolutionChanged(Vector2 obj) => EnsureRenderTargets();

    internal static void EnsureRenderTargets()
    {
        GraphicsDevice device = Main.graphics.GraphicsDevice;
        int width = Math.Max(1, Main.screenWidth);
        int height = Math.Max(1, Main.screenHeight);
        if (!TargetMatches(Render, device, width, height) || !TargetMatches(Render2, device, width, height) ||
            !TargetMatches(BloomRender, device, width, height) || !TargetMatches(SaveScreenRender, device, width, height))
        {
            CreateRender();
            return;
        }

        width = Math.Max(1, device.PresentationParameters.BackBufferWidth);
        height = Math.Max(1, device.PresentationParameters.BackBufferHeight);
        for (int i = 0; i < BloomDownSample.Length; i++)
        {
            if (!TargetMatches(BloomDownSample[i], device, Math.Max(1, width >> i), Math.Max(1, height >> i)))
            {
                CreateRender();
                return;
            }
        }
        for (int i = 0; i < BloomUpSample.Length; i++)
        {
            int shift = BloomUpSample.Length - 1 - i;
            if (!TargetMatches(BloomUpSample[i], device, Math.Max(1, width >> shift), Math.Max(1, height >> shift)))
            {
                CreateRender();
                return;
            }
        }
    }

    private static bool TargetMatches(RenderTarget2D target, GraphicsDevice device, int width, int height) =>
        target != null && !target.IsDisposed && ReferenceEquals(target.GraphicsDevice, device) &&
        target.Width == width && target.Height == height;

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
