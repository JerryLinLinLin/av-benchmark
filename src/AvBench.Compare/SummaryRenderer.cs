using System.Globalization;
using System.Text;

namespace AvBench.Compare;

public static class SummaryRenderer
{
    private const double SignificantDiskDeltaMb = 100.0;

    public static async Task WriteAsync(IReadOnlyList<ComparisonRow> rows, string path, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# AV Benchmark Comparison Report");
        builder.AppendLine();
        builder.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        builder.AppendLine();

        foreach (var group in rows.GroupBy(static row => row.AvName).OrderBy(static group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            var first = group.First();
            builder.AppendLine($"## {group.Key} ({first.AvProduct} v{first.AvVersion}) vs {first.BaselineName}");
            builder.AppendLine();
            builder.AppendLine("| Scenario | Mean Wall (ms) | Slowdown | Kernel CPU % | Baseline Kernel % | Kernel Shift | CV % | Status |");
            builder.AppendLine("|---|---:|---:|---:|---:|---:|---:|---|");

            foreach (var row in group.OrderByDescending(static item => item.SlowdownPct).ThenBy(static item => item.ScenarioId, StringComparer.OrdinalIgnoreCase))
            {
                var slowdown = row.SlowdownPct >= 0
                    ? $"+{row.SlowdownPct:F1}%"
                    : $"{row.SlowdownPct:F1}%";
                var kernelShift = row.KernelCpuSlowdownPct >= 0
                    ? $"+{row.KernelCpuSlowdownPct:F1}pp"
                    : $"{row.KernelCpuSlowdownPct:F1}pp";

                builder.AppendLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "| {0} | {1:F1} | {2} | {3:F1}% | {4:F1}% | {5} | {6:F1}% | {7} |",
                    row.ScenarioId,
                    row.MeanWallMs,
                    slowdown,
                    row.KernelCpuPct,
                    row.BaselineKernelCpuPct,
                    kernelShift,
                    row.CvPct,
                    row.Status));
            }

            var worstSlowdown = group
                .Where(static row => string.Equals(row.Status, "ok", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(static row => row.SlowdownPct)
                .FirstOrDefault();
            if (worstSlowdown is not null)
            {
                builder.AppendLine();
                builder.AppendLine($"Highest slowdown: {worstSlowdown.ScenarioId} at {worstSlowdown.SlowdownPct:+0.0;-0.0;0.0}%");
            }

            var largestKernelShift = group
                .Where(static row => string.Equals(row.Status, "ok", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(static row => row.KernelCpuSlowdownPct)
                .FirstOrDefault();
            if (largestKernelShift is not null && largestKernelShift.KernelCpuSlowdownPct > 0)
            {
                builder.AppendLine();
                builder.AppendLine(
                    $"Largest kernel CPU shift: {largestKernelShift.ScenarioId} at {largestKernelShift.KernelCpuSlowdownPct:+0.0;-0.0;0.0}pp ({largestKernelShift.BaselineKernelCpuPct:F1}% -> {largestKernelShift.KernelCpuPct:F1}%)");
            }

            var largestDiskWriteDelta = group
                .OrderByDescending(static row => Math.Abs(row.SystemDiskWriteBytes - row.BaselineSystemDiskWriteBytes))
                .FirstOrDefault();
            if (largestDiskWriteDelta is not null)
            {
                var deltaMb = BytesToMb(largestDiskWriteDelta.SystemDiskWriteBytes - largestDiskWriteDelta.BaselineSystemDiskWriteBytes);
                if (Math.Abs(deltaMb) >= SignificantDiskDeltaMb)
                {
                    builder.AppendLine();
                    builder.AppendLine(
                        $"Largest system disk write delta: {largestDiskWriteDelta.ScenarioId} at {deltaMb:+0.0;-0.0;0.0} MB ({BytesToMb(largestDiskWriteDelta.BaselineSystemDiskWriteBytes):F1} -> {BytesToMb(largestDiskWriteDelta.SystemDiskWriteBytes):F1} MB)");
                }
            }

            var largestDiskReadDelta = group
                .OrderByDescending(static row => Math.Abs(row.SystemDiskReadBytes - row.BaselineSystemDiskReadBytes))
                .FirstOrDefault();
            if (largestDiskReadDelta is not null)
            {
                var deltaMb = BytesToMb(largestDiskReadDelta.SystemDiskReadBytes - largestDiskReadDelta.BaselineSystemDiskReadBytes);
                if (Math.Abs(deltaMb) >= SignificantDiskDeltaMb)
                {
                    builder.AppendLine();
                    builder.AppendLine(
                        $"Largest system disk read delta: {largestDiskReadDelta.ScenarioId} at {deltaMb:+0.0;-0.0;0.0} MB ({BytesToMb(largestDiskReadDelta.BaselineSystemDiskReadBytes):F1} -> {BytesToMb(largestDiskReadDelta.SystemDiskReadBytes):F1} MB)");
                }
            }

            var noisy = group.Where(static row => string.Equals(row.Status, "noisy", StringComparison.OrdinalIgnoreCase)).ToList();
            if (noisy.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine($"Noisy scenarios: {string.Join(", ", noisy.Select(static row => row.ScenarioId))}");
            }

            var failed = group.Where(static row => string.Equals(row.Status, "failed", StringComparison.OrdinalIgnoreCase)).ToList();
            if (failed.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine($"Failed scenarios: {string.Join(", ", failed.Select(static row => row.ScenarioId))}");
            }

            builder.AppendLine();
        }

        await File.WriteAllTextAsync(path, builder.ToString(), cancellationToken);
    }

    private static double BytesToMb(long bytes)
        => bytes / (1024d * 1024d);
}
