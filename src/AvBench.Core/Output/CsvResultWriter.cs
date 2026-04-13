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
        "repetition",
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
        "file_batch_size",
        "file_total_operations",
        "file_ops_per_sec",
        "file_mean_latency_us"
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
                result.Repetition.ToString(CultureInfo.InvariantCulture),
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
                (result.FileMicrobench?.BatchSize ?? 0).ToString(CultureInfo.InvariantCulture),
                (result.FileMicrobench?.TotalOperations ?? 0).ToString(CultureInfo.InvariantCulture),
                (result.FileMicrobench?.OpsPerSec ?? 0).ToString("F3", CultureInfo.InvariantCulture),
                (result.FileMicrobench?.MeanLatencyUs ?? 0).ToString("F3", CultureInfo.InvariantCulture)));
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
