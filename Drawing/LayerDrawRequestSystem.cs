namespace KL.Drawing;

/// <summary>
/// 按主绘制流程中的目标层，执行自定义绘制请求。
/// 同一个 id 的请求会连续执行，不同 id 之间的顺序不做强保证。
/// </summary>
public sealed class LayerDrawRequestSystem : ModSystem
{
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

    private sealed class DrawRequestGroup
    {
        public readonly List<DrawRequestEntry> Entries = new();
    }

    private static readonly Dictionary<(DrawTargetLayer Layer, DrawTiming Timing), Dictionary<string, DrawRequestGroup>> Requests = new();
    private static readonly Dictionary<(DrawTargetLayer Layer, DrawTiming Timing), List<string>> IdOrder = new();
    private static readonly Dictionary<(DrawTargetLayer Layer, DrawTiming Timing), Dictionary<string, DrawRequestGroup>> BloomRequests = new();
    private static readonly Dictionary<(DrawTargetLayer Layer, DrawTiming Timing), List<string>> BloomIdOrder = new();

    public static void Request(string id, DrawTargetLayer layer, DrawTiming timing, Action drawAction)
    {
        if (drawAction == null)
        {
            return;
        }

        Request(id, layer, timing, _ => drawAction());
    }

    public static void Request(string id, DrawTargetLayer layer, DrawTiming timing, Action<DrawRequestContext> drawAction)
    {
        if (string.IsNullOrWhiteSpace(id) || drawAction == null)
        {
            return;
        }

        AddRequest(Requests, IdOrder, id, layer, timing, drawAction);
    }

    /// <summary>
    /// Requests a complete visual draw that is collected with other bloom requests at the same layer.
    /// KL renders the callback once into an isolated target, then composites its normal appearance
    /// and its blurred bloom back into the requested layer.
    /// </summary>
    public static void RequestBloom(string id, DrawTargetLayer layer, DrawTiming timing, Action drawAction)
    {
        if (drawAction == null)
        {
            return;
        }

        RequestBloom(id, layer, timing, _ => drawAction());
    }

    /// <inheritdoc cref="RequestBloom(string, DrawTargetLayer, DrawTiming, Action)" />
    public static void RequestBloom(string id, DrawTargetLayer layer, DrawTiming timing,
        Action<DrawRequestContext> drawAction)
    {
        if (string.IsNullOrWhiteSpace(id) || drawAction == null)
        {
            return;
        }

        AddRequest(BloomRequests, BloomIdOrder, id, layer, timing, drawAction);
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
        return BloomRequests.TryGetValue((layer, timing), out Dictionary<string, DrawRequestGroup> groups) &&
               groups.Count > 0;
    }

    private static void AddRequest(
        Dictionary<(DrawTargetLayer Layer, DrawTiming Timing), Dictionary<string, DrawRequestGroup>> requestStore,
        Dictionary<(DrawTargetLayer Layer, DrawTiming Timing), List<string>> orderStore,
        string id,
        DrawTargetLayer layer,
        DrawTiming timing,
        Action<DrawRequestContext> drawAction)
    {
        var requestPoint = (layer, timing);
        if (!requestStore.TryGetValue(requestPoint, out Dictionary<string, DrawRequestGroup> groups))
        {
            groups = new Dictionary<string, DrawRequestGroup>();
            requestStore[requestPoint] = groups;
            orderStore[requestPoint] = new List<string>();
        }

        if (!groups.TryGetValue(id, out DrawRequestGroup group))
        {
            group = new DrawRequestGroup();
            groups[id] = group;
            orderStore[requestPoint].Add(id);
        }

        group.Entries.Add(new DrawRequestEntry(drawAction));
    }

    private static bool Flush(
        Dictionary<(DrawTargetLayer Layer, DrawTiming Timing), Dictionary<string, DrawRequestGroup>> requestStore,
        Dictionary<(DrawTargetLayer Layer, DrawTiming Timing), List<string>> orderStore,
        DrawTargetLayer layer,
        DrawTiming timing)
    {
        var requestPoint = (layer, timing);
        if (!requestStore.TryGetValue(requestPoint, out Dictionary<string, DrawRequestGroup> groups) ||
            !orderStore.TryGetValue(requestPoint, out List<string> order))
        {
            return false;
        }

        List<string> snapshotOrder = new List<string>(order);
        requestStore.Remove(requestPoint);
        orderStore.Remove(requestPoint);

        foreach (string id in snapshotOrder)
        {
            if (!groups.TryGetValue(id, out DrawRequestGroup group))
            {
                continue;
            }

            int actionCount = group.Entries.Count;
            for (int i = 0; i < actionCount; i++)
            {
                DrawRequestContext context = new(id, layer, timing, i, actionCount);
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
