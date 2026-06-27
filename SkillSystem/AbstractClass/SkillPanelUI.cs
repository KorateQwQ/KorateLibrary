using System.Linq;
using KL.SkillSystem.AbstractClass;
using SilkyUIFramework;
using SilkyUIFramework.Attributes;
using SilkyUIFramework.Elements;
using SilkyUIFramework.Extensions;
using SilkyUIFramework.Layout;

namespace KL.SkillSystem.SilkyUI;

//技能表总面板，最顶层用于放置技能栏，下面则为所有技能信息（包括未解锁的技能）
/*
[RegisterUI("Vanilla: Radial Hotbars", "KorateLibrary: SkillPanelUI", int.MinValue)]
*/
public abstract class SkillPanelUI : BaseBody, IDraggableUI
{
    #region 实现接口属性

    public bool IsDragging { get; set; }
    public UIView DragUI => this;
    public Vector2 LastMousePosition { get; set; } = Vector2.Zero;

    public PreviewSkillBarUI PreviewSkillBar;
    protected UIElementGroup ContentPanel;
    protected SUIScrollContainer MainContainer;
    protected UIElementGroup MainPanel;
    protected SUIScrollView scrollView;
    protected SkillToolTip SkillToolTip;
    private PanelSkillIcon draggingSkillIcon;
    
    //**技能表图标配置**/
    /// <summary>
    /// 图标外侧旋转边框宽度
    /// </summary>
    public virtual float SkillSlotOutBorder => 2;
    public virtual float SkillSlotBorder => 2;
    public virtual Color SkillSlotBorderColor => Color.Red;
    public virtual Color SkillSlotBackgroundColor => Color.White*0.2f;
    
    public virtual float SkillLevelHintBorder => 2;
    public virtual Color SkillLevelHintBorderColor => Color.White;
    public virtual Color SkillLevelHintBackgroundColor => new Color(0.5f,0.5f,0.5f,1);
    
    public virtual float SkillAddButtonBorder => 2;
    public virtual Color SkillAddButtonBorderColor => Color.White;
    public virtual Color SkillAddButtonBackgroundColor => new Color(0.5f, 0.5f, 0.5f, 1);
    
    public virtual float PreviewSkillBarBorder => 2;
    public virtual Color PreviewSkillBarBorderColor => Color.White;
    public virtual Color PreviewSkillBarBackgroundColor => new Color(0.0f, 0.0f, 0.0f, 0);
    public virtual Vector4 PreviewSkillBarBorderRadius => new Vector4(4);
    //**技能表图标配置**/

    
    public override void OnLeftMouseDown(UIMouseEvent evt)
    {
        ((IDraggableUI)this).StartDrag(Main.MouseScreen);
        base.OnLeftMouseDown(evt);
    }

    public override void OnLeftMouseUp(UIMouseEvent evt)
    {
        ((IDraggableUI)this).StopDrag();
        base.OnLeftMouseUp(evt);
    }
    #endregion
    protected override void Update(GameTime gameTime)
    {
        ((IDraggableUI)this).UpdateDrag(); // 调用接口的拖拽更新
        base.Update(gameTime);
    }

    public void SetDraggingSkillIcon(PanelSkillIcon skillIcon)
    {
        draggingSkillIcon = skillIcon;
    }

    public void ClearDraggingSkillIcon(PanelSkillIcon skillIcon)
    {
        if (draggingSkillIcon == skillIcon)
        {
            draggingSkillIcon = null;
        }
    }

    private void DrawDraggingSkillIcon(SpriteBatch spriteBatch)
    {
        if (draggingSkillIcon?.Skill?.SkillIcon?.Value == null || !draggingSkillIcon.IsDragging)
        {
            return;
        }

        spriteBatch.Draw(draggingSkillIcon.Skill.SkillIcon.Value, Main.MouseScreen, draggingSkillIcon.SourceRectangle,
            draggingSkillIcon.ImageColor, 0f, draggingSkillIcon.Texture2D.Value.Origin(), draggingSkillIcon.ImageScale, 0f, 0f);
    }

    public override void DrawChildren(GameTime gameTime, SpriteBatch sb)
    {
        base.DrawChildren(gameTime, sb);
        DrawDraggingSkillIcon(sb);
    }
    
    /// <summary>
    /// 必须设置，获取所有技能列表
    /// </summary>
    public virtual List<Skill> GetActiveSkillList => GetSkillPlayer()?.ActiveSkill;
    
