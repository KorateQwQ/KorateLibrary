using KL.Extensions;
using KL.SkillSystem.SilkyUI;
using KL.Utils;
using SilkyUIFramework;
using SilkyUIFramework.Elements;

namespace KL.SkillSystem.AbstractClass;

public class DragScrollView : SUIScrollView
{
    private bool _isDraggingScroll;
    private Vector2 _lastMouseScreen;
    private float _elasticOffset;
    private float _elasticVelocity;
    private Color _visibleScrollBarBackgroundColor;
    private (Color Default, Color Hover) _visibleScrollBarBarColor;
    private bool _hasVisibleScrollBarColors;
    private bool _scrollBarHiddenByOverflow;

    public bool DragScrollEnabled { get; set; } = true;
    public float ElasticLimit { get; set; } = 24f;
    public float ElasticDragFactor { get; set; } = 0.42f;
    public float ElasticReturnSpeed { get; set; } = 96f;
    public float ElasticReturnDamping { get; set; } = 0.8f;
    
    static Texture2D triangleTexture;

    protected override void OnInitialize()
    {
        triangleTexture ??= AssetManager.GetTexture("KL.SkillSystem.AbstractClass.Icon_Triangle");
        base.OnInitialize();
    }

    public DragScrollView(Direction direction = Direction.Vertical) : base(direction)
    {
    }

    public override void OnLeftMouseDown(UIMouseEvent evt)
    {
        if (DragScrollEnabled && !IsScrollBarTarget() && !IsHigherPriorityDragTarget())
        {
            _isDraggingScroll = true;
            _lastMouseScreen = Main.MouseScreen;
        }

        base.OnLeftMouseDown(evt);
    }

    public override void OnLeftMouseUp(UIMouseEvent evt)
    {
        StopDragScroll();
        base.OnLeftMouseUp(evt);
    }

    public override void OnMouseLeave(UIMouseEvent evt)
    {
        if (!Main.mouseLeft)
        {
            StopDragScroll();
        }

        base.OnMouseLeave(evt);
    }

    protected override void Update(GameTime gameTime)
    {
        if (_isDraggingScroll)
        {
            if (!Main.mouseLeft)
            {
                StopDragScroll();
            }
            else
            {
                Vector2 mouseDelta = Main.MouseScreen - _lastMouseScreen;
                if (mouseDelta != Vector2.Zero)
                {
                    float axisDelta = Direction == Direction.Horizontal ? mouseDelta.X : mouseDelta.Y;
                    HandleElasticDrag(axisDelta);
                    _lastMouseScreen = Main.MouseScreen;
                }
            }
        }
        else
        {
            UpdateElasticReturn(gameTime);
        }

        base.Update(gameTime);
        UpdateScrollBarVisibility();
    }

    private void StopDragScroll()
    {
        _isDraggingScroll = false;
    }

    private void HandleElasticDrag(float mouseAxisDelta)
    {
        if (mouseAxisDelta == 0f)
        {
            return;
        }

        float remainingMouseDelta = mouseAxisDelta;

        if (_elasticOffset != 0f)
        {
            if (ShouldContinueElasticDrag(remainingMouseDelta))
            {
                AddElasticOffset(remainingMouseDelta);
                ApplyElasticOffset();
                return;
            }

            _elasticOffset = 0f;
            _elasticVelocity = 0f;
            ApplyElasticOffset();
        }

        float currentScroll = GetCurrentAxisScrollPosition();
        float scrollRange = GetCurrentAxisScrollRange();
        float desiredScroll = currentScroll - remainingMouseDelta;
        float clampedScroll = MathHelper.Clamp(desiredScroll, 0f, scrollRange);

        if (clampedScroll != currentScroll)
        {
            SetCurrentAxisScrollPosition(clampedScroll);
        }

        float overflow = desiredScroll - clampedScroll;
        if (overflow != 0f)
        {
            AddElasticOffset(-overflow);
            ApplyElasticOffset();
        }
    }

    private void AddElasticOffset(float mouseAxisDelta)
    {
        float normalizedOffset = MathHelper.Clamp(Math.Abs(_elasticOffset) / Math.Max(1f, ElasticLimit), 0f, 1f);
        float resistance = MathHelper.Lerp(ElasticDragFactor, ElasticDragFactor * 0.15f, normalizedOffset);
        _elasticOffset = MathHelper.Clamp(_elasticOffset + mouseAxisDelta * resistance, -ElasticLimit, ElasticLimit);
        _elasticVelocity = 0f;
    }

    private bool ShouldContinueElasticDrag(float mouseAxisDelta)
    {
        if (Math.Sign(mouseAxisDelta) != Math.Sign(_elasticOffset))
        {
            return false;
        }

        float currentScroll = GetCurrentAxisScrollPosition();
        float scrollRange = GetCurrentAxisScrollRange();
        if (_elasticOffset > 0f)
        {
            return currentScroll <= 0f;
        }

        return currentScroll >= scrollRange;
    }

    private void UpdateElasticReturn(GameTime gameTime)
    {
        if (_elasticOffset == 0f)
        {
            return;
        }

        float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (deltaTime <= 0f)
        {
            ApplyElasticOffset();
            return;
        }

        float acceleration = -_elasticOffset * ElasticReturnSpeed;
        _elasticVelocity += acceleration * deltaTime;
        _elasticVelocity *= MathF.Pow(ElasticReturnDamping, deltaTime * 60f);
        _elasticOffset += _elasticVelocity * deltaTime;

        if (Math.Abs(_elasticOffset) < 0.1f && Math.Abs(_elasticVelocity) < 0.1f)
        {
            _elasticOffset = 0f;
            _elasticVelocity = 0f;
        }

        ApplyElasticOffset();
    }

