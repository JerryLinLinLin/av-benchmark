using System.Text.Json;
using AvBench.Core.Models;
using AvBench.Core.Serialization;

namespace AvBench.Core.Output;

public static class JsonResultWriter
{
    public static async Task WriteAsync(RunResult result, string path, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(result, AvBenchJsonContext.Default.RunResult);
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }
}

