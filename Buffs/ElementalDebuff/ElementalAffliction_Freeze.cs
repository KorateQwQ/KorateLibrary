using KL.DamageSystem;

namespace KL.Buffs.ElementalDebuff;

public class ElementalAffliction_Freeze : ElementaiAffictionDebuff
{
    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
    }

    public override void Update(NPC npc, ref int buffIndex)
    {
        if (npc.GetLogicNpc() is { } logicNpc && logicNpc != npc)
        {
            logicNpc.GetGlobalNPC<ElementalGlobalNPC>().IsLogicFrozen = true;
        }
        
        base.Update(npc, ref buffIndex);
    }
}