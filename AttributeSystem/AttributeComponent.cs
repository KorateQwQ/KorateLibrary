using System.Linq;

namespace KL.AttributeSystem;

/// <summary>
/// 保存每帧重建的普通属性和持续保留的资源属性。
/// 普通属性通过 Commit 统一发布，资源写入通过同一套钩子和事件立即发布。
/// </summary>
public sealed class AttributeComponent
{
    internal const float ChangeEpsilon = 0.0001f;

    private sealed class AttributeState
    {
        public AttributeState(AttributeDefinition definition)
        {
            Definition = definition;
            BaseValue = definition.DefaultBaseValue;
        }

        public AttributeDefinition Definition { get; }

        public float BaseValue { get; set; }

        public float ExtraFlatValue { get; set; }

        public float ExtraPercentValue { get; set; }

        public AttributeSnapshot PublishedValue { get; set; }

        public bool HasPublishedValue { get; set; }
    }

    private readonly Dictionary<AttributeDefinition, AttributeState> _states = new();
    private readonly Dictionary<AttributeDefinition, AttributeChangeEvent> _changeEvents = new();
    private bool _isCommitting;
    private bool _isPreparing;
    private readonly HashSet<AttributeDefinition> _writingResources = new();
    private List<Exception> _publicationErrors;
    private readonly Action<AggregateException> _errorReporter;

    /// <summary>创建独立组件，发布异常在最外层操作结束后以 AggregateException 报告。</summary>
    public AttributeComponent() { }

    /// <summary>
    /// 指定发布异常的统一报告入口。未指定时，最外层操作结束后抛出 AggregateException。
    /// 报告器正常返回表示错误已处理；前置失败的写入仍不生效。
    /// </summary>
    public AttributeComponent(Action<AggregateException> errorReporter)
    {
        _errorReporter = errorReporter;
    }

    /// <summary>写入前修正候选值；执行此钩子不保证最终会发生数值变化。</summary>
    public event EventHandler<PreAttributeChangeEventArgs> PreAttributeChange;

    /// <summary>属性首次发布时触发一次，与实际值变化通知分开。</summary>
    public event EventHandler<AttributeInitializedEventArgs> AttributeInitialized;

    /// <summary>已发布快照的任意组成部分变化时触发。</summary>
    public event EventHandler<AttributeChangedEventArgs> AttributeSnapshotChanged;

    /// <summary>已发布白值变化时触发，即使最终值保持不变。</summary>
    public event EventHandler<AttributeChangedEventArgs> PostAttributeBaseChange;

    /// <summary>仅在已发布最终值变化时触发。</summary>
    public event EventHandler<AttributeChangedEventArgs> PostAttributeChange;

    /// <summary>
    /// 获取单个属性的事件入口。相同属性定义始终返回同一个事件对象。
    /// </summary>
    public AttributeChangeEvent GetAttributeChangeEvent(AttributeDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        GetOrCreateState(definition);
        if (_changeEvents.TryGetValue(definition, out AttributeChangeEvent changeEvent))
        {
            return changeEvent;
        }

        changeEvent = new AttributeChangeEvent(this, definition);
        _changeEvents.Add(definition, changeEvent);
        return changeEvent;
    }

    /// <summary>
    /// 为下一帧重置普通属性，保留资源属性的当前值。
    /// </summary>
    public void ResetForTick()
    {
        RequireCollectionPhase();
        foreach (AttributeState state in _states.Values)
        {
            if (state.Definition.Kind == AttributeKind.Resource)
                continue;
            state.BaseValue = state.Definition.DefaultBaseValue;
            state.ExtraFlatValue = 0f;
            state.ExtraPercentValue = 0f;
        }
    }

    /// <summary>
    /// 增加或减少白值，返回本次写入实际造成的白值差额。
    /// 普通属性累积到本帧候选值，资源属性立即执行前置修正、裁剪和通知。
    /// 普通属性返回的差额尚未经过提交时的钩子，不代表最终值变化量。
    /// </summary>
    public float AddBase(AttributeDefinition definition, float value)
    {
        RequireWriteAllowed(definition);
        float previous = GetOrCreateState(definition).BaseValue;
        return SetBase(definition, previous + value) - previous;
    }

