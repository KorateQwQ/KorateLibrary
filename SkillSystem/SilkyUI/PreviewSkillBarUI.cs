using KL.SkillSystem.SilkyUI;
using SilkyUIFramework;
using SilkyUIFramework.Elements;
using SilkyUIFramework.Extensions;
using SilkyUIFramework.Layout;

namespace KL.SkillSystem.SilkyUI;

public class PreviewSkillBarUI : UIElementGroup
{
    internal int MaxSkillSlot => ActiveSkillList?.Count ?? 0;

    internal List<Skill> ActiveSkillList ;
    public SkillPanelUI SkillPanelUI;
    
    public Color SlotBackgroundColor = Color.Black;
    public Color SlotBorderColor = Color.Black;
    public float SkillSlotBorder = 0.1f;
    /// <summary>
    /// 初始化并注册所有技能槽
    /// </summary>
    void RegisterSkillSlot()
    {
        for (int i = 0; i < MaxSkillSlot; i++)
        {
            var skillSlot = new PreviewSkillSlotUI().Join(this);
            skillSlot.PrewViewSkillBar = this;
            skillSlot.SetSize(60, 60);
            skillSlot.BackgroundColor = SlotBackgroundColor;
            skillSlot.BorderColor = SlotBorderColor;
            skillSlot.Border = SkillSlotBorder;
            skillSlot.FitHeight = false;
            skillSlot.FitWidth = false;
            skillSlot.SlotIndex = i;
        }
    }

    protected override void OnInitialize()
    {
        SetLeft(alignment: 0.5f);
        SetTop(alignment:0);

        //排列方向
        FlexDirection = FlexDirection.Row;

        //自适应子元素大小
        FitWidth = true;
        FitHeight = true;
        
        //内边距
        Padding = new Margin(8f);

        base.OnInitialize();
    }

    public void Register()
    {
        InitSkillFromPlayerData();

        KLSkillManager.OnSkillsUpdated += SkillManagerOnOnSkillsUpdated;
        KLSkillManager.OnSkillsUIUpdated += SkillManagerOnOnSkillsUIUpdated; 
    }

    private void SkillManagerOnOnSkillsUIUpdated()
    {
        InitSkillFromPlayerData();
    }

    private void SkillManagerOnOnSkillsUpdated()
    {
        InitSkillFromPlayerData();
    }

    
    /*void UpdateSkillInfo()
    {
        for (int i = 0; i < Children.Count; i++)
        {
            if (Children[i] is SkillSlotUI slot)
            {
                slot.SkillIcon.Skill = KLSkillManager.GetActiveSkillList()[i];
            }
        }
    }*/
    public void InitSkillFromPlayerData()
    {
        List<Skill> activeSkills = ActiveSkillList;
        RemoveAllChildren();
        RegisterSkillSlot();

        if (activeSkills != null)
        {
            for (int i = 0; i < activeSkills.Count; i++)
            {
                if (activeSkills[i] != null)
                {
                    AddSkillToSlot(activeSkills[i], i);
                }
            }
        }
    }
    public void AddSkillToSlot(Skill skill, int index)
    {
        if (index >= 0 && index < Children.Count)
        {
            GetSkillSlot(index)?.AddSkillToSlot(skill);
        }
    }
    public void RemoveSkill(int index)
    {
        if (index >= 0 && index < Children.Count)
        {
            GetSkillSlot(index)?.RemoveAllChildren();
        }
    }
    public PreviewSkillSlotUI GetSkillSlot(int index)
    {
        if(index>=0&&index<Children.Count)return (PreviewSkillSlotUI)Children[index];
        return null;
    }

    protected override void Update(GameTime gameTime)
    {
        //BorderColor = Color.Black;
        base.Update(gameTime);
    }
}