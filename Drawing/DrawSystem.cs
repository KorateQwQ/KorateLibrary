using System.Diagnostics;
using System.Linq;
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

    private RenderTarget2D saveScreen;

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

    //之前的bloom效果，弃用了（）
    private static bool ShouldBloom = false;
    
    //bloom效果
    private static bool ShouldBloom2 = true;

    private static Vector2 bloomDrawOffset;
    
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
        #if DEBUG
        if (KeyBinds.释放技能.JustPressed)
        {
            //Main.LocalPlayer.GetModPlayer<TimeStopPlayer>().ImmuneTimeStop = true;
            RequestTimeStop(this, 180);
            RequestGreyFilter(180);
        }
        #endif
        
        LayerDrawRequestSystem.Flush(LayerDrawRequestSystem.DrawTargetLayer.InfernoRings, LayerDrawRequestSystem.DrawTiming.Before);
        orig(self);
        LayerDrawRequestSystem.Flush(LayerDrawRequestSystem.DrawTargetLayer.InfernoRings, LayerDrawRequestSystem.DrawTiming.After);

    }

    private void On_MainOnDraw(On_Main.orig_Draw orig, Main self, GameTime gameTime)
    {
        orig(self, gameTime);
    }

    private void On_Main_DrawDust(On_Main.orig_DrawDust orig, Main self)
    {
        LayerDrawRequestSystem.Flush(LayerDrawRequestSystem.DrawTargetLayer.Dust, LayerDrawRequestSystem.DrawTiming.Before);
        orig(self);
        LayerDrawRequestSystem.Flush(LayerDrawRequestSystem.DrawTargetLayer.Dust, LayerDrawRequestSystem.DrawTiming.After);
        StartBloom();
    }

    private void On_Main_DrawPlayers_AfterProjectiles(On_Main.orig_DrawPlayers_AfterProjectiles orig, Main self)
    {
        LayerDrawRequestSystem.Flush(LayerDrawRequestSystem.DrawTargetLayer.PlayersAfterProjectiles, LayerDrawRequestSystem.DrawTiming.Before);
        orig(self);
        LayerDrawRequestSystem.Flush(LayerDrawRequestSystem.DrawTargetLayer.PlayersAfterProjectiles, LayerDrawRequestSystem.DrawTiming.After);
    }

    //Bloom后处理效果
    void StartBloom()
    {
        if (RenderHelper.BloomRender != null&&CanUseRender&&ShouldBloom2)
        {
            Main.spriteBatch.Begin((SpriteSortMode)1, ScreenBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);

            RenderHelper.DrawOverFlowScreenTarget();
            
            DownSampler(bloomItr);
            UpSampler(bloomItr);
            
            RenderHelper.ReDrawScreenTarget();
            
            Main.spriteBatch.End();
            Main.spriteBatch.Begin((SpriteSortMode)1, ScreenBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
            ReColorEffect(Vector4.One*bloomStr,ReColorState.Bloom,RenderHelper.LastTargetRender);
            Main.spriteBatch.Draw(RenderHelper.BloomUpSample[5], new Vector2(0)+bloomDrawOffset, new Color(255,255,255,255));
            
            Main.spriteBatch.End();

        }
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
            LayerDrawRequestSystem.Flush(LayerDrawRequestSystem.DrawTargetLayer.BehindNPCsAndTiles, LayerDrawRequestSystem.DrawTiming.Before);
        }

        if (ReferenceEquals(projCache, self.DrawCacheProjsBehindNPCs))
        {
            CurrentDrawLayer = DrawLayer.BehindNPCs;
            LayerDrawRequestSystem.Flush(LayerDrawRequestSystem.DrawTargetLayer.BehindNPCs, LayerDrawRequestSystem.DrawTiming.Before);
        }

        if (ReferenceEquals(projCache, self.DrawCacheProjsBehindProjectiles))
        {
            CurrentDrawLayer = DrawLayer.BehindProjectiles;
            LayerDrawRequestSystem.Flush(LayerDrawRequestSystem.DrawTargetLayer.BehindProjectiles, LayerDrawRequestSystem.DrawTiming.Before);
        }

        if (ReferenceEquals(projCache, self.DrawCacheProjsOverPlayers))
        {
            CurrentDrawLayer = DrawLayer.OverPlayers;
            LayerDrawRequestSystem.Flush(LayerDrawRequestSystem.DrawTargetLayer.OverPlayers, LayerDrawRequestSystem.DrawTiming.Before);
        }

        if (ReferenceEquals(projCache, self.DrawCacheProjsOverWiresUI))
        {
            CurrentDrawLayer = DrawLayer.OverWiresUI;
            LayerDrawRequestSystem.Flush(LayerDrawRequestSystem.DrawTargetLayer.OverWiresUI, LayerDrawRequestSystem.DrawTiming.Before);
        }
        orig(self, projCache, startSpriteBatch);

        if (ReferenceEquals(projCache, self.DrawCacheProjsBehindNPCsAndTiles))
        {
            LayerDrawRequestSystem.Flush(LayerDrawRequestSystem.DrawTargetLayer.BehindNPCsAndTiles, LayerDrawRequestSystem.DrawTiming.After);
        }

        if (ReferenceEquals(projCache, self.DrawCacheProjsBehindNPCs))
        {
            LayerDrawRequestSystem.Flush(LayerDrawRequestSystem.DrawTargetLayer.BehindNPCs, LayerDrawRequestSystem.DrawTiming.After);
        }

        if (ReferenceEquals(projCache, self.DrawCacheProjsBehindProjectiles))
        {
            LayerDrawRequestSystem.Flush(LayerDrawRequestSystem.DrawTargetLayer.BehindProjectiles, LayerDrawRequestSystem.DrawTiming.After);
        }

        if (ReferenceEquals(projCache, self.DrawCacheProjsOverPlayers))
        {
            LayerDrawRequestSystem.Flush(LayerDrawRequestSystem.DrawTargetLayer.OverPlayers, LayerDrawRequestSystem.DrawTiming.After);
        }

        if (ReferenceEquals(projCache, self.DrawCacheProjsOverWiresUI))
        {
            LayerDrawRequestSystem.Flush(LayerDrawRequestSystem.DrawTargetLayer.OverWiresUI, LayerDrawRequestSystem.DrawTiming.After);
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

    public void PrePareBloomRender(bool startSpriteBatch = true)
    {
        if (!CanUseRender) return;
        if (!ShouldBloom) return;

        SpriteBatch sb = Main.spriteBatch;
        GraphicsDevice gd = Main.instance.GraphicsDevice;

        if (startSpriteBatch)
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None,
                Main.Rasterizer, null);
        sb.End();
        sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None,
            RasterizerState.CullNone);

        if (Main.graphics.GraphicsDevice.GetRenderTargets().Length > 0)
        {
            Texture target = Main.graphics.GraphicsDevice.GetRenderTargets()[0].RenderTarget;
            saveScreen = target as RenderTarget2D;

            gd.SetRenderTarget(RenderHelper.SaveScreenRenderForBloom); //在这个上面绘制一遍原图，相当于“保存”
            gd.Clear(Color.Transparent);
            sb.Draw(saveScreen, Vector2.Zero, Color.White);
            RenderHelper.SwitchRender(RenderHelper.BloomRender, true);
        }
        else
        {
            CanUseRender = false;
            if (startSpriteBatch) Main.spriteBatch.End();
        }
    }

    private void On_Main_DrawProjectiles(On_Main.orig_DrawProjectiles orig, Main self)
    {
        CurrentDrawLayer = DrawLayer.Normal;
        LayerDrawRequestSystem.Flush(LayerDrawRequestSystem.DrawTargetLayer.Projectiles, LayerDrawRequestSystem.DrawTiming.Before);
        orig(self);
        LayerDrawRequestSystem.Flush(LayerDrawRequestSystem.DrawTargetLayer.Projectiles, LayerDrawRequestSystem.DrawTiming.After);
    }

    private void DownSampler(int time = 5)
    {
        Vector2 screenCenter = new Vector2(Main.screenWidth / 2f, Main.screenHeight / 2f);
        for (int i = 0; i < time; i++)
        {
            RenderHelper.SwitchRender(RenderHelper.BloomDownSample[i], state: 0);
            Vector2 screenSize = new Vector2(RenderHelper.BloomDownSample[i].Width,
                RenderHelper.BloomDownSample[i].Height);
            //EndBeginDraw(1,1,false);
            //GaussianBlur(screenSize);
            if (i == 0)
            {
                GaussianBlur(screenSize / 4f, strength: 1.2f);
                DrawInScreen(RenderHelper.BloomRender, screenSize / 2f, Color.White);
            }
            else
            {
                GaussianBlur(screenSize / 4f, 1f);
                DrawInScreen(RenderHelper.BloomDownSample[i - 1], screenSize, Color.White,
                    screenSize / new Vector2(RenderHelper.BloomDownSample[i - 1].Width,
                        RenderHelper.BloomDownSample[i - 1].Height) * 2);
            }
        }
    }

    private void UpSampler(int time = 5)
    {
        float lastResultScale = 0.5f;
        float BackGroundStrength = TotalBloomBackGroundStrength;
        float BlurStrength = TotalBloomStrength;
        int startIndex = 7 - time;
        for (int i = 7 - time; i < startIndex + (time - 1); i++)
        {
            Vector2 screenSize = new Vector2(RenderHelper.BloomUpSample[i].Width,
                RenderHelper.BloomUpSample[i].Height);

            float scale = MathHelper.Lerp(0.5f, 0.2f, (float)i / 5);
            RenderHelper.SwitchRender(RenderHelper.BloomUpSample[i], state: 0);
            GaussianBlurTwice(screenSize / 4f, BackGroundStrength * scale, BlurStrength,
                RenderHelper.BloomDownSample[5 - i]); //MathF.Pow(2,time-2-i)
            //GaussianBlur(screenSize,1f);
            if (i == startIndex)
            {
                DrawInScreen(RenderHelper.BloomDownSample[time - 1], screenSize / 4f, Color.White, Vector2.One); //
            }
            else
            {
                DrawInScreen(RenderHelper.BloomUpSample[i - 1], screenSize / 4f, Color.White, Vector2.One); //
            }
        }
    }
}