    public void AddExtra(AttributeDefinition definition, float value, AttributeModifierKind kind)
    {
        RequireKind(definition, AttributeKind.Rebuilt);
        RequireCollectionPhase();
        AttributeState state = GetOrCreateState(definition);
        switch (kind)
        {
            case AttributeModifierKind.Flat:
                state.ExtraFlatValue += value;
                break;
            case AttributeModifierKind.Percent:
                state.ExtraPercentValue += value;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown attribute modifier kind.");
        }
    }

    public void AddExtraFlat(AttributeDefinition definition, float value)
    {
        AddExtra(definition, value, AttributeModifierKind.Flat);
    }

    public void AddExtraPercent(AttributeDefinition definition, float value)
    {
        AddExtra(definition, value, AttributeModifierKind.Percent);
    }

    /// <summary>读取已发布快照；首次发布前返回定义默认值，不读取本帧候选值。</summary>
    public AttributeSnapshot Get(AttributeDefinition definition)
    {
        return GetPublished(definition);
    }

    public float GetFinalValue(AttributeDefinition definition)
    {
        return Get(definition).FinalValue;
    }

    /// <summary>读取正在收集的候选快照，尚未经过本次提交的前置修正。</summary>
    public AttributeSnapshot GetPending(AttributeDefinition definition)
    {
        return Calculate(GetOrCreateState(definition));
    }

    /// <summary>读取候选最终值，尚未经过本次提交的前置修正。</summary>
    public float GetPendingFinalValue(AttributeDefinition definition)
    {
        return GetPending(definition).FinalValue;
    }

    /// <summary>
    /// 设置白值，返回本次写入后的白值，不改动已有绿值贡献。
    /// 普通属性等待 Commit 时修正和发布；资源属性立即执行前置修正、静态范围裁剪和通知。
    /// 发布后的回调可以写入其他资源；同一调用链不能重复写入正在通知的资源。
    /// 资源前置失败时不写入；若错误报告器正常返回，则返回保留的白值。
    /// </summary>
    public float SetBase(AttributeDefinition definition, float value)
    {
        RequireWriteAllowed(definition);
        if (!float.IsFinite(value))
            throw new ArgumentOutOfRangeException(nameof(value), "属性白值必须是有限数值。");

        AttributeState state = GetOrCreateState(definition);
        if (definition.Kind == AttributeKind.Rebuilt)
        {
            state.BaseValue = value;
            return state.BaseValue;
        }

        if (!_writingResources.Add(definition))
            throw new InvalidOperationException($"不能在同一调用链中重复写入资源 '{definition.DiagnosticName}'。");
        bool isOutermost = _publicationErrors == null;
        if (isOutermost)
            _publicationErrors = new();
        try
        {
            bool isInitialPublication = !state.HasPublishedValue;
            AttributeSnapshot previous = state.PublishedValue;
            var args = new PreAttributeChangeEventArgs(definition, state.HasPublishedValue,
                previous, value, 0f, 0f);
            if (!TryPrepare(args, out AttributeSnapshot current))
                return state.BaseValue;
            Publish(state, current);
            NotifyPublication(definition, isInitialPublication, previous, current);
            return current.BaseValue;
        }
        finally
        {
            _writingResources.Remove(definition);
            if (isOutermost)
                ReportPublicationErrors();
        }
    }

    /// <summary>
    /// 获取最近一次发布的快照。首次发布前返回定义默认值经过静态范围裁剪后的快照。
    /// </summary>
    public AttributeSnapshot GetPublished(AttributeDefinition definition)
    {
        AttributeState state = GetOrCreateState(definition);
        return state.PublishedValue;
    }

    /// <summary>获取最近一次发布的最终值。</summary>
    public float GetPublishedFinalValue(AttributeDefinition definition)
    {
        return GetPublished(definition).FinalValue;
    }

    /// <summary>
    /// 提交本帧普通属性。先执行前置修正，再统一发布，最后按首次发布或实际变化通知。
    /// 前置失败保留旧快照；后置失败继续其余通知。错误在最外层操作结束后统一报告。
    /// </summary>
    public void Commit()
    {
        if (_isCommitting || _writingResources.Count > 0)
        {
            throw new InvalidOperationException("Cannot call Commit from an attribute publication handler.");
        }

        _isCommitting = true;
        _publicationErrors = new();
        try
        {
            CommitCore();
        }
        finally
        {
            _isPreparing = false;
            _isCommitting = false;
            ReportPublicationErrors();
        }
    }

