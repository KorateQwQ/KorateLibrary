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
    /// 获取npc的唯一名称,为模组名+npc类名
    /// </summary>
    /// <param name="npc"></param>
    /// <returns></returns>
    public static string GetUniqueName(this NPC npc)
    {
        return NPCID.Search.GetName(npc.type);
    }
    /// <summary>
    /// 对于特殊多体节npc，返回逻辑npc，控制效果会同时作用于逻辑npc
    /// </summary>
    /// <param name="npc"></param>
    /// <returns></returns>
    public static NPC GetLogicNpc(this NPC npc)
    {
        if (npc == null) return null;

        // 骷髅王的手、机械骷髅王的四种武器部位均通过 ai[1] 指向所属头部。
        // 按实例索引关联，不能把同类型的另一只 Boss 归到同一组。
        if (npc.type == NPCID.SkeletronHand || npc.type == NPCID.PrimeCannon
            || npc.type == NPCID.PrimeSaw || npc.type == NPCID.PrimeVice || npc.type == NPCID.PrimeLaser)
        {
            int headIndex = (int)npc.ai[1];
            int headType = npc.type == NPCID.SkeletronHand ? NPCID.SkeletronHead : NPCID.SkeletronPrime;
            if (headIndex >= 0 && headIndex < Main.npc.Length)
            {
                NPC head = Main.npc[headIndex];
                if (head is { active: true } && head.type == headType)
                    return head;
            }
        }

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

        // 与原版石巨人部位 AI 一致，通过 golemBoss 关联身体；包括二阶段游离头。
        if (npc.type == NPCID.GolemHead || npc.type == NPCID.GolemHeadFree
            || npc.type == NPCID.GolemFistLeft || npc.type == NPCID.GolemFistRight)
        {
            int bodyIndex = NPC.golemBoss;
            if (bodyIndex >= 0 && bodyIndex < Main.npc.Length)
            {
                NPC body = Main.npc[bodyIndex];
                if (body is { active: true } && body.type == NPCID.Golem)
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
    /// 获取元素积蓄/冻结状态的统一锚点。
    /// 先读取 realLife 所指的共享血量主体，再解析多部位 Boss 的逻辑主体（骷髅头、月总核心、石巨人身体等），
    /// 因此月总手这类 realLife 为 -1 的部件也能和核心落到同一个锚点上。
    /// 解析失败或结果已失效时回退到传入的 npc 本身。
    /// </summary>
    /// <param name="npc"></param>
    /// <returns></returns>
    public static NPC GetElementAnchorNPC(this NPC npc)
    {
        if (npc == null) return null;

        NPC anchor = npc.GetRealNpc();
        if (anchor is not { active: true }) anchor = npc;

        NPC logicNpc = anchor.GetLogicNpc();
        if (logicNpc is { active: true }) anchor = logicNpc;

        return anchor;
    }

    /// <summary>
    /// 剩余的冻结 CD（tick）。多部位 Boss 上只有锚点持有关键状态，其余部位转读锚点。
    /// </summary>
    public static int GetFreezeCooldown(this NPC npc)
    {
        if (npc == null || !npc.active) return 0;

        return npc.GetGlobalNPC<ElementalGlobalNPC>().GetFreezeCooldown(npc);
    }

    /// <summary>
    /// 剩余冻结 CD 的秒数。
    /// </summary>
    public static float GetFreezeCooldownSeconds(this NPC npc) => npc.GetFreezeCooldown() / 60f;

    /// <summary>
    /// 当前是否可以开始积累冻结：冻结 CD 归零，且不处于冻结状态。
    /// </summary>
    public static bool IsFreezeAccumulating(this NPC npc)
    {
        if (npc == null || !npc.active) return false;

        return npc.GetGlobalNPC<ElementalGlobalNPC>().IsFreezeAccumulating(npc);
    }

    /// <summary>
    /// 直接扣除剩余冻结 CD，最低减到 0（不储存负 CD）。内部会把目标路由到元素锚点。
    /// </summary>
    public static void ReduceFreezeCooldown(this NPC npc, float seconds)
    {
        if (npc == null || !npc.active) return;

        npc.GetGlobalNPC<ElementalGlobalNPC>().ReduceFreezeCooldown(npc, seconds);
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

    /// <summary>
    /// 将 buff 添加到同一个元素锚点下的全部部位上。
    /// 与 <see cref="AddBuffToSelfAndChildren"/> 的区别：后者只沿 realLife 链扩散，
    /// 而月总手这类部件的 realLife 为 -1，无法通过 realLife 找到核心；
    /// 本方法以 <see cref="GetElementAnchorNPC"/> 的结果为准，把所有解析到同一锚点的部位一起施加。
    /// </summary>
    public static void AddBuffToAnchorParts(this NPC npc, int buffId, int duration, bool quiet = false, bool forcedAddBuff = false)
    {
        if (npc == null) return;

        NPC anchor = npc.GetElementAnchorNPC();
        if (anchor == null)
        {
            if (forcedAddBuff) npc.buffImmune[buffId] = false;
            npc.AddBuffFix(buffId, duration, quiet);
            return;
        }

        int anchorIndex = anchor.whoAmI;
        if (forcedAddBuff) anchor.buffImmune[buffId] = false;
        anchor.AddBuffFix(buffId, duration, quiet);

        foreach (var part in Main.ActiveNPCs)
        {
            if (part.whoAmI == anchorIndex) continue;
            if (part.GetElementAnchorNPC()?.whoAmI != anchorIndex) continue;

            if (forcedAddBuff) part.buffImmune[buffId] = false;
            part.AddBuffFix(buffId, duration, quiet);
        }
    }
}
