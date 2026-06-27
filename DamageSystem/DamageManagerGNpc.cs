using KL.DamageSystem.ElementalDamageClass;
using KL.Utils;

namespace KL.DamageSystem;

public class DamageManagerGNpc : GlobalNPC
{
    public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
    {
        HandleElementalDamage(hit.DamageType);
        base.OnHitByProjectile(npc, projectile, hit, damageDone);
    }

    public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
    {
        HandleElementalDamage(hit.DamageType);
        base.OnHitByItem(npc, player, item, hit, damageDone);
    }

    static void HandleElementalDamage(DamageClass hitDamageClass)
    {
        if (hitDamageClass is ElementalDamage damageClass)
        {
            PrintText(damageClass.DisplayName);
        }
    }
}
