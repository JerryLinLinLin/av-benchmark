using System.Globalization;
using System.Text;

namespace AvBench.Compare;

public static class CompareCsvWriter
{
    private static readonly string[] Headers =
    [
        "scenario_id",
        "av_name",
        "av_product",
        "av_version",
        "baseline_name",
        "repetitions",
        "mean_wall_ms",
        "median_wall_ms",
        "mean_cpu_ms",
        "kernel_cpu_pct",
        "baseline_kernel_cpu_pct",
        "kernel_cpu_slowdown_pct",
        "peak_memory_mb",
        "slowdown_pct",
        "cv_pct",
        "status"
    ];

    public static async Task WriteAsync(IReadOnlyList<ComparisonRow> rows, string path, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", Headers));

        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(",",
                Escape(row.ScenarioId),
                Escape(row.AvName),
                Escape(row.AvProduct),
                Escape(row.AvVersion),
                Escape(row.BaselineName),
                row.Repetitions.ToString(CultureInfo.InvariantCulture),
                row.MeanWallMs.ToString("F1", CultureInfo.InvariantCulture),
                row.MedianWallMs.ToString("F1", CultureInfo.InvariantCulture),
                row.MeanCpuMs.ToString("F1", CultureInfo.InvariantCulture),
                row.KernelCpuPct.ToString("F1", CultureInfo.InvariantCulture),
                row.BaselineKernelCpuPct.ToString("F1", CultureInfo.InvariantCulture),
                row.KernelCpuSlowdownPct.ToString("F1", CultureInfo.InvariantCulture),
                row.PeakMemoryMb.ToString(CultureInfo.InvariantCulture),
                row.SlowdownPct.ToString("F1", CultureInfo.InvariantCulture),
                row.CvPct.ToString("F1", CultureInfo.InvariantCulture),
                Escape(row.Status)));
        }

        await File.WriteAllTextAsync(path, builder.ToString(), cancellationToken);
    }

    private static string Escape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        }

        return value;
    }
}
