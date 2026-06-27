using KL.Utils;
using SilkyUIFramework;
using SilkyUIFramework.Elements;

namespace KL.SkillSystem.SilkyUI;

public class AddOrDeleteButton(bool isAddButton) : SUIImage,IDraggableUI
{
    public bool IsDragging { get; set; }
    public UIView DragUI => this;
    public Vector2 LastMousePosition { get; set; } = Vector2.Zero;

    private float scale = 1f;
    int releaseAnimTime = 10;
    int hideTime = 0;
    bool shouldShow = false;
    enum state
    {
        Press,
        Release
    }
    state currentState = state.Release;
    
    public SkillPanelUI SkillPanelUI = null;
    
    public event Action OnClick;
    
    public override void OnMouseEnter(UIMouseEvent evt)
    {
        base.OnMouseEnter(evt);
    }

    public override void OnMouseLeave(UIMouseEvent evt)
    {
        base.OnMouseLeave(evt);
    }

    public override void OnLeftMouseDown(UIMouseEvent evt)
    {
        currentState = state.Press;
        base.OnLeftMouseDown(evt);
    }
    
    public override void OnLeftMouseUp(UIMouseEvent evt)
    {
        currentState = state.Release;
        releaseAnimTime = 0;
        if (GetElementAt(Main.MouseScreen)==this)
        {
            OnClick?.Invoke();
        }

        base.OnLeftMouseUp(evt);
    }

    public void OnShow()
    {
        shouldShow = true;
    }

    public void OnHide()
    {
        shouldShow = false;
    }

    protected override void OnInitialize()
    {
        Texture2D =isAddButton?  ModContent.Request<Texture2D>("KL/SkillSystem/AddButtionIcon", AssetRequestMode.ImmediateLoad): ModContent.Request<Texture2D>("KL/SkillSystem/DeleteButtionIcon", AssetRequestMode.ImmediateLoad);
        
        BackgroundColor = Color.White*0.2f;
        FitHeight = false;
        FitWidth = false;
        BorderColor = Color.Black;
        Border = 1;
        BorderRadius = new Vector4(8);
        Padding = new Margin(0,0);
        ImageAlign = new Vector2(0.5f);

        
        SetTop(pixels:15,alignment: 0);
        float c = 0.5f;
        BackgroundColor = Color.Black*0.0f;
        Border = 0;
        BorderRadius = new Vector4(0);

        SetSize(20, 12);
        scale = MathHelper.Clamp(scale, 0.8f, 1.2f);
        ImageScale = new Vector2(0.5f)*scale;
        base.OnInitialize();
    }

    protected override void Update(GameTime gameTime)
    {
        float alpha = hideTime / 10f;

        if (!isAddButton) alpha = KLMathF.ClampLerp(0, 1, (hideTime - 5) / 10f);
        SetLeft(pixels:-10f+alpha*5,alignment: 0);


        if (shouldShow)
        {
            if (hideTime < 15)
            {
                hideTime++;
            }

            if (isAddButton) hideTime = Math.Min(hideTime, 10);
        }
        else
        {
            if (hideTime > 0)
            {
                hideTime--;
            }
        }

        IgnoreMouseInteraction = hideTime <= 0;
        
        if (currentState == state.Press)
        {
            if (scale > 0.8f)
            {
                scale -= 0.1f;
            }
        }
        else if (currentState == state.Release)
        {
            if (releaseAnimTime <= 10)
            {
                List<FrameInfo> frameInfos = new List<FrameInfo>(2)
                {
                    new (0.8f,1.2f,4),
                    new (1.1f,1f,10)
                };
                scale = GetFrameValue(frameInfos, releaseAnimTime);
                //PrintText(releaseAnimTime+" "+scale);
                releaseAnimTime++;
            }
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        var position = InnerBounds.Position;
        
        float alpha = hideTime / 10f;
        if (!isAddButton) alpha = KLMathF.ClampLerp(0, 1, (hideTime - 5) / 10f);

        float color = 0.5f;

        EndBeginDrawUI();

        ImageColor=Color.White*alpha;
        DrawRectangle(position+ new Vector2(Width.Pixels/2f,Height.Pixels/2f),new Vector2(20,12)*scale,SkillPanelUI.SkillAddButtonBackgroundColor*alpha,
            0,8*scale,0,SkillPanelUI.SkillAddButtonBorder*scale,SkillPanelUI.SkillAddButtonBorderColor*alpha,true);
        EndBeginDrawUI();

        base.Draw(gameTime, spriteBatch);
    }
}