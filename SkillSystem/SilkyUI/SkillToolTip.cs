using KL.Drawing.Snippets;
using KL.SkillSystem.AbstractClass;
using KL.UI;
using SilkyUIFramework;
using SilkyUIFramework.Attributes;
using SilkyUIFramework.Elements;
using SilkyUIFramework.Extensions;
using SilkyUIFramework.Layout;

namespace KL.SkillSystem.SilkyUI;

public class SkillToolTip : UIElementGroup
{
    private UIElementGroup _header;
    private KLTextView _nameText;
    private KLTextView _levelText;
    private UIElementGroup _toggleRow;
    private ToggleButton _toggleButton;
    private KLTextView _toggleStateText;
    private DragScrollView _descScrollView;
    private KLTextView _descText;

    private SkillUnlockFooterUI _unlockFooter;
    private Skill _currentSkill;
    
    public static SkillToolTip Instance { get; private set; }

    public static bool IsShow { get; set; }
    public bool IsHovered { get; private set; }

    protected override void OnInitialize()
    {
        IsShow = false;

        IgnoreMouseInteraction = false;
        Border = 2f;
        BackgroundColor = Color.Black * 0.5f;
        BorderRadius = new Vector4(6f);
        Padding = new Margin(8f);

        SetWidth(270f, 0f);
        SetHeight(660, 0f);
        FitWidth = false;
        FitHeight = false;

        FlexDirection = FlexDirection.Column;
        Gap = new Size(0, 6);

        _header = new UIElementGroup
        {
            FlexDirection = FlexDirection.Row,
            Width = new Dimension(0, percent: 1f),
            Height = new Dimension(0, percent: 0f),
            FitWidth = false,
            FitHeight = true,
            BackgroundColor = Color.Red * 0,
        }.Join(this);

        _nameText = new KLTextView

        {
            Width = new Dimension(percent: 0.7f),
            FitWidth = false,
            FitHeight = true,
            TextAlign = new Vector2(0f),
            TextScale = 0.3f,
            TextColor = Color.White,
            TextBorder = 0,
            TextBorderColor = Color.White * 0,
            BackgroundColor = Color.Red * 0,
            WordWrap = false,
            Font = FontManager.HarmonyOS_Sans_SC.Value,
        }.Join(_header);

        _levelText = new KLTextView

        {
            Width = new Dimension(percent: 0.3f),
            FitWidth = false,
            FitHeight = true,
            TextAlign = new Vector2(1f),
            TextScale = 0.3f,
            TextColor = Color.White,
            TextBorder = 0,
            TextBorderColor = Color.White * 0,
            BackgroundColor = Color.Blue * 0,
            WordWrap = false,
            Font = FontManager.HarmonyOS_Sans_SC.Value,
        }.Join(_header);

        _toggleRow = new UIElementGroup
        {
            FlexDirection = FlexDirection.Row,
            CrossAlignment = CrossAlignment.Center,
            CrossContentAlignment = CrossContentAlignment.Center,
            Width = new Dimension(percent: 1f),
            Height = new Dimension(0f),
            FitWidth = false,
            FitHeight = false,
            Gap = new Size(10f, 0f),
            Padding = new Margin(0f),
            BackgroundColor = Color.Black*1,
            OverflowHidden = true,
            IgnoreMouseInteraction = true,
        }.Join(this);

        _toggleButton = CreateToggleButton().Join(_toggleRow);
        _toggleButton.ValueChanged += SetCurrentSkillEnabled;

        _toggleStateText = new KLTextView
        {
            Width = new Dimension(188f),
            Height = new Dimension(14f),
            FitWidth = false,
            FitHeight = false,
            FlexShrink = 1f,
            TextAlign = new Vector2(1f, 0.5f),
            TextScale = 0.3f,
            TextColor = Color.White,
            TextBorder = 0f,
            TextBorderColor = Color.Transparent,
            BackgroundColor = Color.Green,
            WordWrap = false,
            Font = FontManager.HarmonyOS_Sans_SC.Value,
            IgnoreMouseInteraction = true,
        }.Join(_toggleRow);

        _descScrollView = new DragScrollView(Direction.Vertical)
        {
            Width = new Dimension(percent: 1f),
            Height = new Dimension(0f),
            FitWidth = false,
            FitHeight = false,
            FlexGrow = 1f,
            FlexShrink = 1f,
            Padding = new Margin(0f),
            BackgroundColor = Color.Transparent,
            Border = 0f,
            Gap = new Size(0f),
        }.Join(this);
        _descScrollView.Mask.OverflowHidden = true;
        _descScrollView.Container.FlexDirection = FlexDirection.Column;
        _descScrollView.Container.FlexWrap = false;
        _descScrollView.Container.MainAlignment = MainAlignment.Start;
        _descScrollView.Container.Width = new Dimension(percent: 1f);
        _descScrollView.Container.Height = new Dimension(0f);
        _descScrollView.Container.FitWidth = false;
        _descScrollView.Container.FitHeight = true;
        _descScrollView.Container.Gap = new Size(0f);

        _descText = new KLTextView
        {
            Width = new Dimension(percent: 1f),
            Height = new Dimension(0f),
            FitWidth = false,
            FitHeight = true,
            FlexShrink = 0f,
            WordWrap = true,
            TextScale = 0.3f,
            TextColor = Color.White,
            TextBorder = 0f,
            TextBorderColor = Color.Transparent,
            BackgroundColor = Color.Transparent,
            Font = FontManager.HarmonyOS_Sans_SC.Value,
        }.Join(_descScrollView.Container);

        _unlockFooter = new SkillUnlockFooterUI
        {
            FlexShrink = 0f,
            Margin = new Margin(0f, 10f, 0f, 0f),
        }.Join(this);
        _unlockFooter.SetTop(0f, 0f);
    
        base.OnInitialize();
        Instance = this;
    }

