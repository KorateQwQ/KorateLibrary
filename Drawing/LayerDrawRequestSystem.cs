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

        var requestPoint = (layer, timing);
        if (!Requests.TryGetValue(requestPoint, out Dictionary<string, DrawRequestGroup> groups))
        {
            groups = new Dictionary<string, DrawRequestGroup>();
            Requests[requestPoint] = groups;
            IdOrder[requestPoint] = new List<string>();
        }

        if (!groups.TryGetValue(id, out DrawRequestGroup group))
        {
            group = new DrawRequestGroup();
            groups[id] = group;
            IdOrder[requestPoint].Add(id);
        }

        group.Entries.Add(new DrawRequestEntry(drawAction));
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
    }

    public static void Flush(DrawTargetLayer layer, DrawTiming timing)
    {
        var requestPoint = (layer, timing);
        if (!Requests.TryGetValue(requestPoint, out Dictionary<string, DrawRequestGroup> groups))
        {
            return;
        }

        if (!IdOrder.TryGetValue(requestPoint, out List<string> order))
        {
            return;
        }

        List<string> snapshotOrder = new List<string>(order);
        Requests.Remove(requestPoint);
        IdOrder.Remove(requestPoint);

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
