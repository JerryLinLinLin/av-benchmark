using System.Globalization;
using System.Text;
using AvBench.Core.Models;

namespace AvBench.Core.Output;

public static class CsvResultWriter
{
    private static readonly string[] Headers =
    [
        "scenario_id",
        "av_name",
        "av_product",
        "av_version",
        "timestamp_utc",
        "command",
        "working_dir",
        "exit_code",
        "wall_ms",
        "user_cpu_ms",
        "kernel_cpu_ms",
        "peak_job_memory_mb",
        "io_read_bytes",
        "io_write_bytes",
        "io_read_ops",
        "io_write_ops",
        "total_processes",
        "p50_us",
        "p95_us",
        "p99_us",
        "max_us",
        "micro_batch_size",
        "micro_total_operations",
        "micro_ops_per_sec",
        "micro_mean_latency_us"
    ];

    public static async Task WriteAsync(IReadOnlyCollection<RunResult> results, string path, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", Headers));

        foreach (var result in results)
        {
            builder.AppendLine(string.Join(",",
                Escape(result.ScenarioId),
                Escape(result.AvName),
                Escape(result.AvProduct),
                Escape(result.AvVersion),
                Escape(result.TimestampUtc.ToString("O", CultureInfo.InvariantCulture)),
                Escape(result.Command),
                Escape(result.WorkingDir),
                result.ExitCode.ToString(CultureInfo.InvariantCulture),
                result.WallMs.ToString(CultureInfo.InvariantCulture),
                result.UserCpuMs.ToString(CultureInfo.InvariantCulture),
                result.KernelCpuMs.ToString(CultureInfo.InvariantCulture),
                result.PeakJobMemoryMb.ToString(CultureInfo.InvariantCulture),
                result.IoReadBytes.ToString(CultureInfo.InvariantCulture),
                result.IoWriteBytes.ToString(CultureInfo.InvariantCulture),
                result.IoReadOps.ToString(CultureInfo.InvariantCulture),
                result.IoWriteOps.ToString(CultureInfo.InvariantCulture),
                result.TotalProcesses.ToString(CultureInfo.InvariantCulture),
                FormatNullable(result.P50Us),
                FormatNullable(result.P95Us),
                FormatNullable(result.P99Us),
                FormatNullable(result.MaxUs),
                (result.Microbench?.BatchSize ?? 0).ToString(CultureInfo.InvariantCulture),
                (result.Microbench?.TotalOperations ?? 0).ToString(CultureInfo.InvariantCulture),
                (result.Microbench?.OpsPerSec ?? 0).ToString("F3", CultureInfo.InvariantCulture),
                (result.Microbench?.MeanLatencyUs ?? 0).ToString("F3", CultureInfo.InvariantCulture)));
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

    private static string FormatNullable(double? value)
        => value.HasValue
            ? value.Value.ToString("F3", CultureInfo.InvariantCulture)
            : string.Empty;
}
