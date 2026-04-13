using System.CommandLine;
using System.Text.Json;
using AvBench.Core.Models;
using AvBench.Core.Serialization;

namespace AvBench.Cli.Commands;

public static class InternalFileCreateDeleteCommand
{
    public static Command Create()
    {
        var rootOption = new Option<DirectoryInfo>("--root")
        {
            Description = "Working directory for the file-create-delete microbenchmark."
        };
        rootOption.Required = true;

        var operationsOption = new Option<int>("--operations")
        {
            Description = "Total number of create/delete operations.",
            DefaultValueFactory = _ => 5000
        };

        var batchSizeOption = new Option<int>("--batch-size")
        {
            Description = "Number of operations per batch.",
            DefaultValueFactory = _ => 100
        };

        var command = new Command("internal-file-create-delete")
        {
            Description = "Internal worker for the file-create-delete microbenchmark.",
            Hidden = true
        };

        command.Options.Add(rootOption);
        command.Options.Add(operationsOption);
        command.Options.Add(batchSizeOption);

        command.SetAction(parseResult =>
        {
            var root = parseResult.GetValue(rootOption)!;
            var totalOperations = parseResult.GetValue(operationsOption);
            var batchSize = parseResult.GetValue(batchSizeOption);

            Directory.CreateDirectory(root.FullName);

            RunBatch(root.FullName, batchSize);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var completed = 0;
            while (completed < totalOperations)
            {
                var currentBatch = Math.Min(batchSize, totalOperations - completed);
                RunBatch(root.FullName, currentBatch);
                completed += currentBatch;
            }

            stopwatch.Stop();

            var metrics = new FileMicrobenchMetrics
            {
                BatchSize = batchSize,
                TotalOperations = totalOperations,
                OpsPerSec = totalOperations / stopwatch.Elapsed.TotalSeconds,
                MeanLatencyUs = stopwatch.Elapsed.TotalMicroseconds / totalOperations
            };

            Console.Out.Write(JsonSerializer.Serialize(metrics, AvBenchJsonContext.Default.FileMicrobenchMetrics));
            return 0;
        });

        return command;
    }

    private static void RunBatch(string root, int count)
    {
        Span<byte> data = stackalloc byte[64];

        for (var index = 0; index < count; index++)
        {
            var path = Path.Combine(root, $"bench_{index:D5}.tmp");

            using (var stream = File.Create(path))
            {
                stream.Write(data);
            }

            File.Delete(path);
        }
    }
}
