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
                        BaselineName = baselineName,
                        Repetitions = scenarioRuns.Count,
                        Status = "failed"
                    });
                    continue;
                }

                var wallSamples = successfulRuns.Select(static run => (double)run.WallMs).ToArray();
                var cpuSamples = successfulRuns.Select(static run => (double)(run.UserCpuMs + run.KernelCpuMs)).ToArray();
                var ioSamples = successfulRuns.Select(static run => (double)(run.IoReadBytes + run.IoWriteBytes)).ToArray();

                var meanWall = wallSamples.Average();
                var meanCpu = cpuSamples.Average();
                var meanIoBytes = ioSamples.Average();
                var cvPct = wallSamples.Length > 1
                    ? StandardDeviation(wallSamples) / meanWall * 100.0
                    : 0.0;

                baselineByScenario.TryGetValue(group.Key, out var baselineScenarioRuns);
                var baselineMeanWall = baselineScenarioRuns?.Average(static run => (double)run.WallMs) ?? 0.0;
                var baselineMeanCpu = baselineScenarioRuns?.Average(static run => (double)(run.UserCpuMs + run.KernelCpuMs)) ?? 0.0;
                var baselineMeanIo = baselineScenarioRuns?.Average(static run => (double)(run.IoReadBytes + run.IoWriteBytes)) ?? 0.0;

                rows.Add(new ComparisonRow
                {
                    ScenarioId = group.Key,
                    AvName = avName,
                    BaselineName = baselineName,
                    Repetitions = successfulRuns.Count,
                    MeanWallMs = Math.Round(meanWall, 1),
                    MedianWallMs = Math.Round(Median(wallSamples), 1),
                    MeanCpuMs = Math.Round(meanCpu, 1),
                    PeakMemoryMb = successfulRuns.Max(static run => run.PeakJobMemoryMb),
                    SlowdownPct = baselineMeanWall > 0
                        ? Math.Round((meanWall - baselineMeanWall) / baselineMeanWall * 100.0, 1)
                        : 0.0,
                    CvPct = Math.Round(cvPct, 1),
                    Status = hasFailures
                        ? "failed"
                        : cvPct > NoisyThresholdPct
                            ? "noisy"
                            : "ok",
                    ResourceHint = DetermineResourceHint(meanCpu, baselineMeanCpu, meanIoBytes, baselineMeanIo)
                });
            }
        }

        return rows;
    }

    private static string DetermineResourceHint(
        double meanCpu,
        double baselineMeanCpu,
        double meanIoBytes,
        double baselineMeanIoBytes)
    {
        var cpuDeltaPct = baselineMeanCpu > 0 ? (meanCpu - baselineMeanCpu) / baselineMeanCpu * 100.0 : 0.0;
        var ioDeltaPct = baselineMeanIoBytes > 0 ? (meanIoBytes - baselineMeanIoBytes) / baselineMeanIoBytes * 100.0 : 0.0;

        if (Math.Abs(cpuDeltaPct - ioDeltaPct) < 10.0)
        {
            return "mixed";
        }

        return cpuDeltaPct > ioDeltaPct ? "cpu" : "io";
    }

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

    public string BaselineName { get; init; } = string.Empty;

    public int Repetitions { get; init; }

    public double MeanWallMs { get; init; }

    public double MedianWallMs { get; init; }

    public double MeanCpuMs { get; init; }

    public long PeakMemoryMb { get; init; }

    public double SlowdownPct { get; init; }

    public double CvPct { get; init; }

    public string Status { get; init; } = string.Empty;

    public string ResourceHint { get; init; } = string.Empty;
}
