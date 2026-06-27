namespace KL.Dusts;

public class testDust : KLBasicDust
{
    public override string Texture { get; }
    
    public override void OnSpawn(Dust dust)
    {
        MainTexture = Mod.Assets.Request<Texture2D>("Dusts/shock", AssetRequestMode.ImmediateLoad).Value;
        //textureTowards = TextureTowards.Random;

        base.OnSpawn(dust);
    }

    public override bool PreDraw(Dust dust)
    {
        int Frame = (int)MathHelper.Lerp(0, 4, dust.LifeProgress());
        EndBeginDraw(2);
        for (int i = 0; i < 1; i++)
        {

        }
        
        EndBeginDraw(3);
        for (int i = 0; i < 3; i++)
        {
            Main.spriteBatch.Draw(MainTexture, dust.position - Main.screenPosition,MainTexture.GetRec(Frame,4,1), dust.color,
                dust.rotation+3.14f/2, MainTexture.Origin(4,1),dust.Size(), SpriteEffects.None, 0);
        }

        
        EndBeginDraw();
        return false;
    }

    public override bool Update(Dust dust)
    {
        base.Update(dust);
        
        return false;
    }
}