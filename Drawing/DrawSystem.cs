using System.Diagnostics;
using System.Linq;
using System.Reflection;
using KL.Configs;
using KL.Dusts;
using KL.Projectiles;
using ReLogic.Graphics;
using Terraria.Graphics.Light;
using Terraria.UI;

namespace KL.Drawing;

public class DrawSystem : ModSystem
{
    public static bool CanUseRender = true;

    private float TotalBloomStrength = 1;
    private float TotalBloomBackGroundStrength = 1;

    public static List<int> DrawCacheProjsBehindNPCsAndTiles = new List<int>(1000);
    public static List<int> DrawCacheProjsBehindNPCs = new List<int>(1000);
    public static List<int> DrawCacheProjsBehindProjectiles = new List<int>(1000);
    public static List<int> DrawCacheProjsOverWiresUI = new List<int>(1000);
    public static List<int> DrawCacheProjsOverPlayers = new List<int>(1000);

    // 延迟绘制的请求队列，用于绘制玻璃折射效果
    public static readonly List<Action> GlassDrawRequests = new List<Action>();


    public static List<RadialBlurInfo> RadialBlurInfos = new List<RadialBlurInfo>(100);

    public static List<RadialWaveWarp> RadialWaveWarpList = new List<RadialWaveWarp>(100);

    public static List<int> DrawBloomProj = new List<int>(1000);

    public static List<Dust> KLDustList = new List<Dust>(8000);

