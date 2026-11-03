namespace KL.Dusts;

public class LineSparkle : KLBasicDust
{
    private const string RequestId = "KL:LineSparkle";
    // 与 LineRotSparkle 一样只取亮线，柔光由共享 Bloom 后处理生成。
    private static Rectangle LineBand => new(0, 45, 244, 12);
    private static float Hdr => 10f;
    private static Texture2D MainTex;
    public override void OnSpawn(Dust dust)
    {
        MainTex ??= Mod.Assets.Request<Texture2D>("Effects/Tex/Sparkle/ShotLine", AssetRequestMode.ImmediateLoad).Value;
        base.OnSpawn(dust);
    }

    public override bool PreDraw(Dust dust) => false;

    public override void SubmitBloomDrawRequest(Dust dust)
    {
        int type = dust.type;
        object data = dust.customData;
        // 同组只切换一次批次；与弹幕和其他粒子共用 Projectiles.After 的 Bloom。
        LayerDrawRequestSystem.RequestBloom(RequestId, LayerDrawRequestSystem.DrawTargetLayer.Projectiles,
            LayerDrawRequestSystem.DrawTiming.After, context =>
        {
            if (context.IsFirst) EndBeginDraw(0, 1, ss: SamplerState.LinearClamp);
            if (dust.active && dust.type == type && ReferenceEquals(dust.customData, data))
                DrawLine(dust);
            if (context.IsLast) EndBeginDraw();
        }, order: LayerDrawRequestSystem.DustOrder);
    }

    private static void DrawLine(Dust dust)
    {
        if (MainTex == null) return;
        float fade = MathHelper.Clamp(1f - dust.LifeProgress(), 0f, 1f);
        if (fade <= 0f) return;
        ReColorEffect(new Vector4(Hdr * fade, Hdr * fade, Hdr * fade, 1f));
        Color color = dust.GetRealColor(dust.color);
        color.A = (byte)(255 * fade);
        Vector2 size = dust.Size();
        Vector2 scale = new(0.2f * size.X * fade, 0.25f * size.Y * MathHelper.Lerp(0.4f, 1f, fade));
        Main.spriteBatch.Draw(MainTex, dust.position - Main.screenPosition, LineBand, color, dust.rotation,
            new Vector2(LineBand.Width, LineBand.Height) * 0.5f, scale, SpriteEffects.None, 0);
    }

    public override bool Update(Dust dust)
    {
        dust.velocity *= 0.92f;
        Lighting.AddLight(dust.position,dust.color.ToVector3());

        return base.Update(dust);
    }
}
