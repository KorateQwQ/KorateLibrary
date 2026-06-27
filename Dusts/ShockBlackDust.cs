namespace KL.Dusts;

public class ShockBlackDust : KLBasicDust
{
    public override void OnSpawn(Dust dust)
    {
        dust.velocity *= 10.81f;
        base.OnSpawn(dust);
    }

    public override bool PreDraw(Dust dust)
    {
        //dust.color = new Color(255, 100, 200);
        float scale = MathHelper.Lerp(1, 2f, dust.LifeProgress());
        float alpha = MathHelper.Lerp(1, 0, dust.LifeProgress());
        int frame = (int)MathHelper.Lerp(0, 4, dust.LifeProgress());
        EndBeginDraw(2,1);

        DrawInWorld(new TextureInfo(Texture2D.Value,frame,4,1),dust.position,dust.color * alpha,dust.Size()* new Vector2(1f,1f)*scale,dust.rotation+3.14f/2f);
        EndBeginDraw();

        return base.PreDraw(dust);
    }

    public override void DrawBloom(Dust dust)
    {
        base.DrawBloom(dust);
    }

    public override bool Update(Dust dust)
    {
        dust.velocity *= 0.92f;
        Lighting.AddLight(dust.position,dust.color.ToVector3());
        return base.Update(dust);   
    }
}