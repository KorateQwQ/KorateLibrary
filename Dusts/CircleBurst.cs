namespace KL.Dusts;

public class CircleBurst : KLBasicDust
{
    public override void OnSpawn(Dust dust)
    {
        base.OnSpawn(dust);
    }

    public override bool PreDraw(Dust dust)
    {
        //dust.color = new Color(255, 100, 200);
        float scale = MathHelper.Lerp(1, 2f, dust.LifeProgress());
        float alpha = MathHelper.Lerp(1, 0, dust.LifeProgress());
        
        EndBeginDraw(1,1);

        DrawInWorld(Texture2D.Value,dust.position,dust.color * alpha,dust.Size() * 0.3f* new Vector2(0.5f,1f)*scale,dust.rotation);
        DrawInWorld(Texture2D.Value,dust.position,dust.color * alpha,dust.Size() * 0.3f* new Vector2(0.5f,1f)*scale,dust.rotation);
        
        EndBeginDraw();
        return base.PreDraw(dust);
    }

    public override void DrawBloom(Dust dust)
    {
        
        base.DrawBloom(dust);
    }

    public override bool Update(Dust dust)
    {
        Lighting.AddLight(dust.position,dust.color.ToVector3());
        return base.Update(dust);
    }
}