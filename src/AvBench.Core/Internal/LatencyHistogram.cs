using System.Diagnostics;

namespace AvBench.Core.Internal;

public sealed class LatencyHistogram
{
    private readonly List<double> _samplesUs = [];
    private double _sumUs;

    public int Count => _samplesUs.Count;

    public double MeanUs => Count > 0 ? _sumUs / Count : 0.0;

    public double MaxUs => Count > 0 ? _samplesUs.Max() : 0.0;

    public void RecordElapsedTicks(long startTimestamp, long endTimestamp)
    {
        var elapsedTicks = endTimestamp - startTimestamp;
        var microseconds = elapsedTicks * 1_000_000d / Stopwatch.Frequency;
        _samplesUs.Add(microseconds);
        _sumUs += microseconds;
    }

    public double GetPercentile(double percentile)
    {
        if (Count == 0)
        {
            return 0.0;
        }

        var ordered = _samplesUs.OrderBy(static sample => sample).ToArray();
        var rank = percentile switch
        {
            <= 0 => 0,
            >= 100 => ordered.Length - 1,
            _ => (int)Math.Ceiling(percentile / 100d * ordered.Length) - 1
        };

        return ordered[Math.Clamp(rank, 0, ordered.Length - 1)];
    }
}
