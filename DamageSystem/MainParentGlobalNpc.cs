using System;
using System.Collections.Generic;
using KL.Utils;
using Terraria.DataStructures;
using Terraria.GameContent.UI.BigProgressBar;

namespace KL.DamageSystem;

/// <summary>
/// 用于在 NPC 生成阶段尝试记录“父 NPC（通常是 Boss） -> 子 NPC（由其派生生成）”的关联信息。
/// 当前实现只做“预期父级”的捕获与调试检查，尚未把父子关系落到稳定的数据结构（例如 child 列表/网络同步）。
/// </summary>
public class MainParentGlobalNpc : GlobalNPC
{
    /// <summary>
    /// 预留字段：用于记录父 NPC 的 whoAmI。
    /// 当前文件内未使用。
    /// </summary>
    public int parentid = -1;

    /// <summary>
    /// 预留字段：用于记录子 NPC 的 whoAmI 列表。
    /// 当前只在 <see cref="OnSpawn"/> 做了初始化，尚未写入实际数据。
    /// </summary>
    public Dictionary<int,int> childid;

    public override bool InstancePerEntity => true;
    
    /// <summary>
    /// 在 <see cref="OnSpawn"/> 阶段捕获到的“预期父 NPC”。
    /// 目前只在调试检查中使用（见 <see cref="CheckAIArrayHasParentIndex"/>）。
    /// </summary>
    public NPC ExpectedParent;
    
    public bool IsWormMainParent = false;

    public int lifeMax = -1;

    public override void OnSpawn(NPC npc, IEntitySource source)
    {
        // 确保 child 列表可用（预留给后续记录“这个 parent 生成了哪些 child”）。
        childid ??= new Dictionary<int,int>();
        
        // 只在服务端/单机执行（Main.netMode == 1 为纯客户端）。
        // 目的是：在 child NPC 生成时，通过 EntitySource_Parent 捕获其父实体。
        if (source is EntitySource_Parent parentSource && Main.netMode != 1)
        {
            // realLife >= 0 通常用于多体节蠕虫（原版会用 realLife 链接头/身/尾）。
            // 这里排除蠕虫体节，避免与原版机制冲突。
            if (parentSource.Entity is NPC { boss: true } parent && npc.realLife < 0)
            {
                // 记录“预期父级”。
                // 注意：这里仅在生成来源是 EntitySource_Parent 且父级是 boss 时记录。
                lifeMax = npc.GetRealMaxHP();

                if (Math.Abs(KLGameStateManager.GetBossState(parent) - KLGameStateManager.GetBossState(npc)) < 0.001f)
                {
                    ExpectedParent = parent;
                    //PrintText($"AddChild SelfName: {npc.FullName} childID {npc.whoAmI} + ParentName:  {parent.FullName} parentid: {parent.whoAmI}");
                    parent.GetGlobalNPC<MainParentGlobalNpc>().AddChild(npc.type,npc.whoAmI);
                    lifeMax = npc.GetRealMaxHP();
                }
            }
        }

        base.OnSpawn(npc, source);
    }
    

    public override void AI(NPC npc)
    {
        if (npc.realLife>=0&&npc.GetRealNpc() is { } wormParent&&wormParent!=npc)
        {
            wormParent.GetGlobalNPC<MainParentGlobalNpc>().IsWormMainParent = true;
        }
        
        base.AI(npc);
    }

    void AddChild(int type, int childid)
    {
        this.childid ??= new Dictionary<int,int>();
        this.childid.TryAdd(type, childid);
    }

}