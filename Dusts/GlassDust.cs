namespace KL.Dusts;

public class GlassDust : KLBasicDust
{
    public override void OnSpawn(Dust dust)
    {
        //ShouldBloom = true;
        base.OnSpawn(dust);
    }

    public override bool PreDraw(Dust dust)
    {
        DrawSystem.GlassDrawRequests.Add(() => DrawRequest(dust));
        //DrawRequest(dust);
        return base.PreDraw(dust);
    }

    public void DrawRequest(Dust dust)
    {
        Asset<Texture2D> clipMask = ModContent.Request<Texture2D>("KL/Effects/Tex/Sparkle/T_Cracks",AssetRequestMode.ImmediateLoad);

        if (dust.DustInfo().DustData != null)
        {
            //EndBeginDraw(1,1,adjustToScreen:true);
            int Frame = (int)dust.DustInfo().DustData;
            //Frame = (int)MathHelper.Lerp(0, 6, dust.LifeProgress());
            ClipEffect(1,mask:clipMask.Value);
            DrawInWorld(new TextureInfo(MainTexture,Frame,3,2), dust.position, dust.color,
                dust.Size() * new Vector2(0.2f), dust.rotation+3.14f/2);  
        }

    }

    public override void DrawBloom(Dust dust)
    {
        base.DrawBloom(dust);
    }

    public override bool Update(Dust dust)
    {
        if (dust.DustInfo() != null && dust.DustInfo().DustData == null)
        {
            dust.DustInfo().DustData =  Main.rand.Next(0, 5);
        }
        
        dust.velocity *= 0.9f;

        return base.Update(dust);
    }
}