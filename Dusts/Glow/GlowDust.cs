namespace KL.Dusts.Glow;

public class GlowDust : KLBasicDust
{
    public override bool PreDraw(Dust dust)
    {
        float alpha = MathHelper.Lerp(1, 0, dust.LifeProgress());
        float scale = MathHelper.Lerp(1, 0, dust.LifeProgress());

        DrawInWorld(new TextureInfo(MainTexture,0, 1, 1), dust.position, dust.color*alpha,
            dust.Size(), dust.rotation);
        
        return false;
    }

    public override void OnSpawn(Dust dust)
    {
        base.OnSpawn(dust);
    }
    public override bool Update(Dust dust)
    {
        dust.velocity *= 0.92f;
        return base.Update(dust);
    }
}