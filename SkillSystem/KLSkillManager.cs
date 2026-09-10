namespace KL.SkillSystem;

public class KLSkillManager : ModSystem
{

    /// <summary>
    /// 技能UI更新事件, 当技能栏中的技能发生变化时触发, 比如拖动技能到技能栏中，交换技能
    /// </summary>
    public static event Action OnSkillsUpdated;
    
    /// <summary>
    /// 技能UI更新事件, 当UI信息发生变化时触发，比如技能栏增加事件等
    /// </summary>
    public static event Action OnSkillsUIUpdated;
    
    private static KLSkillManager klSkillManager;
    private readonly Queue<KLSkillModPlayer> _cooldownUpdates = new();
    private readonly HashSet<KLSkillModPlayer> _queuedPlayers = new();

    public override void Load()
    {
        klSkillManager = this;
        base.Load();
    }
    
    /// <inheritdoc />
    public override void PreUpdatePlayers()
    {
        ClearCooldownUpdates();
        base.PreUpdatePlayers();
    }

    internal static void QueueCooldownUpdate(KLSkillModPlayer skillPlayer)
    {
        if (klSkillManager._queuedPlayers.Add(skillPlayer))
            klSkillManager._cooldownUpdates.Enqueue(skillPlayer);
    }

    /// <summary>等待所有玩家的属性提交和后置联动结束，再结算本帧已登记的技能冷却。</summary>
    public override void PostUpdatePlayers()
    {
        while (_cooldownUpdates.Count > 0)
        {
            KLSkillModPlayer skillPlayer = _cooldownUpdates.Dequeue();
            if (!skillPlayer.Player.active || skillPlayer.Player.dead)
                continue;

            try
            {
                skillPlayer.UpdateCooldownsAfterAttributes();
            }
            catch (Exception exception)
            {
                // 一个业务玩家的技能失败不应打断其他玩家的冷却更新。
                Mod.Logger.Error($"{skillPlayer.GetType().FullName} 技能冷却更新失败。", exception);
            }
        }
        base.PostUpdatePlayers();
    }

    public override void OnWorldUnload()
    {
        ClearCooldownUpdates();
        base.OnWorldUnload();
    }

    public override void Unload()
    {
        ClearCooldownUpdates();
        klSkillManager = null;
        base.Unload();
    }

    private void ClearCooldownUpdates()
    {
        _cooldownUpdates.Clear();
        _queuedPlayers.Clear();
    }
    
    public static void SwitchSkill(List<Skill>activeSkillList,int index1, int index2)
    {
        if (index1 < 0 || index1 >= activeSkillList.Count || index2 < 0 ||
            index2 >= activeSkillList.Count)
        {
            Log($"Error: SwitchSkill: index is out of range");
        }
        (activeSkillList[index1], activeSkillList[index2]) = (activeSkillList[index2], activeSkillList[index1]);
        activeSkillList[index1].SkillSlot = index1;
        activeSkillList[index2].SkillSlot = index2;
        OnSkillsUpdated?.Invoke();
    }

    public static void EquipSkill(List<Skill>activeSkillList, Skill skill, int index = 0)
    {
        if (index < 0 || index >= activeSkillList.Count)
        {
            Log($"Error: EquipSkill: index is out of range");
            return;
        }

        for (int i = 0; i < activeSkillList.Count; i++)
        {
            if (Skill.IsSameSkillType(activeSkillList[i], skill))
            {
                activeSkillList[i] = null;
            }
        }
        activeSkillList[index] = skill;
        skill.SkillSlot = index;
        OnSkillsUpdated?.Invoke();

    }

    public static void UnEquipSkill(List<Skill>activeSkillList,  int index = 0)
    {
        if (index < 0 || index >= activeSkillList.Count) {
            Log($"Error: UnEquipSkill: index is out of range");
            return;
        }
        activeSkillList[index] = null;
        OnSkillsUpdated?.Invoke();
    }
    
}
