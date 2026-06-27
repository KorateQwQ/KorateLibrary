using System;
using System.Collections.Generic;
using KL.Utils;
using Terraria.DataStructures;

namespace KL.DamageSystem;

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
            Action<ElementBuildupContext> onHit)
        {
            Element = element;
            BuffId = buffId;
            BuffDuration = buffDuration;
            Predicate = predicate;
            BuildUpFormula = buildUpFormula;
            MaxBuildUpRatio = maxBuildUpRatio;
            ModifyOnBuildUp = modifyOnBuildUp;
            OnHit = onHit;
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
    }

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
        Action<ElementBuildupContext> onHit = null)
    {
        if (element == ElementType.None)
            return;

        if (buffDuration <= 0)
            buffDuration = 1;

        predicate ??= _ => true;
        buildUpFormula ??= DefaultBuildUpFormula;

        var entry = new Entry(element, buffId, buffDuration, predicate, buildUpFormula, maxBuildUpRatio, modifyOnBuildUp, onHit);
        EntriesByElement[element] = entry;

        if (buffId > 0)
        {
            BuffIdByElement[element] = buffId;

            if (ElementByBuffId.TryGetValue(buffId, out var oldElement) && oldElement != element)
                Log($"ElementBuildupBuffRegistry: buffId {buffId} was already mapped to {oldElement}, overriding to {element}");

            ElementByBuffId[buffId] = element;
        }
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

        if (!entry.Predicate(context))
            return false;

        var g = context.Target.GetGlobalNPC<ElementalGlobalNPC>();

        entry.OnHit?.Invoke(context);

        float newBuildUpValue = entry.BuildUpFormula(context.Hit.SourceDamage);
        float maxBuildUpRatio = entry.MaxBuildUpRatio;
        float maxBuildUpValue = context.Target.lifeMax * maxBuildUpRatio;

        // 保底 60 次攻击打满
        if (newBuildUpValue < maxBuildUpValue / 60f) newBuildUpValue = maxBuildUpValue / 60f;

        if (!g.ElementAccumulation.TryGetValue(context.Element, out ElementalGlobalNPC.BuildUpProgressContext progress))
        {
            progress = new ElementalGlobalNPC.BuildUpProgressContext();
            g.ElementAccumulation[context.Element] = progress;
        }

        progress.Max = maxBuildUpValue;

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
                if(Main.netMode != NetmodeID.MultiplayerClient)context.Target.AddBuffToSelfAndChildren(applyInfo.BuffId, applyInfo.Duration,forcedAddBuff:applyInfo.ForcedAddBuff);
                //PrintText($"Applying buff {applyInfo.BuffId} to {context.Target.FullName} for {applyInfo.Duration} ticks");
            }
        }
        else
        {
            progress.Current += newBuildUpValue;
        }

        progress.VisualTime = 120;
        return true;
    }
}
