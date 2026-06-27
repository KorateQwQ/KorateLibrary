using System.Linq;
using KL.SkillSystem.AbstractClass;
using KL.SkillSystem.SilkyUI;
using SilkyUIFramework;
using SilkyUIFramework.Attributes;
using SilkyUIFramework.Extensions;
using SilkyUIFramework.Layout;

namespace KL.SkillSystem.TemplateSkillUI;

/*
[RegisterUI("Vanilla: Radial Hotbars", "KorateLibrary: BasicSkillBar", int.MinValue)]
*/

public class BasicSkillBar : SkillContainer
{
    public virtual int MaxSkillSlot { get; set; } = 6;
    
    protected override void OnInitialize()
    {
        //IconImage ??= ModContent.Request<Texture2D>("KL/SilkyUI/冰锥", AssetRequestMode.ImmediateLoad);

        SetLeft(alignment: 0.5f);
        SetTop(alignment: 0.15f);

        //排列方向
        FlexDirection = FlexDirection.Row;

        //自适应子元素大小
        FitWidth = true;
        FitHeight = true;

        //圆角
        BorderRadius = new Vector4(4);

        //内边距
        Padding = new Margin(8f);
        Enabled = true;
        
        BorderRadius = new Vector4(30);

        RegisterSkillSlot();
        base.OnInitialize();
    }
    
    protected override SkillSlot CreateSkillSlot(SkillIcon icon = null)
    {
        return new TestSkillSlot(icon);
    }
    void RegisterSkillSlot()
    {
        for (int i = 0; i < MaxSkillSlot; i++)
        {
            var slotUI = CreateSkillSlot().Join(this);
            //slotUI.SlotIndex = i;
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