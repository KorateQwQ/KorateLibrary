using KL.DamageSystem;
using KL.Dusts;
using KL.Dusts.Lightning;
using KL.Dusts.Water;

namespace KL.Buffs.ElementalDebuff;

public class ElementalAffliction_Wet : ElementaiAffictionDebuff
{
    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
    }

    public override void Update(NPC npc, ref int buffIndex)
    {
        int interval = 5;
        if(npc.GetRealNpc() is {} realNpc && realNpc!=npc) interval = 20;
        
        if (GetVisualCounter(npc) % interval == 0)
        {
            KLBasicDust.SpawnDust(npc.Center+new Vector2(Main.rand.NextFloat(-npc.width/2f,npc.width/2f),Main.rand.NextFloat(-npc.height/2f,npc.height/2f)),
                ModContent.DustType<BubbleDust>(),new Vector2(Main.rand.NextFloat(-1.5f,1.5f),-Main.rand.NextFloat(-1.5f,-3.5f)),30,new Color(100,200,255,255),new Vector2(1.0f),attachedEntity:npc);
        }
        
        npc.GetGlobalNPC<ElementalGlobalNPC>().SetAffliction(ElementType.Water, true);
        base.Update(npc, ref buffIndex);
    }
    
}