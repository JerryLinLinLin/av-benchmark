using System.Text.Json.Serialization;

namespace AvBench.Core.Models;

public sealed class AvProfile
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("product")]
    public string Product { get; set; } = string.Empty;

    [JsonPropertyName("product_version")]
    public string ProductVersion { get; set; } = string.Empty;

    [JsonPropertyName("realtime_protection")]
    public bool RealtimeProtection { get; set; }

    [JsonPropertyName("cloud_features")]
    public bool CloudFeatures { get; set; }

    [JsonPropertyName("exclusion_paths")]
    public List<string> ExclusionPaths { get; set; } = [];

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = string.Empty;
}