    private float GetCurrentAxisScrollPosition()
    {
        return Direction == Direction.Horizontal ? ScrollBar.TargetScrollPosition.X : ScrollBar.TargetScrollPosition.Y;
    }

    private float GetCurrentAxisScrollRange()
    {
        return Direction == Direction.Horizontal ? ScrollBar.GetScrollRange().X : ScrollBar.GetScrollRange().Y;
    }

    private void SetCurrentAxisScrollPosition(float value)
    {
        Vector2 current = ScrollBar.CurrentScrollPosition;
        switch (Direction)
        {
            case Direction.Horizontal:
                current.X = value;
                break;
            default:
            case Direction.Vertical:
                current.Y = value;
                break;
        }

        ScrollBar.SetScrollPosition(current);
    }

    private void ApplyElasticOffset()
    {
        switch (Direction)
        {
            case Direction.Horizontal:
                Container.SetDragOffset(x: _elasticOffset, y: 0f);
                break;
            default:
            case Direction.Vertical:
                Container.SetDragOffset(x: 0f, y: _elasticOffset);
                break;
        }

        Container.RecalculatePosition();
    }

    private bool IsScrollBarTarget()
    {
        UIView target = GetElementAt(Main.MouseScreen);
        return target == ScrollBar;
    }

    private bool IsHigherPriorityDragTarget()
    {
        UIView target = GetElementAt(Main.MouseScreen);
        return target is PanelSkillIcon;
    }

    private void UpdateScrollBarVisibility()
    {
        if (HasAxisOverflow())
        {
            ShowScrollBarByOverflow();
            return;
        }

        HideScrollBarByOverflow();
    }

    private bool HasAxisOverflow()
    {
        Vector2 scrollRange = ScrollBar.GetScrollRange();
        return Direction == Direction.Horizontal ? scrollRange.X > 0f : scrollRange.Y > 0f;
    }

    private void HideScrollBarByOverflow()
    {
        if (!_scrollBarHiddenByOverflow)
        {
            CacheVisibleScrollBarColors();
        }

        _scrollBarHiddenByOverflow = true;
        ScrollBar.BackgroundColor = Color.Transparent;
        ScrollBar.BarColor = (Color.Transparent, Color.Transparent);
    }

    private void ShowScrollBarByOverflow()
    {
        if (_scrollBarHiddenByOverflow && _hasVisibleScrollBarColors)
        {
            ScrollBar.BackgroundColor = _visibleScrollBarBackgroundColor;
            ScrollBar.BarColor = _visibleScrollBarBarColor;
        }
        else if (!_scrollBarHiddenByOverflow)
        {
            CacheVisibleScrollBarColors();
        }

        _scrollBarHiddenByOverflow = false;
    }

    private void CacheVisibleScrollBarColors()
    {
        _visibleScrollBarBackgroundColor = ScrollBar.BackgroundColor;
        _visibleScrollBarBarColor = ScrollBar.BarColor;
        _hasVisibleScrollBarColors = true;
    }

    protected override void Draw(GameTime gameTime, SpriteBatch spriteBatch)
    {
        base.Draw(gameTime, spriteBatch);
    }

    public override void DrawChildren(GameTime gameTime, SpriteBatch sb)
    {
        base.DrawChildren(gameTime, sb);
        if (triangleTexture == null)
        {
            return;
        }

        Vector2 leftCenter = Bounds.Position + new Vector2(0f, Bounds.Height * 0.5f);
        Vector2 rightCenter = Bounds.Position + new Vector2(Bounds.Width, Bounds.Height * 0.5f);
        Vector2 topCenter = Bounds.Position + new Vector2(Bounds.Width * 0.5f, 0f);
        Vector2 bottomCenter = Bounds.Position + new Vector2(Bounds.Width * 0.5f, Bounds.Height);
        Vector2 arrowScale = Vector2.One * 0.3f;
        float arrowSwingOffset = MathF.Sin(Main.GlobalTimeWrappedHourly * 7f) * 2f;

        switch (Direction)
        {
            case Direction.Horizontal:
                if (!ScrollBar.HScrolledToTop)
                {
                    DrawInScreen(triangleTexture, leftCenter + new Vector2(-arrowSwingOffset, 0f), rotation: MathHelper.Pi, scale: arrowScale);
                }

                if (!ScrollBar.HScrolledToEnd)
                {
                    DrawInScreen(triangleTexture, rightCenter + new Vector2(arrowSwingOffset, 0f), rotation: 0f, scale: arrowScale);
                }
                break;
            default:
            case Direction.Vertical:
                if (!ScrollBar.VScrolledToTop)
                {
                    DrawInScreen(triangleTexture, topCenter + new Vector2(0f, -arrowSwingOffset), rotation: -MathHelper.PiOver2, scale: arrowScale);
                }

                if (!ScrollBar.VScrolledToEnd)
                {
                    DrawInScreen(triangleTexture, bottomCenter + new Vector2(0f, arrowSwingOffset), rotation: MathHelper.PiOver2, scale: arrowScale);
                }
                break;
        }
    }
}