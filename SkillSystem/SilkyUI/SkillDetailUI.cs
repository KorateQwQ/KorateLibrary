using SilkyUIFramework;
using SilkyUIFramework.Attributes;
using SilkyUIFramework.Elements;
using SilkyUIFramework.Extensions;
using SilkyUIFramework.Layout;

namespace KL.SkillSystem.SilkyUI;
//暂时没用上。
//[RegisterUI("Vanilla: Radial Hotbars", "KorateLibrary: SkillBarUI", int.MinValue)]
public class SkillDetailUI : BaseBody
{
    protected override void OnInitialize()
    {
        SetLeft(alignment:0.5f);
        SetTop(alignment:0.15f);

        //排列方向
        FlexDirection = FlexDirection.Row;
        
        //自适应子元素大小
        FitWidth = true;
        FitHeight = true;

        //圆角
        BorderRadius = new Vector4(4);

        //内边距
        Padding = new Margin(8f);
        

        PreviewSkillSlotUI previewSkillIcon = new PreviewSkillSlotUI().Join(this);
        previewSkillIcon.SetSize(40,40);
        previewSkillIcon.BackgroundColor = Color.Black*0.5f;
        previewSkillIcon.FitHeight = false;
        previewSkillIcon.FitWidth = false;
        
        base.OnInitialize();
    }
}