    private void CommitCore()
    {
        // 资源只在显式写入时发布，避免发布后钩子改写资源后，又收到本批次预先计算的旧资源快照。
        // 复制普通属性列表，允许回调查询或注册其他属性；新普通属性从下一次提交开始发布。
        AttributeState[] states = _states.Values
            .Where(state => state.Definition.Kind == AttributeKind.Rebuilt).ToArray();
        var publications = new (bool IsInitial, AttributeSnapshot Previous, AttributeSnapshot Current)[states.Length];
        for (int i = 0; i < states.Length; i++)
        {
            AttributeState state = states[i];
            bool isInitialPublication = !state.HasPublishedValue;
            AttributeSnapshot previous = state.PublishedValue;
            PreAttributeChangeEventArgs changeArgs = new(state.Definition,
                state.HasPublishedValue, previous, state.BaseValue, state.ExtraFlatValue,
                state.ExtraPercentValue);
            // 任一前置修正失败，整批普通属性不发布，保留上一次完整快照。
            if (!TryPrepare(changeArgs, out AttributeSnapshot current))
                return;

            publications[i] = (isInitialPublication, previous, current);
        }

        // 先发布全部普通属性，确保发布后的回调能读取其他普通属性的本批次最终值。
        for (int i = 0; i < states.Length; i++)
        {
            AttributeState state = states[i];
            AttributeSnapshot values = publications[i].Current;
            Publish(state, values);
        }

        // 普通属性首次发布只通知初始化；后续数值不变时不触发后置钩子。
        for (int i = 0; i < states.Length; i++)
        {
            var publication = publications[i];
            NotifyPublication(states[i].Definition, publication.IsInitial,
                publication.Previous, publication.Current);
        }
    }

    private void InvokePreAttributeChange(PreAttributeChangeEventArgs args)
    {
        PreAttributeChange?.Invoke(this, args);
        if (_changeEvents.TryGetValue(args.Definition, out AttributeChangeEvent changeEvent))
            changeEvent.InvokePreAttributeChange(args);
    }

    private bool TryPrepare(PreAttributeChangeEventArgs args, out AttributeSnapshot current)
    {
        _isPreparing = true;
        try
        {
            InvokePreAttributeChange(args);
            current = args.Definition.Kind == AttributeKind.Resource
                ? CalculateResource(args) : args.Proposed;
            return true;
        }
        catch (Exception exception)
        {
            _publicationErrors.Add(new InvalidOperationException(
                $"属性 '{args.Definition.DiagnosticName}' 前置修正失败，本次写入未发布。", exception));
            current = default;
            return false;
        }
        finally
        {
            _isPreparing = false;
        }
    }

    /// <summary>逐个派发后置处理器；嵌套资源写入与外层提交共享异常收集。</summary>
    internal void InvokeNotification<T>(EventHandler<T> handlers, T args,
        AttributeDefinition definition, string eventName) where T : EventArgs
    {
        if (handlers == null)
            return;

        foreach (EventHandler<T> handler in handlers.GetInvocationList())
        {
            try
            {
                handler(this, args);
            }
            catch (Exception exception)
            {
                _publicationErrors.Add(new InvalidOperationException(
                    $"属性 '{definition.DiagnosticName}' 的 {eventName} 处理器 " +
                    $"'{handler.Method.DeclaringType?.FullName}.{handler.Method.Name}' 失败。", exception));
            }
        }
    }

    private void ReportPublicationErrors()
    {
        List<Exception> errors = _publicationErrors;
        _publicationErrors = null;
        if (errors == null || errors.Count == 0)
            return;

        var aggregate = new AggregateException("属性发布发生错误，详情见内部异常。", errors);
        if (_errorReporter != null)
            _errorReporter(aggregate);
        else
            throw aggregate;
    }

