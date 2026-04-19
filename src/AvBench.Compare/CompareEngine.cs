using AvBench.Core.Models;

namespace AvBench.Compare;

public static class CompareEngine
{
    private const double NoisyThresholdPct = 10.0;
    private const int MinimumReliableSteadyStateSamples = 3;

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
                var successfulRuns = scenarioRuns
                    .Where(static run => run.ExitCode == 0)
                    .OrderBy(static run => run.TimestampUtc)
                    .ToList();
                var firstSuccessfulRun = successfulRuns.FirstOrDefault();
                var steadyStateRuns = successfulRuns.Skip(1).ToList();
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
                        SteadyStateSamples = 0,
                        Status = "failed"
                    });
                    continue;
                }

                allBaselineByScenario.TryGetValue(group.Key, out var allBaselineScenarioRuns);
                baselineByScenario.TryGetValue(group.Key, out var baselineScenarioRuns);
                var baselineSuccessfulRuns = (baselineScenarioRuns ?? [])
                    .OrderBy(static run => run.TimestampUtc)
                    .ToList();
                var baselineFirstSuccessfulRun = baselineSuccessfulRuns.FirstOrDefault();
                var baselineSteadyStateRuns = baselineSuccessfulRuns.Skip(1).ToList();
                var baselineHasFailures = allBaselineScenarioRuns is null
                    || baselineScenarioRuns is null
                    || baselineScenarioRuns.Count != allBaselineScenarioRuns.Count;

                var wallSamples = steadyStateRuns.Select(static run => (double)run.WallMs).ToArray();
                var cpuSamples = steadyStateRuns.Select(static run => (double)(run.UserCpuMs + run.KernelCpuMs)).ToArray();
                var kernelCpuSamples = steadyStateRuns.Select(static run => (double)run.KernelCpuMs).ToArray();
                var systemDiskReadSamples = steadyStateRuns.Select(static run => (double)run.SystemDiskReadBytes).ToArray();
                var systemDiskWriteSamples = steadyStateRuns.Select(static run => (double)run.SystemDiskWriteBytes).ToArray();
                var p50Samples = steadyStateRuns.Where(static run => run.P50Us.HasValue).Select(static run => run.P50Us!.Value).ToArray();
                var p95Samples = steadyStateRuns.Where(static run => run.P95Us.HasValue).Select(static run => run.P95Us!.Value).ToArray();
                var p99Samples = steadyStateRuns.Where(static run => run.P99Us.HasValue).Select(static run => run.P99Us!.Value).ToArray();

                var meanWall = AverageOrZero(wallSamples);
                var medianWall = TryMedian(wallSamples) ?? 0.0;
                var meanCpu = AverageOrZero(cpuSamples);
                var meanKernelCpu = AverageOrZero(kernelCpuSamples);
                var meanSystemDiskReadBytes = AverageOrZero(systemDiskReadSamples);
                var meanSystemDiskWriteBytes = AverageOrZero(systemDiskWriteSamples);
                var cvPct = wallSamples.Length > 1 && meanWall > 0
                    ? StandardDeviation(wallSamples) / meanWall * 100.0
                    : 0.0;
                var kernelCpuPct = CalculatePercent(meanKernelCpu, meanCpu);
                var medianP50Us = TryMedian(p50Samples);
                var medianP95Us = TryMedian(p95Samples);
                var medianP99Us = TryMedian(p99Samples);

                var baselineMeanWall = AverageOrZero(baselineSteadyStateRuns.Select(static run => (double)run.WallMs).ToArray());
                var baselineMedianWall = baselineSteadyStateRuns.Count > 0
                    ? Median(baselineSteadyStateRuns.Select(static run => (double)run.WallMs).ToArray())
                    : 0.0;
                var baselineMeanCpu = baselineSteadyStateRuns.Count > 0
                    ? baselineSteadyStateRuns.Average(static run => (double)(run.UserCpuMs + run.KernelCpuMs))
                    : 0.0;
                var baselineMeanKernelCpu = baselineSteadyStateRuns.Count > 0
                    ? baselineSteadyStateRuns.Average(static run => (double)run.KernelCpuMs)
                    : 0.0;
                var baselineMeanSystemDiskReadBytes = baselineSteadyStateRuns.Count > 0
                    ? baselineSteadyStateRuns.Average(static run => (double)run.SystemDiskReadBytes)
                    : 0.0;
                var baselineMeanSystemDiskWriteBytes = baselineSteadyStateRuns.Count > 0
                    ? baselineSteadyStateRuns.Average(static run => (double)run.SystemDiskWriteBytes)
                    : 0.0;
                var baselineWallSamples = baselineSteadyStateRuns.Select(static run => (double)run.WallMs).ToArray();
                var baselineCvPct = baselineWallSamples.Length > 1 && baselineMeanWall > 0
                    ? StandardDeviation(baselineWallSamples) / baselineMeanWall * 100.0
                    : 0.0;
                var baselineKernelCpuPct = CalculatePercent(baselineMeanKernelCpu, baselineMeanCpu);
                var baselineP95Samples = baselineSteadyStateRuns
                    .Where(static run => run.P95Us.HasValue)
                    .Select(static run => run.P95Us!.Value)
                    .ToArray();
                var baselineMedianP95Us = TryMedian(baselineP95Samples);
                var slowdownPct = baselineMedianWall > 0
                    ? Math.Round((medianWall - baselineMedianWall) / baselineMedianWall * 100.0, 1)
                    : 0.0;
                var firstRunWallMs = firstSuccessfulRun?.WallMs > 0
                    ? (double)firstSuccessfulRun.WallMs
                    : 0.0;
                var baselineFirstRunWallMs = baselineFirstSuccessfulRun?.WallMs > 0
                    ? (double)baselineFirstSuccessfulRun.WallMs
                    : 0.0;
                var firstRunSlowdownPct = baselineFirstRunWallMs > 0 && firstRunWallMs > 0
                    ? Math.Round((firstRunWallMs - baselineFirstRunWallMs) / baselineFirstRunWallMs * 100.0, 1)
                    : 0.0;
                double? p95SlowdownPct = medianP95Us.HasValue && baselineMedianP95Us.HasValue && baselineMedianP95Us.Value > 0
                    ? Math.Round((medianP95Us.Value - baselineMedianP95Us.Value) / baselineMedianP95Us.Value * 100.0, 1)
                    : null;
                var status = BuildStatus(
                    hasFailures || baselineHasFailures,
                    steadyStateRuns.Count,
                    baselineSteadyStateRuns.Count,
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
                    SteadyStateSamples = steadyStateRuns.Count,
                    BaselineSteadyStateSamples = baselineSteadyStateRuns.Count,
                    MeanWallMs = Math.Round(meanWall, 1),
                    MedianWallMs = Math.Round(medianWall, 1),
                    BaselineMedianWallMs = Math.Round(baselineMedianWall, 1),
                    FirstRunWallMs = Math.Round(firstRunWallMs, 1),
                    BaselineFirstRunWallMs = Math.Round(baselineFirstRunWallMs, 1),
                    MeanCpuMs = Math.Round(meanCpu, 1),
                    KernelCpuPct = Math.Round(kernelCpuPct, 1),
                    BaselineKernelCpuPct = Math.Round(baselineKernelCpuPct, 1),
                    KernelCpuSlowdownPct = Math.Round(kernelCpuPct - baselineKernelCpuPct, 1),
                    PeakMemoryMb = steadyStateRuns.Count > 0 ? steadyStateRuns.Max(static run => run.PeakJobMemoryMb) : 0,
                    SystemDiskReadBytes = (long)Math.Round(meanSystemDiskReadBytes, MidpointRounding.AwayFromZero),
                    BaselineSystemDiskReadBytes = (long)Math.Round(baselineMeanSystemDiskReadBytes, MidpointRounding.AwayFromZero),
                    SystemDiskWriteBytes = (long)Math.Round(meanSystemDiskWriteBytes, MidpointRounding.AwayFromZero),
                    BaselineSystemDiskWriteBytes = (long)Math.Round(baselineMeanSystemDiskWriteBytes, MidpointRounding.AwayFromZero),
                    SlowdownPct = slowdownPct,
                    FirstRunSlowdownPct = firstRunSlowdownPct,
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

    private static string BuildStatus(
        bool hasFailures,
        int steadyStateSamples,
        int baselineSteadyStateSamples,
        double cvPct,
        double baselineCvPct,
        double slowdownPct)
    {
        if (hasFailures)
        {
            return "failed";
        }

        if (steadyStateSamples < MinimumReliableSteadyStateSamples || baselineSteadyStateSamples < MinimumReliableSteadyStateSamples)
        {
            return "insufficient";
        }

        if (slowdownPct < 0)
        {
            return "anomaly";
        }

        if (cvPct > NoisyThresholdPct || baselineCvPct > NoisyThresholdPct)
        {
            return "noisy";
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

    private static double AverageOrZero(IReadOnlyList<double> values)
        => values.Count == 0 ? 0.0 : values.Average();

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

    public int SteadyStateSamples { get; init; }

    public int BaselineSteadyStateSamples { get; init; }

    public double MeanWallMs { get; init; }

    public double MedianWallMs { get; init; }

    public double BaselineMedianWallMs { get; init; }

    public double FirstRunWallMs { get; init; }

    public double BaselineFirstRunWallMs { get; init; }

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

    public double FirstRunSlowdownPct { get; init; }

    public double CvPct { get; init; }

    public double BaselineCvPct { get; init; }

    public double? MedianP50Us { get; init; }

    public double? MedianP95Us { get; init; }

    public double? MedianP99Us { get; init; }

    public double? BaselineMedianP95Us { get; init; }

    public double? P95SlowdownPct { get; init; }

    public string Status { get; init; } = string.Empty;
}
