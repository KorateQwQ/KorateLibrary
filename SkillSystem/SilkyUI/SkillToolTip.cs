using KL.DamageSystem;
using KL.Drawing.Snippets;
using SilkyUIFramework;
using SilkyUIFramework.Attributes;
using SilkyUIFramework.Elements;
using SilkyUIFramework.Extensions;
using SilkyUIFramework.Layout;

namespace KL.SkillSystem.SilkyUI;

public class SkillToolTip : UIElementGroup
{
    private UIElementGroup _header;
    private UITextView _nameText;
    private UITextView _levelText;
    private UITextView _descText;
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

        _nameText = new UITextView
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

        _levelText = new UITextView
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

        _descText = new UITextView
        {
            Width = new Dimension(percent: 1f),
            FitWidth = false,
            FitHeight = false,
            Height = new Dimension(480f),
            WordWrap = true,
            TextScale = 0.3f,
            TextColor = Color.White,
            TextBorder = 0f,
            TextBorderColor = Color.White * 0,
            BackgroundColor = Color.Green * 0,
            Font = FontManager.HarmonyOS_Sans_SC.Value,
            Top = new(10,0)

        }.Join(this);

        _unlockFooter = new SkillUnlockFooterUI().Join(this);
    
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
        base.Update(gameTime);
    }

    public void SetTooltip(Skill skill, string name, string level, string desc, PanelSkillIcon skillIcon = null)
    {
        _currentSkill = skill;
        _nameText.Text = name;
        _levelText.Text = level;
        _descText.Text = desc;
        _unlockFooter.SetSkill(skill, skillIcon);
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

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        /*_descText.Text= $"Elaina: α 造成100 {ElementType.Fire.GetIcon(size:48,offsetY:2)} 火元素伤害火元素\n伤" +
                        $"害火元素伤害火元素伤害ElainaElainaElaina火" +
                        $"元素伤害火元素伤害火元素伤害火元素伤害ElainaElainaElaina";*/
        var position = Bounds.Position;

        Texture2D tex = ModContent.Request<Texture2D>("KL/Effects/Tex/Sparkle/ShotLineSPA", AssetRequestMode.ImmediateLoad).Value;
        Texture2D cross = ModContent.Request<Texture2D>("KL/Effects/Tex/Sparkle/Cross", AssetRequestMode.ImmediateLoad).Value;

        Vector2 center = position + new Vector2(135, 35);
        
        DrawInScreen(tex, center-new Vector2(-70,0), scale: new Vector2(0.6f,0.1f), color: new Color(255,255,255,0));
        DrawInScreen(tex, center-new Vector2(70,0), scale: new Vector2(0.6f,0.1f), color: new Color(255,255,255,0));
        DrawInScreen(cross, center-new Vector2(0,0), scale: new Vector2(0.04f,0.02f), color: new Color(255,255,255,0));

        base.Draw(gameTime, spriteBatch);
    }
}