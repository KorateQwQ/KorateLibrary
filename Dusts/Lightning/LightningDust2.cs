namespace KL.Dusts.Lightning;

public class LightningDust2 : KLBasicDust
{
    private static Texture2D light;
    public override void OnSpawn(Dust dust)
    {
        //ShouldBloom = true;
        light ??= Mod.Assets.Request<Texture2D>("Dusts/Spark", AssetRequestMode.ImmediateLoad).Value;
        base.OnSpawn(dust);
    }

    
    public override bool PreDraw(Dust dust)
    {
        int frame = (int)MathHelper.Lerp(0, 9, dust.LifeProgress());
        float lightAlpha = MathHelper.Lerp( 0,1, (dust.CurrentLifeTime()-5)/dust.LifeTime());
        lightAlpha = Math.Max(lightAlpha, 0);
        
        
        EndBeginDraw(0,1);
        Vector4 color = 5.5f * dust.color.ToVector4()*lightAlpha;
        ReColorEffect(color);

        float scale = 0.2f;
        dust.color.A = 0;
        DrawInWorld(new TextureInfo(MainTexture,frame,3,3), dust.position, dust.color,
            dust.Size()*scale, dust.rotation-3.14f/2);
        /*DrawInWorld(light, dust.position,dust.color*lightAlpha,
            dust.Size()*0.3f, dust.rotation);  */

        EndBeginDraw();

        return base.PreDraw(dust);
    }

    public override bool Update(Dust dust)
    {

        dust.velocity *= 0.9f;
        Vector3 color = dust.color.ToVector3();

        Lighting.AddLight(dust.position, color*(1-dust.LifeProgress()));

        return base.Update(dust);
    }
}