using KL.DamageSystem;

namespace KL.Buffs.ElementalDebuff;

public abstract class ElementaiAffictionDebuff : ModBuff
{
    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true;
        base.SetStaticDefaults();
    }

    public ElementType GetElementType()
    {
        if(ElementBuildupBuffRegistry.TryGetElementByBuffId(Type, out ElementType elementType))
        {
            return elementType;
        }

        return ElementType.None;
    }

    public int GetVisualCounter(NPC npc)
    {
        ElementalGlobalNPC globalNPC = npc.GetGlobalNPC<ElementalGlobalNPC>();
        globalNPC.ElementAccumulation.TryGetValue(GetElementType(),out ElementalGlobalNPC.BuildUpProgressContext buildUpProgressContext);
        if (buildUpProgressContext != null)
            return buildUpProgressContext.UpdateCounter;
        return 0;
    }
    public override void Update(NPC npc, ref int buffIndex)
    {
        int duration = npc.buffTime[buffIndex];

        if (ElementBuildupBuffRegistry.TryGetElementByBuffId(Type, out ElementType elementType))
        {
            ElementalGlobalNPC globalNPC = npc.GetGlobalNPC<ElementalGlobalNPC>();
            globalNPC.SetDuration(elementType, duration);
            globalNPC.SetAffliction(elementType, true);
        }
        base.Update(npc, ref buffIndex);
    }
}