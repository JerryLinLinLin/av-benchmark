using System.Text.Json.Serialization;

namespace AvBench.Core.Models;

public sealed class SuiteManifest
{
    [JsonPropertyName("created_utc")]
    public DateTime CreatedUtc { get; set; }

    [JsonPropertyName("bench_directory")]
    public string BenchDirectory { get; set; } = string.Empty;

    [JsonPropertyName("runner_version")]
    public string RunnerVersion { get; set; } = string.Empty;

    [JsonPropertyName("repos")]
    public List<RepoEntry> Repos { get; set; } = [];

    [JsonPropertyName("workloads")]
    public List<WorkloadEntry> Workloads { get; set; } = [];

    [JsonPropertyName("tools")]
    public Dictionary<string, string> Tools { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public RepoEntry GetRequiredRepo(string name)
    {
        return Repos.SingleOrDefault(entry => string.Equals(entry.Name, name, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Suite manifest does not contain the '{name}' repo.");
    }

    public WorkloadEntry GetRequiredWorkload(string name)
    {
        return Workloads.SingleOrDefault(entry => string.Equals(entry.Name, name, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Suite manifest does not contain the '{name}' workload.");
    }
}

public sealed class RepoEntry
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("sha")]
    public string Sha { get; set; } = string.Empty;

    [JsonPropertyName("source_kind")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SourceKind { get; set; }

    [JsonPropertyName("source_reference")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SourceReference { get; set; }

    [JsonPropertyName("archive_url")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ArchiveUrl { get; set; }

    [JsonPropertyName("local_path")]
    public string LocalPath { get; set; } = string.Empty;
}

public sealed class WorkloadEntry
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("repo_name")]
    public string RepoName { get; set; } = string.Empty;

    [JsonPropertyName("working_directory")]
    public string WorkingDirectory { get; set; } = string.Empty;

    [JsonPropertyName("build_directory")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? BuildDirectory { get; set; }

    [JsonPropertyName("incremental_touch_path")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? IncrementalTouchPath { get; set; }
}
