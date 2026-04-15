using System.Diagnostics;

namespace AvBench.Core.Internal;

public sealed class LatencyHistogram
{
    private readonly long[] _ticks;
    private int _count;
    private long _totalTicks;
    private bool _sorted;

    public LatencyHistogram(int capacity)
    {
        _ticks = new long[Math.Max(1, capacity)];
    }

    public int Count => _count;

    public double MeanUs => Count > 0
        ? _totalTicks * 1_000_000d / Stopwatch.Frequency / Count
        : 0.0;

    public double MaxUs
    {
        get
        {
            EnsureSorted();
            return Count > 0 ? TicksToMicroseconds(_ticks[Count - 1]) : 0.0;
        }
    }

    public void Record(long elapsedTicks)
    {
        if (_count >= _ticks.Length)
        {
            throw new InvalidOperationException($"Latency histogram capacity {_ticks.Length} exceeded.");
        }

        _ticks[_count++] = elapsedTicks;
        _totalTicks += elapsedTicks;
        _sorted = false;
    }

    public double GetPercentile(double percentile)
    {
        if (Count == 0)
        {
            return 0.0;
        }

        EnsureSorted();
        var rank = percentile switch
        {
            <= 0 => 0,
            >= 100 => Count - 1,
            _ => (int)Math.Ceiling(percentile / 100d * Count) - 1
        };

        return TicksToMicroseconds(_ticks[Math.Clamp(rank, 0, Count - 1)]);
    }

    public void RecordElapsedTicks(long startTimestamp, long endTimestamp)
        => Record(endTimestamp - startTimestamp);

    private void EnsureSorted()
    {
        if (_sorted || Count == 0)
        {
            return;
        }

        Array.Sort(_ticks, 0, Count);
        _sorted = true;
    }

    private static double TicksToMicroseconds(long ticks)
        => ticks * 1_000_000d / Stopwatch.Frequency;
}
