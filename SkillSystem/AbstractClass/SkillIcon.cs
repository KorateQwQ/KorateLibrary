using KL.SkillSystem.SilkyUI;
using SilkyUIFramework;
using SilkyUIFramework.Elements;

namespace KL.SkillSystem.AbstractClass;

public abstract class SkillIcon(Skill skill) : SUIImage,IDraggableUI
{
    public UIView DragUI => this;

    public bool IsDragging { get; set; }
    
    public Vector2 LastMousePosition { get; set; } = Vector2.Zero;
    
    protected static Effect CDEffect;
    
    public Skill Skill { get; set; }

    public bool InCD => Skill?.CurrentCD>0;
    int FlashTime { get; set; }

    public virtual bool CanDragAt(Vector2 mousePosition)
    {
        if(Skill?.ModSkill == null)return false;
        
        return Skill.ModSkill.CanDragInSkillBar();
    }

    public void UpdateDrag()
    {
        if (IsDragging)
        {
            //DoSomething
        }
    }
        
    public override void OnLeftMouseDown(UIMouseEvent evt)
    {
        ((IDraggableUI)this).StartDrag(Main.MouseScreen);
        base.OnLeftMouseDown(evt);
    }

    public override void OnLeftMouseUp(UIMouseEvent evt)
    {
        ((IDraggableUI)this).StopDrag();
    }
    
    
    protected override void OnInitialize()  
    {
        Skill = skill;
        CDEffect ??= ModContent.Request<Effect>("KL/Effects/Content/CDEffect", AssetRequestMode.ImmediateLoad).Value;
        
        Texture2D = Skill.SkillIcon;
        
        //自适应子元素大小
        FitWidth = false;
        FitHeight = false;

        //圆角
        BorderRadius = new Vector4(4);

        //内边距
        Padding = new Margin(8);
        ImageAlign = new Vector2(0.5f);
        base.OnInitialize();
    }

    protected override void Update(GameTime gameTime)
    {
        UpdateDrag(); // 调用接口的拖拽更新
        /*
        ImageAlign = new Vector2(0.5f);
        BackgroundColor = Color.Black*0.5f;
        */


        if (skill != null)
        {
            if (InCD && Skill.CurrentCD == 0)
            {
                FlashTime = 30;
            }

            if (FlashTime > 0) FlashTime--;
        }

        base.Update(gameTime);
    }

    public Vector2 GetDrawCenter()
    {
        var position = InnerBounds.Position;
        var size = (Vector2)InnerBounds.Size;

        var imageOriginalSize = ImageOriginalSize;
        var completeOffset = ImageOffset + size * ImagePercent + (size - imageOriginalSize * ImageScale) * ImageAlign;

        return position + completeOffset + new Vector2(Texture2D.Width() / 2f) * ImageScale;
    }

    public Vector2 GetDrawSize()
    {
        return ImageScale;
    }
    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        if (Skill == null) return;

        if (Parent is SkillSlot slot)
        {
            if (IsDragging)
            {
                slot.SkillIsDragging = true;
            }
            else slot.SkillIsDragging = false;
        }
        
        var finalPosition = GetDrawCenter();
        
        CDEffect.Parameters["time"].SetValue(1-(Skill.CurrentCD/Skill.MaxCD));
        CDEffect.Parameters["flashTime"].SetValue(FlashTime/30f);

        EndBeginDrawUI(0,1,true,null,CDEffect);
        if(Skill.ModSkill.PreDrawSkillIcon(finalPosition, GetDrawSize(),Color.White,CDEffect))base.Draw(gameTime, spriteBatch);
        Skill.ModSkill.PostDrawSkillIcon(finalPosition,GetDrawSize(),Color.White,CDEffect);
        EndBeginDrawUI();
        
    }
}