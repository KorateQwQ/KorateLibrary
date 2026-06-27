namespace KL.Projectiles;

public class KLGlobalProjectile : GlobalProjectile
{
    public override bool InstancePerEntity=>true;

    public int NumOfOwingProjectiles = 0;

    //弹幕是否应该被计数，伊蕾娜的水球在飞出后并不属于手持弹幕，因此不应该被计数
    public bool ShouldBeCount = true;

    public override bool PreAI(Projectile projectile)
    {
        NumOfOwingProjectiles = 0;
        var owner = Main.player[projectile.owner];
        if (owner is { active: true }&&ShouldBeCount)
        {
            if (owner.GetModPlayer<KlGlobalProjectile_Owner>() is { } KLOwner)
            {
                if (KLOwner.OwningProjectiles.ContainsKey(projectile.type))
                {
                    NumOfOwingProjectiles += KLOwner.OwningProjectiles[projectile.type];
                    KLOwner.OwningProjectiles[projectile.type]+=1;
                }
                else
                {
                    bool result = KLOwner.OwningProjectiles.TryAdd(projectile.type, 1);
                    //KLOwner.OwningProjectiles.Add(projectile.type,1);
                    //Main.NewText("Already Have: " + KLOwner.OwningProjectiles[projectile.type]);
                }
                
            }
        }
        return base.PreAI(projectile);
    }

    class KlGlobalProjectile_Owner : ModPlayer
    {
        public Dictionary<int, int> OwningProjectiles = new (1000);

        public override void ResetEffects()
        {
            OwningProjectiles.Clear();
            base.ResetEffects();
        }
    }
}