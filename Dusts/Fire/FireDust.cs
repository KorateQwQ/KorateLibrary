namespace KL.Dusts.Fire;

public class FireDust : KLBasicDust
{
    private static Texture2D light;
    public override void OnSpawn(Dust dust)
    {
        //ShouldBloom = true;
        light ??= Mod.Assets.Request<Texture2D>("Dusts/Spark_Premultiplied", AssetRequestMode.ImmediateLoad).Value;
        base.OnSpawn(dust);
    }

    
    public override bool PreDraw(Dust dust)
    {
        int Frame = 0;
        Frame = (int)MathHelper.Lerp(0, 16, dust.LifeProgress());
        float lightAlpha = MathHelper.Lerp( 0,1, (dust.CurrentLifeTime()-5)/dust.LifeTime());
        lightAlpha = Math.Max(lightAlpha, 0);
        
        
        //dust.rotation = -3.14f/2;

        DrawInWorld(new TextureInfo(MainTexture,Frame,4,4), dust.position, dust.color,
            dust.Size(), dust.rotation+3.14f/2);
        Vector4 color = new Color(255, 150, 50, 0).ToVector4() * dust.color.ToVector4();
        
        DrawInWorld(light, dust.position, new Color(255, 150, 50, 255)*lightAlpha,
            dust.Size()*1f, dust.rotation);  
        
        return base.PreDraw(dust);
    }

    public override bool Update(Dust dust)
    {

        dust.velocity *= 0.9f;
        Vector3 color = new Color(255, 150, 50, 255).ToVector3() * dust.color.ToVector3();

        Lighting.AddLight(dust.position, color*(1-dust.LifeProgress()));

        return base.Update(dust);
    }

}