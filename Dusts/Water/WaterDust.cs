namespace KL.Dusts.Water;

public class WaterDust : KLBasicDust
{
    private Texture2D MainTex;
    private Texture2D noise;

    public override void OnSpawn(Dust dust)
    {
        MainTex ??= Mod.Assets.Request<Texture2D>("Dusts/Water/WaterDust", AssetRequestMode.ImmediateLoad).Value;
        noise ??= Mod.Assets.Request<Texture2D>("Effects/Tex/noiA", AssetRequestMode.ImmediateLoad).Value;
        //ShouldBloom = true;
        base.OnSpawn(dust);
    }

    public override bool PreDraw(Dust dust)
    {
        if (dust.DustInfo().DustData is WaterDustData data)
        {
            int type = data.WaterType;
            int Frame = data.Frame;
            float threshold = MathHelper.Lerp(0, 1, dust.LifeProgress());
            float length = MathHelper.Lerp(1, 1, dust.LifeProgress());
            EndBeginDraw(0, 1);

            //dust.color.A = 0;
            ClipEffect(threshold, mask: noise, maskTime: new Vector2(0),
                maskScale: new Vector2(0.5f)); //,(Main.GameUpdateCount%60)/60f

            DrawInWorld(new TextureInfo(MainTex, Frame, 2), dust.position, dust.GetRealColor(dust.color),
                dust.Size() * new Vector2(0.1f * length, 0.1f * length), dust.rotation + 3.14f / 2f);

            EndBeginDraw();
        }

        return base.PreDraw(dust);
    }

    public override void DrawBloom(Dust dust)
    {
        base.DrawBloom(dust);
    }

    public override bool Update(Dust dust)
    {
        if (dust.DustInfo() != null && dust.DustInfo().DustData == null)
        {
            dust.DustInfo().DustData = new WaterDustData(0, Main.rand.Next(0, 2));
        }

        dust.velocity *= 0.9f;
        Lighting.AddLight(dust.position, dust.color.ToVector3());

        return base.Update(dust);
    }

    //已弃用
    public class WaterDustData
    {
        public WaterDustData(int waterType, int frame)
        {
            WaterType = waterType;
            Frame = frame;
        }

        public int WaterType = 0;
        public int Frame = 0;
    }
}