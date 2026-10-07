using System.Linq;
using KL.Drawing;

namespace KL.Projectiles;

/// <summary>Manages trails that are refreshed by their owner while being drawn.</summary>
public sealed class TrailManager : ModSystem
{
    public sealed class TrailHandle
    {
        internal int Id { get; }

        internal TrailHandle(int id) => Id = id;
    }

    private sealed class TrailEntry
    {
        public required TrailHandle Handle;
        public required Vector2[] Points;
        public required Action<Vector2[], float> Draw;
        public required int Lifetime;
        public required int MaxLifetime;
        public required LayerDrawRequestSystem.DrawTargetLayer Layer;
        public required LayerDrawRequestSystem.DrawTiming Timing;
        public required bool Bloom;
        public required bool ShrinkPathOnExpire;
    }

    private static readonly Dictionary<int, TrailEntry> Entries = new();
    private static int nextId;

    /// <summary>Creates or refreshes a trail. Point data is copied and survives its submitting projectile.</summary>
    /// <remarks>Update the source points before calling this method. For KLProjectile.OldCenter,
    /// call base.PreDraw(ref lightColor) first; later updates cannot change this copied snapshot.</remarks>
    public static void CreateOrUpdateTrail<TState>(
        ref TrailHandle handle,
        Vector2[] points,
        int lifetime,
        TState state,
        Action<TState, Vector2[], float> draw,
        LayerDrawRequestSystem.DrawTargetLayer layer = LayerDrawRequestSystem.DrawTargetLayer.Projectiles,
        LayerDrawRequestSystem.DrawTiming timing = LayerDrawRequestSystem.DrawTiming.After,
        bool bloom = false,
        bool shrinkPathOnExpire = true)
    {
        if (Main.dedServ || points == null || points.Length < 2 || lifetime <= 0 || draw == null)
            return;

        if (handle == null || !Entries.TryGetValue(handle.Id, out TrailEntry entry))
        {
            handle = new TrailHandle(++nextId);
            entry = new TrailEntry
            {
                Handle = handle,
                Points = Array.Empty<Vector2>(),
                Draw = (_, _) => { },
                Lifetime = lifetime,
                MaxLifetime = lifetime,
                Layer = layer,
                Timing = timing,
                Bloom = bloom,
                ShrinkPathOnExpire = shrinkPathOnExpire
            };
            Entries.Add(handle.Id, entry);
        }

        entry.Points = (Vector2[])points.Clone();
        entry.Lifetime = lifetime;
        entry.MaxLifetime = lifetime;
        entry.Layer = layer;
        entry.Timing = timing;
        entry.Bloom = bloom;
        entry.ShrinkPathOnExpire = shrinkPathOnExpire;
        entry.Draw = (submittedPoints, remainingRatio) => draw(state, submittedPoints, remainingRatio);
    }

    public override void PostUpdateProjectiles()
    {
        if (!Main.gamePaused)
        {
            foreach (int id in Entries.Keys.ToArray())
            {
                TrailEntry entry = Entries[id];
                if (--entry.Lifetime <= 0)
                    Entries.Remove(id);
            }
        }

        base.PostUpdateProjectiles();
    }

    public static void SubmitDrawRequests()
    {
        foreach (TrailEntry entry in Entries.Values)
        {
            TrailEntry submittedEntry = entry;
            Action draw = () =>
            {
                float remainingRatio = MathHelper.Clamp(
                    submittedEntry.Lifetime / (float)submittedEntry.MaxLifetime, 0f, 1f);
                Vector2[] points = submittedEntry.ShrinkPathOnExpire
                    ? TrimPath(submittedEntry.Points, remainingRatio)
                    : submittedEntry.Points;
                submittedEntry.Draw(points, remainingRatio);
            };
            string id = $"KL:ManagedTrail:{entry.Handle.Id}";
            if (entry.Bloom)
                LayerDrawRequestSystem.RequestBloom(id, entry.Layer, entry.Timing, draw);
            else
                LayerDrawRequestSystem.Request(id, entry.Layer, entry.Timing, draw);
        }
    }

    private static Vector2[] TrimPath(Vector2[] points, float remainingRatio)
    {
        if (remainingRatio >= 1f)
            return points;

        float totalLength = 0f;
        for (int i = 1; i < points.Length; i++)
            totalLength += Vector2.Distance(points[i - 1], points[i]);

        float visibleLength = totalLength * remainingRatio;
        if (visibleLength <= 0f || totalLength <= 0f)
            return Array.Empty<Vector2>();

        var trimmedPoints = new List<Vector2> { points[0] };
        float traversed = 0f;
        for (int i = 1; i < points.Length; i++)
        {
            float segmentLength = Vector2.Distance(points[i - 1], points[i]);
            if (traversed + segmentLength >= visibleLength)
            {
                if (segmentLength > 0f)
                {
                    float progress = (visibleLength - traversed) / segmentLength;
                    trimmedPoints.Add(Vector2.Lerp(points[i - 1], points[i], progress));
                }
                break;
            }

            trimmedPoints.Add(points[i]);
            traversed += segmentLength;
        }

        return trimmedPoints.ToArray();
    }

    public override void OnWorldUnload()
    {
        Entries.Clear();
        base.OnWorldUnload();
    }

    public override void Unload()
    {
        Entries.Clear();
        nextId = 0;
        base.Unload();
    }
}
