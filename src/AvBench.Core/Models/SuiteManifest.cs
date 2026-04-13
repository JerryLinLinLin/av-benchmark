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

    [JsonPropertyName("incremental_touch_path")]
    public string IncrementalTouchPath { get; set; } = string.Empty;

    [JsonPropertyName("repos")]
    public List<RepoEntry> Repos { get; set; } = [];

    [JsonPropertyName("tools")]
    public Dictionary<string, string> Tools { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class RepoEntry
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("sha")]
    public string Sha { get; set; } = string.Empty;

    [JsonPropertyName("local_path")]
    public string LocalPath { get; set; } = string.Empty;
}

