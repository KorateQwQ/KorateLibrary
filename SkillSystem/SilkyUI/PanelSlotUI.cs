using KL.SkillSystem.SilkyUI;
using SilkyUIFramework;
using SilkyUIFramework.Elements;
using SilkyUIFramework.Extensions;
using SilkyUIFramework.Layout;
using Terraria.GameContent.UI.Elements;

namespace KL.SkillSystem.SilkyUI;

///面板技能槽，可以加点，查看技能等级，将技能从此处拖到技能预览槽：PreviewSkillBarUI，可以将技能置入技能栏。
public class PanelSlotUI : UIElementGroup,IDraggableUI
{
    public bool IsDragging { get; set; }
    public UIView DragUI => this;
    public Vector2 LastMousePosition { get; set; } = Vector2.Zero;
    
    public PanelSkillIcon SkillIcon;

    private UIElementGroup SkillAndButtonsGroup;
    private AddOrDeleteButton PlusButton;
    private AddOrDeleteButton DeleteButton;
    private UIElementGroup Buttons;
    private PanelSkillLevelText LevelHint;
    
    static Effect partRectangle;

    private float borderRotTimer = 0f;
    private float borderRot = 0f;
    private bool isBorderRotating = false;
    
    public SkillPanelUI SkillPanelUI;
    
    public PanelSlotUI(PanelSkillIcon skill, SkillPanelUI skillPanelUI)
    {
        SkillPanelUI = skillPanelUI;
        partRectangle ??= ModContent.Request<Effect>("KL/Effects/Content/BasicShape/RoundedRectBorderSegment", AssetRequestMode.ImmediateLoad).Value;
        Border = 2f;
        BorderColor = Color.Black;
        
        //圆角
        BorderRadius = new Vector4(4);

        //内边距
        Padding = new Margin(0,0,0,2);
        
        FitHeight = false;
        FitWidth = false;
        Width = new Dimension(60) ;
        Height  = new Dimension(75) ;
        
        FlexDirection = FlexDirection.Column;
        CrossAlignment = CrossAlignment.Center;
        
        SetTop(4);

        
        SkillAndButtonsGroup = new UIElementGroup().Join(this);
        SkillAndButtonsGroup.FlexDirection = FlexDirection.Row;
        SkillAndButtonsGroup.MainAlignment = MainAlignment.Center;
        SkillAndButtonsGroup.SetTop(alignment:0f);
        SkillAndButtonsGroup.SetLeft(alignment:0f);
        SkillAndButtonsGroup.FitWidth = true;
        SkillAndButtonsGroup.FitHeight = true;
        SkillAndButtonsGroup.BackgroundColor = Color.Black*0;
        SkillAndButtonsGroup.Gap = new Size(5, 0);
        SkillAndButtonsGroup.Padding = new Margin(2, 4);
        SkillAndButtonsGroup.IgnoreMouseInteraction = true;

        SkillIcon = skill;
        SkillIcon.SetSize(56,56);
        //SkillIcon.ImageScale = new Vector2(SkillIcon.Width.Pixels/(SkillIcon.Texture2D.Width()*0.95f), SkillIcon.Height.Pixels/SkillIcon.Texture2D.Height()*0.95f);
        
        //居中
        SkillIcon.ImageAlign = new Vector2(0.5f);
        SkillIcon.SetLeft(alignment: 0.1f);
        SkillIcon.SetTop(alignment: 0.1f);
        
        SkillIcon.FitHeight = false;
        SkillIcon.FitWidth = false;
        SkillIcon.Join(SkillAndButtonsGroup);


        
        UIElementGroup buttons = new UIElementGroup().Join(SkillAndButtonsGroup);
        Buttons = buttons;
        Buttons.MainAlignment = MainAlignment.Start;
        Buttons.CrossAlignment = CrossAlignment.Center;
        Buttons.CrossContentAlignment = CrossContentAlignment.Center;
        Buttons.SetTop(alignment:0f);
        Buttons.SetLeft(alignment:0f);  
        Buttons.FlexDirection = FlexDirection.Column;
        Buttons.BackgroundColor = Color.Black*0;
        Buttons.FitWidth = true;
        Buttons.FitWidth = true;
        Buttons.Padding = new Margin(0);
        Buttons.Gap = new Size(0, 2);
        
        
        /*AddOrDeleteButton plusButton = new AddOrDeleteButton(true).Join(Buttons);
        PlusButton = plusButton;
        PlusButton.OnClick += OnClickPlus;
        PlusButton.SkillPanelUI = SkillPanelUI;
        
        AddOrDeleteButton deleteButton = new AddOrDeleteButton(false).Join(Buttons);
        DeleteButton = deleteButton;
        DeleteButton.OnClick += OnClickDelete;
        DeleteButton.SkillPanelUI = SkillPanelUI;*/

        LevelHint = new PanelSkillLevelText().Join(this);
        LevelHint.SkillPanelUI = SkillPanelUI;

        BorderColor = Color.White;
        Border = 0;
        Width = new Dimension(75);
        Height = new Dimension(70);
        FitWidth = false;
        FitHeight = false;
        BackgroundColor = Color.Black * 0.0f;

        SkillAndButtonsGroup.Gap = new Size(0, 0);
        SkillAndButtonsGroup.Padding = new Margin(0, 0);
        SkillAndButtonsGroup.BackgroundColor = Color.Transparent;

        SkillIcon.SetLeft(alignment: 0.0f);
        SkillIcon.SetTop(alignment: 0.0f);
        
        SkillIcon.BorderColor = SkillPanelUI.SkillSlotBorderColor;
        SkillIcon.Border = SkillPanelUI.SkillSlotBorder;
        SkillIcon.BorderRadius = new Vector4(4);
        SkillIcon.ImageScale = new Vector2(1.00f);
        SkillIcon.SetWidth(60);
        SkillIcon.SetHeight(60);
        SkillIcon.Padding = new Margin(0, 0);

        Buttons.Gap = new Size(0, 10);

        //UpdateLayoutFromFree();
    }

