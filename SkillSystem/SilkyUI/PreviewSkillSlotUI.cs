using SilkyUIFramework;
using SilkyUIFramework.Attributes;
using SilkyUIFramework.Elements;
using SilkyUIFramework.Extensions;

namespace KL.SkillSystem.SilkyUI;

/// <summary>
/// 技能槽UI，在previewSkillBar上作为技能的容器。
/// </summary>
public class PreviewSkillSlotUI : UIElementGroup
{
    public bool HasSkill => Children.Count > 0;
    
    public bool SkillIsDragging { get; set; }
    
    public PanelSkillIcon SkillIcon;
    public PreviewSkillBarUI PrewViewSkillBar;

    public int SlotIndex = 0;
    public PreviewSkillSlotUI(PanelSkillIcon skill = null)
    {
        //SetSize(16f * 30f, 9f * 30f);
        //SetGap(10f);

        //Positioning = Positioning.Relative;
        Border = 2f;
        BorderColor = Color.Black;
        
        //圆角
        BorderRadius = new Vector4(4);

        //内边距
        Padding = new Margin(0f);
        
        Margin = new Margin(3f,0);

        SetSize(42, 42);
        BackgroundColor = Color.White*0;
        FitHeight = false;
        FitWidth = false;
        
        SkillIcon = skill;
        
        IgnoreMouseInteraction = false;
        OverflowHidden = true;
    }

    public void AddSkillToSlot(Skill skill)
    {
        var skillUI = new PanelSkillIcon(skill).Join(this);
        skillUI.PrewViewSkillBar = PrewViewSkillBar;
        skillUI.SkillPanelUI = PrewViewSkillBar?.SkillPanelUI;
        
        skillUI.SetSize(Width.Pixels,Height.Pixels);
        //图片绘制倍率
        skillUI.ImageScale = new Vector2(Width.Pixels/skillUI.Texture2D.Width()*0.95f, Height.Pixels/skillUI.Texture2D.Height()*0.95f);
        //居中
        skillUI.ImageAlign = new Vector2(0.5f);
        skillUI.SetLeft(alignment: 0.5f);
        skillUI.SetTop(alignment: 0.5f);
        
        skillUI.BackgroundColor = Color.Black*0f;
        skillUI.FitHeight = false;
        skillUI.FitWidth = false;
        
        SkillIcon = skillUI;
        

        //Main.LocalPlayer.GetModPlayer<SkillModPlayer>().UpdateActiveSkill();
    }
    public override void OnMouseEnter(UIMouseEvent evt)
    {
        base.OnMouseEnter(evt);
    }

    public override void OnMouseLeave(UIMouseEvent evt)
    {
        base.OnMouseLeave(evt);
    }

    public override void OnMouseMove(UIMouseEvent evt)
    {
        base.OnMouseMove(evt);
    }

    public override void OnLeftMouseDown(UIMouseEvent evt)
    {
        base.OnLeftMouseDown(evt);
    }

    public override void OnLeftMouseUp(UIMouseEvent evt)
    {
        base.OnLeftMouseUp(evt);
    }

    protected override void OnInitialize()
    {
        base.OnInitialize();
    }
    
    //一个slot只能容纳一个元素
    protected override void OnAddChild(UIView child)
    {
        base.OnAddChild(child);
    }

    protected override void Update(GameTime gameTime)
    {
        //SkillIcon.ImageScale = new Vector2(0.5f);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        ZIndex = SkillIsDragging ? 2 : 1;
        base.Draw(gameTime, spriteBatch);
    }
    
}