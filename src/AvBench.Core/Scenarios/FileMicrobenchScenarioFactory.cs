using System.Text.Json;
using AvBench.Core.Models;
using AvBench.Core.Serialization;

namespace AvBench.Core.Scenarios;

public static class FileMicrobenchScenarioFactory
{
    public static ScenarioDefinition Create(string executablePath, string benchDirectory)
    {
        var workingDirectory = Path.Combine(benchDirectory, "microbench", "file-create-delete");
        Directory.CreateDirectory(workingDirectory);

        return new ScenarioDefinition
        {
            Id = "file-create-delete",
            FileName = executablePath,
            Arguments = $"internal-file-create-delete --root \"{workingDirectory}\" --operations 5000 --batch-size 100",
            WorkingDirectory = workingDirectory,
            PrepareAsync = cancellationToken =>
            {
                if (Directory.Exists(workingDirectory))
                {
                    foreach (var file in Directory.EnumerateFiles(workingDirectory, "*", SearchOption.TopDirectoryOnly))
                    {
                        File.Delete(file);
                    }
                }

                return Task.CompletedTask;
            },
            EnrichResultFromLogs = (runResult, stdoutLogPath, _) =>
            {
                var json = File.ReadAllText(stdoutLogPath).Trim();
                if (string.IsNullOrWhiteSpace(json))
                {
                    return;
                }

                var metrics = JsonSerializer.Deserialize(json, AvBenchJsonContext.Default.FileMicrobenchMetrics)
                    ?? throw new InvalidOperationException("File microbench output was not valid JSON.");

                runResult.FileMicrobench = metrics;
                runResult.P50Us = metrics.P50Us;
                runResult.P95Us = metrics.P95Us;
                runResult.P99Us = metrics.P99Us;
                runResult.MaxUs = metrics.MaxUs;
            }
        };
    }
}
