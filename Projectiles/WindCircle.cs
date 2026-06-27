using System.IO;

namespace KL.Projectiles;

public class WindCircle : KLProjectile
{
    public Vector2[] 坐标组 = new Vector2[220];
    public float MaxTime = 60;
    public float start = 0;
    public float defWidth = 150;
    public float defHeight = 50;
    public int DrawTimes = 1;
    public int FrontBlendState = 1;
    public int BackBlendState = 1;

    public Texture2D tex;
    
    public Color FrontColor = Color.White;
    
    public Color BackColor = Color.Black;

    private float alpha = 1;

    private int Layer = 0;

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.WriteRGB(FrontColor);
        writer.WriteRGB(BackColor);
        writer.Write(defWidth);
        writer.Write(defHeight);
        base.SendExtraAI(writer);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        FrontColor = reader.ReadRGB();
        BackColor = reader.ReadRGB();
        defWidth = reader.ReadSingle();
        defHeight = reader.ReadSingle();
        base.ReceiveExtraAI(reader);
    }

    public override void SetDefaults()
    {
        tex ??= Mod.Assets.Request<Texture2D>("Effects/Tex/Wind/windNoi2", AssetRequestMode.ImmediateLoad).Value;
        坐标组 = new Vector2[220];

        MaxTime = Main.rand.Next(20, 40);
        Projectile.timeLeft = (int)MaxTime;
        Projectile.rotation = Main.rand.NextFloat(-3.14f / 2f, 3.14f / 2f);
        start = Main.rand.NextFloat(-3.14f, 3.14f);
        defWidth = Main.rand.NextFloat(100, 150);
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.width = 1;
        Projectile.height = 1;
        
        base.SetDefaults();
    }
    public override void AI()
    {
        if (Projectile.velocity != Vector2.Zero) Projectile.rotation = Projectile.velocity.ToRotation();//+ 3.14f / 2 

        float startRot = start + MathHelper.Lerp(4, 0, Projectile.timeLeft / MaxTime);
        float eachRot = 0.02f;
        float width = MathHelper.Lerp(defWidth * 2, defWidth, Projectile.timeLeft / MaxTime);
        float height = MathHelper.Lerp(defHeight * 2, defHeight, Projectile.timeLeft / MaxTime);

        for (int i = 0; i < 坐标组.Length; i++)
        {
            坐标组[i] = Projectile.Center + new Vector2((float)Math.Cos(startRot + eachRot * i) * width, (float)Math.Sin(startRot + eachRot * i) * height);

        }

        alpha = MathHelper.Lerp(0, 1, Projectile.timeLeft / MaxTime);
        base.AI();
    }

    public override bool PreDraw(ref Color lightColor)
    {
        return false;
    }

    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers,
        List<int> overWiresUI)
    {
        behindNPCsAndTiles.Add(index);
        base.DrawBehind(index, behindNPCsAndTiles, behindNPCs, behindProjectiles, overPlayers, overWiresUI);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        return base.Colliding(projHitbox, targetHitbox);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        base.OnHitNPC(target, hit, damageDone);
    }
}