    private static readonly FieldInfo SpriteBatchBeginCalled = typeof(SpriteBatch).GetField("beginCalled",
        BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo SpriteBatchSortMode = typeof(SpriteBatch).GetField("sortMode",
        BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo SpriteBatchBlendState = typeof(SpriteBatch).GetField("blendState",
        BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo SpriteBatchSamplerState = typeof(SpriteBatch).GetField("samplerState",
        BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo SpriteBatchDepthStencilState = typeof(SpriteBatch).GetField("depthStencilState",
        BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo SpriteBatchRasterizerState = typeof(SpriteBatch).GetField("rasterizerState",
        BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo SpriteBatchCustomEffect = typeof(SpriteBatch).GetField("customEffect",
        BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo SpriteBatchTransformMatrix = typeof(SpriteBatch).GetField("transformMatrix",
        BindingFlags.Instance | BindingFlags.NonPublic);

    private readonly Stopwatch _sw = new Stopwatch();

    // 记录最高与平均绘制耗时（单位：ElapsedTicks）
    private long _maxDrawTicks;
    private double _avgDrawTicks;
    private long _drawSamples;

    private int totalBloomTimes = 0;
    private int totalSpecBloomSwitchTimes = 0;

    private static float bloomStr = 1;
    //bloom迭代次数
    private static int bloomItr = 5;

    //bloom效果
    private static bool ShouldBloom2 = true;

    private static Vector2 bloomDrawOffset = Vector2.Zero;

    private static bool capturingBloom;
    // RGB follows the caller's blend mode; alpha records how much destination color is occluded.
    // Additive light never occludes the destination, even where several HDR draws overlap.
    private static readonly BlendState BloomAdditiveBlend = new()
    {
        ColorSourceBlend = Blend.SourceAlpha,
        ColorDestinationBlend = Blend.One,
        AlphaSourceBlend = Blend.Zero,
        AlphaDestinationBlend = Blend.One
    };
    private static readonly BlendState BloomStraightAlphaBlend = new()
    {
        ColorSourceBlend = Blend.SourceAlpha,
        ColorDestinationBlend = Blend.InverseSourceAlpha,
        AlphaSourceBlend = Blend.One,
        AlphaDestinationBlend = Blend.InverseSourceAlpha
    };

    /// <summary>Preserves RGB blending while recording coverage in a RequestBloom target.
    /// KL draw helpers apply this automatically; use it when setting GraphicsDevice.BlendState directly.</summary>
    public static BlendState GetBlendStateForBloom(BlendState blendState)
    {
        if (!capturingBloom)
            return blendState;
        if (ReferenceEquals(blendState, BlendState.Additive))
            return BloomAdditiveBlend;
        if (ReferenceEquals(blendState, BlendState.NonPremultiplied))
            return BloomStraightAlphaBlend;
        return blendState;
    }
    
    public enum DrawLayer
    {
        BehindNPCsAndTiles = 0,
        BehindNPCs = 1,
        BehindProjectiles = 2,
        Normal = 3,
        OverPlayers = 4,
        OverWiresUI = 5
    }

    public static DrawLayer CurrentDrawLayer = DrawLayer.Normal;
    
    public static void SetBloomInfo(float bloomStrength, int bloomIteration,Vector2 bloomDrawOffset = default,bool shouldBloom = true)
    {
        bloomStr = bloomStrength;
        bloomItr = bloomIteration;
        DrawSystem.bloomDrawOffset = bloomDrawOffset;
        ShouldBloom2 = shouldBloom;
    }

    /// <summary>
    /// 是否开启了bloom后处理效果
    /// </summary>
    /// <returns></returns>
    public static bool GetShouldBloom()
    {
        return ShouldBloom2;
    }
    public override void Load()
    {
        On_Main.DrawProjectiles += On_Main_DrawProjectiles;
        On_Main.DrawCachedProjs += On_Main_DrawCachedProjs;
        On_Main.CacheProjDraws += On_Main_CacheProjDraws;
        On_Main.DrawDust += On_Main_DrawDust;
        On_Main.DrawPlayers_AfterProjectiles += On_Main_DrawPlayers_AfterProjectiles;


        On_Main.Draw += On_MainOnDraw;

        On_Main.DrawBG += On_Main_DrawBG;
        On_Main.DrawInfernoRings += On_Main_DrawInfernoRings;
        base.Load();
    }
    

    public override void PostUpdateProjectiles()
    {
        for (int i = RadialBlurInfos.Count - 1; i >= 0; i--)
        {
            var radialBlurInfo = RadialBlurInfos[i];
            radialBlurInfo.Update();

            if (!radialBlurInfo.Active)
            {
                RadialBlurInfos.RemoveAt(i);
            }
        }

        // 使用反向遍历，一边更新一边删除
        for (int i = RadialWaveWarpList.Count - 1; i >= 0; i--)
        {
            var radialWaveWarp = RadialWaveWarpList[i];
            radialWaveWarp.Update();

            if (!radialWaveWarp.Active)
            {
                RadialWaveWarpList.RemoveAt(i);
            }
        }

        base.PostUpdateProjectiles();
    }

    private void On_Main_DrawBG(On_Main.orig_DrawBG orig, Main self)
    {
        orig(self);
    }

    private void On_Main_DrawInfernoRings(On_Main.orig_DrawInfernoRings orig, Main self)
    {
        #if DEBUG && KL_DEBUG_KEYBINDS
        if (KeyBinds.释放技能.JustPressed)
        {
            //Main.LocalPlayer.GetModPlayer<TimeStopPlayer>().ImmuneTimeStop = true;
            RequestTimeStop(this, 180);
            RequestGreyFilter(180);
        }
        #endif
        
        FlushLayerRequests(LayerDrawRequestSystem.DrawTargetLayer.InfernoRings, LayerDrawRequestSystem.DrawTiming.Before);
        orig(self);
        FlushLayerRequests(LayerDrawRequestSystem.DrawTargetLayer.InfernoRings, LayerDrawRequestSystem.DrawTiming.After);

    }

    private void On_MainOnDraw(On_Main.orig_Draw orig, Main self, GameTime gameTime)
    {
        orig(self, gameTime);
    }

    private void On_Main_DrawDust(On_Main.orig_DrawDust orig, Main self)
    {
        FlushLayerRequests(LayerDrawRequestSystem.DrawTargetLayer.Dust, LayerDrawRequestSystem.DrawTiming.Before);
        orig(self);
        FlushLayerRequests(LayerDrawRequestSystem.DrawTargetLayer.Dust, LayerDrawRequestSystem.DrawTiming.After);
    }

    private void On_Main_DrawPlayers_AfterProjectiles(On_Main.orig_DrawPlayers_AfterProjectiles orig, Main self)
    {
        FlushLayerRequests(LayerDrawRequestSystem.DrawTargetLayer.PlayersAfterProjectiles, LayerDrawRequestSystem.DrawTiming.Before);
        orig(self);
        FlushLayerRequests(LayerDrawRequestSystem.DrawTargetLayer.PlayersAfterProjectiles, LayerDrawRequestSystem.DrawTiming.After);
    }

    private void FlushLayerRequests(LayerDrawRequestSystem.DrawTargetLayer layer,
        LayerDrawRequestSystem.DrawTiming timing)
    {
        LayerDrawRequestSystem.Flush(layer, timing);
        if (!LayerDrawRequestSystem.HasBloomRequests(layer, timing))
            return;

        long start = Stopwatch.GetTimestamp();
        try { RenderRequestedBloom(layer, timing); }
        finally { RenderPerformanceDiagnostics.RecordBloom(start, layer, timing); }
    }

    private static void CaptureBloomRequests(LayerDrawRequestSystem.DrawTargetLayer layer,
        LayerDrawRequestSystem.DrawTiming timing)
    {
        bool wasCapturing = capturingBloom;
        capturingBloom = true;
        try
        {
            LayerDrawRequestSystem.FlushBloom(layer, timing);
            EndSpriteBatchIfActive(Main.spriteBatch);
        }
        finally
        {
            capturingBloom = wasCapturing;
        }
    }

    // Renders complete requested visuals once, then derives their normal and bloom results.
    private void RenderRequestedBloom(LayerDrawRequestSystem.DrawTargetLayer layer,
        LayerDrawRequestSystem.DrawTiming timing)
    {
        if (!LayerDrawRequestSystem.HasBloomRequests(layer, timing))
        {
            return;
        }

        if (!ShouldBloom2)
        {
            RenderBloomRequestsWithoutPostProcess(layer, timing);
            return;
        }
        RenderHelper.EnsureRenderTargets();
        GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
        // CanUseRender is false for Retro/Trippy lighting and for WaveQuality 0; both disable
        // the vanilla scene target, so they share the backbuffer path.
        if (!CanUseRender || graphicsDevice.GetRenderTargets().Length == 0)
        {
            RenderBloomRequestsWithoutSceneTarget(layer, timing);
            return;
        }

        if (graphicsDevice.GetRenderTargets()[0].RenderTarget is not RenderTarget2D sceneTarget)
        {
            RenderBloomRequestsWithoutPostProcess(layer, timing);
            return;
        }
        SpriteBatch spriteBatch = Main.spriteBatch;
        SpriteBatchState? previousState = CaptureSpriteBatchState(spriteBatch);
        SamplerState previousAuxiliarySampler = graphicsDevice.SamplerStates[1];
        EndSpriteBatchIfActive(spriteBatch);

        graphicsDevice.SetRenderTarget(RenderHelper.SaveScreenRender);
        graphicsDevice.Clear(Color.Transparent);
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone);
        spriteBatch.Draw(sceneTarget, Vector2.Zero, Color.White);
        spriteBatch.End();

        graphicsDevice.SetRenderTarget(RenderHelper.BloomRender);
        graphicsDevice.Clear(Color.Transparent);
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState,
            DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        CaptureBloomRequests(layer, timing);

        // The callback draws once. Its normal appearance and bloom are both derived from this source.
        EnsureSpriteBatchActive(spriteBatch);

        BeginBloomPass(RenderHelper.Render);
        // Keep the original HDR RGB. Normalizing it by luminance changes the base visual
        // even when bloom strength is zero; only the glow extraction applies a threshold.
        spriteBatch.Draw(RenderHelper.BloomRender, Vector2.Zero, Color.White);

        // Blur only HDR pixels above the bloom threshold, not the complete visual.
        // Render2 is scratch space here; the final scene composite is built after UpSampler.
        BeginBloomPass(RenderHelper.Render2);
        DrawOverflowColorEffect();
        spriteBatch.Draw(RenderHelper.BloomRender, Vector2.Zero, Color.White);

        int iterationCount = Math.Clamp(bloomItr, 2, RenderHelper.BloomDownSample.Length);
        DownSampler(RenderHelper.Render2, iterationCount);
        UpSampler(iterationCount);

        // These targets already contain premultiplied results. Multiplying by alpha again
        // lowers scene coverage, so subsequent bloom layers darken the earlier visuals.
        BeginBloomPass(RenderHelper.Render2);
        spriteBatch.Draw(RenderHelper.SaveScreenRender, Vector2.Zero, Color.White);
        spriteBatch.Draw(RenderHelper.Render, Vector2.Zero, Color.White);

        BeginBloomPass(sceneTarget);
        ReColorEffect(Vector4.One * bloomStr, ReColorState.Bloom, RenderHelper.Render2);
        spriteBatch.Draw(RenderHelper.BloomUpSample[5],
            bloomDrawOffset, null, Color.White, 0, Vector2.Zero,
            new Vector2(sceneTarget.Width, sceneTarget.Height) /
                new Vector2(RenderHelper.BloomUpSample[5].Width, RenderHelper.BloomUpSample[5].Height), SpriteEffects.None, 0);
        EndSpriteBatchIfActive(spriteBatch);
        graphicsDevice.SamplerStates[1] = previousAuxiliarySampler;
        RestoreSpriteBatchState(spriteBatch, previousState);
    }

    // Retro/Trippy lighting and WaveQuality 0 draw directly to the backbuffer, so there is no scene texture
    // to sample. Bloom can still be produced from the requested visual itself; the
    // transparent background makes the final pass a foreground-only composite.
    private void RenderBloomRequestsWithoutSceneTarget(LayerDrawRequestSystem.DrawTargetLayer layer,
        LayerDrawRequestSystem.DrawTiming timing)
    {
        GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
        SpriteBatch spriteBatch = Main.spriteBatch;
        SpriteBatchState? previousState = CaptureSpriteBatchState(spriteBatch);
        SamplerState previousAuxiliarySampler = graphicsDevice.SamplerStates[1];
        EndSpriteBatchIfActive(spriteBatch);

        PresentationParameters presentation = graphicsDevice.PresentationParameters;
        RenderTargetUsage previousUsage = presentation.RenderTargetUsage;
        Viewport previousViewport = graphicsDevice.Viewport;
        Rectangle previousScissor = graphicsDevice.ScissorRectangle;
        // FNA clears a DiscardContents backbuffer on rebind, even without an explicit Clear.
        presentation.RenderTargetUsage = RenderTargetUsage.PreserveContents;
        try
        {
            graphicsDevice.SetRenderTarget(RenderHelper.BloomRender);
            graphicsDevice.Clear(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            CaptureBloomRequests(layer, timing);
            EnsureSpriteBatchActive(spriteBatch);

            BeginBloomPass(RenderHelper.Render);
            spriteBatch.Draw(RenderHelper.BloomRender, Vector2.Zero, Color.White);

            BeginBloomPass(RenderHelper.Render2);
            DrawOverflowColorEffect();
            spriteBatch.Draw(RenderHelper.BloomRender, Vector2.Zero, Color.White);

            int iterationCount = Math.Clamp(bloomItr, 2, RenderHelper.BloomDownSample.Length);
            DownSampler(RenderHelper.Render2, iterationCount, ignoreCanUseRender: true);
            UpSampler(iterationCount, ignoreCanUseRender: true);

            BeginBloomPass(RenderHelper.Render2);
            spriteBatch.Draw(RenderHelper.Render, Vector2.Zero, Color.White);

            BeginBloomPass(null, clearTarget: false);
            ReColorEffect(Vector4.One * bloomStr, ReColorState.Bloom, RenderHelper.Render2);
            spriteBatch.Draw(RenderHelper.BloomUpSample[5], bloomDrawOffset, Color.White);
        }
        finally
        {
            EndSpriteBatchIfActive(spriteBatch);
            graphicsDevice.SetRenderTarget(null);
            presentation.RenderTargetUsage = previousUsage;
            graphicsDevice.Viewport = previousViewport;
            graphicsDevice.ScissorRectangle = previousScissor;
            graphicsDevice.SamplerStates[1] = previousAuxiliarySampler;
            RestoreSpriteBatchState(spriteBatch, previousState);
        }
    }

    private static void RenderBloomRequestsWithoutPostProcess(LayerDrawRequestSystem.DrawTargetLayer layer,
        LayerDrawRequestSystem.DrawTiming timing)
    {
        SpriteBatch spriteBatch = Main.spriteBatch;
        SpriteBatchState? previousState = CaptureSpriteBatchState(spriteBatch);
        EndSpriteBatchIfActive(spriteBatch);
        spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState,
            DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        LayerDrawRequestSystem.FlushBloom(layer, timing);
        EndSpriteBatchIfActive(spriteBatch);
        RestoreSpriteBatchState(spriteBatch, previousState);
    }

    private static void EndSpriteBatchIfActive(SpriteBatch spriteBatch)
    {
        if (SpriteBatchBeginCalled?.GetValue(spriteBatch) is true)
        {
            spriteBatch.End();
        }
    }

    private static void EnsureSpriteBatchActive(SpriteBatch spriteBatch)
    {
        if (SpriteBatchBeginCalled?.GetValue(spriteBatch) is not true)
        {
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, RasterizerState.CullNone);
        }
    }

    private readonly record struct SpriteBatchState(
        SpriteSortMode SortMode,
        BlendState BlendState,
        SamplerState SamplerState,
        DepthStencilState DepthStencilState,
        RasterizerState RasterizerState,
        Effect Effect,
        Matrix TransformMatrix);

    private static SpriteBatchState? CaptureSpriteBatchState(SpriteBatch spriteBatch)
    {
        if (SpriteBatchBeginCalled?.GetValue(spriteBatch) is not true)
        {
            return null;
        }

        return new SpriteBatchState(
            (SpriteSortMode)SpriteBatchSortMode.GetValue(spriteBatch),
            (BlendState)SpriteBatchBlendState.GetValue(spriteBatch),
            (SamplerState)SpriteBatchSamplerState.GetValue(spriteBatch),
            (DepthStencilState)SpriteBatchDepthStencilState.GetValue(spriteBatch),
            (RasterizerState)SpriteBatchRasterizerState.GetValue(spriteBatch),
            (Effect)SpriteBatchCustomEffect.GetValue(spriteBatch),
            (Matrix)SpriteBatchTransformMatrix.GetValue(spriteBatch));
    }

    private static void RestoreSpriteBatchState(SpriteBatch spriteBatch, SpriteBatchState? state)
    {
        if (state is not SpriteBatchState value)
        {
            return;
        }

        spriteBatch.Begin(value.SortMode, value.BlendState, value.SamplerState, value.DepthStencilState,
            value.RasterizerState, value.Effect, value.TransformMatrix);
    }

    private void On_Main_CacheProjDraws(On_Main.orig_CacheProjDraws orig, Main self)
    {
        LayerDrawRequestSystem.ClearFrame();
        DrawCacheProjsBehindNPCsAndTiles.Clear();
        DrawCacheProjsBehindNPCs.Clear();
        DrawCacheProjsBehindProjectiles.Clear();
        DrawCacheProjsOverWiresUI.Clear();
        DrawCacheProjsOverPlayers.Clear();
        DrawBloomProj.Clear();

        KLDustList.Clear();

        
        CanUseRender = true;
        if (Lighting.Mode is LightMode.Retro or LightMode.Trippy)
        {
            //Lighting.Mode = LightMode.Color;
            CanUseRender = false;
        }

        if (Main.WaveQuality < 1)
        {
            //Main.WaveQuality = 1;
            CanUseRender = false;
        }

        orig(self);

        // DrawBehind has sampled and refreshed every managed trail before any target layer is drawn.
        TrailManager.SubmitDrawRequests();

        /*// 对所有列表按type进行排序
        DrawCacheProjsBehindNPCsAndTiles.Sort((projIndex1, projIndex2) =>
            Main.projectile[projIndex1].type.CompareTo(Main.projectile[projIndex2].type));

        DrawCacheProjsBehindNPCs.Sort((projIndex1, projIndex2) =>
            Main.projectile[projIndex1].type.CompareTo(Main.projectile[projIndex2].type));

        DrawCacheProjsBehindProjectiles.Sort((projIndex1, projIndex2) =>
            Main.projectile[projIndex1].type.CompareTo(Main.projectile[projIndex2].type));

        DrawCacheProjsOverWiresUI.Sort((projIndex1, projIndex2) =>
            Main.projectile[projIndex1].type.CompareTo(Main.projectile[projIndex2].type));

        DrawCacheProjsOverPlayers.Sort((projIndex1, projIndex2) =>
            Main.projectile[projIndex1].type.CompareTo(Main.projectile[projIndex2].type));

        DrawBloomProj.Sort((projIndex1, projIndex2) =>
            Main.projectile[projIndex1].type.CompareTo(Main.projectile[projIndex2].type));

        KLDustList.Sort((dust1, dust2) => dust1.type.CompareTo(dust2.type));*/
    }

    private void On_Main_DrawCachedProjs(On_Main.orig_DrawCachedProjs orig, Main self, List<int> projCache,
        bool startSpriteBatch)
    {
                
        if (ReferenceEquals(projCache, self.DrawCacheProjsBehindNPCsAndTiles))
        {
            CurrentDrawLayer = DrawLayer.BehindNPCsAndTiles;
            FlushLayerRequests(LayerDrawRequestSystem.DrawTargetLayer.BehindNPCsAndTiles, LayerDrawRequestSystem.DrawTiming.Before);
        }

        if (ReferenceEquals(projCache, self.DrawCacheProjsBehindNPCs))
        {
            CurrentDrawLayer = DrawLayer.BehindNPCs;
            FlushLayerRequests(LayerDrawRequestSystem.DrawTargetLayer.BehindNPCs, LayerDrawRequestSystem.DrawTiming.Before);
        }

        if (ReferenceEquals(projCache, self.DrawCacheProjsBehindProjectiles))
        {
            CurrentDrawLayer = DrawLayer.BehindProjectiles;
            FlushLayerRequests(LayerDrawRequestSystem.DrawTargetLayer.BehindProjectiles, LayerDrawRequestSystem.DrawTiming.Before);
        }

        if (ReferenceEquals(projCache, self.DrawCacheProjsOverPlayers))
        {
            CurrentDrawLayer = DrawLayer.OverPlayers;
            FlushLayerRequests(LayerDrawRequestSystem.DrawTargetLayer.OverPlayers, LayerDrawRequestSystem.DrawTiming.Before);
        }

        if (ReferenceEquals(projCache, self.DrawCacheProjsOverWiresUI))
        {
            CurrentDrawLayer = DrawLayer.OverWiresUI;
            FlushLayerRequests(LayerDrawRequestSystem.DrawTargetLayer.OverWiresUI, LayerDrawRequestSystem.DrawTiming.Before);
        }
        orig(self, projCache, startSpriteBatch);

        if (ReferenceEquals(projCache, self.DrawCacheProjsBehindNPCsAndTiles))
        {
            FlushLayerRequests(LayerDrawRequestSystem.DrawTargetLayer.BehindNPCsAndTiles, LayerDrawRequestSystem.DrawTiming.After);
        }

        if (ReferenceEquals(projCache, self.DrawCacheProjsBehindNPCs))
        {
            FlushLayerRequests(LayerDrawRequestSystem.DrawTargetLayer.BehindNPCs, LayerDrawRequestSystem.DrawTiming.After);
        }

        if (ReferenceEquals(projCache, self.DrawCacheProjsBehindProjectiles))
        {
            FlushLayerRequests(LayerDrawRequestSystem.DrawTargetLayer.BehindProjectiles, LayerDrawRequestSystem.DrawTiming.After);
        }

        if (ReferenceEquals(projCache, self.DrawCacheProjsOverPlayers))
        {
            FlushLayerRequests(LayerDrawRequestSystem.DrawTargetLayer.OverPlayers, LayerDrawRequestSystem.DrawTiming.After);
        }

        if (ReferenceEquals(projCache, self.DrawCacheProjsOverWiresUI))
        {
            FlushLayerRequests(LayerDrawRequestSystem.DrawTargetLayer.OverWiresUI, LayerDrawRequestSystem.DrawTiming.After);
        }
        
        if (!Main.LocalPlayer.active) return;
        if(!CanUseRender)return;
        
        if (ReferenceEquals(projCache, self.DrawCacheProjsOverWiresUI))
        {
            if (RadialBlurInfos.Count > 0 || RadialWaveWarpList.Count > 0) RenderRadialBlurAndWaterWave();
        }
        
    }

    //处理所有径向模糊和水波效果
    void RenderRadialBlurAndWaterWave()
    {
        if (!Main.LocalPlayer.active) return;
        if (!CanUseRender) return;

        foreach (var radialBlur in RadialBlurInfos)
        {
            RenderHelper.SaveScreenTarget();
            RenderHelper.SwitchRender(Main.screenTarget);

            float strength = MathHelper.Lerp(radialBlur.Strength, 0, radialBlur.CurrentFrame / radialBlur.TotalFrames);
            RadialBlurEffect(radialBlur.Position.GetScreenPosition(), strength, radialBlur.Iterations);

            Main.spriteBatch.Draw(RenderHelper.SaveScreenRender, Vector2.Zero, Color.White);
        }

        foreach (var radialBlur in RadialWaveWarpList)
        {
            RenderHelper.SaveScreenTarget();
            RenderHelper.SwitchRender(Main.screenTarget);
            float time = radialBlur.CurrentFrame / radialBlur.TotalFrames;
            float smoothTime = MathHelper.SmoothStep(0, 1, time);

            float radius = MathHelper.Lerp(radialBlur.MinRadius, radialBlur.MaxRadius, time);
            float maxScale = MathHelper.Lerp(radialBlur.MaxScale, 1f, smoothTime);


            EndBeginDraw(0, 1, false);
            AirDistortionEffect_RadialWaveWarp(radialBlur.Position.GetScreenPosition(), 1 - smoothTime, maxScale,
                radius);
            Main.spriteBatch.Draw(RenderHelper.SaveScreenRender, Vector2.Zero, Color.White);
        }

        EndBeginDraw();
    }

    private void On_Main_DrawProjectiles(On_Main.orig_DrawProjectiles orig, Main self)
    {
        CurrentDrawLayer = DrawLayer.Normal;
        FlushLayerRequests(LayerDrawRequestSystem.DrawTargetLayer.Projectiles, LayerDrawRequestSystem.DrawTiming.Before);
        orig(self);
        // Dust.PreDraw runs later than this target. Submit from live dust now so particles
        // share this frame's projectile bloom, with order keeping them above the bodies.
        KLBasicDust.SubmitBloomDrawRequests();
        FlushLayerRequests(LayerDrawRequestSystem.DrawTargetLayer.Projectiles, LayerDrawRequestSystem.DrawTiming.After);
    }

    // Bloom samples radiance, not pixel art. Set the destination before Begin so the
    // SpriteBatch projection belongs to this viewport, and explicitly filter both inputs.
    // Do not change the world's default sampler or the callbacks' own material choices.
    private static void BeginBloomPass(RenderTarget2D target, BlendState blend = null, bool clearTarget = true)
    {
        var device = Main.instance.GraphicsDevice;
        var batch = Main.spriteBatch;
        EndSpriteBatchIfActive(batch);
        device.SetRenderTarget(target);
        if (clearTarget) device.Clear(Color.Transparent);
        batch.Begin(SpriteSortMode.Immediate, blend ?? BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone);
        device.SamplerStates[1] = SamplerState.LinearClamp;
    }

    private void DownSampler(Texture2D bloomSource, int time = 5, bool ignoreCanUseRender = false)
    {
        if (!ignoreCanUseRender && !CanUseRender) return;
        Texture2D source = bloomSource;
        for (int i = 0; i < time; i++)
        {
            var target = RenderHelper.BloomDownSample[i];
            BeginBloomPass(target, BlendState.Opaque);
            // Contiguous source texels prefilter thin HDR features before each reduction.
            // targetSize/4 used to skip 4–8 source pixels and replicate separated lobes.
            GaussianBlur(new Vector2(source.Width, source.Height), i == 0 ? 1.2f : 1f);
            Main.spriteBatch.Draw(source, new Rectangle(0, 0, target.Width, target.Height), Color.White);
            source = target;
        }
    }

    private void UpSampler(int time = 5, bool ignoreCanUseRender = false)
    {
        if (!ignoreCanUseRender && !CanUseRender) return;
        int startIndex = 7 - time;
        Texture2D source = RenderHelper.BloomDownSample[time - 1];
        for (int i = startIndex; i < RenderHelper.BloomUpSample.Length; i++)
        {
            float scale = MathHelper.Lerp(0.5f, 0.2f, (float)i / 5);
            var target = RenderHelper.BloomUpSample[i];
            BeginBloomPass(target, BlendState.Opaque);
            GaussianBlurTwice(new Vector2(source.Width, source.Height), TotalBloomBackGroundStrength * scale,
                TotalBloomStrength, RenderHelper.BloomDownSample[5 - i]);
            Main.spriteBatch.Draw(source, new Rectangle(0, 0, target.Width, target.Height), Color.White);
            source = target;
        }
    }
}
