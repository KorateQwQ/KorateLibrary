namespace KL.Dusts.Water;

public class WaterDust2 : KLBasicDust
{
    public override void OnSpawn(Dust dust)
    {
        //ShouldBloom = true;
        base.OnSpawn(dust);
    }

    public override bool PreDraw(Dust dust)
    {
        int Frame = 0;
        Frame = (int)MathHelper.Lerp(0, 16, dust.LifeProgress());

        EndBeginDraw(1);
                
        DrawInWorld(new TextureInfo(MainTexture,Frame,4,4), dust.position, dust.color,
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