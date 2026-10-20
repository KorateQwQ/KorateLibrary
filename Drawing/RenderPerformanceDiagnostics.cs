using System.Diagnostics;

namespace KL.Drawing;

/// <summary>Low-cost wall-clock diagnostics. No GPU readbacks, queries or forced synchronization.</summary>
public sealed class RenderPerformanceDiagnostics : ModSystem
{
    private static long previousDrawStart, lastReport;
    private static double updateMs, bloomMs, allocationMs, slowestBloomMs;
    private static int bloomCount, allocationCount;
    private static LayerDrawRequestSystem.DrawTargetLayer slowestLayer;
    private static LayerDrawRequestSystem.DrawTiming slowestTiming;
    private static string lastSkill = "none";
    private static long lastSkillTime;
    private static double lastSkillMs;
    private static double peakSkillMs;
    private static string peakSkill = "none";
    private static int gen0, gen1, gen2;

    public override void Load()
    {
        if (Main.dedServ) return;
        On_Main.Update += MeasureUpdate;
        On_Main.Draw += MeasureDraw;
        gen0 = GC.CollectionCount(0);
        gen1 = GC.CollectionCount(1);
        gen2 = GC.CollectionCount(2);
    }

    public override void Unload()
    {
        On_Main.Update -= MeasureUpdate;
        On_Main.Draw -= MeasureDraw;
        previousDrawStart = lastReport = lastSkillTime = 0;
        lastSkill = "none";
        lastSkillMs = 0;
        ResetFrame();
    }

    public readonly struct SkillMeasurement : IDisposable
    {
        private readonly string name;
        private readonly long start;
        internal SkillMeasurement(string name)
        {
            this.name = name;
            start = Stopwatch.GetTimestamp();
        }
        public void Dispose()
        {
            lastSkill = name;
            lastSkillTime = Stopwatch.GetTimestamp();
            lastSkillMs = Milliseconds(start, lastSkillTime);
            if (lastSkillMs > peakSkillMs)
            {
                peakSkillMs = lastSkillMs;
                peakSkill = name;
            }
        }
    }

    public static SkillMeasurement MeasureSkill(string name) => new(name);

    internal static void RecordBloom(long start, LayerDrawRequestSystem.DrawTargetLayer layer,
        LayerDrawRequestSystem.DrawTiming timing)
    {
        double elapsed = Milliseconds(start, Stopwatch.GetTimestamp());
        bloomMs += elapsed;
        bloomCount++;
        if (elapsed > slowestBloomMs)
        {
            slowestBloomMs = elapsed;
            slowestLayer = layer;
            slowestTiming = timing;
        }
    }

    internal static void RecordAllocation(long start)
    {
        allocationMs += Milliseconds(start, Stopwatch.GetTimestamp());
        allocationCount++;
    }

    private static double Milliseconds(long start, long end) => (end - start) * 1000.0 / Stopwatch.Frequency;

    private static void MeasureUpdate(On_Main.orig_Update orig, Main self, GameTime gameTime)
    {
        long start = Stopwatch.GetTimestamp();
        try { orig(self, gameTime); }
        finally { updateMs += Milliseconds(start, Stopwatch.GetTimestamp()); }
    }

    private void MeasureDraw(On_Main.orig_Draw orig, Main self, GameTime gameTime)
    {
        long start = Stopwatch.GetTimestamp();
        double gap = previousDrawStart == 0 ? 0 : Milliseconds(previousDrawStart, start);
        previousDrawStart = start;
        try { orig(self, gameTime); }
        finally
        {
            long end = Stopwatch.GetTimestamp();
            double drawMs = Milliseconds(start, end);
            int current0 = GC.CollectionCount(0), current1 = GC.CollectionCount(1), current2 = GC.CollectionCount(2);
            if (!Main.gameMenu && (drawMs >= 100 || updateMs >= 100 || gap >= 200) &&
                (lastReport == 0 || Milliseconds(lastReport, end) >= 1000))
            {
                lastReport = end;
                Mod.Logger.Warn($"[RenderStall] update={updateMs:F1}ms draw={drawMs:F1}ms drawGap={gap:F1}ms; " +
                    $"bloom={bloomMs:F1}ms/{bloomCount} passes worst={slowestBloomMs:F1}ms@{slowestLayer}/{slowestTiming}; " +
                    $"targetAlloc={allocationMs:F1}ms/{allocationCount}; skill={lastSkill}:{lastSkillMs:F1}ms " +
                    $"age={(lastSkillTime == 0 ? -1 : Milliseconds(lastSkillTime, end)):F0}ms; " +
                    $"peakSkill={peakSkill}:{peakSkillMs:F1}ms; " +
                    $"GC={current0 - gen0}/{current1 - gen1}/{current2 - gen2}; " +
                    $"screen={Main.screenWidth}x{Main.screenHeight} lighting={Lighting.Mode} waves={Main.WaveQuality} " +
                    $"bloomEnabled={DrawSystem.GetShouldBloom()} focus={Main.hasFocus} paused={Main.gamePaused}. " +
                    "Wall-clock timings only; drawGap includes time outside these hooks, not a GPU duration.");
            }
            gen0 = current0; gen1 = current1; gen2 = current2;
            ResetFrame();
        }
    }

    private static void ResetFrame()
    {
        updateMs = bloomMs = allocationMs = slowestBloomMs = 0;
        bloomCount = allocationCount = 0;
        peakSkillMs = 0;
        peakSkill = "none";
        slowestLayer = default;
        slowestTiming = default;
    }
}
