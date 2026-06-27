using KL.SkillSystem.AbstractClass;

namespace KL.SkillSystem.TemplateSkillUI;

public class BasicSkillIcon(Skill skill) : SkillIcon(skill)
{
    protected override void OnInitialize()
    {
        base.OnInitialize();
        if (Parent != null)
        {
            //适应skillSlot的大小和圆角效果
            SetSize(Parent.Width.Pixels,Parent.Height.Pixels);
            BorderRadius = Parent.BorderRadius;
            
            ImageScale = new Vector2(Parent.Width.Pixels/Texture2D.Width()*0.95f, Parent.Height.Pixels/Texture2D.Height()*0.95f);

            ImageAlign = new Vector2(0.5f);
            SetLeft(alignment: 0.5f);
            SetTop(alignment: 0.5f);
        
            FitWidth = false;
            FitHeight = false;
            ImageAlign = new Vector2(0.5f);
            BackgroundColor = Color.Black*0.5f;
        }
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