using KL.SkillSystem.AbstractClass;
using SilkyUIFramework;

namespace KL.SkillSystem.TemplateSkillUI;

public class BasicSkillSlot(SkillIcon skillIcon) : SkillSlot(skillIcon)
{
    protected override float SlotBorder { get; set; } = 2f;
    
    protected override Color SlotBorderColor { get; set; } = Color.Black;
    
    protected override Color SlotBackgroundColor { get; set; } = Color.Black * 0.5f;
    
    //圆角角度
    protected override Vector4 SlotBorderRadius { get; set; } = new Vector4(4);
    
    protected override float SlotPadding { get; set; } = 0f;
    
    protected override Vector2 SlotSize{ get; set; } = new Vector2(42, 42);
    


    protected override void OnInitialize()
    {
        base.OnInitialize();
    }

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);
    }
}