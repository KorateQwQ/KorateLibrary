using KL.Utils;
using KL.Utils.Net;

namespace KL.DamageSystem;

/// <summary>冻结积蓄携带每次攻击自己的点数，由权威端累加；冰元素飘字与积蓄分开结算。</summary>
public sealed class FreezeBuildupPlayer : KLModPlayer
{
    public void Request(NPC target, float amount, int damageDone)
    {
        if (Player.whoAmI != Main.myPlayer || damageDone <= 0 || !float.IsFinite(amount) || amount <= 0) return;
        if (Main.netMode == NetmodeID.MultiplayerClient)
            RPC(nameof(Apply), [target.whoAmI, target.type, amount], KLNetModule.NetSendType.ClientToServer);
        else
            Apply(target.whoAmI, target.type, amount);
    }

    public void Apply(int targetIndex, int expectedType, float amount)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !Player.active || Player.dead
            || !float.IsFinite(amount) || amount <= 0 || amount > ElementBuildUpSystem.FreezeCapacity
            || targetIndex < 0 || targetIndex >= Main.maxNPCs) return;
        NPC target = Main.npc[targetIndex];
        if (!target.active || target.life <= 0 || target.type != expectedType || target.friendly) return;
        target.GetGlobalNPC<ElementalGlobalNPC>().OnHitByElement(ElementType.Ice, target,
            new NPC.HitInfo(), Player.whoAmI, amount);
        target.GetElementAnchorNPC().netUpdate = true;
    }
}
