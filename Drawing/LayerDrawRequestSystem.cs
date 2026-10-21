namespace KL.Drawing;

/// <summary>
/// 按主绘制流程中的目标层，执行自定义绘制请求。
/// 同层同一时机按 order 从小到大绘制；同 order 按 id 首次提交顺序执行。
/// 同一个 (id, order) 的请求会连续执行；同层同一时机的 Bloom 请求共享一次后处理。
/// </summary>
public sealed class LayerDrawRequestSystem : ModSystem
{
    /// <summary>Draw managed trails below projectile bodies within a shared bloom target.</summary>
    public const int TrailOrder = -100;
    /// <summary>Default order of projectile bodies.</summary>
    public const int ProjectileOrder = 0;
    /// <summary>Draw bloom particles above projectile bodies within a shared bloom target.</summary>
    public const int DustOrder = 100;

    public enum DrawTargetLayer
    {
        BehindNPCsAndTiles,
        BehindNPCs,
        BehindProjectiles,
        Projectiles,
        PlayersAfterProjectiles,
        OverPlayers,
        Dust,
        OverWiresUI,
        InfernoRings
    }

    public enum DrawTiming
    {
        Before,
        After
    }

    public enum DrawAnchor
    {
        AfterBehindNPCsAndTiles,
        AfterBehindNPCs,
        AfterBehindProjectiles,
        AfterDrawProjectiles,
        AfterDrawPlayersAfterProjectiles,
        AfterDrawDust,
        AfterOverWiresUI,
        AfterDrawInfernoRings
    }

    public readonly record struct DrawRequestContext(
        string Id,
        DrawTargetLayer Layer,
        DrawTiming Timing,
        int ActionIndex,
        int ActionCount)
    {
        /// <summary>Sort order within this layer/timing; lower values draw first.</summary>
        public int Order { get; init; }
        public bool IsFirst => ActionIndex == 0;
        public bool IsLast => ActionIndex == ActionCount - 1;
    }

    private sealed class DrawRequestEntry
    {
        public Action<DrawRequestContext> DrawAction { get; }

        public DrawRequestEntry(Action<DrawRequestContext> drawAction)
        {
            DrawAction = drawAction;
        }
    }

    private sealed class DrawRequestGroup(int sequence)
    {
        public readonly int Sequence = sequence;
        public readonly List<DrawRequestEntry> Entries = new();
    }

    private static readonly Dictionary<(DrawTargetLayer Layer, DrawTiming Timing), Dictionary<(string Id, int Order), DrawRequestGroup>> Requests = new();
    private static readonly Dictionary<(DrawTargetLayer Layer, DrawTiming Timing), List<(string Id, int Order)>> IdOrder = new();
    private static readonly Dictionary<(DrawTargetLayer Layer, DrawTiming Timing), Dictionary<(string Id, int Order), DrawRequestGroup>> BloomRequests = new();
    private static readonly Dictionary<(DrawTargetLayer Layer, DrawTiming Timing), List<(string Id, int Order)>> BloomIdOrder = new();

    public static void Request(string id, DrawTargetLayer layer, DrawTiming timing, Action drawAction, int order = 0)
    {
        if (drawAction == null)
        {
            return;
        }

        Request(id, layer, timing, _ => drawAction(), order);
    }

    public static void Request(string id, DrawTargetLayer layer, DrawTiming timing, Action<DrawRequestContext> drawAction, int order = 0)
    {
        if (string.IsNullOrWhiteSpace(id) || drawAction == null)
        {
            return;
        }

        AddRequest(Requests, IdOrder, id, layer, timing, drawAction, order);
    }

    /// <summary>
    /// Requests a complete visual draw that is collected with other bloom requests at the same layer.
    /// KL renders the callback once into an isolated target, then composites its normal appearance
    /// and its blurred bloom back into the requested layer.
    /// Lower order values draw first within the same layer/timing target; they do not add a bloom pass.
    /// Draw through KL helpers, or pass direct device blend states through
    /// DrawSystem.GetBlendStateForBloom, so additive light and alpha coverage remain distinct.
    /// </summary>
    public static void RequestBloom(string id, DrawTargetLayer layer, DrawTiming timing, Action drawAction, int order = 0)
    {
        if (drawAction == null)
        {
            return;
        }

        RequestBloom(id, layer, timing, _ => drawAction(), order);
    }

    /// <inheritdoc cref="RequestBloom(string, DrawTargetLayer, DrawTiming, Action, int)" />
    public static void RequestBloom(string id, DrawTargetLayer layer, DrawTiming timing,
        Action<DrawRequestContext> drawAction, int order = 0)
    {
        if (string.IsNullOrWhiteSpace(id) || drawAction == null)
        {
            return;
        }

        AddRequest(BloomRequests, BloomIdOrder, id, layer, timing, drawAction, order);
    }

    public static void Request(string id, DrawAnchor anchor, Action drawAction)
    {
        (DrawTargetLayer layer, DrawTiming timing) = AnchorToRequestPoint(anchor);
        Request(id, layer, timing, drawAction);
    }

    public static void Request(string id, DrawAnchor anchor, Action<DrawRequestContext> drawAction)
    {
        (DrawTargetLayer layer, DrawTiming timing) = AnchorToRequestPoint(anchor);
        Request(id, layer, timing, drawAction);
    }

    public static void RequestBefore(string id, DrawTargetLayer layer, Action drawAction) => Request(id, layer, DrawTiming.Before, drawAction);

