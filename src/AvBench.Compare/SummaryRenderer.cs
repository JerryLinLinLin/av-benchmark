using System.Globalization;
using System.Text;

namespace AvBench.Compare;

public static class SummaryRenderer
{
    public static async Task WriteAsync(IReadOnlyList<ComparisonRow> rows, string path, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# AV Benchmark Comparison Report");
        builder.AppendLine();
        builder.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        builder.AppendLine();

        foreach (var group in rows.GroupBy(static row => row.AvName).OrderBy(static group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"## {group.Key}");
            builder.AppendLine();
            builder.AppendLine("| Scenario | Slowdown % | Mean Wall (ms) | Mean CPU (ms) | CV % | Status | Hint |");
            builder.AppendLine("|---|---:|---:|---:|---:|---|---|");

            foreach (var row in group.OrderByDescending(static item => item.SlowdownPct).ThenBy(static item => item.ScenarioId, StringComparer.OrdinalIgnoreCase))
            {
                builder.AppendLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "| {0} | {1:F1} | {2:F1} | {3:F1} | {4:F1} | {5} | {6} |",
                    row.ScenarioId,
                    row.SlowdownPct,
                    row.MeanWallMs,
                    row.MeanCpuMs,
                    row.CvPct,
                    row.Status,
                    row.ResourceHint));
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
}
