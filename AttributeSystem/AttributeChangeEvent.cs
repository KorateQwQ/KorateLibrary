namespace KL.AttributeSystem;

/// <summary>
/// 单个属性的提交钩子与变化通知入口。
/// </summary>
public sealed class AttributeChangeEvent
{
    private readonly AttributeComponent _owner;
    private readonly AttributeDefinition _definition;

    internal AttributeChangeEvent(AttributeComponent owner, AttributeDefinition definition)
    {
        _owner = owner;
        _definition = definition;
    }

    /// <summary>
    /// 此事件入口对应的属性定义。
    /// </summary>
    public AttributeDefinition Definition => _definition;

    /// <summary>
    /// 写入前修正候选值；修正后即使数值不变，也会执行此钩子。
    /// </summary>
    public event EventHandler<PreAttributeChangeEventArgs> PreAttributeChange;

    /// <summary>
    /// 属性首次发布时触发一次，不触发后置变化钩子。
    /// </summary>
    public event EventHandler<AttributeInitializedEventArgs> AttributeInitialized;

    /// <summary>
    /// 属性已发布快照的任意组成部分变化时触发。
    /// </summary>
    public event EventHandler<AttributeChangedEventArgs> SnapshotChanged;

    /// <summary>属性已发布白值变化时触发。</summary>
    public event EventHandler<AttributeChangedEventArgs> PostAttributeBaseChange;

    /// <summary>
    /// 属性已发布最终值变化时触发。
    /// </summary>
    public event EventHandler<AttributeChangedEventArgs> PostAttributeChange;

    /// <summary>
    /// 订阅最终值变化，返回可通过释放来取消监听的订阅对象。
    /// </summary>
    public IDisposable Subscribe(EventHandler<AttributeChangedEventArgs> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        PostAttributeChange += handler;
        return new Subscription(this, handler);
    }

    /// <summary>
    /// 移除之前订阅的一个最终值变化处理器。
    /// </summary>
    public void Unsubscribe(EventHandler<AttributeChangedEventArgs> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        PostAttributeChange -= handler;
    }

    internal void InvokePreAttributeChange(PreAttributeChangeEventArgs args)
    {
        PreAttributeChange?.Invoke(_owner, args);
    }

    internal void InvokeInitialized(AttributeInitializedEventArgs args)
    {
        _owner.InvokeNotification(AttributeInitialized, args, _definition, nameof(AttributeInitialized));
    }

    internal void InvokeSnapshotChanged(AttributeChangedEventArgs args)
    {
        _owner.InvokeNotification(SnapshotChanged, args, _definition, nameof(SnapshotChanged));
    }

    internal void InvokePostAttributeChange(AttributeChangedEventArgs args)
    {
        _owner.InvokeNotification(PostAttributeChange, args, _definition, nameof(PostAttributeChange));
    }

    internal void InvokePostAttributeBaseChange(AttributeChangedEventArgs args)
    {
        _owner.InvokeNotification(PostAttributeBaseChange, args, _definition, nameof(PostAttributeBaseChange));
    }

    private sealed class Subscription : IDisposable
    {
        private readonly AttributeChangeEvent _event;
        private readonly EventHandler<AttributeChangedEventArgs> _handler;
        private bool _disposed;

        public Subscription(AttributeChangeEvent @event,
            EventHandler<AttributeChangedEventArgs> handler)
        {
            _event = @event;
            _handler = handler;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _event.Unsubscribe(_handler);
        }
    }
}
