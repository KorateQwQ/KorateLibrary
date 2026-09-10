namespace KL.AttributeSystem;

/// <summary>
/// 为提供属性组件的玩家类提供便捷访问方法。
/// </summary>
public static class AttributeProviderExtensions
{
    /// <summary>读取最近一次已发布的快照；首次发布前返回定义默认值。</summary>
    public static AttributeSnapshot GetAttribute(this IAttributeProvider provider,
        AttributeDefinition definition)
    {
        return GetComponent(provider).Get(definition);
    }

    public static float GetAttributeValue(this IAttributeProvider provider,
        AttributeDefinition definition)
    {
        return GetComponent(provider).GetFinalValue(definition);
    }

    /// <summary>计算阶段显式读取候选快照，尚未经过本次提交的前置修正。</summary>
    public static AttributeSnapshot GetPendingAttribute(this IAttributeProvider provider,
        AttributeDefinition definition)
    {
        return GetComponent(provider).GetPending(definition);
    }

    /// <summary>读取候选最终值，尚未经过本次提交的前置修正。</summary>
    public static float GetPendingAttributeValue(this IAttributeProvider provider,
        AttributeDefinition definition)
    {
        return GetComponent(provider).GetPendingFinalValue(definition);
    }

    public static AttributeSnapshot GetPublishedAttribute(this IAttributeProvider provider,
        AttributeDefinition definition)
    {
        return GetComponent(provider).GetPublished(definition);
    }

    public static float GetPublishedAttributeValue(this IAttributeProvider provider,
        AttributeDefinition definition)
    {
        return GetComponent(provider).GetPublishedFinalValue(definition);
    }

    /// <summary>
    /// 从属性提供者获取单个属性的事件入口。
    /// </summary>
    public static AttributeChangeEvent GetAttributeChangeEvent(this IAttributeProvider provider,
        AttributeDefinition definition)
    {
        return GetComponent(provider).GetAttributeChangeEvent(definition);
    }

    private static AttributeComponent GetComponent(IAttributeProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        return provider.Attributes ?? throw new InvalidOperationException(
            $"{provider.GetType().FullName} does not have an attribute component bound.");
    }
}
