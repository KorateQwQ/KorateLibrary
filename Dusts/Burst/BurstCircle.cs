using KL.Utils;

namespace KL.Dusts.Burst;

public class BurstCircle : KLBasicDust
{
    public override bool PreDraw(Dust dust)
    {
        float alpha = MathHelper.Lerp(1, 0, dust.LifeProgress());
        float scale = MathHelper.Lerp(1, 1.3f, dust.LifeProgress());

        int frame = Math.Clamp((int)(dust.LifeProgress() * 4f), 0, 3);
        DrawInWorld(new TextureInfo(MainTexture,0, 1, 1), dust.position, dust.color*alpha,
            dust.Size()*scale, dust.rotation);
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