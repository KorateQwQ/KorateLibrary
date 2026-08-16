using SilkyUIFramework;
using SilkyUIFramework.Elements;
using SilkyUIFramework.Extensions;
using SilkyUIFramework.Layout;

namespace KL.UI;

/// <summary>
/// 可配置、可继承的双状态切换按钮。
/// </summary>
public class ToggleButton : UIElementGroup
{
    private bool _isOn;
    private bool _pressed;
    private bool _initialized;
    private float _visualProgress;

    protected UIElementGroup Thumb { get; private set; }

    public virtual Vector2 ToggleSize { get; set; } = new(48f, 26f);
    public virtual float ThumbDiameter { get; set; } = 20f;
    public virtual float ThumbInset { get; set; } = 3f;
    public virtual float AnimationDuration { get; set; } = 0.15f;

    public virtual Color OffBackgroundColor { get; set; } = Color.White;
    public virtual Color OnBackgroundColor { get; set; } = new(76, 175, 80);
    public virtual Color OffThumbColor { get; set; } = new(128, 118, 108);
    public virtual Color OnThumbColor { get; set; } = new(128, 118, 108);

    public bool IsOn
    {
        get => _isOn;
        set => SetValue(value);
    }

    public event Action<bool> ValueChanged;

    protected override void OnInitialize()
    {
        FitWidth = false;
        FitHeight = false;
        IgnoreMouseInteraction = false;
        Padding = new Margin(0f);
        Border = 0f;
        BorderColor = Color.Transparent;
        SetSize(ToggleSize.X, ToggleSize.Y);
        BorderRadius = new Vector4(ToggleSize.Y * 0.5f);

        Thumb = CreateThumb().Join(this);
        Thumb.FitWidth = false;
        Thumb.FitHeight = false;
        Thumb.IgnoreMouseInteraction = true;
        Thumb.Padding = new Margin(0f);
        Thumb.Border = 0f;
        Thumb.BorderColor = Color.Transparent;
        Thumb.SetSize(ThumbDiameter, ThumbDiameter);
        Thumb.SetTop(ThumbInset, 0f);
        Thumb.BorderRadius = new Vector4(ThumbDiameter * 0.5f);

        _visualProgress = _isOn ? 1f : 0f;
        ApplyVisualState(_visualProgress);
        _initialized = true;
        base.OnInitialize();
    }

    protected virtual UIElementGroup CreateThumb()
    {
        return new UIElementGroup();
    }

    protected virtual bool CanToggle()
    {
        return true;
    }

    public override void OnLeftMouseDown(UIMouseEvent evt)
    {
        _pressed = CanToggle();
        base.OnLeftMouseDown(evt);
    }

    public override void OnLeftMouseUp(UIMouseEvent evt)
    {
        bool clicked = _pressed && GetElementAt(Main.MouseScreen) == this;
        _pressed = false;
        if (clicked)
        {
            Toggle();
        }

        base.OnLeftMouseUp(evt);
    }

    public override void OnMouseLeave(UIMouseEvent evt)
    {
        _pressed = false;
        base.OnMouseLeave(evt);
    }

    protected virtual void Toggle()
    {
        SetValue(!_isOn);
    }

    public virtual void SetValue(bool value, bool notify = true, bool animate = true)
    {
        if (_isOn == value)
        {
            if (_initialized && !animate)
            {
                _visualProgress = value ? 1f : 0f;
                ApplyVisualState(_visualProgress);
            }

            return;
        }

        _isOn = value;
        if (_initialized && !animate)
        {
            _visualProgress = value ? 1f : 0f;
            ApplyVisualState(_visualProgress);
        }

        if (notify)
        {
            OnValueChanged(value);
        }
    }

    protected virtual void OnValueChanged(bool value)
    {
        ValueChanged?.Invoke(value);
    }

    protected override void Update(GameTime gameTime)
    {
        float target = _isOn ? 1f : 0f;
        if (_visualProgress != target)
        {
            float duration = Math.Max(AnimationDuration, 0.001f);
            float step = (float)gameTime.ElapsedGameTime.TotalSeconds / duration;
            _visualProgress = MoveTowards(_visualProgress, target, step);
            ApplyVisualState(_visualProgress);
        }

        base.Update(gameTime);
    }

    protected virtual void ApplyVisualState(float progress)
    {
        float easedProgress = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(progress, 0f, 1f));
        float travelDistance = Math.Max(0f, ToggleSize.X - ThumbDiameter - ThumbInset * 2f);

        BackgroundColor = Color.Lerp(OffBackgroundColor, OnBackgroundColor, easedProgress);
        if (Thumb != null)
        {
            Thumb.BackgroundColor = Color.Lerp(OffThumbColor, OnThumbColor, easedProgress);
            Thumb.SetLeft(ThumbInset + travelDistance * easedProgress, 0f);
        }
    }

    private static float MoveTowards(float current, float target, float maxDelta)
    {
        if (Math.Abs(target - current) <= maxDelta)
        {
            return target;
        }

        return current + Math.Sign(target - current) * maxDelta;
    }
}
