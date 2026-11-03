using System;
using System.Collections.Generic;
using System.IO;
using KL.Buffs.ElementalDebuff;
using KL.Extensions;
using KL.Utils;
using Terraria.ModLoader.IO;

namespace KL.DamageSystem;

/// <summary>命中前捕获身份，允许命中后处理致死伤害，同时拒绝已复用的 NPC 槽位。</summary>
public readonly record struct FireBurnHit(int TargetIndex, int TargetGeneration, int AnchorIndex, int AnchorGeneration, bool WasBurning);

/// <summary>燃烧状态属于统一元素主体；体节命中、续燃、首次奖励与 DOT 都归到同一主体。</summary>
public sealed class FireBurnNPC : GlobalNPC
{
    public const int ExposureTicks = 180;
    public const double DamageIgnitionFraction = .5;
    public const int DurationTicks = 600;
    /// <summary>与冻结共用无新增积蓄时的清空期限；每次有效积累重新计时。</summary>
    public const int ExposureIdleResetTicks = ElementBuildupBuffRegistry.DefaultIdleResetTime;
    private static int nextInstance;
    private readonly Dictionary<int, (double Ticks, uint LastHit)> exposure = new();
    private sealed class BeamPulse(int identity, int sequence)
    {
        public readonly int Identity = identity, Sequence = sequence;
        public readonly HashSet<(int Index, int Generation)> Targets = new();
        public double DamageTicks, AppliedTicks;
        public int TimeTicks;
        public bool ConsumedWater;
    }
    private readonly Dictionary<int, BeamPulse> lastBeamPulse = new();
    private bool rewarded;
    private bool managesProgress;
    private int damageClock;
    private uint lastDamageTick = uint.MaxValue;
    public int InstanceId { get; private set; }
    public int SourcePlayer { get; private set; } = -1;
    public int DamagePerTick { get; private set; } = 1;
    public override bool InstancePerEntity => true;

    public override void SetDefaults(NPC npc)
    {
        InstanceId = Main.netMode == NetmodeID.MultiplayerClient ? 0 : ++nextInstance;
        exposure.Clear();
        lastBeamPulse.Clear();
        rewarded = false;
        managesProgress = false;
        SourcePlayer = -1;
        DamagePerTick = 1;
        damageClock = 0;
        lastDamageTick = uint.MaxValue;
    }

    internal static NPC GetBurnAnchor(NPC npc)
    {
        if (npc is not { active: true } || npc.life <= 0) return null;
        // 已失去共享血量主体的残留体节不能退回为新的独立点燃目标。
        if (npc.realLife >= 0 && npc.realLife != npc.whoAmI
            && (npc.realLife >= Main.npc.Length || Main.npc[npc.realLife] is not { active: true, life: > 0 })) return null;
        return npc.GetElementAnchorNPC();
    }

    public static bool IsBurning(NPC npc) => GetBurnAnchor(npc) is { active: true } anchor
        && anchor.HasBuff<ElementalAffliction_Fire>();

    public static FireBurnHit? CaptureHit(NPC target)
    {
        if (target is not { active: true } || target.life <= 0 || target.friendly || target.immortal || target.dontTakeDamage)
            return null;
        NPC anchor = GetBurnAnchor(target);
        if (anchor is not { active: true } || anchor.life <= 0 || anchor.lifeMax <= 0) return null;
        return new(target.whoAmI, target.GetGlobalNPC<FireBurnNPC>().InstanceId,
            anchor.whoAmI, anchor.GetGlobalNPC<FireBurnNPC>().InstanceId, IsBurning(target));
    }

