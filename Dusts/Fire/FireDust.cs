namespace KL.Dusts.Fire;

public class FireDust : KLBasicDust
{
    private const string RequestId = "KL:FireDust";
    // 序列帧本体乘 HDR 倍率，柔光交给与弹幕共用的 Projectiles.After Bloom
    private static float Hdr => 2.2f;

    public override bool PreDraw(Dust dust) => false;

    public override void SubmitBloomDrawRequest(Dust dust)
    {
        int type = dust.type;
        object data = dust.customData;
        // 同组只切换一次批次；与弹幕和其他粒子共用 Projectiles.After 的 Bloom
        LayerDrawRequestSystem.RequestBloom(RequestId, LayerDrawRequestSystem.DrawTargetLayer.Projectiles,
            LayerDrawRequestSystem.DrawTiming.After, context =>
        {
            if (context.IsFirst) EndBeginDraw(0, 1, ss: SamplerState.LinearClamp);
            if (dust.active && dust.type == type && ReferenceEquals(dust.customData, data))
                DrawFire(dust);
            if (context.IsLast) EndBeginDraw();
        }, order: LayerDrawRequestSystem.DustOrder);
    }

    private void DrawFire(Dust dust)
    {
        if (MainTexture == null) return;
        int Frame = (int)MathHelper.Lerp(0, 16, dust.LifeProgress());
        ReColorEffect(new Vector4(Hdr, Hdr, Hdr, 1f));
        DrawInWorld(new TextureInfo(MainTexture,Frame,4,4), dust.position, dust.GetRealColor(dust.color),
            dust.Size(), dust.rotation+3.14f/2);
    }

    public override bool Update(Dust dust)
    {

        dust.velocity *= 0.9f;
        Vector3 color = new Color(255, 150, 50, 255).ToVector3() * dust.color.ToVector3();

        Lighting.AddLight(dust.position, color*(1-dust.LifeProgress()));

        return base.Update(dust);
    }

}
