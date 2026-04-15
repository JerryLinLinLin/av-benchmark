using System.Text.Json.Serialization;

namespace AvBench.Core.Models;

public sealed class RunResult
{
    [JsonPropertyName("scenario_id")]
    public string ScenarioId { get; set; } = string.Empty;

    [JsonPropertyName("av_name")]
    public string AvName { get; set; } = string.Empty;

    [JsonPropertyName("av_product")]
    public string AvProduct { get; set; } = string.Empty;

    [JsonPropertyName("av_version")]
    public string AvVersion { get; set; } = string.Empty;

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

    [JsonPropertyName("system_disk_read_bytes")]
    public long SystemDiskReadBytes { get; set; }

    [JsonPropertyName("system_disk_write_bytes")]
    public long SystemDiskWriteBytes { get; set; }

    [JsonPropertyName("p50_us")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? P50Us { get; set; }

    [JsonPropertyName("p95_us")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? P95Us { get; set; }

    [JsonPropertyName("p99_us")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? P99Us { get; set; }

    [JsonPropertyName("max_us")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? MaxUs { get; set; }

    [JsonPropertyName("machine")]
    public MachineInfo Machine { get; set; } = new();

    [JsonPropertyName("runner_version")]
    public string RunnerVersion { get; set; } = string.Empty;

    [JsonPropertyName("suite_manifest_sha")]
    public string SuiteManifestSha { get; set; } = string.Empty;

    [JsonPropertyName("microbench")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public MicrobenchMetrics? Microbench { get; set; }
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

public sealed class MicrobenchMetrics
{
    [JsonPropertyName("batch_size")]
    public int BatchSize { get; set; }

    [JsonPropertyName("total_operations")]
    public int TotalOperations { get; set; }

    [JsonPropertyName("ops_per_sec")]
    public double OpsPerSec { get; set; }

    [JsonPropertyName("mean_latency_us")]
    public double MeanLatencyUs { get; set; }

    [JsonPropertyName("p50_us")]
    public double P50Us { get; set; }

    [JsonPropertyName("p95_us")]
    public double P95Us { get; set; }

    [JsonPropertyName("p99_us")]
    public double P99Us { get; set; }

    [JsonPropertyName("max_us")]
    public double MaxUs { get; set; }
}
