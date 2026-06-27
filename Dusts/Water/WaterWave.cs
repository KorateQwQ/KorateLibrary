namespace KL.Dusts.Water;

public class WaterWave : KLBasicDust
{
    private static Texture2D noise;
    private static Texture2D tex;

    public override void OnSpawn(Dust dust)
    {
        noise ??= Mod.Assets.Request<Texture2D>("Effects/Tex/cellnoise", AssetRequestMode.ImmediateLoad).Value;
        tex  ??= Mod.Assets.Request<Texture2D>("Effects/Tex/WaterCircle", AssetRequestMode.ImmediateLoad).Value;
        //ShouldBloom = true;
        base.OnSpawn(dust);
    }

    public override bool PreDraw(Dust dust)
    {
        int Frame = 0;
        float threshold = MathHelper.Lerp(0, 1, dust.LifeProgress());
        float length = MathHelper.Lerp(1, 1, dust.LifeProgress());
        float smoothStep = MathHelper.SmoothStep( 1,0, dust.LifeProgress());

        EndBeginDraw(1, 1);
        
        HeatNoisePerturbEffect(0.05f,PerLinNoiseX,new Vector2((float)(Main.timeForVisualEffects%1800)/180f));
        for (int i = 0; i < 1; i++)
        {
            DrawInWorld(new TextureInfo(MainTexture), dust.position, dust.color*smoothStep,
                dust.Size() * new Vector2(0.5f), dust.rotation + 3.14f / 2f);  
        }

        
        EndBeginDraw();
        return base.PreDraw(dust);
    }

    public override void DrawBloom(Dust dust)
    {

        base.DrawBloom(dust);
    }

    public override bool Update(Dust dust)
    {
        dust.velocity *= 0.0f;
        dust.SetSize(dust.Size() + new Vector2(0.05f));
        
        Lighting.AddLight(dust.position, dust.color.ToVector3());

        return base.Update(dust);
    }
    
}