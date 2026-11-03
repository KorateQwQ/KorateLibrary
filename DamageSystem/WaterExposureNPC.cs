using System;
using System.Collections.Generic;
using KL.Buffs.ElementalDebuff;
using KL.Extensions;
using KL.Utils;

namespace KL.DamageSystem;

/// <summary>水附着按付费脉冲积累，每个元素主体各持有一份状态；不从伤害推算潮湿。</summary>
public sealed class WaterExposureNPC : GlobalNPC
{
    public const int ExposureTicks = 180;
    public const int DurationTicks = 600;
    // 按十秒状态上限的百分比消耗：射线持续命中一秒足以移除满时长的异种状态。
    public const float ReactionFractionPerSecond = 1f;
    private readonly Dictionary<int, (int Ticks, uint LastHit)> exposure = new();
    private readonly Dictionary<(int Player, int Type, int Identity), (int Sequence, uint Tick)> pulses = new();
    private bool managesProgress;
    public override bool InstancePerEntity => true;

    public override void SetDefaults(NPC npc)
    {
        exposure.Clear();
        pulses.Clear();
        managesProgress = false;
    }

    public static bool IsWet(NPC npc) => FireBurnNPC.GetBurnAnchor(npc) is { } anchor
        && anchor.HasBuff<ElementalAffliction_Wet>();

    public static int ReactionTicks(int attachmentTicks) =>
        Math.Clamp((int)Math.Ceiling(DurationTicks * ReactionFractionPerSecond * attachmentTicks / 60d), 1, DurationTicks);

