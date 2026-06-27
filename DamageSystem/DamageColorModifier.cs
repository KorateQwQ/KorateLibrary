using KL.Utils;
using Terraria.DataStructures;

namespace KL.DamageSystem;

public class DamageColorModifier : ModSystem
{
    public override void Load()
    {
        On_NPC.StrikeNPC_HitInfo_bool_bool += On_NPCOnStrikeNPC_HitInfo_bool_bool;
        base.Load();
    }

    private int On_NPCOnStrikeNPC_HitInfo_bool_bool(On_NPC.orig_StrikeNPC_HitInfo_bool_bool orig, NPC self, NPC.HitInfo hit, bool fromNet, bool noPlayerInteraction)
    {
        // 在所有端：只在 Strike 内解析 Knockback 的额外小数位，解出元素 tag 并恢复真实击退
        if (ElementTagCodec.TryDecodeFromKnockback(ref hit.Knockback, out byte elementId)
            && ElementTables.IdToColor.TryGetValue(elementId, out var elementColor))
        {
            hit.HideCombatText = true;

            string text = $"{hit.Damage}";
            if (hit.Crit) text += "!";

            CombatText.NewText(new Rectangle((int)self.position.X, (int)self.position.Y, self.width, self.height), elementColor, text, hit.Crit);

            if (self.GetGlobalNPC<ElementalGlobalNPC>() is { } globalNPC)
            {
                //PrintText($"globalNPC: {globalNPC.GetCurrentAccumulation((ElementType)elementId)} damage: {hit.Damage} knockback: {hit.Knockback}");
                globalNPC.OnHitByElement((ElementType)elementId,self, hit);
            }
            return orig(self, hit, fromNet, noPlayerInteraction);
        }

        // 兼容：仍支持按 DamageClass 染色
        if (DamageManager.GetDamageColor(hit.DamageType) is { } color)
        {
            double num = hit.Damage;
            bool crit = hit.Crit;
            hit.HideCombatText = true;

            string text = $"{(int)num}";
            if (crit) text += "!";

            CombatText.NewText(new Rectangle((int)self.position.X, (int)self.position.Y, self.width, self.height), color, text, crit);
        }

        return orig(self, hit, fromNet, noPlayerInteraction);
    }
}