using AvBench.Core.Models;

namespace AvBench.Compare;

public static class CompareEngine
{
    private const double NoisyThresholdPct = 10.0;

    public static List<ComparisonRow> Compare(
        IReadOnlyList<RunResult> baselineRuns,
        IReadOnlyDictionary<string, List<RunResult>> namedRuns)
    {
        var rows = new List<ComparisonRow>();
        var baselineName = baselineRuns.FirstOrDefault()?.AvName ?? "baseline-os";

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

                var wallSamples = successfulRuns.Select(static run => (double)run.WallMs).ToArray();
                var cpuSamples = successfulRuns.Select(static run => (double)(run.UserCpuMs + run.KernelCpuMs)).ToArray();
                var kernelCpuSamples = successfulRuns.Select(static run => (double)run.KernelCpuMs).ToArray();
                var systemDiskReadSamples = successfulRuns.Select(static run => (double)run.SystemDiskReadBytes).ToArray();
                var systemDiskWriteSamples = successfulRuns.Select(static run => (double)run.SystemDiskWriteBytes).ToArray();

                var meanWall = wallSamples.Average();
                var meanCpu = cpuSamples.Average();
                var meanKernelCpu = kernelCpuSamples.Average();
                var meanSystemDiskReadBytes = systemDiskReadSamples.Average();
                var meanSystemDiskWriteBytes = systemDiskWriteSamples.Average();
                var cvPct = wallSamples.Length > 1
                    ? StandardDeviation(wallSamples) / meanWall * 100.0
                    : 0.0;
                var kernelCpuPct = CalculatePercent(meanKernelCpu, meanCpu);

                baselineByScenario.TryGetValue(group.Key, out var baselineScenarioRuns);
                var baselineMeanWall = baselineScenarioRuns?.Average(static run => (double)run.WallMs) ?? 0.0;
                var baselineMeanCpu = baselineScenarioRuns?.Average(static run => (double)(run.UserCpuMs + run.KernelCpuMs)) ?? 0.0;
                var baselineMeanKernelCpu = baselineScenarioRuns?.Average(static run => (double)run.KernelCpuMs) ?? 0.0;
                var baselineMeanSystemDiskReadBytes = baselineScenarioRuns?.Average(static run => (double)run.SystemDiskReadBytes) ?? 0.0;
                var baselineMeanSystemDiskWriteBytes = baselineScenarioRuns?.Average(static run => (double)run.SystemDiskWriteBytes) ?? 0.0;
                var baselineKernelCpuPct = CalculatePercent(baselineMeanKernelCpu, baselineMeanCpu);

                rows.Add(new ComparisonRow
                {
                    ScenarioId = group.Key,
                    AvName = avName,
                    AvProduct = successfulRuns[0].AvProduct,
                    AvVersion = successfulRuns[0].AvVersion,
                    BaselineName = baselineName,
                    Sessions = scenarioRuns.Count,
                    MeanWallMs = Math.Round(meanWall, 1),
                    MedianWallMs = Math.Round(Median(wallSamples), 1),
                    MeanCpuMs = Math.Round(meanCpu, 1),
                    KernelCpuPct = Math.Round(kernelCpuPct, 1),
                    BaselineKernelCpuPct = Math.Round(baselineKernelCpuPct, 1),
                    KernelCpuSlowdownPct = Math.Round(kernelCpuPct - baselineKernelCpuPct, 1),
                    PeakMemoryMb = successfulRuns.Max(static run => run.PeakJobMemoryMb),
                    SystemDiskReadBytes = (long)Math.Round(meanSystemDiskReadBytes, MidpointRounding.AwayFromZero),
                    BaselineSystemDiskReadBytes = (long)Math.Round(baselineMeanSystemDiskReadBytes, MidpointRounding.AwayFromZero),
                    SystemDiskWriteBytes = (long)Math.Round(meanSystemDiskWriteBytes, MidpointRounding.AwayFromZero),
                    BaselineSystemDiskWriteBytes = (long)Math.Round(baselineMeanSystemDiskWriteBytes, MidpointRounding.AwayFromZero),
                    SlowdownPct = baselineMeanWall > 0
                        ? Math.Round((meanWall - baselineMeanWall) / baselineMeanWall * 100.0, 1)
                        : 0.0,
                    CvPct = Math.Round(cvPct, 1),
                    Status = hasFailures
                        ? "failed"
                        : cvPct > NoisyThresholdPct
                            ? "noisy"
                            : "ok"
                });
            }
        }

        return rows;
    }

    private static double CalculatePercent(double numerator, double denominator)
        => denominator > 0 ? numerator / denominator * 100.0 : 0.0;

    private static double Median(IReadOnlyList<double> values)
    {
        var ordered = values.OrderBy(static value => value).ToArray();
        var middle = ordered.Length / 2;
        return ordered.Length % 2 == 0
            ? (ordered[middle - 1] + ordered[middle]) / 2.0
            : ordered[middle];
    }

    private static double StandardDeviation(IReadOnlyList<double> values)
    {
        var mean = values.Average();
        var variance = values.Sum(value => Math.Pow(value - mean, 2)) / values.Count;
        return Math.Sqrt(variance);
    }
}

public sealed class ComparisonRow
{
    public string ScenarioId { get; init; } = string.Empty;

    public string AvName { get; init; } = string.Empty;

    public string AvProduct { get; init; } = string.Empty;

    public string AvVersion { get; init; } = string.Empty;

    public string BaselineName { get; init; } = string.Empty;

    public int Sessions { get; init; }

    public double MeanWallMs { get; init; }

    public double MedianWallMs { get; init; }

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

    public string Status { get; init; } = string.Empty;
}
