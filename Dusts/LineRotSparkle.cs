namespace KL.Dusts;

/// <summary>
/// 和LineSparkle基本一致，但是粒子会朝一个随机的方向旋转
/// </summary>
public class LineRotSparkle : KLBasicDust
{
    private static Texture2D MainTex;
    private static Texture2D light;
    public override void OnSpawn(Dust dust)
    {
        MainTex ??= Mod.Assets.Request<Texture2D>("Effects/Tex/Sparkle/ShotLine", AssetRequestMode.ImmediateLoad).Value;
        light ??= Mod.Assets.Request<Texture2D>("Dusts/Spark", AssetRequestMode.ImmediateLoad).Value;
        
        base.OnSpawn(dust);
    }

    public override bool PreDraw(Dust dust)
    {
                
        float length = MathHelper.Lerp(1, 0, dust.LifeProgress());
        DrawInWorld(MainTex,dust.position,dust.GetRealColor(dust.color), dust.Size()*new Vector2(0.2f* length,0.1f* length),dust.rotation);
        
        //float length = MathHelper.Lerp(1, 0, dust.LifeProgress());
        //EndBeginDraw(1,1);
        //ReColorEffect(new Vector4(1)*2f);
        
        DrawInWorld(light,dust.position,dust.GetRealColor(dust.color)* length, dust.Size()*new Vector2(0.02f,0.02f) * length+ new Vector2(0.1f),dust.rotation+3.14f/2f);
        
        //EndBeginDraw(1,1);
        //ReColorEffect(new Vector4(1)*1.8f);
        
        DrawInWorld(MainTex,dust.position,dust.GetRealColor(dust.color), dust.Size()*new Vector2(0.1f* length,0.03f),dust.rotation);
        //EndBeginDraw();

        
        //EndBeginDraw();
        
        return false;
    }

    public override void DrawBloom(Dust dust)
    {
        base.DrawBloom(dust);
    }

    public override bool Update(Dust dust)
    {
        if(dust.DustInfo()!=null&& dust.DustInfo().DustData==null) dust.DustInfo().DustData = Main.rand.NextFloat(-0.08f, 0.08f);

        if (dust.DustInfo().DustData is float rot)
        {
            dust.velocity = dust.velocity.RotatedBy(rot);
        }
        dust.velocity *= 0.9f;
        Lighting.AddLight(dust.position,dust.color.ToVector3());

        return base.Update(dust);
    }
}