    public static bool ApplyBeamHit(FireBurnHit hit, int player, int ticks, int dotDamage, int damageDone,
        int sourceIdentity, int pulseSequence)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || player < 0 || player >= Main.maxPlayers
            || !Main.player[player].active || Main.player[player].dead || damageDone <= 0 || ticks <= 0
            || sourceIdentity < 0 || pulseSequence <= 0 || hit.TargetIndex < 0 || hit.TargetIndex >= Main.maxNPCs
            || hit.AnchorIndex < 0 || hit.AnchorIndex >= Main.maxNPCs) return false;
        NPC target = Main.npc[hit.TargetIndex], anchor = Main.npc[hit.AnchorIndex];
        if (target == null || anchor == null || !target.TryGetGlobalNPC<FireBurnNPC>(out var targetState)
            || !anchor.TryGetGlobalNPC<FireBurnNPC>(out var anchorState)
            || targetState.InstanceId != hit.TargetGeneration || anchorState.InstanceId != hit.AnchorGeneration
            || anchor.lifeMax <= 0 || target.friendly || target.immortal
            || (!target.active && target.life > 0) || (!anchor.active && anchor.life > 0)) return false;
        // damageDone 为实际命中结算值；以主体最大生命为分母，50% 对应整条180点积累。
        double damageTicks = Math.Min(ExposureTicks, damageDone * (ExposureTicks / DamageIgnitionFraction) / anchor.lifeMax);
        return anchorState.ApplyToAnchor(anchor, player, ticks, dotDamage, false,
            sourceIdentity, pulseSequence, damageTicks, hit.TargetIndex, hit.TargetGeneration, true, hit.WasBurning);
    }

    public bool ApplyExposure(NPC npc, int player, int ticks, int damage, bool ignite = false,
        int sourceIdentity = -1, int pulseSequence = 0)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !npc.active || npc.life <= 0
            || npc.friendly || npc.dontTakeDamage || npc.immortal || player < 0 || player >= Main.maxPlayers
            || !Main.player[player].active || Main.player[player].dead || ticks <= 0) return false;
        NPC anchor = GetBurnAnchor(npc);
        if (anchor is not { active: true } || anchor.life <= 0) return false;
        return anchor.GetGlobalNPC<FireBurnNPC>().ApplyToAnchor(anchor, player, ticks, damage, ignite, sourceIdentity, pulseSequence);
    }

    private bool ApplyToAnchor(NPC npc, int player, int ticks, int damage, bool ignite, int sourceIdentity, int pulseSequence,
        double damageTicks = 0, int hitIndex = -1, int hitGeneration = 0, bool allowDefeated = false, bool wasBurningAtHit = false)
    {
        double gain = Math.Max(Math.Clamp(ticks, 1, ExposureTicks), damageTicks);
        BeamPulse currentPulse = null;
        // 每次脉冲只提供首个有效部位的一份附着，后续体节与重复包均不再提供积累。
        if (!ignite && sourceIdentity >= 0 && pulseSequence > 0)
        {
            if (!lastBeamPulse.TryGetValue(player, out var pulse) || pulse.Identity != sourceIdentity || pulseSequence > pulse.Sequence)
                lastBeamPulse[player] = pulse = new(sourceIdentity, pulseSequence);
            else if (pulseSequence < pulse.Sequence) return false;
            // 同一脉冲的体节只接受首份附着，不能叠加各体节伤害补偿瞬间点燃。
            if (pulse.Targets.Count > 0 || !pulse.Targets.Add((hitIndex, hitGeneration))) return false;
            if (pulse.ConsumedWater) return false;
            currentPulse = pulse;
            pulse.TimeTicks = Math.Max(pulse.TimeTicks, Math.Clamp(ticks, 1, ExposureTicks));
            pulse.DamageTicks = Math.Min(ExposureTicks, pulse.DamageTicks + damageTicks);
            double budget = Math.Max(pulse.TimeTicks, pulse.DamageTicks);
            gain = budget - pulse.AppliedTicks;
            pulse.AppliedTicks = budget;
            if (gain <= 0) return false;
        }
        if (!npc.GetGlobalNPC<ElementalGlobalNPC>().TryBeginAttachment(npc, ElementType.Fire)) return false;
        if (WaterExposureNPC.ConsumeWet(npc, ignite ? ExposureTicks : ticks))
        {
            if (currentPulse != null) currentPulse.ConsumedWater = true;
            exposure.Clear();
            RefreshProgress(npc);
            return false;
        }
        int buff = ModContent.BuffType<ElementalAffliction_Fire>();
        managesProgress = true;
        if (!ignite && !npc.HasBuff(buff))
        {
            exposure.TryGetValue(player, out var current);
            // 使用与冻结相同的闲置期限；同一 tick 的重复回调不能重复积蓄。
            if ((sourceIdentity < 0 || pulseSequence <= 0) && current.Ticks > 0 && current.LastHit == Main.GameUpdateCount) return false;
            double accumulated = Main.GameUpdateCount - current.LastHit >= ExposureIdleResetTicks ? 0 : current.Ticks;
            accumulated += gain;
            exposure[player] = (accumulated, Main.GameUpdateCount);
            if (npc.active && npc.life > 0) RefreshProgress(npc);
            if (accumulated + 1e-9 < ExposureTicks) return false;
        }
        bool wasBurning = npc.HasBuff(buff) || wasBurningAtHit;
        if (!npc.active || npc.life <= 0)
        {
            if (!allowDefeated) return false;
            exposure.Clear();
            NotifyFirstIgnition(npc, player, wasBurning);
            return true;
        }
        npc.GetGlobalNPC<WaterExposureNPC>().ClearExposure(npc);
        npc.AddBuffToAnchorParts(buff, DurationTicks, forcedAddBuff: true);
        if (!npc.HasBuff(buff)) return false;
        SourcePlayer = player;
        DamagePerTick = Math.Clamp(damage, 1, 1000);
        if (!wasBurning) damageClock = 0;
        exposure.Clear();
        RefreshProgress(npc);
        npc.netUpdate = true;
        NotifyFirstIgnition(npc, player, wasBurning);
        return true;
    }

    private void NotifyFirstIgnition(NPC npc, int player, bool wasBurning)
    {
        if (!rewarded)
        {
            rewarded = true;
            if (!wasBurning) FireBurnSystem.NotifyIgnited(npc, player);
        }
    }

    public override void PostAI(NPC npc)
    {
        if (managesProgress && Main.netMode != NetmodeID.MultiplayerClient && GetBurnAnchor(npc) == npc)
            RefreshProgress(npc);
    }

    // 战斗积蓄仍按各玩家分别累计；旧元素圈显示最接近点燃的有效积蓄，不把多人进度相加。
    internal void ClearExposure(NPC npc)
    {
        exposure.Clear();
        RefreshProgress(npc);
    }

    internal void RefreshProgress(NPC npc)
    {
        int buffIndex = npc.FindBuffIndex(ModContent.BuffType<ElementalAffliction_Fire>());
        bool burning = buffIndex >= 0;
        double current = 0;
        if (!burning)
            foreach (var value in exposure.Values)
                if (Main.GameUpdateCount - value.LastHit < ExposureIdleResetTicks) current = Math.Max(current, value.Ticks);
        if (burning || current == 0) exposure.Clear();

        var progress = npc.GetGlobalNPC<ElementalGlobalNPC>().GetOrCreateAccumulation(ElementType.Fire);
        float visible = current > 0 || burning ? 120f : 0f;
        float shownProgress = (float)Math.Min(current, ExposureTicks);
        if (progress.Current != shownProgress || progress.InAffliction != burning || progress.VisualTime != visible)
            npc.netUpdate = true;
        progress.Current = shownProgress;
        progress.Max = ExposureTicks;
        progress.InAffliction = burning;
        progress.Duration = burning ? npc.buffTime[buffIndex] : 0;
        progress.MaxDuration = DurationTicks;
        progress.VisualTime = visible;
        // 本类按 LastHit 清除过期积蓄，禁止另一套通用闲置计时提前清空或延后清空显示。
        progress.IdleResetTime = 0;
        progress.IdleTime = 0;
    }

    public void UpdateDamage(NPC npc)
    {
        NPC anchor = GetBurnAnchor(npc);
        if (anchor is not { active: true } || !anchor.HasBuff<ElementalAffliction_Fire>()) return;
        if (anchor != npc)
        {
            anchor.GetGlobalNPC<FireBurnNPC>().UpdateDamage(anchor);
            return;
        }
        if (Main.netMode == NetmodeID.MultiplayerClient || lastDamageTick == Main.GameUpdateCount) return;
        lastDamageTick = Main.GameUpdateCount;
        if (++damageClock < 30) return;
        damageClock = 0;
        if (!npc.active || npc.life <= 0 || npc.dontTakeDamage || npc.immortal) return;
        if (SourcePlayer >= 0 && SourcePlayer < Main.maxPlayers && Main.player[SourcePlayer].active)
            npc.PlayerInteraction(SourcePlayer);
        // 固定小额 DOT，不经玩家增伤、暴击或元素积蓄；仍走原版受击、死亡、掉落和网络结算。
        var hit = new NPC.HitInfo { Damage = DamagePerTick, SourceDamage = DamagePerTick,
            DamageType = DamageClass.Default, HitDirection = 0,
            Knockback = ElementTagCodec.EncodeIntoKnockback(0, 51) };
        npc.StrikeNPC(hit, noPlayerInteraction: true);
        if (Main.netMode == NetmodeID.Server) NetMessage.SendStrikeNPC(npc, hit);
    }

    public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter writer)
    {
        writer.Write(InstanceId);
        writer.Write(SourcePlayer);
        writer.Write(DamagePerTick);
    }

    public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader reader)
    {
        InstanceId = reader.ReadInt32();
        SourcePlayer = reader.ReadInt32();
        DamagePerTick = reader.ReadInt32();
    }
}

public sealed class FireBurnSystem : ModSystem
{
    public static event Action<NPC, int> Ignited;
    internal static void NotifyIgnited(NPC npc, int player) => Ignited?.Invoke(npc, player);
    public override void Unload() => Ignited = null;
}
