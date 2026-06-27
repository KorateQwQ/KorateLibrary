using System;
using System.Collections.Generic;
using KL.SkillSystem.AbstractClass;
using SilkyUIFramework;
using SilkyUIFramework.Elements;
using SilkyUIFramework.Extensions;
using SilkyUIFramework.Layout;

namespace KL.SkillSystem.SilkyUI;

public class SkillUnlockFooterUI : UIElementGroup
{
    private readonly SUIScrollView _itemScrollView;
    private readonly UIElementGroup _itemContainer;
    private readonly UITextView _skillPointText;
    private readonly SkillUnlockButton _unlockButton;
    private Skill _skill;
    private PanelSkillIcon _skillIcon;

    public SkillUnlockFooterUI()
    {
        FlexDirection = FlexDirection.Column;
        FitWidth = false;
        FitHeight = false;
        Width = new Dimension(percent: 1f);
        Height = new Dimension(120f);
        Padding = new Margin(10f, 12f, 10f, 10f);
        BackgroundColor = Color.Black * 0.25f;
        Border = 1f;
        BorderColor = Color.White * 0.15f;
        BorderRadius = new Vector4(6f);
        Gap = new Size(0f, 8f);
        IgnoreMouseInteraction = false;
        SetTop(10);

        _itemScrollView = new DragScrollView(Direction.Horizontal)
        {
            Width = new Dimension(percent: 1f),
            Height = new Dimension(58f),
            FitWidth = false,
            FitHeight = false,
            Padding = new Margin(0f),
            BackgroundColor = Color.Transparent,
            Border = 0f,
            Gap = new Vector2(6f),
        }.Join(this);
        _itemScrollView.Mask.OverflowHidden = true;
        _itemScrollView.Container.Gap = new Vector2(6f, 0f);
        _itemContainer = _itemScrollView.Container;
        _itemContainer.IgnoreMouseInteraction = true;
        _itemContainer.FitWidth = true;

        UIElementGroup bottomBar = new UIElementGroup
        {
            Width = new Dimension(percent: 1f),
            Height = new Dimension(40f),
            FitWidth = false,
            FitHeight = false,
        }.Join(this);

        _skillPointText = new UITextView
        {
            Width = new Dimension(120f),
            Height = new Dimension(28f),
            FitWidth = false,
            FitHeight = false,
            TextAlign = new Vector2(0f, 1f),
            TextScale = 0.3f,
            TextColor = new Color(255, 255, 255),
            TextBorder = 0f,
            TextBorderColor = Color.Transparent,
            BackgroundColor = Color.Transparent,
            Font = FontManager.HarmonyOS_Sans_SC.Value,
        }.Join(bottomBar);
        _skillPointText.SetLeft(0f, 0f);
        _skillPointText.SetTop(8f, 0f);

        _unlockButton = new SkillUnlockButton().Join(bottomBar);
        _unlockButton.SetLeft(10,0.0f);
        _unlockButton.SetTop(4f, 0f);
        _unlockButton.OnClick += TryUnlockCurrentSkill;
        
    }

    public void SetSkill(Skill skill, PanelSkillIcon skillIcon = null)
    {
        _skill = skill;
        _skillIcon = skillIcon;
        Refresh();
    }

    public void Refresh()
    {
        List<SkillUnlockItem> items = new();
        int skillPointCost = 0;

        if (_skill?.ModSkill != null)
        {
            CollectUnlockRequirement(_skill.ModSkill.UnlockCondition, items, ref skillPointCost);
        }

        RefreshItemIcons(items);
        RefreshSkillPointText(skillPointCost);

        bool skillNeedsUnlock = _skill?.BasicStatus == Skill.SKillBasicStatus.Lock;
        //_unlockButton.vi = skillNeedsUnlock;
        _unlockButton.IgnoreMouseInteraction = !skillNeedsUnlock;
        //Visible = _skill != null;

    }

