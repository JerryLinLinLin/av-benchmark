using System.Text.Json.Serialization;
using AvBench.Core.Models;

namespace AvBench.Core.Serialization;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(MicrobenchMetrics))]
[JsonSerializable(typeof(List<RunResult>))]
[JsonSerializable(typeof(RunResult))]
[JsonSerializable(typeof(SuiteManifest))]
public partial class AvBenchJsonContext : JsonSerializerContext
{
}
