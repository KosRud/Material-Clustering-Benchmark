using System;

public static class BenchmarkHelper
{
    private static readonly System.Diagnostics.Stopwatch stopwatch =
        new System.Diagnostics.Stopwatch();

    private static void RunWithoutGC(Action action)
    {
        var savedGCMode = UnityEngine.Scripting.GarbageCollector.GCMode;

        UnityEngine.Scripting.GarbageCollector.GCMode = UnityEngine
            .Scripting
            .GarbageCollector
            .Mode
            .Disabled;
        {
            // no GC section
            action();
        }
        UnityEngine.Scripting.GarbageCollector.GCMode = savedGCMode;
    }

    /// <summary>
    /// Runs the provided <paramref name="action" /> and measures the time it required to finish. Ensures GC will not trigger while running the action, as it would interfere with the measurement.
    /// </summary>
    /// <param name="action">Action to run.</param>
    /// <returns>Time in milliseconds.</returns>
    public static long MeasureTime(Action action)
    {
        stopwatch.Restart();
        RunWithoutGC(action);
        stopwatch.Stop();
        return stopwatch.ElapsedMilliseconds;
    }
}
