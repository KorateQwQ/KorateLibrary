using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Localization;

namespace KL.SkillSystem;

/// <summary>
/// 单个物品解锁需求
/// </summary>
public readonly struct SkillUnlockItem
{
    public int ItemType { get; }
    public int Stack { get; }

    public SkillUnlockItem(int itemType, int stack)
    {
        ItemType = itemType;
        Stack = Math.Max(stack, 1);
    }

    public string GetDescription()
    {
        return $"{Lang.GetItemNameValue(ItemType)} x{Stack}";
    }
}

/// <summary>
/// 技能解锁条件基类
/// </summary>
public abstract class SkillUnlockCondition
{
    public static SkillUnlockCondition None { get; } = new NoSkillUnlockCondition();

    public bool TryUnlock(KLSkillModPlayer skillPlayer, ModSkill modSkill)
    {
        Player player = skillPlayer?.Player;
        if (skillPlayer == null || player == null || !player.active || modSkill?.Skill == null)
        {
            return false;
        }

        if (!CanUnlock(skillPlayer, modSkill))
        {
            return false;
        }

        Consume(skillPlayer, modSkill);
        return true;
    }

    public abstract bool CanUnlock(KLSkillModPlayer skillPlayer, ModSkill modSkill);

    public virtual void Consume(KLSkillModPlayer skillPlayer, ModSkill modSkill)
    {
    }

    public abstract string GetDescription(KLSkillModPlayer skillPlayer, ModSkill modSkill);

    public static SkillUnlockCondition ByItems(params SkillUnlockItem[] items)
    {
        return new ItemSkillUnlockCondition(items);
    }

    public static SkillUnlockCondition BySkillPoint(int skillPoint)
    {
        return new SkillPointUnlockCondition(skillPoint);
    }

    public static SkillUnlockCondition ByItemsAndSkillPoint(int skillPoint, params SkillUnlockItem[] items)
    {
        return new CompositeSkillUnlockCondition(new ItemSkillUnlockCondition(items), new SkillPointUnlockCondition(skillPoint));
    }

    public static SkillUnlockCondition Custom(Func<KLSkillModPlayer, ModSkill, bool> canUnlock, Action<KLSkillModPlayer, ModSkill> consume = null,
        Func<KLSkillModPlayer, ModSkill, string> description = null)
    {
        return new CustomSkillUnlockCondition(canUnlock, consume, description);
    }

    protected static int CountItem(Player player, int itemType)
    {
        int total = 0;
        for (int i = 0; i < player.inventory.Length; i++)
        {
            Item item = player.inventory[i];
            if (item != null && !item.IsAir && item.type == itemType)
            {
                total += item.stack;
            }
        }

        return total;
    }

    protected static void ConsumeItem(Player player, int itemType, int amount)
    {
        int remaining = amount;
        for (int i = 0; i < player.inventory.Length && remaining > 0; i++)
        {
            Item item = player.inventory[i];
            if (item == null || item.IsAir || item.type != itemType)
            {
                continue;
            }

            int consumeCount = Math.Min(item.stack, remaining);
            item.stack -= consumeCount;
            remaining -= consumeCount;

            if (item.stack <= 0)
            {
                item.TurnToAir();
            }
        }
    }
}

public sealed class NoSkillUnlockCondition : SkillUnlockCondition
{
    public override bool CanUnlock(KLSkillModPlayer skillPlayer, ModSkill modSkill)
    {
        return true;
    }

    public override string GetDescription(KLSkillModPlayer skillPlayer, ModSkill modSkill)
    {
        return "无条件";
    }
}

/// <summary>
/// 多个物品解锁条件
/// </summary>
public sealed class ItemSkillUnlockCondition : SkillUnlockCondition
{
    public IReadOnlyList<SkillUnlockItem> Items { get; }

    public ItemSkillUnlockCondition(params SkillUnlockItem[] items)
    {
        Items = items ?? Array.Empty<SkillUnlockItem>();
    }

    public override bool CanUnlock(KLSkillModPlayer skillPlayer, ModSkill modSkill)
    {
        Player player = skillPlayer?.Player;
        if (player == null)
        {
            return false;
        }

        for (int i = 0; i < Items.Count; i++)
        {
            SkillUnlockItem item = Items[i];
            if (CountItem(player, item.ItemType) < item.Stack)
            {
                return false;
            }
        }

        return true;
    }

