using System.CommandLine;
using System.Text.Json;
using AvBench.Core.Internal;
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

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var histogram = new LatencyHistogram();
            RunBatches(root.FullName, totalOperations, batchSize, histogram);
            stopwatch.Stop();

            var metrics = new FileMicrobenchMetrics
            {
                BatchSize = batchSize,
                TotalOperations = totalOperations,
                OpsPerSec = totalOperations / stopwatch.Elapsed.TotalSeconds,
                MeanLatencyUs = histogram.MeanUs,
                P50Us = histogram.GetPercentile(50),
                P95Us = histogram.GetPercentile(95),
                P99Us = histogram.GetPercentile(99),
                MaxUs = histogram.MaxUs
            };

            Console.Out.Write(JsonSerializer.Serialize(metrics, AvBenchJsonContext.Default.FileMicrobenchMetrics));
            return 0;
        });

        return command;
    }

    private static void RunBatches(string root, int totalOperations, int batchSize, LatencyHistogram histogram)
    {
        Span<byte> data = stackalloc byte[64];
        var completed = 0;

        while (completed < totalOperations)
        {
            var currentBatch = Math.Min(batchSize, totalOperations - completed);
            for (var index = 0; index < currentBatch; index++)
            {
                var operationIndex = completed + index;
                var path = Path.Combine(root, $"bench_{operationIndex:D5}.tmp");
                var start = System.Diagnostics.Stopwatch.GetTimestamp();

                using (var stream = File.Create(path))
                {
                    stream.Write(data);
                }

                File.Delete(path);

                var end = System.Diagnostics.Stopwatch.GetTimestamp();
                histogram.RecordElapsedTicks(start, end);
            }

            completed += currentBatch;
        }
    }
}
