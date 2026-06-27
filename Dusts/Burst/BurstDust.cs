namespace KL.Dusts.Burst;

public class BurstDust : KLBasicDust
{
    public override bool PreDraw(Dust dust)
    {
        float alpha = MathHelper.Lerp(1, 0, dust.LifeProgress());
        float scale = MathHelper.Lerp(1, 0, dust.LifeProgress());

        int frame = Math.Clamp((int)(dust.LifeProgress() * 8f), 0, 7);
        DrawInWorld(new TextureInfo(MainTexture,frame, 2, 4), dust.position, dust.color,
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