using KL.DamageSystem;
using KL.Utils;
using Terraria.ID;

namespace KL.Extensions;

public static class NPCExtentions
{
    /// <summary>
    /// 是否为一个可攻击对象,石巨人的头等无敌单位会被排除。
    /// </summary>
    /// <param name="npc"></param>
    /// <param name="dontHurtCritters"> 是否攻击小动物，可以直接传player.dontHurtCritters</param>
    /// <param name="includeImmortal">默认包括傀儡等假人，追踪或者吸血等效果应该排除假人</param>
    /// <returns></returns>
    public static bool IsAttackable(this NPC npc,bool dontHurtCritters = false, bool includeImmortal = true)
    {
        if (npc == null || !npc.active) return false;
        if (dontHurtCritters && npc.CountsAsACritter) return false;
        if (!includeImmortal && npc.immortal) return false;
        if (npc.dontTakeDamage) return false;
        return true;
    }

    public static NPC GetRealNpc(this NPC npc)
    {
        if (npc == null) return null;

        if (npc.realLife >= Main.npc.Length || npc.realLife < 0) return npc;

        return Main.npc[npc.realLife];
    }

    /// <summary>
    /// 对于特殊多体节npc，返回逻辑npc，控制效果会同时作用于逻辑npc
    /// </summary>
    /// <param name="npc"></param>
    /// <returns></returns>
    public static NPC GetLogicNpc(this NPC npc)
    {
        if (npc == null) return null;

        // 月亮领主：手/头等部件通过 ai[3] 指向核心
        if (npc.type == NPCID.MoonLordHand || npc.type == NPCID.MoonLordHead)
        {
            int coreIndex = (int)npc.ai[3];
            if (coreIndex >= 0 && coreIndex < Main.npc.Length)
            {
                NPC core = Main.npc[coreIndex];
                if (core.active && core.type == NPCID.MoonLordCore)
                    return core;
            }
        }

        // 石巨人头：通过 golemBoss 指向身体
        if (npc.type == NPCID.GolemHead)
        {
            int bodyIndex = NPC.golemBoss;
            if (bodyIndex >= 0 && bodyIndex < Main.npc.Length)
            {
                NPC body = Main.npc[bodyIndex];
                if (body.active && body.type == NPCID.Golem)
                    return body;
            }
        }

        // 火星飞碟炮/炮塔：先通过 ai[0] 找到核心
        if (npc.type == NPCID.MartianSaucerTurret || npc.type == NPCID.MartianSaucerCannon)
        {
            int coreIndex = (int)npc.ai[0];
            if (coreIndex >= 0 && coreIndex < Main.npc.Length)
            {
                NPC core = Main.npc[coreIndex];
                if (core.active && core.type == NPCID.MartianSaucerCore) 
                    return core;
            }
        }

        // 荷兰大炮：通过 ai[0] 指向荷兰飞盗船
        if (npc.type == NPCID.PirateShipCannon)
        {
            int shipIndex = (int)npc.ai[0];
            if (shipIndex >= 0 && shipIndex < Main.npc.Length)
            {
                NPC ship = Main.npc[shipIndex];
                if (ship.active && ship.type == NPCID.PirateShip)
                    return ship;
            }
        }

        return npc;
    }
    
    /// <summary>
    /// 仅服务器使用！这个只能用于boss！对于骷髅王的手这种东西无效，但是对于月总的头有效，因为血条贴图是根据头显示的,建议只用来做击杀检测。
    /// </summary>
    /// <param name="npc"></param>
    /// <returns></returns>
    public static int GetRealMaxHP(this NPC npc)
    {
        int realMaxHP = NpcHPHelper.GetRealMaxHPInternal(npc);
        var g = npc.GetGlobalNPC<MainParentGlobalNpc>();
        if (g.ExpectedParent is { active: true })
        {
            int parentMaxHP = NpcHPHelper.GetRealMaxHPInternal(g.ExpectedParent);
            return Math.Max(realMaxHP, parentMaxHP);
        }

        if (g.childid is { Count: > 0 })
        {
            int maxHP = realMaxHP;
            foreach (var child in g.childid)
            {
                if (child.Value < 0 || child.Value >= Main.npc.Length ||
                    Main.npc[child.Value].type != child.Key) continue;
                
                int childMaxHP = NpcHPHelper.GetRealMaxHPInternal(Main.npc[child.Value]);
                //PrintText($"GetRealMaxHP From childMaxHP {child}: {childMaxHP}");
                maxHP = Math.Max(maxHP, childMaxHP);
            }
            return maxHP;
        }
        return realMaxHP;
    }

    /// <summary>
    /// 修复服务端直接调用 <see cref="NPC.AddBuff(int, int, bool)"/> 时的同步顺序问题。
    /// 服务端下会先实际添加 buff，再发送 54 包同步完整 buff 列表；其他情况下保持原版行为。
    /// </summary>
    /// <param name="npc"></param>
    /// <param name="buffId"></param>
    /// <param name="duration"></param>
    /// <param name="quiet"></param>
    public static void AddBuffFix(this NPC npc, int buffId, int duration, bool quiet = false)
    {
        if (npc == null)
            return;

        if (Main.netMode == NetmodeID.Server && !quiet && npc.whoAmI >= 0)
        {
            npc.AddBuff(buffId, duration, quiet: true);
            NetMessage.SendData(54, -1, -1, null, npc.whoAmI);
            return;
        }

        npc.AddBuff(buffId, duration, quiet);
    }

    /// <summary>
    /// 对于冰冻这种需要强制绑定所有关联实体的buff使用，这个方法会自动添加到所有关联实体上。
    /// </summary>
    /// <param name="npc"></param>
    /// <param name="buffId"></param>
    /// <param name="duration"></param>
    /// <param name="quiet"></param>
    /// <param name="forcedAddBuff"></param>
    /// <returns></returns>
    public static void AddBuffToSelfAndChildren(this NPC npc, int buffId, int duration, bool quiet = false,bool forcedAddBuff = false)
    {
        //PrintText($"Try to AddBuffToSelfAndChildren {npc}: buffId: {buffId}, duration: {duration}, quiet: {quiet}, forcedAddBuff: {forcedAddBuff}" );
        if(npc.whoAmI<0)
        {
            if(forcedAddBuff)npc.buffImmune[buffId] = false;
            npc.AddBuffFix(buffId, duration, quiet);
            return;
        }

        if (npc.GetRealNpc() is { active: true } realNpc && realNpc != npc)
        {
            if(forcedAddBuff)realNpc.buffImmune[buffId] = false;
            realNpc.AddBuffFix(buffId, duration, quiet);
            foreach (var child in Main.ActiveNPCs)
            {
                if (child.realLife == realNpc.whoAmI)
                {
                    if(forcedAddBuff)child.buffImmune[buffId] = false;
                    child.AddBuffFix(buffId, duration, quiet);
                }
            }
            return;
        }
        if(forcedAddBuff)npc.buffImmune[buffId] = false;
        npc.AddBuffFix(buffId, duration, quiet);
        foreach (var child in Main.ActiveNPCs)
        {
            if (child.realLife == npc.whoAmI)
            {
                if(forcedAddBuff)child.buffImmune[buffId] = false;
                child.AddBuffFix(buffId, duration, quiet);
            }
        }
    }
}