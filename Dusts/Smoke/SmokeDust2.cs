namespace KL.Dusts.Smoke;

public class SmokeDust2 : KLBasicDust
{
    public override bool PreDraw(Dust dust)
    {
        float alpha = MathHelper.Lerp(1, 0, dust.LifeProgress());
        float scale = MathHelper.Lerp(1, 0, dust.LifeProgress());

        int frame = Math.Clamp((int)(dust.LifeProgress() * 64f), 0, 63);
        DrawInWorld(new TextureInfo(MainTexture,frame, 8, 8), dust.position, dust.color,
            dust.Size(), dust.rotation);
        
        return false;
    }

    public override void OnSpawn(Dust dust)
    {
        base.OnSpawn(dust);
    }
    public override bool Update(Dust dust)
    {
        return base.Update(dust);
    }
}