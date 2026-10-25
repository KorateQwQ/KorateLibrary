using KL.Buffs.ElementalDebuff;
using KL.Drawing.Snippets;
using KL.Extensions;
using KL.Utils;

namespace KL.DamageSystem;

/// <summary>
/// 元素积蓄系统生命周期管理。
/// </summary>
public class ElementBuildUpSystem : ModSystem
{
    /// <summary>冻结条统一容量；命中点数属于各攻击来源，不在这里决定命中次数。</summary>
    public const float FreezeCapacity = 100f;
    /// <summary>冻结条积满后的冻结时长（tick）。</summary>
    public const int FreezeDuration = 240;
    /// <summary>富文本中冻结专有名词使用的 klicon 图标路径。</summary>
    public const string FreezeTermIcon = "KL/Drawing/Snippets/Icons/IceIcon";
    /// <summary>权威端实际施加冻结后通知，参数为元素锚点及本次命中的玩家；未知来源为 -1。</summary>
    public static event System.Action<NPC, int> FreezeTriggered;

    internal static void NotifyFreezeTriggered(NPC anchor, int sourcePlayer) => FreezeTriggered?.Invoke(anchor, sourcePlayer);
    /// <summary>
    /// 加载元素积蓄系统。
    /// </summary>
    public override void Load()
    {
        ElementBuildupBuffRegistry.Register(
            ElementType.Fire,
            ModContent.BuffType<ElementalAffliction_Fire>(),
            600,
            CommonPredict,
            x => x,
            0.05f,
            ModifyFireBuildUp,
            OnHit_Fire);

        // 冰：标准100点条。攻击来源传入各自积蓄量；未配置来源默认不积蓄。
        // 积蓄与 CD 统一挂到元素锚点，多部位 Boss 共享一套冻结状态。
        ElementBuildupBuffRegistry.RegisterHitCount(
            ElementType.Ice,
            ModContent.BuffType<ElementalAffliction_Freeze>(),
            FreezeDuration,
            FreezeCapacity,
            0f,
            FreezePredict,
            ModifyFreezeBuildUp,
            routeToAnchor: true);

        ElementBuildupBuffRegistry.Register(
            ElementType.Lightning,
            ModContent.BuffType<ElementalAffliction_Electrocuted>(),
            600,
            CommonPredict,
            x => x,
            0.05f,
            ModifyLightningBuildUp);

        ElementBuildupBuffRegistry.Register(
            ElementType.Water,
            ModContent.BuffType<ElementalAffliction_Wet>(),
            600,
            CommonPredict,
            x => x,
            0.05f,
            ModifyWetBuildUp);

        KLTerms.Register(new KLTerm(FreezeTermIcon, new Color(143, 227, 255),
            () => Language.GetTextValue("Mods.KL.Terms.Freeze.Name"),
            () => Language.GetTextValue("Mods.KL.Terms.Freeze.Description", FreezeCapacity),
            () =>
            [
                (Language.GetTextValue("Mods.KL.Terms.Freeze.Duration"), (FreezeDuration / 60f).ToString("0.##"), Language.GetTextValue("Mods.KL.Terms.Seconds")),
                (Language.GetTextValue("Mods.KL.Terms.Freeze.Cooldown"), (ElementalGlobalNPC.FreezeCooldownDuration / 60f).ToString("0.##"), Language.GetTextValue("Mods.KL.Terms.Seconds")),
            ]));

        base.Load();
    }

    void ModifyFireBuildUp(ElementBuildupContext context, ref ElementBuildupBuffRegistry.BuildUpApplyInfo applyInfo)
    {
        // 默认逻辑会根据注册信息施加 debuff；如需自定义可在此修改 applyInfo
    }

    void OnHit_Fire(ElementBuildupContext context)
    {
        
    }

    void ModifyFreezeBuildUp(ElementBuildupContext context, ref ElementBuildupBuffRegistry.BuildUpApplyInfo applyInfo)
    {
        // 默认逻辑会根据注册信息施加 debuff；如需自定义可在此修改 applyInfo
    }

    void ModifyLightningBuildUp(ElementBuildupContext context, ref ElementBuildupBuffRegistry.BuildUpApplyInfo applyInfo)
    {
        // 默认逻辑会根据注册信息施加 debuff；如需自定义可在此修改 applyInfo
    }

    void ModifyWetBuildUp(ElementBuildupContext context, ref ElementBuildupBuffRegistry.BuildUpApplyInfo applyInfo)
    {
        // 默认逻辑会根据注册信息施加 debuff；如需自定义可在此修改 applyInfo
    }

    /// <summary>
    /// 通用预测函数，如果NPC已处于元素异常状态，则不积累元素
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    bool CommonPredict(ElementBuildupContext context)
    {
        ElementalGlobalNPC npc = context.Target.GetGlobalNPC<ElementalGlobalNPC>();

        return !npc.GetAffliction(context.Element);
    }

    /// <summary>
    /// 冻结的积累条件：冻结 CD 已经归零，且当前不处于冻结中。
    /// 仅用 <see cref="CommonPredict"/> 是不够的——冻结 buff 消失的那一刻就满足“未处于异常”，
    /// 但此时 CD 才刚开始计时，直接放行会导致刚解冻就立刻重新冻结。
    /// </summary>
    bool FreezePredict(ElementBuildupContext context)
    {
        // TryApply 已经把目标收敛到锚点，这里直接问锚点即可。
        return context.Target.IsFreezeAccumulating();
    }

    public override void Unload()
    {
        FreezeTriggered = null;
        ElementBuildupBuffRegistry.Clear();
        base.Unload();
    }
}
