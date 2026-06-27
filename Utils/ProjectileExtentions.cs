namespace KL.Extensions;

public static class ProjectileExtentions
{
    public static DrawSystem.DrawLayer GetCurrentDrawLayer(this Projectile projectile)
    {
        return DrawSystem.CurrentDrawLayer;
    }
    /// <summary>
    /// 调整跟随玩家的弹幕位置，使其在斜坡时能正常显示而非剧烈抖动
    /// </summary>
    /// <param name="projectile"></param>
    /// <param name="center"></param>
    /// <param name="playerId"></param>
    /// <returns></returns>
    public static Vector2 AdjustRealPosition(this Projectile projectile,Vector2 center, int playerId = -1)
    {
        if(playerId==-1)playerId=projectile.owner;
        return (center+new Vector2(0,Main.player[playerId].gfxOffY)).Floor() ;
    }
    public static void DrawSelf(this Projectile projectile,Color color, float scale)
    {
        var tex = TextureAssets.Projectile[projectile.type].Value;
        Main.spriteBatch.Draw(tex, projectile.Center - Main.screenPosition, tex.Frame(), color,
            projectile.rotation, tex.Origin(), scale, SpriteEffects.None, 0);
    }

    //世界坐标转屏幕坐标（别用utils的那个不对）
    public static Vector2 GetScreenPosition(this Vector2 position)
    {
        return Vector2.Transform(position-Main.screenPosition, Main.GameViewMatrix.ZoomMatrix);
    }

    /// <summary>
    /// 获取向量的垂直分量，默认标准化。
    /// </summary>
    /// <param name="vector2"></param>
    /// <param name="normalize"></param>
    /// <returns></returns>
    public static Vector2 GetPerpendicular(this Vector2 vector2,bool normalize = true)
    {
        Vector2 perpendicular = new Vector2(-vector2.Y, vector2.X);
        if (normalize) perpendicular = perpendicular.SafeNormalize(new Vector2(1, 0));
        return perpendicular;
    }

    public static Vector2 AjustPositionByPlayer(this Vector2 position, Player player)
    {
        return (position+new Vector2(0,player.gfxOffY)).Floor() ;
    }

    public static void NetUpdate(this Projectile projectile)
    {
        NetMessage.SendData(MessageID.SyncProjectile, -1, -1, null, projectile.whoAmI);
    }

}