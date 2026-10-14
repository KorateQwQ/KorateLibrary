using System;
using System.Collections.Generic;
using KL.Extensions;
using KL.Utils;
using Terraria.DataStructures;

namespace KL.DamageSystem;

/// <summary>
/// 积蓄值的累积方式。
/// </summary>
internal enum ElementBuildUpMode
{
    /// <summary>
    /// 按伤害比例累积，积蓄上限由 <see cref="ElementBuildupBuffRegistry.Entry.MaxBuildUpRatio"/> 与目标生命上限决定。
    /// </summary>
    DamageRatio,

    /// <summary>
    /// 按命中次数累积，与伤害无关，积蓄上限固定为 <see cref="ElementBuildupBuffRegistry.Entry.FixedThreshold"/>，
    /// 每次有效命中增加 <see cref="ElementBuildupBuffRegistry.Entry.GainPerHit"/> 点。
    /// </summary>
    HitCount,
}

internal sealed class ElementBuildupContext
{
    public ElementBuildupContext(NPC target, ElementType element, NPC.HitInfo hit)
    {
        Target = target;
        Element = element;
        Hit = hit;
    }

    // 目标 NPC
    public NPC Target { get; set; }
    

    // 本次更新对应的元素类型
    public ElementType Element { get; set; }

    // 本次命中的信息（用于 onHit / predicate / modify 使用）
    public NPC.HitInfo Hit { get; set; }
}

internal static class ElementBuildupBuffRegistry
{
    /// <summary>
    /// 用于存储元素积蓄信息,拥有字段：BuffId, Duration, ForcedAddBuff, ApplyDebuff, ResetProgress
    /// </summary>
    internal struct BuildUpApplyInfo
    {
        public int BuffId;
        public int Duration;
        public bool ForcedAddBuff;

        // 是否执行默认“施加 debuff”逻辑
        public bool ApplyDebuff;

        // 是否重置积蓄条
        public bool ResetProgress;
    }

    internal delegate void ModifyBuildUpDelegate(ElementBuildupContext context, ref BuildUpApplyInfo applyInfo);

    // 单条规则：元素 -> 条件判定 -> 积蓄公式 -> 满额结算（默认施加 Buff，可被 modify 覆盖）
    internal readonly struct Entry
    {
        public Entry(
            ElementType element,
            int buffId,
            int buffDuration,
            Func<ElementBuildupContext, bool> predicate,
            Func<float, float> buildUpFormula,
            float maxBuildUpRatio,
            ModifyBuildUpDelegate modifyOnBuildUp,
            Action<ElementBuildupContext> onHit,
            ElementBuildUpMode mode,
            float fixedThreshold,
            float gainPerHit,
            bool routeToAnchor,
            int idleResetTime)
        {
            Element = element;
            BuffId = buffId;
            BuffDuration = buffDuration;
            Predicate = predicate;
            BuildUpFormula = buildUpFormula;
            MaxBuildUpRatio = maxBuildUpRatio;
            ModifyOnBuildUp = modifyOnBuildUp;
            OnHit = onHit;
            Mode = mode;
            FixedThreshold = fixedThreshold;
            GainPerHit = gainPerHit;
            RouteToAnchor = routeToAnchor;
            IdleResetTime = idleResetTime;
        }

        public ElementType Element { get; }

        public int BuffId { get; }

        // Buff 持续时间（tick）
        public int BuffDuration { get; }

        /// <summary>
        /// 用于判定这个元素积蓄是否可累积并结算为 buff。
        /// </summary>
        public Func<ElementBuildupContext, bool> Predicate { get; }

        // 异常积蓄比例，根据这个比例会把伤害转化为异常值
        public Func<float, float> BuildUpFormula { get; }

        // 异常长度比例，根据敌人真实血量的比例来计算异常长度
        public float MaxBuildUpRatio { get; }

        // 满额时的可选修改回调：可修改施加的 buffId/持续时间，也可以禁止默认施加逻辑
        public ModifyBuildUpDelegate ModifyOnBuildUp { get; }

        // 每次命中时的回调（可为空）
        public Action<ElementBuildupContext> OnHit { get; }

        // 积蓄值的累积方式
        public ElementBuildUpMode Mode { get; }

        // HitCount 模式下的固定积蓄上限（点）
        public float FixedThreshold { get; }

        // HitCount 模式下每次有效命中增加的积蓄点数
        public float GainPerHit { get; }

        // 是否把积蓄/异常统一挂到元素锚点上（多部位 Boss 共享一套积蓄与 CD）
        public bool RouteToAnchor { get; }

        // 距上次有效积蓄超过该时长（tick）后清空积蓄，<=0 表示永不自动清空
        public int IdleResetTime { get; }
    }