    public override void Consume(KLSkillModPlayer skillPlayer, ModSkill modSkill)
    {
        Player player = skillPlayer?.Player;
        if (player == null)
        {
            return;
        }

        for (int i = 0; i < Items.Count; i++)
        {
            SkillUnlockItem item = Items[i];
            ConsumeItem(player, item.ItemType, item.Stack);
        }
    }

    public override string GetDescription(KLSkillModPlayer skillPlayer, ModSkill modSkill)
    {
        if (Items.Count == 0)
        {
            return string.Empty;
        }

        return string.Join("\n", Items.Select(static item => $"- {item.GetDescription()}"));
    }
}

/// <summary>
/// SP 点数解锁条件
/// </summary>
public sealed class SkillPointUnlockCondition : SkillUnlockCondition
{
    public int SkillPointCost { get; }

    public SkillPointUnlockCondition(int skillPointCost)
    {
        SkillPointCost = Math.Max(skillPointCost, 0);
    }

    public override bool CanUnlock(KLSkillModPlayer skillPlayer, ModSkill modSkill)
    {
        if (SkillPointCost <= 0)
        {
            return true;
        }

        return skillPlayer != null && skillPlayer.CanCostSkillPoint(SkillPointCost);
    }

    public override void Consume(KLSkillModPlayer skillPlayer, ModSkill modSkill)
    {
        if (SkillPointCost <= 0)
        {
            return;
        }

        skillPlayer?.TryCostSkillPoint(SkillPointCost);
    }

    public override string GetDescription(KLSkillModPlayer skillPlayer, ModSkill modSkill)
    {
        if (SkillPointCost <= 0)
        {
            return string.Empty;
        }

        return $"- SP x{SkillPointCost}";
    }
}

/// <summary>
/// 组合解锁条件，所有子条件都满足时才可解锁
/// </summary>
public sealed class CompositeSkillUnlockCondition : SkillUnlockCondition
{
    public IReadOnlyList<SkillUnlockCondition> Conditions { get; }

    public CompositeSkillUnlockCondition(params SkillUnlockCondition[] conditions)
    {
        Conditions = conditions?.Where(static condition => condition != null).ToArray() ?? Array.Empty<SkillUnlockCondition>();
    }

    public override bool CanUnlock(KLSkillModPlayer skillPlayer, ModSkill modSkill)
    {
        for (int i = 0; i < Conditions.Count; i++)
        {
            if (!Conditions[i].CanUnlock(skillPlayer, modSkill))
            {
                return false;
            }
        }

        return true;
    }

    public override void Consume(KLSkillModPlayer skillPlayer, ModSkill modSkill)
    {
        for (int i = 0; i < Conditions.Count; i++)
        {
            Conditions[i].Consume(skillPlayer, modSkill);
        }
    }

    public override string GetDescription(KLSkillModPlayer skillPlayer, ModSkill modSkill)
    {
        if (Conditions.Count == 0)
        {
            return "无条件";
        }

        return string.Join("\n", Conditions.Select(condition => condition.GetDescription(skillPlayer, modSkill)).Where(static text => !string.IsNullOrWhiteSpace(text)));
    }
}

/// <summary>
/// 自定义解锁条件
/// </summary>
public sealed class CustomSkillUnlockCondition : SkillUnlockCondition
{
    private readonly Func<KLSkillModPlayer, ModSkill, bool> _canUnlock;
    private readonly Action<KLSkillModPlayer, ModSkill> _consume;
    private readonly Func<KLSkillModPlayer, ModSkill, string> _description;

    public CustomSkillUnlockCondition(Func<KLSkillModPlayer, ModSkill, bool> canUnlock, Action<KLSkillModPlayer, ModSkill> consume = null,
        Func<KLSkillModPlayer, ModSkill, string> description = null)
    {
        _canUnlock = canUnlock ?? throw new ArgumentNullException(nameof(canUnlock));
        _consume = consume;
        _description = description;
    }

    public override bool CanUnlock(KLSkillModPlayer skillPlayer, ModSkill modSkill)
    {
        return _canUnlock(skillPlayer, modSkill);
    }

    public override void Consume(KLSkillModPlayer skillPlayer, ModSkill modSkill)
    {
        _consume?.Invoke(skillPlayer, modSkill);
    }

    public override string GetDescription(KLSkillModPlayer skillPlayer, ModSkill modSkill)
    {
        return _description?.Invoke(skillPlayer, modSkill) ?? "满足自定义条件";
    }
}