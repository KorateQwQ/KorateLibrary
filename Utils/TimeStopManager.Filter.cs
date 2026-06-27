using KL.Dusts;
using Terraria.Graphics.Effects;

namespace KL.Utils;

public class TimeStopManager_Filter : ModSystem
{
    static RenderTarget2D 前景;
    static RenderTarget2D 后景;
    static RenderTarget2D player;

    private List<Player> _playersThatDrawAfterProjectiles = new List<Player>(255);
    private List<Player> _playersThatDrawAfterProjectiles2 = new List<Player>(255);

    public override void Load()
    {
        On_Main.Draw += On_MainOnDraw;
        Terraria.Graphics.Effects.On_FilterManager.EndCapture +=
            FilterManager_EndCapture; //原版绘制场景的最后部分——滤镜。在这里运用render保证不会与原版冲突
        Main.OnResolutionChanged += Main_OnResolutionChanged;


        On_Main.DrawPlayers_AfterProjectiles += On_Main_DrawPlayers_AfterProjectiles;
        On_Main.DrawCachedProjs += On_Main_DrawCachedProjs;
        On_Main.RefreshPlayerDrawOrder += On_Main_RefreshPlayerDrawOrder;
        On_Main.DrawDust += On_Main_DrawDust;
        base.Load();
    }

    private static void Main_OnResolutionChanged(Vector2 obj) => CreateRender();

