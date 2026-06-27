using KL.DamageSystem;
using KL.Dusts;
using KL.Dusts.Lightning;
using Terraria.Utilities;

namespace KL.Buffs.ElementalDebuff;

public class ElementalAffliction_Electrocuted : ElementaiAffictionDebuff
{
    public override void SetStaticDefaults()
    {
        base.SetStaticDefaults();
    }

    public override void Update(NPC npc, ref int buffIndex)
    {
        int interval = 10;
        if(npc.GetRealNpc() is {} realNpc && realNpc!=npc) interval = 30;
        
        
        if (GetVisualCounter(npc) % interval == 0)
        {
            KLBasicDust.SpawnDust(npc.Center+new Vector2(Main.rand.NextFloat(-npc.width/2f,npc.width/2f),Main.rand.NextFloat(-npc.height/2f,npc.height/2f)),
                ModContent.DustType<LightningDust>(),new Vector2(Main.rand.NextFloat(-0.5f,0.5f),-Main.rand.NextFloat(-0.5f,0.5f)),30,new Color(150,220,255,255)*0.7f,new Vector2(0.8f),attachedEntity:npc);
        }
        
        base.Update(npc, ref buffIndex);
    }

}