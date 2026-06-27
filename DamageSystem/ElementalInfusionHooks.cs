using KL.Buffs.ElementalDebuff;

namespace KL.DamageSystem;

/// <summary>
/// 玩家元素附着会影响没有附着的弹幕以及武器。
/// </summary>
public class ElementalInfusionPlayer : ModPlayer
{
    public ElementType InfusionElement;

    public override void ResetEffects()
    {
        InfusionElement = ElementType.None;
        //InfusionElement = ElementType.Fire;
        base.ResetEffects();
    }

    public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers)
    {
        // 规则：只有当攻击本体没有元素时，才补全为玩家附着元素
        if (item.GetGlobalItem<ElementalGlobalItem>().InfusionElement == ElementType.None)
        {
            ElementHitInfoHelper.TryAttachElementTag(ref modifiers, InfusionElement);
        }
        
        base.ModifyHitNPCWithItem(item, target, ref modifiers);
    }

    public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers)
    {
        //modifiers.FlatBonusDamage += 500;
        // 规则：只有当攻击本体没有元素时，才补全为玩家附着元素
        if (proj.GetGlobalProjectile<ElementalGlobalProjectile>().InfusionElement == ElementType.None)
        {
            ElementHitInfoHelper.TryAttachElementTag(ref modifiers, InfusionElement);
        }
        
        
        /*//测试效果。
        if (target.HasBuff<ElementalAffliction_Electrocuted>())
        {
            modifiers.FinalDamage *= 2;
        }
        
        if (proj.DamageType == DamageClass.Melee)
        {
            //modifiers.FlatBonusDamage += target.lifeMax / 10f;

            ElementHitInfoHelper.TryAttachElementTag(ref modifiers, ElementType.Fire);
        }
        if (proj.DamageType == DamageClass.Magic)
        {
            ElementHitInfoHelper.TryAttachElementTag(ref modifiers, ElementType.Ice);
        }

        if (proj.DamageType == DamageClass.Ranged)
        {
            ElementHitInfoHelper.TryAttachElementTag(ref modifiers, ElementType.Water);
        }*/

        base.ModifyHitNPCWithProj(proj, target, ref modifiers);
    }
}

public class ElementalGlobalProjectile : GlobalProjectile
{
    public ElementType InfusionElement;

    public override bool InstancePerEntity => true;

    public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
    {
        // 规则：攻击本体自带元素时，优先使用本体元素
        ElementHitInfoHelper.TryAttachElementTag(ref modifiers, InfusionElement);
        base.ModifyHitNPC(projectile, target, ref modifiers);
    }
}

public class ElementalGlobalItem : GlobalItem
{
    public ElementType InfusionElement;

    public override bool InstancePerEntity => true;

    public override void ModifyHitNPC(Item item, Player player, NPC target, ref NPC.HitModifiers modifiers)
    {
        // 规则：攻击本体自带元素时，优先使用本体元素
        ElementHitInfoHelper.TryAttachElementTag(ref modifiers, InfusionElement);
        base.ModifyHitNPC(item, player, target, ref modifiers);
    }
}
