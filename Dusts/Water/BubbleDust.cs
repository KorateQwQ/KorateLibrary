namespace KL.Dusts.Water;

public class BubbleDust : KLBasicDust
{

    public override void OnSpawn(Dust dust)
    {
        //ShouldBloom = true;
        base.OnSpawn(dust);
    }

    public override bool PreDraw(Dust dust)
    {

        int Frame = 0;
        float length = MathHelper.SmoothStep(1, 0, dust.LifeProgress());

        if (dust.DustInfo().DustData is int)
        {
            Frame = (int)dust.DustInfo().DustData;
        }

        //ClipEffect(threshold, mask: noise, maskTime: new Vector2(0),
           // maskScale: new Vector2(0.5f)); //,(Main.GameUpdateCount%60)/60f

        DrawInWorld(new TextureInfo(MainTexture, Frame, 2,2), dust.position, dust.GetRealColor(dust.color)*length,
            dust.Size() * new Vector2(0.3f), dust.rotation + 3.14f / 2f);


        return base.PreDraw(dust);
    }

    public override void DrawBloom(Dust dust)
    {
        base.DrawBloom(dust);
    }

    public override bool Update(Dust dust)
    {
        if (dust.DustInfo().DustData == null)
        {
            dust.DustInfo().DustData = Main.rand.Next(0, 4);
        }
        dust.velocity *= 0.98f;
        //Lighting.AddLight(dust.position, dust.color.ToVector3());

        return base.Update(dust);
    }
    
}