    private void RefreshSkillPointText(int skillPointCost)
    {
        int currentSkillPoint = _skillIcon?.SkillPanelUI?.GetCurrentSkillPoint() ?? 0;
        bool notEnoughSkillPoint = skillPointCost > currentSkillPoint;
        string skillPointCostText = notEnoughSkillPoint ? $"[c/ff5555:{skillPointCost}]" : skillPointCost.ToString();

        _skillPointText.Text = $"SP: {skillPointCostText}/{currentSkillPoint}";
        _skillPointText.TextColor = new Color(255, 255, 255);
    }

    private void RefreshItemIcons(List<SkillUnlockItem> items)
    {
        _itemContainer.RemoveAllChildren();
        if (items.Count == 0)
        {
            new UITextView
            {
                Width = new Dimension(percent: 1f),
                Height = new Dimension(48f),
                FitWidth = false,
                FitHeight = false,
                Text = "无物品需求",
                TextAlign = new Vector2(0f, 0.5f),
                TextScale = 0.26f,
                TextColor = Color.White * 0.7f,
                TextBorder = 0f,
                TextBorderColor = Color.Transparent,
                BackgroundColor = Color.Transparent,
                Font = FontManager.HarmonyOS_Sans_SC.Value,
            }.Join(_itemContainer);
            return;
        }

        const float itemWidth = 42f;
        const float itemHeight = 54f;
        const float rowHeight = 68f;
        const float itemGap = 6f;
        float rowWidth = items.Count * itemWidth + Math.Max(0, items.Count - 1) * itemGap;
        
        UIElementGroup row = new UIElementGroup
        {
            Width = new Dimension(rowWidth),
            Height = new Dimension(rowHeight),
            FitWidth = false,
            FitHeight = true,
            FlexDirection = FlexDirection.Row,
            CrossAlignment = CrossAlignment.Center,
            Gap = new Size(itemGap, 0f),
            BackgroundColor = Color.Transparent,
            Border = 0f,
            BorderColor = Color.Transparent,
            Padding = new Margin(0f),
            IgnoreMouseInteraction = true,
        }.Join(_itemContainer);
        row.SetLeft(-rowWidth * 0f, 0.0f);
        row.SetTop(0f, 0f);

        for (int i = 0; i < items.Count; i++)
        {
            new SkillUnlockItemIconUI(items[i])
            {
                CrossAlignment = CrossAlignment.Center,
            }.Join(row);
        }
        _itemScrollView.ScrollBar.BarColor = (Color.Black * 1f, Color.Black * 0.3f);
        _itemScrollView.ScrollBar.BackgroundColor = Color.Black * 0.25f;
    }

    private void TryUnlockCurrentSkill()
    {
        _skillIcon?.TryUnlockFromFooter();
        Refresh();
    }

    private static void CollectUnlockRequirement(SkillUnlockCondition condition, List<SkillUnlockItem> items, ref int skillPointCost)
    {
        switch (condition)
        {
            case null:
                break;
            case ItemSkillUnlockCondition itemCondition:
                items.AddRange(itemCondition.Items);
                break;
            case SkillPointUnlockCondition skillPointCondition:
                skillPointCost += skillPointCondition.SkillPointCost;
                break;
            case CompositeSkillUnlockCondition compositeCondition:
                foreach (SkillUnlockCondition childCondition in compositeCondition.Conditions)
                {
                    CollectUnlockRequirement(childCondition, items, ref skillPointCost);
                }
                break;
        }
    }

    protected override void Update(GameTime gameTime)
    {
        if (_skill != null)
        {
            //Refresh();
        }

        base.Update(gameTime);
    }
}

internal sealed class SkillUnlockItemIconUI : UIElementGroup
{
    private readonly SkillUnlockItem _unlockItem;
    private readonly SUIItemSlot _itemSlot;
    private readonly UITextView _stackText;

