namespace KL.Dusts;

public class LineSparkle : KLBasicDust
{
    private static Texture2D MainTex;
    private static Texture2D light;
    public override void OnSpawn(Dust dust)
    {
        MainTex ??= Mod.Assets.Request<Texture2D>("Dusts/ShotLine", AssetRequestMode.ImmediateLoad).Value;
        light ??= Mod.Assets.Request<Texture2D>("Dusts/Spark", AssetRequestMode.ImmediateLoad).Value;

        //ShouldBloom = true;
        base.OnSpawn(dust);
    }

    public override bool PreDraw(Dust dust)
    {
        float length = MathHelper.Lerp(1, 0, dust.LifeProgress());
        DrawInWorld(MainTex,dust.position,dust.GetRealColor(dust.color), dust.Size()*new Vector2(0.2f* length,0.1f* length),dust.rotation);
        DrawInWorld(MainTex,dust.position,dust.GetRealColor(dust.color), dust.Size()*new Vector2(0.1f* length,0.03f),dust.rotation);

        for (int i = 0; i < 3; i++)
        {
            DrawInWorld(MainTex,dust.position,dust.GetRealColor(dust.color)*0.15f*((10-i)/10f), 3.8f*dust.Size()*new Vector2(0.1f* length,0.6f* length)*(1+i*0.01f)/*+ new Vector2(0.1f)*/,dust.rotation);
        }
        
        return false;
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