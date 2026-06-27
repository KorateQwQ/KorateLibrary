using KL.SkillSystem.SilkyUI;

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

    public override void Load()
    {
        klSkillManager = this;
        base.Load();
    }
    
    /// <inheritdoc />
    public override void PreUpdatePlayers()
    {
        base.PreUpdatePlayers();
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