    public static void RequestBefore(string id, DrawTargetLayer layer, Action<DrawRequestContext> drawAction) => Request(id, layer, DrawTiming.Before, drawAction);

    public static void RequestAfter(string id, DrawTargetLayer layer, Action drawAction) => Request(id, layer, DrawTiming.After, drawAction);

    public static void RequestAfter(string id, DrawTargetLayer layer, Action<DrawRequestContext> drawAction) => Request(id, layer, DrawTiming.After, drawAction);

    public static void RequestAfterDust(string id, Action drawAction) => RequestAfter(id, DrawTargetLayer.Dust, drawAction);

    public static void RequestAfterDust(string id, Action<DrawRequestContext> drawAction) => RequestAfter(id, DrawTargetLayer.Dust, drawAction);

    public static void ClearFrame()
    {
        Requests.Clear();
        IdOrder.Clear();
        BloomRequests.Clear();
        BloomIdOrder.Clear();
    }

    public static void Flush(DrawTargetLayer layer, DrawTiming timing)
    {
        Flush(Requests, IdOrder, layer, timing);
    }

    /// <summary>
    /// Runs complete visual draws registered for a layer. DrawSystem owns the render target and
    /// invokes this once per populated layer.
    /// </summary>
    public static bool FlushBloom(DrawTargetLayer layer, DrawTiming timing)
    {
        return Flush(BloomRequests, BloomIdOrder, layer, timing);
    }

    public static bool HasBloomRequests(DrawTargetLayer layer, DrawTiming timing)
    {
        return BloomRequests.TryGetValue((layer, timing), out var groups) &&
               groups.Count > 0;
    }

    private static void AddRequest(
        Dictionary<(DrawTargetLayer Layer, DrawTiming Timing), Dictionary<(string Id, int Order), DrawRequestGroup>> requestStore,
        Dictionary<(DrawTargetLayer Layer, DrawTiming Timing), List<(string Id, int Order)>> orderStore,
        string id,
        DrawTargetLayer layer,
        DrawTiming timing,
        Action<DrawRequestContext> drawAction,
        int order)
    {
        var requestPoint = (layer, timing);
        if (!requestStore.TryGetValue(requestPoint, out var groups))
        {
            groups = new Dictionary<(string Id, int Order), DrawRequestGroup>();
            requestStore[requestPoint] = groups;
            orderStore[requestPoint] = new List<(string Id, int Order)>();
        }

        var key = (id, order);
        if (!groups.TryGetValue(key, out DrawRequestGroup group))
        {
            group = new DrawRequestGroup(groups.Count);
            groups[key] = group;
            orderStore[requestPoint].Add(key);
        }

        group.Entries.Add(new DrawRequestEntry(drawAction));
    }

    private static bool Flush(
        Dictionary<(DrawTargetLayer Layer, DrawTiming Timing), Dictionary<(string Id, int Order), DrawRequestGroup>> requestStore,
        Dictionary<(DrawTargetLayer Layer, DrawTiming Timing), List<(string Id, int Order)>> orderStore,
        DrawTargetLayer layer,
        DrawTiming timing)
    {
        var requestPoint = (layer, timing);
        if (!requestStore.TryGetValue(requestPoint, out var groups) ||
            !orderStore.TryGetValue(requestPoint, out var order))
        {
            return false;
        }

        var snapshotOrder = new List<(string Id, int Order)>(order);
        snapshotOrder.Sort((left, right) =>
        {
            int comparison = left.Order.CompareTo(right.Order);
            return comparison != 0 ? comparison : groups[left].Sequence.CompareTo(groups[right].Sequence);
        });
        requestStore.Remove(requestPoint);
        orderStore.Remove(requestPoint);

        foreach (var key in snapshotOrder)
        {
            if (!groups.TryGetValue(key, out DrawRequestGroup group))
            {
                continue;
            }

            int actionCount = group.Entries.Count;
            for (int i = 0; i < actionCount; i++)
            {
                DrawRequestContext context = new(key.Id, layer, timing, i, actionCount) { Order = key.Order };
                group.Entries[i].DrawAction?.Invoke(context);
            }
        }

        return true;
    }

    public static void Flush(DrawAnchor anchor)
    {
        (DrawTargetLayer layer, DrawTiming timing) = AnchorToRequestPoint(anchor);
        Flush(layer, timing);
    }

    private static (DrawTargetLayer Layer, DrawTiming Timing) AnchorToRequestPoint(DrawAnchor anchor)
    {
        return anchor switch
        {
            DrawAnchor.AfterBehindNPCsAndTiles => (DrawTargetLayer.BehindNPCsAndTiles, DrawTiming.After),
            DrawAnchor.AfterBehindNPCs => (DrawTargetLayer.BehindNPCs, DrawTiming.After),
            DrawAnchor.AfterBehindProjectiles => (DrawTargetLayer.BehindProjectiles, DrawTiming.After),
            DrawAnchor.AfterDrawProjectiles => (DrawTargetLayer.Projectiles, DrawTiming.After),
            DrawAnchor.AfterDrawPlayersAfterProjectiles => (DrawTargetLayer.PlayersAfterProjectiles, DrawTiming.After),
            DrawAnchor.AfterDrawDust => (DrawTargetLayer.Dust, DrawTiming.After),
            DrawAnchor.AfterOverWiresUI => (DrawTargetLayer.OverWiresUI, DrawTiming.After),
            DrawAnchor.AfterDrawInfernoRings => (DrawTargetLayer.InfernoRings, DrawTiming.After),
            _ => (DrawTargetLayer.Projectiles, DrawTiming.After)
        };
    }
}
