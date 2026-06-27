namespace KL.Dusts.Fire;

public class FireDust2 : KLBasicDust
{
    private static Texture2D light;

    public override void OnSpawn(Dust dust)
    {
        light ??= Mod.Assets.Request<Texture2D>("Dusts/Spark", AssetRequestMode.ImmediateLoad).Value;
        //ShouldBloom = true;
        base.OnSpawn(dust);
    }

    public override bool PreDraw(Dust dust)
    {

        int Frame = 0;
        Frame = (int)MathHelper.Lerp(0, 8, dust.LifeProgress());
        
        float lightAlpha = MathHelper.Lerp( 0,1, (dust.CurrentLifeTime()-5)/dust.LifeTime());
        lightAlpha = Math.Max(lightAlpha, 0);
        
        EndBeginDraw(1);
        
        SpriteEffects se = SpriteEffects.None;
        if (dust.DustInfo().DustData != null)
        {
            se = (dust.DustInfo().DustData as int?) > 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        }
        
        DrawInWorld(new TextureInfo(MainTexture,Frame,4,2), dust.position, dust.color,
            dust.Size(), dust.rotation+3.14f/2,spriteEffects:se);  
        
        Vector4 color = new Color(255, 150, 50, 255).ToVector4() * dust.color.ToVector4();
        
        DrawInWorld(light, dust.position,new Color(color)*lightAlpha,
            dust.Size()*0.5f, dust.rotation);  
        EndBeginDraw();

        return base.PreDraw(dust);
    }

    public override void DrawBloom(Dust dust)
    {
        base.DrawBloom(dust);
    }

    public override bool Update(Dust dust)
    {
        dust.DustInfo().DustData ??= Main.rand.Next(0, 2);
        
        dust.velocity *= 0.9f;
        Vector3 color = new Color(255, 150, 50, 255).ToVector3() * dust.color.ToVector3();

        Lighting.AddLight(dust.position, color*(1-dust.LifeProgress()));

        return base.Update(dust);
    }

}