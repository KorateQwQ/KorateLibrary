using SilkyUIFramework.Elements;

namespace KL.SkillSystem.SilkyUI;

public class PanelSkillLevelText : UITextView
{
    public int Level = 0;
    public bool ShouldShow = false;
    public SkillPanelUI SkillPanelUI;
    
    protected override void OnInitialize()
    {
        Font = FontManager.HarmonyOS_Sans_SC.Value;
        SetSize(70,20);
        SetTop(-5);
        SetLeft(5);
        TextScale = 0.5f;
        BackgroundColor = Color.Black*0f;
        FitHeight = false;
        FitWidth = false;
        BorderRadius = new Vector4(4);
        IgnoreMouseInteraction = true;
        TextAlign = new Vector2(0.4f,0.5f);
        TextBorder = 0.0f;
        TextColor = Color.White;
        TextScale = 0.4f;
        TextBorderColor = Color.White*0;
        base.OnInitialize();
    }

    protected override void Update(GameTime gameTime)
    {
        Text = $"Lv. {Level}";
        SetTop(2,0);
        SetLeft(2,0.0f);
        TextAlign = new Vector2(0.5f,0.5f);
        TextScale = 0.2f;
        //BackgroundColor = Color.Black*1f;
        SetSize(50,15);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        BackgroundColor = SkillPanelUI.SkillLevelHintBackgroundColor;
        BorderColor = SkillPanelUI.SkillLevelHintBorderColor;
        Border = SkillPanelUI.SkillLevelHintBorder;

        if (!ShouldShow) return;
        /*float color = 0.5f; 
        var position = Bounds.Position;
        //PrintText(SkillPanelUI==null?"null":"not null");
        EndBeginDrawUI();
        float skew = 0.1f;
        DrawRectangle(position+ new Vector2(Width.Pixels/2f,Height.Pixels/2f),new Vector2(52,14),
            SkillPanelUI.SkillLevelHintBackgroundColor,0,4,0,SkillPanelUI.SkillLevelHintBorder,SkillPanelUI.SkillLevelHintBorderColor,true);
        EndBeginDrawUI();*/
        base.Draw(gameTime, spriteBatch);

    }
}