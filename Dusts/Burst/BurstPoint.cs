namespace KL.Dusts.Burst;

public class BurstPoint : KLBasicDust
{
    public override bool PreDraw(Dust dust)
    {
        float alpha = MathHelper.Lerp(1, 0, dust.LifeProgress());
        float scale = MathHelper.Lerp(1, 0, dust.LifeProgress());

        int frame = Math.Clamp((int)(dust.LifeProgress() * 4f), 0, 3);
        DrawInWorld(new TextureInfo(MainTexture,frame, 2, 2), dust.position, dust.color,
            dust.Size(), dust.rotation + MathHelper.PiOver2);
        return false;
    }

    public override void OnSpawn(Dust dust)
    {
        base.OnSpawn(dust);
    }
    public override bool Update(Dust dust)
    {
        //dust.velocity *= 0.92f;
        return base.Update(dust);
    }
}