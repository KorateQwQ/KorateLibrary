namespace KL.Dusts;

/// <summary>
/// 和LineSparkle基本一致，但是粒子会朝一个随机的方向旋转。
/// 只画一条 HDR 亮线，光晕由同层 Bloom 产生，不再叠加辉光贴图。
/// </summary>
public class LineRotSparkle : KLBasicDust
{
    private const string RequestId = "KL:LineRotSparkle";
    // ShotLine 244×102：亮线居中约 7 像素，上下是大片偏灰的柔光，AlphaBlend 下会压暗背景；只取中间亮带
    private static Rectangle LineBand => new(0, 45, 244, 12);
    // 单层纯色靠倍率超过 Bloom 阈值：中心过曝泛白，边缘保留粒子颜色。
    // 细线面积小，Bloom 模糊后能量分散，倍率需明显高于面状光效才有可见光晕
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
        // 同 id 的请求合并成一组：首项切到 Immediate 以便逐个设置 HDR，末项恢复默认批次
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
        float fade = MathHelper.Lerp(1, 0, dust.LifeProgress());
        if (fade <= 0f) return;
        // 淡出同时作用于亮度与覆盖度，避免只剩遮挡、在亮背景上发灰
        ReColorEffect(new Vector4(Hdr * fade, Hdr * fade, Hdr * fade, 1f));
        Color color = dust.color;
        color.A = (byte)(255 * fade);
        Vector2 size = dust.Size();
        Vector2 scale = new(0.26f * size.X * fade, 0.25f * size.Y * MathHelper.Lerp(0.4f, 1f, fade));
        Main.spriteBatch.Draw(MainTex, dust.position - Main.screenPosition, LineBand, color, dust.rotation,
            new Vector2(LineBand.Width, LineBand.Height) * 0.5f, scale, SpriteEffects.None, 0);
    }

    public override bool Update(Dust dust)
    {
        if(dust.DustInfo()!=null&& dust.DustInfo().DustData==null) dust.DustInfo().DustData = Main.rand.NextFloat(-0.08f, 0.08f);

        if (dust.DustInfo().DustData is float rot)
        {
            dust.velocity = dust.velocity.RotatedBy(rot);
        }
        dust.velocity *= 0.9f;
        Lighting.AddLight(dust.position,dust.color.ToVector3());

        return base.Update(dust);
    }
}
