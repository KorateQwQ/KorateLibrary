using KL.Buffs.ElementalDebuff;
using KL.Utils;

namespace KL.DamageSystem;

/// <summary>
/// 元素积蓄系统生命周期管理。
/// </summary>
public class ElementBuildUpSystem : ModSystem
{
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

        ElementBuildupBuffRegistry.Register(
            ElementType.Ice,
            ModContent.BuffType<ElementalAffliction_Freeze>(),
            600,
            CommonPredict,
            x => x,
            0.05f,
            ModifyFreezeBuildUp);

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

    public override void Unload()
    {
        ElementBuildupBuffRegistry.Clear();
        base.Unload();
    }
}
