using System.Text.Json.Serialization;

namespace AvBench.Core.Models;

public sealed class RunResult
{
    [JsonPropertyName("scenario_id")]
    public string ScenarioId { get; set; } = string.Empty;

    [JsonPropertyName("av_profile")]
    public string AvProfile { get; set; } = string.Empty;

    [JsonPropertyName("repetition")]
    public int Repetition { get; set; }

    [JsonPropertyName("timestamp_utc")]
    public DateTime TimestampUtc { get; set; }

    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    [JsonPropertyName("working_dir")]
    public string WorkingDir { get; set; } = string.Empty;

    [JsonPropertyName("exit_code")]
    public int ExitCode { get; set; }

    [JsonPropertyName("wall_ms")]
    public long WallMs { get; set; }

    [JsonPropertyName("user_cpu_ms")]
    public long UserCpuMs { get; set; }

    [JsonPropertyName("kernel_cpu_ms")]
    public long KernelCpuMs { get; set; }

    [JsonPropertyName("peak_job_memory_mb")]
    public long PeakJobMemoryMb { get; set; }

    [JsonPropertyName("io_read_bytes")]
    public ulong IoReadBytes { get; set; }

    [JsonPropertyName("io_write_bytes")]
    public ulong IoWriteBytes { get; set; }

    [JsonPropertyName("io_read_ops")]
    public ulong IoReadOps { get; set; }

    [JsonPropertyName("io_write_ops")]
    public ulong IoWriteOps { get; set; }

    [JsonPropertyName("total_processes")]
    public uint TotalProcesses { get; set; }

    [JsonPropertyName("av_samples")]
    public List<AvSample> AvSamples { get; set; } = [];

    [JsonPropertyName("machine")]
    public MachineInfo Machine { get; set; } = new();

    [JsonPropertyName("runner_version")]
    public string RunnerVersion { get; set; } = string.Empty;

    [JsonPropertyName("suite_manifest_sha")]
    public string SuiteManifestSha { get; set; } = string.Empty;

    [JsonPropertyName("file_microbench")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public FileMicrobenchMetrics? FileMicrobench { get; set; }
}

public sealed class AvSample
{
    [JsonPropertyName("process")]
    public string Process { get; set; } = string.Empty;

    [JsonPropertyName("mean_cpu_pct")]
    public double MeanCpuPct { get; set; }

    [JsonPropertyName("peak_ws_mb")]
    public long PeakWsMb { get; set; }
}

public sealed class MachineInfo
{
    [JsonPropertyName("os")]
    public string Os { get; set; } = string.Empty;

    [JsonPropertyName("cpu")]
    public string Cpu { get; set; } = string.Empty;

    [JsonPropertyName("ram_gb")]
    public int RamGb { get; set; }

    [JsonPropertyName("storage")]
    public string Storage { get; set; } = string.Empty;
}

public sealed class FileMicrobenchMetrics
{
    [JsonPropertyName("batch_size")]
    public int BatchSize { get; set; }

    [JsonPropertyName("total_operations")]
    public int TotalOperations { get; set; }

    [JsonPropertyName("ops_per_sec")]
    public double OpsPerSec { get; set; }

    [JsonPropertyName("mean_latency_us")]
    public double MeanLatencyUs { get; set; }
}

