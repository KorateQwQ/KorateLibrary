namespace KL.Dusts.Stone;

public class GlowStone: KLBasicDust
{
    public override void OnSpawn(Dust dust)
    {
        base.OnSpawn(dust);
    }

    public override bool PreDraw(Dust dust)
    {
        if (dust.DustInfo().randFrame >= 0)
        {
            float alpha = MathHelper.Lerp(2, 0, dust.LifeProgress());
            float scale = MathHelper.Lerp(1, 1, dust.LifeProgress());

            DrawInWorld(new TextureInfo(MainTexture, dust.DustInfo().randFrame, 3, 3), dust.position, dust.color*alpha,
                dust.Size()*scale, dust.rotation + MathHelper.PiOver2);
        }

        return false;
    }

    public override bool Update(Dust dust)
    {
        if (dust.DustInfo() != null && dust.DustInfo().randFrame < 0)
        {
            dust.DustInfo().randFrame = Main.rand.Next(0, 9);
        }
        dust.velocity.Y += 0.2f;

        dust.velocity *= 0.92f;
        return base.Update(dust);
    }
}