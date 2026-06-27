using KL.DamageSystem;
using KL.Dusts;
using KL.Dusts.Fire;
using Terraria.Utilities;

namespace KL.Buffs.ElementalDebuff;

public class ElementalAffliction_Fire : ElementaiAffictionDebuff
{
    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
    }
    

    public override void Update(NPC npc, ref int buffIndex)
    {
        
        int interval = 10;
        if(npc.GetRealNpc() is {} realNpc && realNpc!=npc) interval = 20;
        
        if (GetVisualCounter(npc) % interval == 0)
        {
            KLBasicDust.SpawnDust(npc.Center+new Vector2(Main.rand.NextFloat(-npc.width/2f,npc.width/2f),Main.rand.NextFloat(-npc.height/2f,npc.height/2f)),
                ModContent.DustType<FireDust>(),new Vector2(Main.rand.NextFloat(-0.5f,0.5f),-1),30,new Color(255,255,255,0),new Vector2(Main.rand.NextFloat(0.2f,0.4f)));
        }

        int duration = npc.buffTime[buffIndex];

        if (duration%30==0&&ServerOrLocalMode())
        {
            npc.GetGlobalNPC<ElementalGlobalNPC>().ApplyAfflictionDamage(npc, npc.lifeMax / 20, ElementType.Fire);
        }
        base.Update(npc, ref buffIndex);
    }

}