    // 默认 15 秒无有效积蓄后清空
    public const int DefaultIdleResetTime = 900;

    static Func<float, float> DefaultBuildUpFormula { get; } = x => MathF.Max(1, KLMathF.ClampLerp(1, 100, x / 1000f));

    // 每种元素对应一条规则
    static readonly Dictionary<ElementType, Entry> EntriesByElement = new();

    // buffId <-> element 的双向映射
    static readonly Dictionary<int, ElementType> ElementByBuffId = new();
    static readonly Dictionary<ElementType, int> BuffIdByElement = new();

    public static bool TryGetElementByBuffId(int buffId, out ElementType element)
        => ElementByBuffId.TryGetValue(buffId, out element);

    public static bool TryGetBuffIdByElement(ElementType element, out int buffId)
        => BuffIdByElement.TryGetValue(element, out buffId);

    // 清空注册表，用于热重载或卸载时避免重复注册
    public static void Clear()
    {
        EntriesByElement.Clear();
        ElementByBuffId.Clear();
        BuffIdByElement.Clear();
    }

    // 注册一条元素积蓄触发 Buff 的规则
    public static void Register(
        ElementType element,
        int buffId,
        int buffDuration,
        Func<ElementBuildupContext, bool> predicate,
        Func<float, float> buildUpFormula,
        float maxBuildUpRatio = 0.1f,
        ModifyBuildUpDelegate modifyOnBuildUp = null,
        Action<ElementBuildupContext> onHit = null,
        ElementBuildUpMode mode = ElementBuildUpMode.DamageRatio,
        float fixedThreshold = 0f,
        float gainPerHit = 1f,
        bool routeToAnchor = false,
        int idleResetTime = DefaultIdleResetTime)
    {
        if (element == ElementType.None)
            return;

        if (buffDuration <= 0)
            buffDuration = 1;

        predicate ??= _ => true;
        buildUpFormula ??= DefaultBuildUpFormula;

        var entry = new Entry(element, buffId, buffDuration, predicate, buildUpFormula, maxBuildUpRatio, modifyOnBuildUp, onHit,
            mode, fixedThreshold, gainPerHit, routeToAnchor, idleResetTime);
        EntriesByElement[element] = entry;

        if (buffId > 0)
        {
            BuffIdByElement[element] = buffId;

            if (ElementByBuffId.TryGetValue(buffId, out var oldElement) && oldElement != element)
                Log($"ElementBuildupBuffRegistry: buffId {buffId} was already mapped to {oldElement}, overriding to {element}");

            ElementByBuffId[buffId] = element;
        }
    }

    /// <summary>
    /// 注册一条按命中次数累积的规则：与伤害无关，固定阈值，每次有效命中增加固定点数。
    /// </summary>
    public static void RegisterHitCount(
        ElementType element,
        int buffId,
        int buffDuration,
        float threshold,
        float gainPerHit,
        Func<ElementBuildupContext, bool> predicate = null,
        ModifyBuildUpDelegate modifyOnBuildUp = null,
        Action<ElementBuildupContext> onHit = null,
        bool routeToAnchor = false,
        int idleResetTime = DefaultIdleResetTime)
    {
        Register(
            element,
            buffId,
            buffDuration,
            predicate,
            null,
            0f,
            modifyOnBuildUp,
            onHit,
            ElementBuildUpMode.HitCount,
            threshold,
            gainPerHit,
            routeToAnchor,
            idleResetTime);
    }

