using KL.Utils.Net;

namespace KL.AttributeSystem;

public interface IAttributeProvider
{
    AttributeComponent Attributes { get; }
}

/// <summary>
/// 将属性组件挂载到玩家的可选基类，负责每帧重置和提交。
/// 已有玩家类也可以自行实现 IAttributeProvider 接口。
/// </summary>
public abstract class AttributeModPlayer : KLModPlayer, IAttributeProvider
{
    public AttributeComponent Attributes { get; }

    protected AttributeModPlayer()
    {
        // 所有最外层提交和资源写入共用日志入口，后置异常不打断其余业务通知。
        Attributes = new AttributeComponent(error => Mod.Logger.Error(
            $"{GetType().FullName} 属性发布失败。", error));
    }

    /// <summary>
    /// 获取属性最近一次已发布的完整快照；首次发布前返回定义默认值。
    /// </summary>
    public AttributeSnapshot GetAttribute(AttributeDefinition definition)
    {
        return Attributes.Get(definition);
    }

    /// <summary>
    /// 获取属性最近一次已发布的最终值。
    /// </summary>
    public float GetAttributeValue(AttributeDefinition definition)
    {
        return Attributes.GetFinalValue(definition);
    }

    /// <summary>计算阶段显式读取候选快照，尚未经过本次提交的前置修正。</summary>
    public AttributeSnapshot GetPendingAttribute(AttributeDefinition definition)
    {
        return Attributes.GetPending(definition);
    }

    /// <summary>读取候选最终值，尚未经过本次提交的前置修正。</summary>
    public float GetPendingAttributeValue(AttributeDefinition definition)
    {
        return Attributes.GetPendingFinalValue(definition);
    }

    /// <summary>
    /// 获取属性最近一次已发布快照中的最终值。
    /// </summary>
    public float GetPublishedAttributeValue(AttributeDefinition definition)
    {
        return Attributes.GetPublishedFinalValue(definition);
    }

    public override void ResetEffects()
    {
        Attributes.ResetForTick();
        base.ResetEffects();
    }

    /// <summary>
    /// 发布本帧普通属性。派生类需要消费本帧结果时，应先调用 base.PostUpdate()。
    /// 此入口不保证其他 ModPlayer 的 PostUpdate 已执行。
    /// </summary>
    public override void PostUpdate()
    {
        Attributes.Commit();
        base.PostUpdate();
    }

}