    private static void CreateRender()
    {
        前景 = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight,
            false, SurfaceFormat.Vector4, DepthFormat.None);
        后景 = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight,
            false, SurfaceFormat.Vector4, DepthFormat.None);
        player = new RenderTarget2D(Main.graphics.GraphicsDevice, Main.screenWidth, Main.screenHeight,
            false, SurfaceFormat.Vector4, DepthFormat.None);
    }


    private void FilterManager_EndCapture(On_FilterManager.orig_EndCapture orig, FilterManager self,
        RenderTarget2D finalTexture, RenderTarget2D screenTarget1, RenderTarget2D screenTarget2, Color clearColor)
    {
        if (前景 == null || 后景 == null || player == null) CreateRender();
        orig(self, finalTexture, screenTarget1, screenTarget2, clearColor);
    }

    private void On_MainOnDraw(On_Main.orig_Draw orig, Main self, GameTime gameTime)
    {
        orig(self, gameTime);
    }

    private void On_Main_RefreshPlayerDrawOrder(On_Main.orig_RefreshPlayerDrawOrder orig, Main self)
    {
        TimeStopGlobalProjectile.SpecialDrawInTimeStop = false;

        _playersThatDrawAfterProjectiles2.Clear();
        _playersThatDrawAfterProjectiles.Clear();
        if (Main.gameMenu)
            return;
        Player player = null;
        for (int i = 0; i < 255; i++)
        {
            player = Main.player[i];
            if (i != Main.myPlayer && player.active && !player.outOfRange)
            {
                if (player.isLockedToATile)
                {
                    //_playersThatDrawBehindNPCs.Add(player);
                }
                else
                    _playersThatDrawAfterProjectiles.Add(player);
            }
        }

        player = Main.LocalPlayer;
        if (player.isLockedToATile)
        {
            //_playersThatDrawBehindNPCs.Add(player);
        }
        else
            _playersThatDrawAfterProjectiles.Add(player);

        orig(self);
    }

    private void On_Main_DrawDust(On_Main.orig_DrawDust orig, Main self)
    {
        orig(self);
    }

    private void On_Main_DrawCachedProjs(On_Main.orig_DrawCachedProjs orig, Main self, List<int> projCache,
        bool startSpriteBatch)
    {
        if (ReferenceEquals(projCache, self.DrawCacheProjsOverWiresUI) && GreyEffect && Main.LocalPlayer.active &&
            DrawSystem.CanUseRender)
        {
            if (GreyEffect && Main.graphics.GraphicsDevice.GetRenderTargets().Length > 0 &&
                Main.graphics.GraphicsDevice.GetRenderTargets()[0].RenderTarget != null)
            {
                GraphicsDevice gd = Main.instance.GraphicsDevice;
                SpriteBatch sb = Main.spriteBatch;
                RenderTarget2D target =
                    Main.graphics.GraphicsDevice.GetRenderTargets()[0].RenderTarget as RenderTarget2D;

                sb.End();
                sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState,
                    DepthStencilState.None, RasterizerState.CullNone, null);
                gd.SetRenderTarget(后景); //保存后景绘制在·
                gd.Clear(Color.Transparent);

                sb.Draw(target, Vector2.Zero, Color.White);
                /*sb.End();
                    sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.Transform);*/
                //切换到主屏幕，绘制前景并应用黑白shader
                gd.SetRenderTarget(Main.screenTarget);
                gd.Clear(Color.Transparent);

                sb.End();
                sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState,
                    DepthStencilState.None, RasterizerState.CullNone, null);
                ReColorEffect(new Vector4(1), ReColorState.Grey);
                sb.Draw(前景, Vector2.Zero, Color.White);

                sb.End();
                sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState,
                    DepthStencilState.None, RasterizerState.CullNone, null, Main.Transform);
                sb.Draw(player, Vector2.Zero, Color.White);

                sb.End();
                sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState,
                    DepthStencilState.None, RasterizerState.CullNone, null);
                ReColorEffect(new Vector4(1), ReColorState.Grey);
                sb.Draw(后景, Vector2.Zero, Color.White);

                sb.End();
                sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState,
                    DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
                
                TimeStopGlobalProjectile.SpecialDrawInTimeStop = true;
                orig(self, projCache, startSpriteBatch);

                DrawModDustOrig();

                /*RenderHelper.ReDrawScreenTarget();
                    sb.End();
                    sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None,  Main.Rasterizer, null,  Main.Transform);
                    sb.Draw(Homura.render, Vector2.Zero, Color.White);*/
            }
        }
        else
        {
            orig(self, projCache, startSpriteBatch);
        }
    }

    private void On_Main_DrawPlayers_AfterProjectiles(On_Main.orig_DrawPlayers_AfterProjectiles orig, Main self)
    {
        if (GreyEffect && Main.graphics.GraphicsDevice.GetRenderTargets().Length > 0 &&
            Main.graphics.GraphicsDevice.GetRenderTargets()[0].RenderTarget != null)
        {
            foreach (Player player in _playersThatDrawAfterProjectiles)
            {
                if (!player.GetModPlayer<TimeStopPlayer>().CanMoveInTimeStop)
                {
                    Main.PlayerRenderer.DrawPlayer(Main.Camera, player, player.position, 0f, player.fullRotationOrigin);
                }
                else
                {
                    _playersThatDrawAfterProjectiles2.Add(player);
                }
            }

            RenderTarget2D currentTarget =
                Main.graphics.GraphicsDevice.GetRenderTargets()[0].RenderTarget as RenderTarget2D;

            Main.LocalPlayer.gravDir = 1;
            GraphicsDevice gd = Main.instance.GraphicsDevice;
            SpriteBatch sb = Main.spriteBatch;
            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None,
                RasterizerState.CullNone, null);
            sb.End();

            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None,
                RasterizerState.CullNone, null);
            //保存前景绘制
            gd.SetRenderTarget(前景);
            gd.Clear(Color.Transparent);
            sb.Draw(currentTarget, Vector2.Zero, Color.White);
            sb.End();



            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None,
                RasterizerState.CullNone, null);
            //保存可动人物绘制
            gd.SetRenderTarget(player);
            gd.Clear(Color.Transparent);
            foreach (Player player in _playersThatDrawAfterProjectiles2)
            {
                if (player.GetModPlayer<TimeStopPlayer>().CanMoveInTimeStop)
                {
                    Main.PlayerRenderer.DrawPlayer(Main.Camera, player, player.position, player.fullRotation,
                        player.fullRotationOrigin);
                }
            }


            sb.End();

            sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None,
                RasterizerState.CullNone, null);
            gd.SetRenderTarget(currentTarget); //切换到主屏幕
            gd.Clear(Color.Transparent);
            //sb.Draw(Homura.前景, Vector2.Zero, Color.White);
            sb.End();
        }
        else orig(self);
    }

    //不干掉原版绘制，而是额外在粒子上画一层灰的
    void DrawModDustOrig()
    {
        //stop = true;
        foreach (var dustInfo in StopDustList)
        {
            if (dustInfo.dustIndex >= 0 && dustInfo.dustIndex < Main.dust.Length)
            {
                Main.dust[dustInfo.dustIndex].active = false;
            }
        }

        Main.spriteBatch.End();
        //调用原版绘制粒子，只绘制可动粒子
        var drawDustMethod = typeof(Main).GetMethod("DrawDust",
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (drawDustMethod != null) drawDustMethod.Invoke(Main.instance, null);

        foreach (var dustInfo in StopDustList)
        {
            if (dustInfo.dustIndex >= 0 && dustInfo.dustIndex < Main.dust.Length)
            {
                Main.dust[dustInfo.dustIndex].active = true;
            }
        }

        StopDustList.Clear();

        //Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, Main.DefaultSamplerState,
            DepthStencilState.None, Main.Rasterizer, null, Main.Transform);

        if (GreyEffect)
        {
            for (int i = 0; i < Main.maxDustToDraw; i++)
            {
                Dust dust = Main.dust[i];
                ModDust modDust = DustLoader.GetDust(dust.type);
                //Main.NewText(modDust == null);
                if (!dust.active)
                    continue;


                if (modDust != null && i >= 0 && i < TimeStopDustList.Count && TimeStopDustList[i].timeInStop > 0)
                {
                    Color newColor = Lighting.GetColor((int)((double)dust.position.X + 4.0) / 16,
                        (int)((double)dust.position.Y + 4.0) / 16);
                    newColor = dust.GetAlpha(newColor);
                    float scale = dust.GetVisualScale();
                    if (modDust.PreDraw(dust))
                    {
                        Main.spriteBatch.Draw(modDust.Texture2D.Value, dust.position - Main.screenPosition, dust.frame,
                            newColor, dust.rotation, new Vector2(4f, 4f), scale, SpriteEffects.None, 0f);

                        if (dust.color != default)
                        {
                            Main.spriteBatch.Draw(modDust.Texture2D.Value, dust.position - Main.screenPosition,
                                dust.frame, dust.GetColor(newColor), dust.rotation, new Vector2(4f, 4f), scale,
                                SpriteEffects.None, 0f);
                        }
                    }


                    if (newColor == Color.Black)
                        dust.active = false;

                    continue;
                }
            }
        }
    }
}