namespace KL.Dusts.Water;

public class BubbleDust2 : KLBasicDust
{
    private static Texture2D noise;
    public override void OnSpawn(Dust dust)
    {
        noise ??= Mod.Assets.Request<Texture2D>("Effects/Tex/Perlin", AssetRequestMode.ImmediateLoad).Value;
        //ShouldBloom = true;
        base.OnSpawn(dust);
    }

    public override bool PreDraw(Dust dust)
    {

        float threshold = 0;

        if (dust.CurrentLifeTime() < 60)
        {
            threshold = MathHelper.Lerp( 1f, 0,dust.CurrentLifeTime()/60f);
        }
        float length = MathHelper.SmoothStep(1, 0, dust.LifeProgress());
        EndBeginDraw(1, 1);

        ClipEffect(threshold, mask: noise, maskTime: new Vector2(0),
            maskScale: new Vector2(2.5f)); //,(Main.GameUpdateCount%60)/60f

        DrawInWorld(MainTexture, dust.position, dust.GetRealColor(dust.color),
            dust.Size().X * new Vector2(1), dust.rotation + 3.14f / 2f);

        EndBeginDraw();

        return base.PreDraw(dust);
    }

    public override void DrawBloom(Dust dust)
    {
        base.DrawBloom(dust);
    }

    public override bool Update(Dust dust)
    {
        dust.velocity *= 0.98f;
        //Lighting.AddLight(dust.position, dust.color.ToVector3());

        return base.Update(dust);
    }
    
}