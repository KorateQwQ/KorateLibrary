namespace KL.Dusts.Water;

public class WaterDust3 : KLBasicDust
{
    public override void OnSpawn(Dust dust)
    {
        //ShouldBloom = true;
        base.OnSpawn(dust);
    }

    public override bool PreDraw(Dust dust)
    {
        int Frame = 0;
        float threshold = MathHelper.Lerp(0, 1, dust.LifeProgress());
        float length = MathHelper.Lerp(1, 1, dust.LifeProgress());
        
        Frame = (int)MathHelper.Lerp(0, 8, dust.LifeProgress());

        EndBeginDraw(1);
        
        DrawInWorld(new TextureInfo(MainTexture,Frame,4,2), dust.position, dust.color,
            dust.Size() * new Vector2(0.5f), dust.rotation + 3.14f / 2f);  
        EndBeginDraw();

        return base.PreDraw(dust);
    }

    public override void DrawBloom(Dust dust)
    {
        base.DrawBloom(dust);
    }

    public override bool Update(Dust dust)
    {
        dust.velocity *= 0.9f;
        Lighting.AddLight(dust.position, dust.color.ToVector3());

        return base.Update(dust);
    }
    
}