using KL.SkillSystem.SilkyUI;
using SilkyUIFramework;
using SilkyUIFramework.Elements;

namespace KL.SkillSystem.AbstractClass;

public abstract class SkillSlot(SkillIcon skillIcon) : UIElementGroup
{
    //技能正在拖动，因此需要额外绘制
    public bool SkillIsDragging { get; set; }
    public bool HasSkill => Children.Count > 0;
    
    public SkillIcon SkillIcon;

    protected virtual float SlotBorder { get; set; } = 2f;
    
    protected virtual Color SlotBorderColor { get; set; } = Color.Black;
    
    protected virtual Color SlotBackgroundColor { get; set; } = Color.Black * 0.5f;
    
    //圆角角度
    protected virtual Vector4 SlotBorderRadius { get; set; } = new Vector4(4);
    
    protected virtual float SlotPadding { get; set; } = 0f;
    
    protected virtual Vector2 SlotSize{ get; set; } = new Vector2(42, 42);

    protected override void OnInitialize()
    {
        InitSlot();
        base.OnInitialize();
    }

    protected void InitSlot()
    {
        SkillIcon = skillIcon;
        Border = SlotBorder;
        BorderColor = Color.Black;
        
        //圆角
        BorderRadius = SlotBorderRadius;

        //内边距
        Padding =SlotPadding;
        
        SetSize(SlotSize.X, SlotSize.Y);
        BackgroundColor = SlotBackgroundColor;

        //技能槽默认裁切内部技能图标
        OverflowHidden = true;

    }

    protected override void OnAddChild(UIView child)
    {
        base.OnAddChild(child);
    }

    protected override void Update(GameTime gameTime)
    {
        if (HasSkill)
        {
            //SkillToolTip.IsShow = true;

        }
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);

        
    }

    protected override void DrawRenderTarget(SpriteBatch spriteBatch, RenderTarget2D renderTarget, Vector2 position)
    {
        base.DrawRenderTarget(spriteBatch, renderTarget, position);
    }

    public override void HandleDraw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.HandleDraw(gameTime, spriteBatch);
        if (SkillIsDragging)
        {
            ZIndex = 2;
            spriteBatch.Draw(SkillIcon.Skill.SkillIcon.Value, Main.MouseScreen,SkillIcon.Texture2D.Frame(),
                SkillIcon.ImageColor, 0f, SkillIcon.Texture2D.Value.Origin(), SkillIcon.GetDrawSize(), 0f, 0f);
        }
        else
        {
            ZIndex = 1;
        }
    }
    
}