    public SkillUnlockItemIconUI(SkillUnlockItem unlockItem)
    {
        _unlockItem = unlockItem;
        Width = new Dimension(42f);
        Height = new Dimension(54f);
        FitWidth = false;
        FitHeight = false;
        BackgroundColor = Color.Transparent;
        Border = 0f;
        BorderColor = Color.Transparent;
        BorderRadius = new Vector4(0f);
        Padding = new Margin(0f);
        IgnoreMouseInteraction = true;

        Item displayItem = new();
        if (_unlockItem.ItemType > 0)
        {
            displayItem.SetDefaults(_unlockItem.ItemType);
            displayItem.stack = Math.Max(1, _unlockItem.Stack);
        }

        _itemSlot = new SUIItemSlot
        {
            Width = new Dimension(36f),
            Height = new Dimension(36f),
            FitWidth = false,
            FitHeight = false,
            BackgroundColor = Color.Black * 0.28f,
            Border = 1f,
            BorderColor = Color.White * 0.2f,
            BorderRadius = new Vector4(4f),
            Padding = new Margin(0f),
            IgnoreMouseInteraction = false,
            ItemInteractive = false,
            DisplayItemStack = false,
            DisplayItemInfo = true,
            Item = displayItem,
            ItemIconSizeLimit = 24f,
            ItemScale = 0.7f,
            ItemAlign = new Vector2(0.5f),
        }.Join(this);
        _itemSlot.SetLeft(3f, 0f);
        _itemSlot.SetTop(0f, 0f);

        _stackText = new UITextView
        {
            Width = new Dimension(percent: 1f),
            Height = new Dimension(14f),
            FitWidth = false,
            FitHeight = false,
            Text = $"x{_unlockItem.Stack}",
            TextAlign = new Vector2(0.5f, 0f),
            TextScale = 0.22f,
            TextColor = Color.White,
            TextBorder = 0f,
            TextBorderColor = Color.Transparent,
            BackgroundColor = Color.Transparent,
            Font = FontManager.HarmonyOS_Sans_SC.Value,
            IgnoreMouseInteraction = true
        }.Join(this);
        _stackText.SetLeft(-36f, 0f);
        _stackText.SetTop(0f, 0.2f);
    }
}

internal sealed class SkillUnlockButton : UIElementGroup
{
    private bool _pressed;
    private bool _hovered;
    private readonly UITextView _text;

    public event Action OnClick;

    public SkillUnlockButton()
    {
        Width = new Dimension(100f);
        Height = new Dimension(30f);
        FitWidth = false;
        FitHeight = false;
        BackgroundColor = new Color(60, 110, 180) * 0.85f;
        Border = 1;
        BorderColor = Color.White * 0.85f;
        BorderRadius = new Vector4(6f);
        Padding = new Margin(0f);
        IgnoreMouseInteraction = false;

        _text = new UITextView
        {
            Width = new Dimension(percent: 1f),
            Height = new Dimension(percent: 1f),
            FitWidth = false,
            FitHeight = false,
            Text = "解锁",
            TextAlign = new Vector2(0.5f),
            TextScale = 0.3f,
            TextColor = Color.White,
            TextBorder = 0f,
            TextBorderColor = Color.Transparent,
            BackgroundColor = Color.Transparent,
            Font = FontManager.HarmonyOS_Sans_SC.Value,
        }.Join(this);
        _text.IgnoreMouseInteraction = true;
    }

    public override void OnMouseEnter(UIMouseEvent evt)
    {
        _hovered = true;
        base.OnMouseEnter(evt);
    }

    public override void OnMouseLeave(UIMouseEvent evt)
    {
        _hovered = false;
        _pressed = false;
        base.OnMouseLeave(evt);
    }

    public override void OnLeftMouseDown(UIMouseEvent evt)
    {
        _pressed = true;
        base.OnLeftMouseDown(evt);
    }

    public override void OnLeftMouseUp(UIMouseEvent evt)
    {
        bool clicked = _pressed && GetElementAt(Main.MouseScreen) == this;
        _pressed = false;
        if (clicked)
        {
            OnClick?.Invoke();
        }

        base.OnLeftMouseUp(evt);
    }

    protected override void Update(GameTime gameTime)
    {
        Color baseColor = new Color(255, 255, 255) * 0.25f;
        if (_hovered)
        {
            baseColor = new Color(255, 255, 255) * 0.5f;
        }

        if (_pressed)
        {
            baseColor = new Color(40, 90, 160) * 0.95f;
        }
        
        BackgroundColor = baseColor;
        base.Update(gameTime);
    }
}