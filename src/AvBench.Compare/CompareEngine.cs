using AvBench.Core.Models;

namespace AvBench.Compare;

public static class CompareEngine
{
    private const double NoisyThresholdPct = 10.0;
    private const double AnomalySlowdownThresholdPct = -10.0;
    private const int MinimumReliableSessions = 3;

    public static List<ComparisonRow> Compare(
        IReadOnlyList<RunResult> baselineRuns,
        IReadOnlyDictionary<string, List<RunResult>> namedRuns)
    {
        var rows = new List<ComparisonRow>();
        var baselineName = baselineRuns.FirstOrDefault()?.AvName ?? "baseline-os";

        var allBaselineByScenario = baselineRuns
            .GroupBy(static run => run.ScenarioId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static group => group.Key, static group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        var baselineByScenario = baselineRuns
            .Where(static run => run.ExitCode == 0)
            .GroupBy(static run => run.ScenarioId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(static group => group.Key, static group => group.ToList(), StringComparer.OrdinalIgnoreCase);

        foreach (var (avName, runs) in namedRuns.OrderBy(static pair => pair.Key, StringComparer.OrdinalIgnoreCase))
        {
            var grouped = runs
                .GroupBy(static run => run.ScenarioId, StringComparer.OrdinalIgnoreCase)
                .OrderBy(static group => group.Key, StringComparer.OrdinalIgnoreCase);

            foreach (var group in grouped)
            {
                var scenarioRuns = group.ToList();
                var successfulRuns = scenarioRuns.Where(static run => run.ExitCode == 0).ToList();
                var hasFailures = successfulRuns.Count != scenarioRuns.Count;

                if (successfulRuns.Count == 0)
                {
                    rows.Add(new ComparisonRow
                    {
                        ScenarioId = group.Key,
                        AvName = avName,
                        AvProduct = scenarioRuns[0].AvProduct,
                        AvVersion = scenarioRuns[0].AvVersion,
                        BaselineName = baselineName,
                        Sessions = scenarioRuns.Count,
                        Status = "failed"
                    });
                    continue;
                }

                successfulRuns = TryExcludeOutlier(
                    successfulRuns,
                    static run => run.WallMs,
                    NoisyThresholdPct,
                    out var excludedRuns);

                allBaselineByScenario.TryGetValue(group.Key, out var allBaselineScenarioRuns);
                baselineByScenario.TryGetValue(group.Key, out var baselineScenarioRuns);
                var baselineHasFailures = allBaselineScenarioRuns is null
                    || baselineScenarioRuns is null
                    || baselineScenarioRuns.Count != allBaselineScenarioRuns.Count;

                var filteredBaselineRuns = baselineScenarioRuns ?? [];
                filteredBaselineRuns = TryExcludeOutlier(
                    filteredBaselineRuns,
                    static run => run.WallMs,
                    NoisyThresholdPct,
                    out var baselineExcludedRuns);

                var wallSamples = successfulRuns.Select(static run => (double)run.WallMs).ToArray();
                var cpuSamples = successfulRuns.Select(static run => (double)(run.UserCpuMs + run.KernelCpuMs)).ToArray();
                var kernelCpuSamples = successfulRuns.Select(static run => (double)run.KernelCpuMs).ToArray();
                var systemDiskReadSamples = successfulRuns.Select(static run => (double)run.SystemDiskReadBytes).ToArray();
                var systemDiskWriteSamples = successfulRuns.Select(static run => (double)run.SystemDiskWriteBytes).ToArray();
                var p50Samples = successfulRuns.Where(static run => run.P50Us.HasValue).Select(static run => run.P50Us!.Value).ToArray();
                var p95Samples = successfulRuns.Where(static run => run.P95Us.HasValue).Select(static run => run.P95Us!.Value).ToArray();
                var p99Samples = successfulRuns.Where(static run => run.P99Us.HasValue).Select(static run => run.P99Us!.Value).ToArray();

                var meanWall = wallSamples.Average();
                var medianWall = Median(wallSamples);
                var meanCpu = cpuSamples.Average();
                var meanKernelCpu = kernelCpuSamples.Average();
                var meanSystemDiskReadBytes = systemDiskReadSamples.Average();
                var meanSystemDiskWriteBytes = systemDiskWriteSamples.Average();
                var cvPct = wallSamples.Length > 1
                    ? StandardDeviation(wallSamples) / meanWall * 100.0
                    : 0.0;
                var kernelCpuPct = CalculatePercent(meanKernelCpu, meanCpu);
                var medianP50Us = TryMedian(p50Samples);
                var medianP95Us = TryMedian(p95Samples);
                var medianP99Us = TryMedian(p99Samples);

                var baselineMeanWall = filteredBaselineRuns.Count > 0
                    ? filteredBaselineRuns.Average(static run => (double)run.WallMs)
                    : 0.0;
                var baselineMedianWall = filteredBaselineRuns.Count > 0
                    ? Median(filteredBaselineRuns.Select(static run => (double)run.WallMs).ToArray())
                    : 0.0;
                var baselineMeanCpu = filteredBaselineRuns.Count > 0
                    ? filteredBaselineRuns.Average(static run => (double)(run.UserCpuMs + run.KernelCpuMs))
                    : 0.0;
                var baselineMeanKernelCpu = filteredBaselineRuns.Count > 0
                    ? filteredBaselineRuns.Average(static run => (double)run.KernelCpuMs)
                    : 0.0;
                var baselineMeanSystemDiskReadBytes = filteredBaselineRuns.Count > 0
                    ? filteredBaselineRuns.Average(static run => (double)run.SystemDiskReadBytes)
                    : 0.0;
                var baselineMeanSystemDiskWriteBytes = filteredBaselineRuns.Count > 0
                    ? filteredBaselineRuns.Average(static run => (double)run.SystemDiskWriteBytes)
                    : 0.0;
                var baselineWallSamples = filteredBaselineRuns.Select(static run => (double)run.WallMs).ToArray();
                var baselineCvPct = baselineWallSamples.Length > 1
                    ? StandardDeviation(baselineWallSamples) / baselineWallSamples.Average() * 100.0
                    : 0.0;
                var baselineKernelCpuPct = CalculatePercent(baselineMeanKernelCpu, baselineMeanCpu);
                var baselineP95Samples = filteredBaselineRuns
                    .Where(static run => run.P95Us.HasValue)
                    .Select(static run => run.P95Us!.Value)
                    .ToArray();
                var baselineMedianP95Us = TryMedian(baselineP95Samples);
                var slowdownPct = baselineMedianWall > 0
                    ? Math.Round((medianWall - baselineMedianWall) / baselineMedianWall * 100.0, 1)
                    : 0.0;
                double? p95SlowdownPct = medianP95Us.HasValue && baselineMedianP95Us.HasValue && baselineMedianP95Us.Value > 0
                    ? Math.Round((medianP95Us.Value - baselineMedianP95Us.Value) / baselineMedianP95Us.Value * 100.0, 1)
                    : null;
                var status = BuildStatus(
                    hasFailures || baselineHasFailures,
                    scenarioRuns.Count,
                    allBaselineScenarioRuns?.Count ?? 0,
                    cvPct,
                    baselineCvPct,
                    slowdownPct);

                rows.Add(new ComparisonRow
                {
                    ScenarioId = group.Key,
                    AvName = avName,
                    AvProduct = successfulRuns[0].AvProduct,
                    AvVersion = successfulRuns[0].AvVersion,
                    BaselineName = baselineName,
                    Sessions = scenarioRuns.Count,
                    BaselineSessions = allBaselineScenarioRuns?.Count ?? 0,
                    ExcludedRuns = excludedRuns,
                    BaselineExcludedRuns = baselineExcludedRuns,
                    MeanWallMs = Math.Round(meanWall, 1),
                    MedianWallMs = Math.Round(medianWall, 1),
                    BaselineMedianWallMs = Math.Round(baselineMedianWall, 1),
                    MeanCpuMs = Math.Round(meanCpu, 1),
                    KernelCpuPct = Math.Round(kernelCpuPct, 1),
                    BaselineKernelCpuPct = Math.Round(baselineKernelCpuPct, 1),
                    KernelCpuSlowdownPct = Math.Round(kernelCpuPct - baselineKernelCpuPct, 1),
                    PeakMemoryMb = successfulRuns.Max(static run => run.PeakJobMemoryMb),
                    SystemDiskReadBytes = (long)Math.Round(meanSystemDiskReadBytes, MidpointRounding.AwayFromZero),
                    BaselineSystemDiskReadBytes = (long)Math.Round(baselineMeanSystemDiskReadBytes, MidpointRounding.AwayFromZero),
                    SystemDiskWriteBytes = (long)Math.Round(meanSystemDiskWriteBytes, MidpointRounding.AwayFromZero),
                    BaselineSystemDiskWriteBytes = (long)Math.Round(baselineMeanSystemDiskWriteBytes, MidpointRounding.AwayFromZero),
                    SlowdownPct = slowdownPct,
                    CvPct = Math.Round(cvPct, 1),
                    BaselineCvPct = Math.Round(baselineCvPct, 1),
                    MedianP50Us = RoundNullable(medianP50Us),
                    MedianP95Us = RoundNullable(medianP95Us),
                    MedianP99Us = RoundNullable(medianP99Us),
                    BaselineMedianP95Us = RoundNullable(baselineMedianP95Us),
                    P95SlowdownPct = RoundNullable(p95SlowdownPct),
                    Status = status
                });
            }
        }

        return rows;
    }

    private static double CalculatePercent(double numerator, double denominator)
        => denominator > 0 ? numerator / denominator * 100.0 : 0.0;

    private static List<T> TryExcludeOutlier<T>(
        List<T> runs,
        Func<T, long> wallSelector,
        double noisyThreshold,
        out int excluded)
    {
        excluded = 0;

        if (runs.Count < 4)
        {
            return runs;
        }

        var samples = runs.Select(run => (double)wallSelector(run)).ToArray();
        var mean = samples.Average();
        var cv = mean > 0
            ? StandardDeviation(samples) / mean * 100.0
            : 0.0;

        if (cv <= noisyThreshold)
        {
            return runs;
        }

        var median = Median(samples);
        var worstIndex = 0;
        var worstDistance = double.MinValue;
        for (var index = 0; index < samples.Length; index++)
        {
            var distance = Math.Abs(samples[index] - median);
            if (distance > worstDistance)
            {
                worstDistance = distance;
                worstIndex = index;
            }
        }

        var remaining = runs.Where((_, index) => index != worstIndex).ToList();
        if (remaining.Count < 4)
        {
            return runs;
        }

        var remainingSamples = remaining.Select(run => (double)wallSelector(run)).ToArray();
        var remainingMean = remainingSamples.Average();
        var remainingCv = remainingMean > 0
            ? StandardDeviation(remainingSamples) / remainingMean * 100.0
            : 0.0;

        if (remainingCv <= noisyThreshold)
        {
            excluded = 1;
            return remaining;
        }

        return runs;
    }

    private static string BuildStatus(
        bool hasFailures,
        int sessions,
        int baselineSessions,
        double cvPct,
        double baselineCvPct,
        double slowdownPct)
    {
        if (hasFailures)
        {
            return "failed";
        }

        if (sessions < MinimumReliableSessions || baselineSessions < MinimumReliableSessions)
        {
            return "insufficient";
        }

        if (cvPct > NoisyThresholdPct || baselineCvPct > NoisyThresholdPct)
        {
            return "noisy";
        }

        if (slowdownPct < AnomalySlowdownThresholdPct)
        {
            return "anomaly";
        }

        return "ok";
    }

    private static double Median(IReadOnlyList<double> values)
    {
        var ordered = values.OrderBy(static value => value).ToArray();
        var middle = ordered.Length / 2;
        return ordered.Length % 2 == 0
            ? (ordered[middle - 1] + ordered[middle]) / 2.0
            : ordered[middle];
    }

    private static double? TryMedian(IReadOnlyList<double> values)
        => values.Count == 0 ? null : Median(values);

    private static double StandardDeviation(IReadOnlyList<double> values)
    {
        var mean = values.Average();
        var variance = values.Sum(value => Math.Pow(value - mean, 2)) / values.Count;
        return Math.Sqrt(variance);
    }

    private static double? RoundNullable(double? value)
        => value.HasValue
            ? Math.Round(value.Value, 1)
            : null;
}

public sealed class ComparisonRow
{
    public string ScenarioId { get; init; } = string.Empty;

    public string AvName { get; init; } = string.Empty;

    public string AvProduct { get; init; } = string.Empty;

    public string AvVersion { get; init; } = string.Empty;

    public string BaselineName { get; init; } = string.Empty;

    public int Sessions { get; init; }

    public int BaselineSessions { get; init; }

    public int ExcludedRuns { get; init; }

    public int BaselineExcludedRuns { get; init; }

    public double MeanWallMs { get; init; }

    public double MedianWallMs { get; init; }

    public double BaselineMedianWallMs { get; init; }

    public double MeanCpuMs { get; init; }

    public double KernelCpuPct { get; init; }

    public double BaselineKernelCpuPct { get; init; }

    public double KernelCpuSlowdownPct { get; init; }

    public long PeakMemoryMb { get; init; }

    public long SystemDiskReadBytes { get; init; }

    public long BaselineSystemDiskReadBytes { get; init; }

    public long SystemDiskWriteBytes { get; init; }

    public long BaselineSystemDiskWriteBytes { get; init; }

    public double SlowdownPct { get; init; }

    public double CvPct { get; init; }

    public double BaselineCvPct { get; init; }

    public double? MedianP50Us { get; init; }

    public double? MedianP95Us { get; init; }

    public double? MedianP99Us { get; init; }

    public double? BaselineMedianP95Us { get; init; }

    public double? P95SlowdownPct { get; init; }

    public string Status { get; init; } = string.Empty;
}