    // 尝试根据当前积蓄上下文触发规则
    public static bool TryApply(in ElementBuildupContext context)
    {
        /*if (Main.netMode == NetmodeID.Server)
            return false;*/

        if (!EntriesByElement.TryGetValue(context.Element, out var entry))
            return false;

        if (context.Target is null)
        {
            return false;
        }

        // 多部位 Boss：先把目标收敛到统一锚点，后续 predicate / 积蓄 / 结算都基于锚点。
        // 这一步必须在 predicate 之前完成，因为 predicate 往往要读取锚点上的积蓄与 CD 状态。
        if (entry.RouteToAnchor)
        {
            NPC anchor = context.Target.GetElementAnchorNPC();
            if (anchor is not { active: true })
                return false;

            context.Target = anchor;
        }

        if (!entry.Predicate(context))
            return false;

        var g = context.Target.GetGlobalNPC<ElementalGlobalNPC>();

        entry.OnHit?.Invoke(context);

        float newBuildUpValue;
        float maxBuildUpValue;

        if (entry.Mode == ElementBuildUpMode.HitCount)
        {
            maxBuildUpValue = entry.FixedThreshold;
            newBuildUpValue = entry.GainPerHit;
        }
        else
        {
            newBuildUpValue = entry.BuildUpFormula(context.Hit.SourceDamage);
            maxBuildUpValue = context.Target.lifeMax * entry.MaxBuildUpRatio;

            // 保底 60 次攻击打满
            if (newBuildUpValue < maxBuildUpValue / 60f) newBuildUpValue = maxBuildUpValue / 60f;
        }

        // 阈值非法时视为该规则未配置完成，直接跳过，避免出现除零或瞬间满额。
        if (maxBuildUpValue <= 0f)
            return false;

        if (!g.ElementAccumulation.TryGetValue(context.Element, out ElementalGlobalNPC.BuildUpProgressContext progress))
        {
            progress = new ElementalGlobalNPC.BuildUpProgressContext();
            g.ElementAccumulation[context.Element] = progress;
        }

        progress.Max = maxBuildUpValue;
        progress.IdleResetTime = entry.IdleResetTime;

        if (progress.Current + newBuildUpValue >= maxBuildUpValue)
        {
            var applyInfo = new BuildUpApplyInfo
            {
                BuffId = entry.BuffId,
                Duration = entry.BuffDuration,
                ForcedAddBuff = true,
                ApplyDebuff = entry.BuffId > 0 && entry.BuffDuration > 0,
                ResetProgress = true,
            };

            entry.ModifyOnBuildUp?.Invoke(context, ref applyInfo);
            
            progress.MaxDuration = applyInfo.Duration;
            
            if (applyInfo.ResetProgress)
                progress.Current = 0;

            if (applyInfo.ApplyDebuff && applyInfo.BuffId > 0 && applyInfo.Duration > 0)
            {
                if(Main.netMode != NetmodeID.MultiplayerClient)
                {
                    if (entry.RouteToAnchor)
                        context.Target.AddBuffToAnchorParts(applyInfo.BuffId, applyInfo.Duration, forcedAddBuff: applyInfo.ForcedAddBuff);
                    else
                        context.Target.AddBuffToSelfAndChildren(applyInfo.BuffId, applyInfo.Duration, forcedAddBuff: applyInfo.ForcedAddBuff);
                }
                //PrintText($"Applying buff {applyInfo.BuffId} to {context.Target.FullName} for {applyInfo.Duration} ticks");
            }
        }
        else
        {
            progress.Current += newBuildUpValue;
        }

        progress.IdleTime = 0;
        progress.VisualTime = 120;
        return true;
    }
}
