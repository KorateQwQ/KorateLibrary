using KL.SkillSystem.AbstractClass;
using SilkyUIFramework.Elements;
using SilkyUIFramework.Extensions;

namespace KL.SkillSystem.TemplateSkillUI;

public class TestSkillSlot(SkillIcon skillIcon) : BasicSkillSlot(skillIcon)
{
    protected override Vector4 SlotBorderRadius { get; set; } = new Vector4(20);

    protected override void OnInitialize()
    {
        base.OnInitialize();
        AddSkillToSlot(Skill.NewSkill(typeof(TestSkill)));
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);
    }
    
    public void AddSkillToSlot(Skill skill)
    {
        var skillUI = new BasicSkillIcon(skill).Join(this);
        
        //skillUI.SetSize(Width.Pixels,Height.Pixels);
        //图片绘制倍率
        //skillUI.ImageScale = new Vector2(Width.Pixels/skillUI.Texture2D.Width()*0.95f, Height.Pixels/skillUI.Texture2D.Height()*0.95f);
        //居中
        /*skillUI.ImageAlign = new Vector2(0.5f);
        skillUI.SetLeft(alignment: 0.5f);
        skillUI.SetTop(alignment: 0.5f);*/

        //skillUI.BorderRadius = BorderRadius;
        
        /*skillUI.BackgroundColor = Color.Black*0f;
        skillUI.FitHeight = false;
        skillUI.FitWidth = false;*/
        
        SkillIcon = skillUI;
        //Main.LocalPlayer.GetModPlayer<SkillModPlayer>().UpdateActiveSkill();
    }
}