    protected override void OnInitialize()
    {
        //IconImage ??= ModContent.Request<Texture2D>("KL/SilkyUI/冰锥", AssetRequestMode.ImmediateLoad);

        SetLeft(alignment: 0.5f);
        SetTop(alignment: 0.15f);

        //排列方向
        FlexDirection = FlexDirection.Column;
        Gap = new Size(0, 8);

        //自适应子元素大小
        FitWidth = false;
        FitHeight = false;

        //圆角
        BorderRadius = new Vector4(4);

        //内边距
        Padding = new Margin(left: 32, top: 20, right: 10, bottom: 30);

        SetWidth(1100);
        SetHeight(800);
        
        PreviewSkillBarUI previewSkillBar = new PreviewSkillBarUI()
        {
            SlotBackgroundColor = SkillSlotBackgroundColor,
            SlotBorderColor = SkillSlotBorderColor,
            SkillSlotBorder = SkillSlotBorder,
            Border = PreviewSkillBarBorder,
            BorderColor = PreviewSkillBarBorderColor,
            BackgroundColor = PreviewSkillBarBackgroundColor,
            BorderRadius = PreviewSkillBarBorderRadius,
            SkillPanelUI = this
            
        }.Join(this);
        previewSkillBar.SetTop(0,0,0);
        previewSkillBar.MainAlignment = MainAlignment.Center;
        previewSkillBar.ActiveSkillList = GetActiveSkillList;
        PreviewSkillBar = previewSkillBar;
        PreviewSkillBar.Register();
        //Enabled = false;

        ContentPanel = new UIElementGroup
        {
            FlexDirection = FlexDirection.Row,
            Gap = new Size(12, 0),
            Padding = new Margin(0),
            FitWidth = true,
            FitHeight = false,
            Height = new Dimension(700f),
            BackgroundColor = Color.Transparent,
        }.Join(this);

        scrollView = new DragScrollView(Direction.Vertical)
        {
            Gap = new Vector2(4f),
            Container = { Gap = new Vector2(4f)},
            Padding = new Margin(0),
            Width = new Dimension(750f),

        }.Join(ContentPanel);
        scrollView.SetWidth(750f, 0f);
        scrollView.SetHeight(660, 0f);
        scrollView.Mask.OverflowHidden = false;
        scrollView.OverflowHidden = true;
        scrollView.BorderRadius = new Vector4(4);
        scrollView.Padding = new Margin(20);
        MainContainer = scrollView.Container;

        MainPanel = new UIElementGroup()
        {
            Width = new Dimension(percent:0.98f),
            //Height = new Dimension(percent:0.98f)
            FitHeight = true,
            Gap = new Size(0,20),
            Padding = 0,
            FlexDirection = FlexDirection.Column,
            IgnoreMouseInteraction = true,
            OverflowHidden = false,
        }.Join(MainContainer);

        SkillToolTip = new SkillToolTip().Join(ContentPanel);

        //PanelSlotUI panelSlot = new PanelSlotUI().Join(this);
        RegisterAllSkill();
        base.OnInitialize();
    }

    /// <summary>
    /// 获取技能表需要显示的技能，默认获取所有技能。
    /// </summary>
    /// <returns></returns>
    protected virtual Dictionary<string, Skill> GetAllSkill()
    {
        return Skill.RegisterSkill;
    }

    protected abstract KLSkillModPlayer GetSkillPlayer();

    public virtual int GetCurrentSkillPoint()
    {
        return GetSkillPlayer()?.SkillPoint ?? 0;
    }

    public virtual bool TryUnlockPanelSkill(Skill skill)
    {
        if (skill?.ModSkill == null || skill.BasicStatus != Skill.SKillBasicStatus.Lock)
        {
            return false;
        }

        KLSkillModPlayer skillPlayer = GetSkillPlayer();
        if (skillPlayer == null || !skill.ModSkill.UnlockCondition.TryUnlock(skillPlayer, skill.ModSkill))
        {
            return false;
        }

        skillPlayer.UnlockSkill(skill);
        return true;
    }

    protected virtual Skill GetPanelSkillInstance(Skill registeredSkill)
    {
        if (registeredSkill?.ModSkill == null)
        {
            return null;
        }

        Type skillType = registeredSkill.ModSkill.GetType();
        string skillTypeName = skillType.Name;
        KLSkillModPlayer skillPlayer = GetSkillPlayer();
        if (skillPlayer?.UnlockedSkill != null && skillPlayer.UnlockedSkill.TryGetValue(skillTypeName, out Skill ownedSkill))
        {
            return ownedSkill;
        }

        return Skill.NewSkill(skillType, registeredSkill.Mod);
    }
    
    /// <summary>
    /// 注册所有技能
    /// </summary>
    protected void RegisterAllSkill()
    {
        var sortedSkills = GetAllSkill()
            .OrderBy(skill => GetUIPositionAttribute(skill.Value).State)
            .ThenBy(skill => GetUIPositionAttribute(skill.Value).Pixels)
            .ToList(); // 转换为列表存储

        int currentState = -1;
        float totalWidthOfLine = 0;
        UIElementGroup currentSkillLine = new UIElementGroup();
        
        foreach (var skill in sortedSkills)
        {
            Skill panelSkill = GetPanelSkillInstance(skill.Value);
            if (panelSkill?.ModSkill == null)
            {
                continue;
            }

            SkillUIInfoAttribute infoAttribute = GetUIPositionAttribute(skill.Value);
            int state = infoAttribute.State;
            float pixcels = infoAttribute.Pixels;
            //新开一行技能栏
            if (state != currentState)
            {
                currentState = state;
                totalWidthOfLine = 0;
                currentSkillLine.Gap = new Size(20,0);
                currentSkillLine = new UIElementGroup().Join(MainPanel);
                currentSkillLine.BackgroundColor = Color.White*0;
                currentSkillLine.FitHeight = true;
                currentSkillLine.Width = MainPanel.Width;
                currentSkillLine.OverflowHidden = false;
                currentSkillLine.IgnoreMouseInteraction = true;
            }
            
            PanelSlotUI panelSlot = new PanelSlotUI(new PanelSkillIcon(panelSkill)
            {
                SkillPanelUI = this,
                PrewViewSkillBar = PreviewSkillBar,
                BackgroundColor = SkillSlotBackgroundColor,
            },this)
            {
                
            }.Join(currentSkillLine);
            
            panelSlot.SetLeft(pixcels-totalWidthOfLine);
            totalWidthOfLine += panelSlot.Bounds.Width; //panelSlot.Width.Pixels+panelSlot.Border;

        }
    }
    //            PanelSlotUI panelSlot = new PanelSlotUI(new BasicSkillUI(skill.Value)).Join(MainPanel);

    private SkillUIInfoAttribute GetUIPositionAttribute(Skill skill)
    {
        var type = skill.ModSkill.GetType();
        var attributes = type.GetCustomAttributes(typeof(SkillUIInfoAttribute), false);
        return attributes.Length > 0 ? (SkillUIInfoAttribute)attributes[0] : null;
    }
    
}