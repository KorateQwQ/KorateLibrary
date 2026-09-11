namespace KL.AttributeSystem;

/// <summary>
/// 角色通用 RPG 属性的业务基类。具体 Mod 在派生类中向同一组件添加专属属性和规则。
/// 组件的创建、重置、提交和异常报告由 AttributeModPlayer 负责。
/// </summary>
public abstract class RPGAttributeModPlayer : AttributeModPlayer
{
    /// <summary>提前注册通用属性，确保没有技能系统或属性贡献时也能在首次提交中执行其规则。</summary>
    protected RPGAttributeModPlayer()
    {
        Attributes.GetPublished(CharacterAttributes.CooldownHaste);
    }

    /// <summary>最近一次已发布的技能急速，范围为 0–500。</summary>
    public float CooldownHaste => GetAttributeValue(CharacterAttributes.CooldownHaste);

    /// <summary>急速对应的冷却缩减比例，以小数表示；100 急速对应 0.5。</summary>
    public float CooldownReductionPercent => CharacterAttributes.GetCooldownReductionPercent(CooldownHaste);

    /// <summary>急速对应的冷却时长倍率；100 急速对应 0.5 倍时长。</summary>
    public float CooldownDurationMultiplier => CharacterAttributes.GetCooldownDurationMultiplier(CooldownHaste);
}
