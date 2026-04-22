using System.Globalization;
using System.Text;

namespace AvBench.Compare;

public static class SummaryRenderer
{
    private const double SignificantDiskDeltaMb = 100.0;
    private const double SignificantKernelShiftPp = 1.0;
    private static readonly string[] ScenarioOrder =
    [
        "file-create-delete",
        "archive-extract",
        "file-enum-large-dir",
        "file-copy-large",
        "hardlink-create",
        "junction-create",
        "process-create-wait",
        "ext-sensitivity-exe",
        "ext-sensitivity-dll",
        "ext-sensitivity-js",
        "ext-sensitivity-ps1",
        "dll-load-unique",
        "file-write-content",
        "new-exe-run",
        "new-exe-run-motw",
        "thread-create",
        "mem-alloc-protect",
        "mem-map-file",
        "net-connect-loopback",
        "net-dns-resolve",
        "registry-crud",
        "pipe-roundtrip",
        "token-query",
        "crypto-hash-verify",
        "com-create-instance",
        "wmi-query",
        "fs-watcher",
        "ripgrep-clean-build",
        "ripgrep-incremental-build",
        "roslyn-clean-build",
        "roslyn-incremental-build"
    ];

    private static readonly IReadOnlyDictionary<string, int> ScenarioOrderById = ScenarioOrder
        .Select(static (scenarioId, index) => new KeyValuePair<string, int>(scenarioId, index))
        .ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.OrdinalIgnoreCase);

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
            builder.AppendLine("| Scenario | Median Wall (ms) | First-Run Wall (ms) | All-Runs Mean Wall (ms) | Slowdown | First-Run Slowdown | p95 Slowdown | Disk Read Delta (MB) | Disk Write Delta (MB) | CV % | Baseline CV % | Status |");
            builder.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|");

            foreach (var row in OrderRows(group))
            {
                var diskReadDeltaMb = BytesToMb(row.SystemDiskReadBytes - row.BaselineSystemDiskReadBytes);
                var diskWriteDeltaMb = BytesToMb(row.SystemDiskWriteBytes - row.BaselineSystemDiskWriteBytes);

                builder.AppendLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "| {0} | {1:F1} | {2} | {3} | {4} | {5} | {6} | {7} | {8} | {9:F1}% | {10:F1}% | {11} |",
                    row.ScenarioId,
                    row.MedianWallMs,
                    FormatWall(row.FirstRunWallMs),
                    FormatWall(row.AllRunsMeanWallMs),
                    FormatPercent(row.SlowdownPct),
                    FormatPercent(row.FirstRunSlowdownPct),
                    FormatNullablePercent(row.P95SlowdownPct),
                    FormatDeltaMb(diskReadDeltaMb),
                    FormatDeltaMb(diskWriteDeltaMb),
                    row.CvPct,
                    row.BaselineCvPct,
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
            if (largestKernelShift is not null && largestKernelShift.KernelCpuSlowdownPct >= SignificantKernelShiftPp)
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

            var insufficient = group.Where(static row => string.Equals(row.Status, "insufficient", StringComparison.OrdinalIgnoreCase)).ToList();
            if (insufficient.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine($"Insufficient data (< 3 steady-state samples after first-run exclusion): {string.Join(", ", insufficient.Select(static row => row.ScenarioId))}");
            }

            var anomalies = group.Where(static row => string.Equals(row.Status, "anomaly", StringComparison.OrdinalIgnoreCase)).ToList();
            if (anomalies.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine($"Anomaly scenarios (AV appears faster - likely caching artifact): {string.Join(", ", anomalies.Select(static row => row.ScenarioId))}");
            }

            var failed = group.Where(static row => string.Equals(row.Status, "failed", StringComparison.OrdinalIgnoreCase)).ToList();
            if (failed.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine($"Failed scenarios: {string.Join(", ", failed.Select(static row => row.ScenarioId))}");
            }

            builder.AppendLine();
        }

        AppendCrossAvComparison(rows, builder);

        await File.WriteAllTextAsync(path, builder.ToString(), cancellationToken);
    }

    private static double BytesToMb(long bytes)
        => bytes / (1024d * 1024d);

    private static IOrderedEnumerable<ComparisonRow> OrderRows(IEnumerable<ComparisonRow> rows)
        => rows
            .OrderBy(static row => GetScenarioOrder(row.ScenarioId))
            .ThenBy(static row => row.ScenarioId, StringComparer.OrdinalIgnoreCase);

    private static int GetScenarioOrder(string scenarioId)
        => ScenarioOrderById.TryGetValue(scenarioId, out var index)
            ? index
            : int.MaxValue;

    private static void AppendCrossAvComparison(IReadOnlyList<ComparisonRow> rows, StringBuilder builder)
    {
        var avGroups = rows
            .GroupBy(static row => row.AvName)
            .OrderBy(static group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (avGroups.Count < 2)
        {
            return;
        }

        AppendCrossAvComparisonTable(
            rows,
            builder,
            "## Cross-AV steady-state comparison",
            "Cells are slowdown vs baseline using median wall time after excluding each side's earliest successful run.",
            "baseline median (ms)",
            static row => row.BaselineMedianWallMs,
            FormatSteadyStateCrossAvCell);

        AppendCrossAvComparisonTable(
            rows,
            builder,
            "## Cross-AV first-run comparison",
            "Cells are slowdown vs baseline using the AV's earliest successful run against the baseline steady-state median.",
            "baseline median (ms)",
            static row => row.BaselineMedianWallMs,
            FormatFirstRunCrossAvCell);

        builder.AppendLine("`*` in the steady-state table marks a non-ok result (`failed`, `insufficient`, `noisy`, or `anomaly`).");
        builder.AppendLine("First-run cells do not inherit `noisy` or `insufficient` markers because CV and steady-state sample count are not meaningful for a single first-run sample; `failed*` means no successful first run was available, and a negative first-run slowdown is marked as an anomaly.");
        builder.AppendLine();
    }

    private static void AppendCrossAvComparisonTable(
        IReadOnlyList<ComparisonRow> rows,
        StringBuilder builder,
        string title,
        string description,
        string baselineHeader,
        Func<ComparisonRow, double> baselineSelector,
        Func<ComparisonRow?, string> cellFormatter)
    {
        var avGroups = rows
            .GroupBy(static row => row.AvName)
            .OrderBy(static group => group.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        builder.AppendLine(title);
        builder.AppendLine();
        builder.AppendLine(description);
        builder.AppendLine();

        var headers = new List<string> { "Scenario", baselineHeader };
        headers.AddRange(avGroups.Select(static group => group.Key));
        builder.AppendLine($"| {string.Join(" | ", headers)} |");
        builder.AppendLine($"|{string.Join("|", Enumerable.Repeat("---", headers.Count))}|");

        var rowsByScenario = rows
            .GroupBy(static row => row.ScenarioId, StringComparer.OrdinalIgnoreCase)
            .OrderBy(static group => GetScenarioOrder(group.Key))
            .ThenBy(static group => group.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var scenarioGroup in rowsByScenario)
        {
            var cells = new List<string>
            {
                scenarioGroup.Key,
                FormatBaselineWall(scenarioGroup.FirstOrDefault() is { } firstRow ? baselineSelector(firstRow) : null)
            };

            foreach (var avGroup in avGroups)
            {
                var row = scenarioGroup.FirstOrDefault(item => string.Equals(item.AvName, avGroup.Key, StringComparison.OrdinalIgnoreCase));
                cells.Add(cellFormatter(row));
            }

            builder.AppendLine($"| {string.Join(" | ", cells)} |");
        }

        builder.AppendLine();
    }

    private static string FormatPercent(double value)
    {
        var rounded = Math.Round(value, 1);
        if (rounded == 0)
        {
            return "0.0%";
        }

        return rounded > 0
            ? $"+{rounded:F1}%"
            : $"{rounded:F1}%";
    }

    private static string FormatNullablePercent(double? value)
        => value.HasValue
            ? FormatPercent(value.Value)
            : "-";

    private static string FormatDeltaMb(double value)
        => value >= 0
            ? $"+{value:F1}"
            : $"{value:F1}";

    private static string FormatBaselineWall(double? value)
        => value.HasValue && value.Value > 0
            ? value.Value.ToString("F1", CultureInfo.InvariantCulture)
            : "-";

    private static string FormatWall(double value)
        => value > 0
            ? value.ToString("F1", CultureInfo.InvariantCulture)
            : "-";

    private static string FormatSteadyStateCrossAvCell(ComparisonRow? row)
    {
        if (row is null)
        {
            return "-";
        }

        if (row.MedianWallMs <= 0 || row.BaselineMedianWallMs <= 0)
        {
            return string.Equals(row.Status, "failed", StringComparison.OrdinalIgnoreCase)
                ? "failed*"
                : "-";
        }

        var formatted = FormatPercent(row.SlowdownPct);
        return string.Equals(row.Status, "ok", StringComparison.OrdinalIgnoreCase)
            ? formatted
            : $"{formatted}*";
    }

    private static string FormatFirstRunCrossAvCell(ComparisonRow? row)
    {
        if (row is null)
        {
            return "-";
        }

        if (row.FirstRunWallMs <= 0 || row.BaselineMedianWallMs <= 0)
        {
            return string.Equals(row.Status, "failed", StringComparison.OrdinalIgnoreCase)
                ? "failed*"
                : "-";
        }

        var formatted = FormatPercent(row.FirstRunSlowdownPct);
        return row.FirstRunSlowdownPct < 0
            ? $"{formatted}*"
            : formatted;
    }
}
