namespace KL.Dusts.Fire;

public class FireDust2 : KLBasicDust
{
    private const string RequestId = "KL:FireDust2";
    // 同 FireDust：序列帧本体乘 HDR 倍率，柔光交给与弹幕共用的 Projectiles.After Bloom（原先另叠的 Spark 光斑由 Bloom 代替）。
    // FireDust2.png 是直通 alpha，AlphaBlend 下改用预乘版（伊蕾娜 Tools/FireBurstPreview/premultiply_textures.py 生成）
    private static float Hdr => 2.8f;
    private static Texture2D premultiplied;

    public override void OnSpawn(Dust dust)
    {
        premultiplied ??= Mod.Assets.Request<Texture2D>("Dusts/Fire/FireDust2_Premultiplied", AssetRequestMode.ImmediateLoad).Value;
        base.OnSpawn(dust);
    }

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
        if (premultiplied == null) return;
        int Frame = (int)MathHelper.Lerp(0, 8, dust.LifeProgress());

        SpriteEffects se = SpriteEffects.None;
        if (dust.DustInfo().DustData != null)
        {
            se = (dust.DustInfo().DustData as int?) > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        }

        ReColorEffect(new Vector4(Hdr, Hdr, Hdr, 1f));
        DrawInWorld(new TextureInfo(premultiplied,Frame,4,2), dust.position, dust.GetRealColor(dust.color),
            dust.Size(), dust.rotation+3.14f/2,spriteEffects:se);
    }

    public override bool Update(Dust dust)
    {
        dust.DustInfo().DustData ??= Main.rand.Next(0, 2);

        dust.velocity *= 0.9f;
        Vector3 color = new Color(255, 150, 50, 255).ToVector3() * dust.color.ToVector3();

        Lighting.AddLight(dust.position, color*(1-dust.LifeProgress()));

        return base.Update(dust);
    }

}