    public override void OnMouseEnter(UIMouseEvent evt)
    {
        IsHovered = true;
        base.OnMouseEnter(evt);
    }

    public override void OnMouseLeave(UIMouseEvent evt)
    {
        IsHovered = false;
        base.OnMouseLeave(evt);
    }

    protected override void Update(GameTime gameTime)
    { 
        _toggleRow.Margin = new Margin(0f, 12f, 0f, 0f);
        _toggleRow.Padding = new Margin(0f);
        _toggleRow.Gap = new Size(8f, 0f);
        _toggleRow.Height = new Dimension(20f);
        _toggleRow.BackgroundColor = Color.Transparent;

        _toggleStateText.Width = new Dimension(0f);
        _toggleStateText.Height = new Dimension(0f);
        _toggleStateText.FlexGrow = 1f;
        _toggleStateText.FlexShrink = 1f;
        _toggleStateText.TextAlign = new Vector2(1f, 0.5f);
        _toggleStateText.BackgroundColor = Color.Green*0;
        
        _descScrollView.BackgroundColor = Color.Transparent;
        _descScrollView.Margin = new Margin(0f, 8f, 0f, 0f);
        _descScrollView.Top = new Anchor(0,0);
        _descText.Margin = new Margin(0f);

        Padding = new Margin(8f,0,8,8);

        _header.Margin = new Margin(0, 8, 0, 0);

        Gap = new Size(0f, 0f);
        //Padding = new Margin(0f);
        ModSkill modSkill = _currentSkill?.ModSkill;
        if (modSkill?.IsToggleable == true && _toggleButton.IsOn != modSkill.IsEnabled)
        {
            _toggleButton.SetValue(modSkill.IsEnabled, notify: false);
            _toggleStateText.Text = GetToggleStateText(modSkill.IsEnabled);
        }

        base.Update(gameTime);
    }

    public void SetTooltip(Skill skill, string name, string level, string desc, PanelSkillIcon skillIcon = null)
    {
        _currentSkill = skill;
        _nameText.Text = name;
        _levelText.Text = level;
        _descText.Text = desc;
        _descScrollView.ScrollBar.SetScrollPosition(Vector2.Zero);
        RefreshToggleRow();
        _unlockFooter.SetSkill(skill, skillIcon);
    }

    protected virtual ToggleButton CreateToggleButton()
    {
        return new ToggleButton
        {
            ToggleSize = new Vector2(42f, 24f),
            ThumbDiameter = 18f,
            ThumbInset = 3f,
        };
    }

    protected virtual string GetToggleStateText(bool isEnabled)
    {
        return isEnabled ? "已开启" : "未开启";
    }

    private void RefreshToggleRow()
    {
        ModSkill modSkill = _currentSkill?.ModSkill;
        bool isToggleable = modSkill?.IsToggleable == true;
        
        _toggleRow.Height = new Dimension(isToggleable ? 32f : 0f);
        _toggleRow.Invalid = !isToggleable;
        _toggleRow.DisableMouseInteraction = !isToggleable;

        if (!isToggleable)
        {
            _toggleStateText.Text = string.Empty;
            return;
        }

        _toggleButton.SetValue(modSkill.IsEnabled, notify: false, animate: false);
        _toggleStateText.Text = GetToggleStateText(modSkill.IsEnabled);
    }

    private void SetCurrentSkillEnabled(bool isEnabled)
    {
        ModSkill modSkill = _currentSkill?.ModSkill;
        if (modSkill?.IsToggleable != true)
        {
            return;
        }

        modSkill.IsEnabled = isEnabled;
        _toggleStateText.Text = GetToggleStateText(isEnabled);
    }

    public void RefreshUnlockFooter()
    {
        _unlockFooter.Refresh();
    }

    public void SetTooltip(string name, string level, string desc)
    {
        SetTooltip(null, name, level, desc);
    }

    public bool IsCurrentSkill(Skill skill)
    {
        if (skill == null || _currentSkill == null)
        {
            return false;
        }

        return Skill.IsSameSkillType(skill, _currentSkill);
    }


}