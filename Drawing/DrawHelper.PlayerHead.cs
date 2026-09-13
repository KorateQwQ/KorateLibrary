namespace KL.Drawing;

using Terraria.DataStructures;

public partial class DrawHelper : ModSystem
{
    // DrawPlayerHead is only called from the main draw thread. Keeping these
    // buffers here avoids allocating three lists for every avatar while also
    // matching the buffers used by LegacyPlayerRenderer.
    private static readonly List<DrawData> StaticHeadDrawData = [];
    private static readonly List<int> StaticHeadDust = [];
    private static readonly List<int> StaticHeadGore = [];

    /// <summary>
    /// 绘制一个不会随玩家移动产生头部摆动的静态玩家头像。
    ///
    /// 头像复用 tModLoader 的头部绘制管线，因此其它 Mod 注册的
    /// PlayerDrawLayer、ModifyDrawInfo 和装备外观仍然会生效。绘制期间只暂时
    /// 冻结会影响头部动画的玩家状态，结束后会完整恢复原玩家状态。
    /// </summary>
    /// <param name="player">要绘制的玩家。</param>
    /// <param name="position">头像中心位置。</param>
    /// <param name="alpha">绘制透明度。</param>
    /// <param name="scale">绘制缩放。</param>
    /// <param name="borderColor">原版头部渲染器使用的边框颜色。</param>
    /// <param name="forceRightFacing">是否固定为向右朝向，避免头像因玩家转身跳动。</param>
    /// <param name="forceUpright">是否固定为正常重力方向，避免反重力改变头像的垂直基准。</param>
    public static void DrawStaticPlayerHead(
        Player player,
        Vector2 position,
        float alpha = 1f,
        float scale = 1f,
        Color borderColor = default,
        bool forceRightFacing = true,
        bool forceUpright = true)
    {
        if (player is null || !player.active || player.ShouldNotDraw)
        {
            return;
        }

        Rectangle originalBodyFrame = player.bodyFrame;
        Rectangle originalLegFrame = player.legFrame;
        double headFrameCounter = player.headFrameCounter;
        double bodyFrameCounter = player.bodyFrameCounter;
        double legFrameCounter = player.legFrameCounter;
        float headRotation = player.headRotation;
        float bodyRotation = player.bodyRotation;
        float legRotation = player.legRotation;
        Vector2 headPosition = player.headPosition;
        Vector2 bodyPosition = player.bodyPosition;
        Vector2 legPosition = player.legPosition;
        Vector2 headVelocity = player.headVelocity;
        Vector2 bodyVelocity = player.bodyVelocity;
        Vector2 legVelocity = player.legVelocity;
        Vector2 velocity = player.velocity;
        float fullRotation = player.fullRotation;
        Vector2 fullRotationOrigin = player.fullRotationOrigin;
        float gfxOffY = player.gfxOffY;
        int direction = player.direction;
        float gravDir = player.gravDir;

        try
        {
            // HeadOnlySetup uses bodyFrame.Y to apply the movement bobbing offset.
            // Keeping the frame size while selecting its first row gives the same
            // equipment and hair frame every time the avatar is drawn.
            Rectangle staticBodyFrame = originalBodyFrame;
            Rectangle staticLegFrame = originalLegFrame;
            staticBodyFrame.Y = 0;
            staticLegFrame.Y = 0;
            player.bodyFrame = staticBodyFrame;
            player.legFrame = staticLegFrame;

            player.headFrameCounter = 0d;
            player.bodyFrameCounter = 0d;
            player.legFrameCounter = 0d;
            player.headRotation = 0f;
            player.bodyRotation = 0f;
            player.legRotation = 0f;
            player.headPosition = Vector2.Zero;
            player.bodyPosition = Vector2.Zero;
            player.legPosition = Vector2.Zero;
            player.headVelocity = Vector2.Zero;
            player.bodyVelocity = Vector2.Zero;
            player.legVelocity = Vector2.Zero;
            // Some third-party head layers inspect the player's movement
            // vector directly instead of using the frame fields above.
            player.velocity = Vector2.Zero;
            player.fullRotation = 0f;
            player.fullRotationOrigin = Vector2.Zero;
            player.gfxOffY = 0f;
            if (forceRightFacing)
            {
                player.direction = 1;
            }
            if (forceUpright)
            {
                // HeadOnlySetup and several vanilla/custom head layers use
                // gravDir to mirror offsets vertically. A static portrait has
                // a fixed canvas, so reverse gravity must not move its anchor.
                player.gravDir = 1f;
            }

            DrawStaticPlayerHeadLayers(player, position, alpha, scale, borderColor);
        }
        finally
        {
            player.bodyFrame = originalBodyFrame;
            player.legFrame = originalLegFrame;
            player.headFrameCounter = headFrameCounter;
            player.bodyFrameCounter = bodyFrameCounter;
            player.legFrameCounter = legFrameCounter;
            player.headRotation = headRotation;
            player.bodyRotation = bodyRotation;
            player.legRotation = legRotation;
            player.headPosition = headPosition;
            player.bodyPosition = bodyPosition;
            player.legPosition = legPosition;
            player.headVelocity = headVelocity;
            player.bodyVelocity = bodyVelocity;
            player.legVelocity = legVelocity;
            player.velocity = velocity;
            player.fullRotation = fullRotation;
            player.fullRotationOrigin = fullRotationOrigin;
            player.gfxOffY = gfxOffY;
            player.direction = direction;
            player.gravDir = gravDir;
        }
    }

    private static void DrawStaticPlayerHeadLayers(
        Player player,
        Vector2 position,
        float alpha,
        float scale,
        Color borderColor)
    {
        StaticHeadDrawData.Clear();
        StaticHeadDust.Clear();
        StaticHeadGore.Clear();

        Terraria.DataStructures.PlayerDrawSet drawInfo = default;
        drawInfo.HeadOnlySetup(
            player,
            StaticHeadDrawData,
            StaticHeadDust,
            StaticHeadGore,
            position.X + Main.screenPosition.X,
            position.Y + Main.screenPosition.Y,
            alpha,
            scale);

        // This is the head-only portion of LegacyPlayerRenderer's
        // DrawPlayerInternal. It still executes ModifyDrawInfo and every
        // registered head PlayerDrawLayer, which is what lets visual mods
        // such as 伊蕾娜 provide their custom player appearance.
        PlayerLoader.ModifyDrawInfo(ref drawInfo);
        foreach (var layer in PlayerDrawLayerLoader.GetDrawLayers(drawInfo))
        {
            if (layer.IsHeadLayer)
            {
                layer.DrawWithTransformationAndChildren(ref drawInfo);
            }
        }

        Terraria.DataStructures.PlayerDrawLayers.DrawPlayer_MakeIntoFirstFractalAfterImage(ref drawInfo);
        Terraria.DataStructures.PlayerDrawLayers.DrawPlayer_TransformDrawData(ref drawInfo);
        if (scale != 1f)
        {
            Terraria.DataStructures.PlayerDrawLayers.DrawPlayer_ScaleDrawData(ref drawInfo, scale);
        }

        Terraria.DataStructures.PlayerDrawLayers.DrawPlayer_RenderAllLayers(ref drawInfo);
    }
}