    private void NotifyPublication(AttributeDefinition definition, bool isInitialPublication,
        AttributeSnapshot previous, AttributeSnapshot current)
    {
        _changeEvents.TryGetValue(definition, out AttributeChangeEvent changeEvent);
        if (isInitialPublication)
        {
            var initializedArgs = new AttributeInitializedEventArgs(definition, current);
            changeEvent?.InvokeInitialized(initializedArgs);
            InvokeNotification(AttributeInitialized, initializedArgs, definition, nameof(AttributeInitialized));
            return;
        }

        AttributeChangedEventArgs changedArgs = new(definition, previous, current);
        if (changedArgs.ChangeKind == AttributeChangeKind.None)
        {
            return;
        }

        // 单属性业务钩子先执行，组件级观察者随后读取该钩子修正过的相关资源。
        if (changedArgs.BaseValueChanged)
        {
            changeEvent?.InvokePostAttributeBaseChange(changedArgs);
            InvokeNotification(PostAttributeBaseChange, changedArgs, definition, nameof(PostAttributeBaseChange));
        }

        if (changedArgs.FinalValueChanged)
        {
            changeEvent?.InvokePostAttributeChange(changedArgs);
            InvokeNotification(PostAttributeChange, changedArgs, definition, nameof(PostAttributeChange));
        }
        changeEvent?.InvokeSnapshotChanged(changedArgs);
        InvokeNotification(AttributeSnapshotChanged, changedArgs, definition, nameof(AttributeSnapshotChanged));
    }

    private void RequireCollectionPhase()
    {
        if (_isCommitting || _isPreparing || _writingResources.Count > 0)
            throw new InvalidOperationException("发布期间不能重置或直接修改普通属性；前置修正请修改事件参数。");
    }

    private void RequireWriteAllowed(AttributeDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (_isPreparing)
            throw new InvalidOperationException("前置钩子只能通过事件参数修正候选值，不能直接写入属性。");
        if (definition.Kind == AttributeKind.Rebuilt)
            RequireCollectionPhase();
    }

    private static void Publish(AttributeState state, AttributeSnapshot values)
    {
        state.BaseValue = values.BaseValue;
        state.ExtraFlatValue = values.ExtraFlatValue;
        state.ExtraPercentValue = values.ExtraPercentValue;
        state.PublishedValue = values;
        state.HasPublishedValue = true;
    }

    private static void RequireKind(AttributeDefinition definition, AttributeKind kind)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (definition.Kind != kind)
            throw new InvalidOperationException($"Attribute '{definition.DiagnosticName}' is not a {kind} attribute.");
    }

    private static AttributeSnapshot CalculateResource(PreAttributeChangeEventArgs args)
    {
        if (!float.IsFinite(args.BaseValue))
            throw new InvalidOperationException("资源候选值必须是有限数值。");
        if (args.ExtraFlatValue != 0f || args.ExtraPercentValue != 0f)
            throw new InvalidOperationException("Resources cannot have extra modifiers; modify the resource maximum instead.");
        // 裁剪结果同时写回白值，避免再次写入时恢复已经丢弃的资源。
        return args.Proposed;
    }

    private AttributeState GetOrCreateState(AttributeDefinition definition)
    {
        if (definition == null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (_states.TryGetValue(definition, out AttributeState state))
        {
            return state;
        }

        state = new AttributeState(definition);
        if (definition.Kind == AttributeKind.Resource)
        {
            // 读取默认资源不等于初始化；首次显式写入才发送初始化通知。
            var args = new PreAttributeChangeEventArgs(definition, false, default,
                definition.DefaultBaseValue, 0f, 0f);
            Publish(state, CalculateResource(args));
            state.HasPublishedValue = false;
        }
        else
        {
            // 默认快照与候选值分离，首次提交前也不会泄露收集中的加成。
            state.PublishedValue = Calculate(state);
        }
        _states.Add(definition, state);
        return state;
    }

    private static AttributeSnapshot Calculate(AttributeState state)
    {
        float extraValue = state.ExtraFlatValue + state.BaseValue * state.ExtraPercentValue;
        float finalValue = state.Definition.ClampFinalValue(state.BaseValue + extraValue);
        return new AttributeSnapshot(state.BaseValue, extraValue, state.ExtraFlatValue,
            state.ExtraPercentValue, finalValue);
    }
}