    private void OnClickPlus()
    {
        //Main.NewText("Click Plus");
        SkillIcon.Skill?.ModSkill.TryLevelUp();
    }
    private void OnClickDelete()
    {
        //Main.NewText("Click Delete");
        SkillIcon.Skill?.ModSkill.TryLevelDown();
    }

    public void TryShowToolTip()
    {
        Skill skill = SkillIcon?.Skill;
        if (skill?.ModSkill?.TryGetToolTip(out string name, out string level, out string desc) is true)
        {
            SkillToolTip.Instance.SetTooltip(skill, name, level, desc, SkillIcon);
            SkillToolTip.IsShow = true;
        }
    }

    public override void OnMouseEnter(UIMouseEvent evt)
    {
        //SkillIcon.Skill.BasicStatus = Skill.SKillBasicStatus.Hide;
        if (SkillIcon.Skill.BasicStatus == Skill.SKillBasicStatus.UnLock)
        {
            PlusButton?.OnShow();
            DeleteButton?.OnShow();
        }

        base.OnMouseEnter(evt);
    }

    public override void OnMouseLeave(UIMouseEvent evt)
    {
        if (SkillIcon.Skill.BasicStatus == Skill.SKillBasicStatus.UnLock)
        {
            PlusButton?.OnHide();
            DeleteButton?.OnHide();
        }
        base.OnMouseLeave(evt);
    }

    public override void OnLeftMouseDown(UIMouseEvent evt)
    {
        TryShowToolTip();
        base.OnLeftMouseDown(evt);
    }

    protected override void Update(GameTime gameTime)
    {
        bool isSelected = SkillToolTip.Instance?.IsCurrentSkill(SkillIcon.Skill) == true;
        if (isSelected)
        {
            isBorderRotating = true;
            borderRotTimer++;
            float cycleFrame = borderRotTimer % 60f;
            borderRot = cycleFrame < 60f ? cycleFrame * 3f : 0f;
        }
        else
        {
            isBorderRotating = false;
            borderRotTimer = 0f;
            borderRot = 0f;
        }

        LevelHint.Level = SkillIcon.Skill?.Level ?? 0;
        LevelHint.ShouldShow = SkillIcon?.Skill?.BasicStatus == Skill.SKillBasicStatus.UnLock;
        
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var position = Bounds.Position;
        var size = Bounds.Size;

        base.Draw(gameTime, spriteBatch);
        EndBeginDrawUI(0,1,ss:SamplerState.PointClamp);
        partRectangle.SetValue("width",56);
        partRectangle.SetValue("height",56);
        partRectangle.SetValue("cornerRadius",13);
        partRectangle.SetValue("borderColor",SkillPanelUI.SkillSlotBorderColor.ToVector4());
        partRectangle.SetValue("lineAngle",-45+borderRot);
        partRectangle.SetValue("lineLength",0.15f);
        partRectangle.SetValue("lineWidth",SkillPanelUI.SkillSlotOutBorder);
        
        partRectangle.Apply();
        DrawInScreen(PanelSkillIcon.NullTexture.Value,position+new Vector2(37.5f,30),scale:new Vector2(1.2f),color:Color.White);

        
        partRectangle.SetValue("lineAngle",135+borderRot);
        partRectangle.Apply();
        DrawInScreen(PanelSkillIcon.NullTexture.Value,position+new Vector2(37.5f,30),scale:new Vector2(1.2f));

        EndBeginDrawUI();


    }
}