    public static bool ApplyHit(FireBurnHit hit, int player, int attachmentTicks, int projectileType, int identity, int sequence)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || player < 0 || player >= Main.maxPlayers
            || !Main.player[player].active || Main.player[player].dead || attachmentTicks <= 0 || attachmentTicks > ExposureTicks
            || identity < 0 || sequence <= 0 || hit.TargetIndex < 0 || hit.TargetIndex >= Main.maxNPCs
            || hit.AnchorIndex < 0 || hit.AnchorIndex >= Main.maxNPCs) return false;
        NPC target = Main.npc[hit.TargetIndex], anchor = Main.npc[hit.AnchorIndex];
        if (target == null || anchor is not { active: true, life: > 0 } || target.friendly || anchor.friendly
            || target.immortal || anchor.immortal
            || target.GetGlobalNPC<FireBurnNPC>().InstanceId != hit.TargetGeneration
            || anchor.GetGlobalNPC<FireBurnNPC>().InstanceId != hit.AnchorGeneration) return false;
        var state = anchor.GetGlobalNPC<WaterExposureNPC>();
        var key = (player, projectileType, identity);
        if (state.pulses.TryGetValue(key, out var previous) && sequence <= previous.Sequence) return false;
        state.pulses[key] = (sequence, Main.GameUpdateCount);
        if (!anchor.GetGlobalNPC<ElementalGlobalNPC>().TryBeginAttachment(anchor, ElementType.Water)) return false;
        state.managesProgress = true;
        // 一次水球/脉冲对同一主体只处理一次，灭火的这一击不会再积累潮湿。
        if (anchor.HasBuff<ElementalAffliction_Fire>() || hit.WasBurning)
        {
            state.exposure.Clear();
            ConsumeStatus(anchor, ModContent.BuffType<ElementalAffliction_Fire>(), ReactionTicks(attachmentTicks));
            anchor.GetGlobalNPC<FireBurnNPC>().RefreshProgress(anchor);
            state.RefreshProgress(anchor);
            return true;
        }
        int wet = ModContent.BuffType<ElementalAffliction_Wet>();
        int index = anchor.FindBuffIndex(wet);
        if (index >= 0)
        {
            // 沿用燃烧的续时方式：有效附着补回十秒，但不会越过上限。
            anchor.AddBuffToAnchorParts(wet, DurationTicks, forcedAddBuff: true);
        }
        else
        {
            state.exposure.TryGetValue(player, out var current);
            int accumulated = Main.GameUpdateCount - current.LastHit >= ElementBuildupBuffRegistry.DefaultIdleResetTime ? 0 : current.Ticks;
            accumulated = Math.Min(ExposureTicks, accumulated + attachmentTicks);
            state.exposure[player] = (accumulated, Main.GameUpdateCount);
            if (accumulated >= ExposureTicks)
            {
                anchor.GetGlobalNPC<FireBurnNPC>().ClearExposure(anchor);
                anchor.AddBuffToAnchorParts(wet, DurationTicks, forcedAddBuff: true);
                state.exposure.Clear();
            }
        }
        state.RefreshProgress(anchor);
        anchor.netUpdate = true;
        return true;
    }

    // 命中同一脉冲的其余体节是否跳过，由 FireBurnNPC 在消耗前记住。
    internal static bool ConsumeWet(NPC anchor, int attachmentTicks)
    {
        if (!IsWet(anchor)) return false;
        ConsumeStatus(anchor, ModContent.BuffType<ElementalAffliction_Wet>(), ReactionTicks(attachmentTicks));
        anchor.GetGlobalNPC<WaterExposureNPC>().RefreshProgress(anchor);
        return true;
    }

    internal static void ConsumeStatus(NPC anchor, int buff, int ticks)
    {
        int index = anchor.FindBuffIndex(buff);
        if (index < 0) return;
        int remaining = Math.Max(0, anchor.buffTime[index] - ticks);
        foreach (NPC part in Main.ActiveNPCs)
        {
            if (part != anchor && part.GetElementAnchorNPC()?.whoAmI != anchor.whoAmI) continue;
            int partIndex = part.FindBuffIndex(buff);
            if (partIndex < 0) continue;
            if (remaining == 0) part.DelBuff(partIndex);
            else part.buffTime[partIndex] = remaining;
            part.netUpdate = true;
            if (Main.netMode == NetmodeID.Server) NetMessage.SendData(54, -1, -1, null, part.whoAmI);
        }
    }

    internal void ClearExposure(NPC npc)
    {
        exposure.Clear();
        RefreshProgress(npc);
    }

    private void RefreshProgress(NPC npc)
    {
        int index = npc.FindBuffIndex(ModContent.BuffType<ElementalAffliction_Wet>());
        int current = 0;
        if (index < 0)
            foreach (var value in exposure.Values)
                if (Main.GameUpdateCount - value.LastHit < ElementBuildupBuffRegistry.DefaultIdleResetTime)
                    current = Math.Max(current, value.Ticks);
        if (index >= 0 || current == 0) exposure.Clear();
        var progress = npc.GetGlobalNPC<ElementalGlobalNPC>().GetOrCreateAccumulation(ElementType.Water);
        float visible = current > 0 || index >= 0 ? 120f : 0f;
        if (progress.Current != current || progress.InAffliction != (index >= 0) || progress.VisualTime != visible)
            npc.netUpdate = true;
        progress.Current = current;
        progress.Max = ExposureTicks;
        progress.InAffliction = index >= 0;
        progress.Duration = index >= 0 ? Math.Min(DurationTicks, npc.buffTime[index]) : 0;
        progress.MaxDuration = DurationTicks;
        progress.VisualTime = visible;
        progress.IdleResetTime = progress.IdleTime = 0;
    }

    public override void PostAI(NPC npc)
    {
        if (!managesProgress || Main.netMode == NetmodeID.MultiplayerClient || FireBurnNPC.GetBurnAnchor(npc) != npc) return;
        RefreshProgress(npc);
        if (Main.GameUpdateCount % 900 != 0) return;
        foreach (var key in new List<(int Player, int Type, int Identity)>(pulses.Keys))
            if (Main.GameUpdateCount - pulses[key].Tick > 1800) pulses.Remove(key);
    }
}
