using System.CommandLine;
using System.Text.Json;
using AvBench.Core.Models;
using AvBench.Core.Serialization;

namespace AvBench.Compare;

public static class CompareCommand
{
    public static RootCommand Create()
    {
        var baselineOption = new Option<DirectoryInfo>("--baseline")
        {
            Description = "Result directory for the baseline AV configuration."
        };
        baselineOption.Required = true;

        var inputOption = new Option<DirectoryInfo[]>("--input")
        {
            Description = "One or more result directories to compare against the baseline.",
            AllowMultipleArgumentsPerToken = true
        };
        inputOption.Required = true;

        var outputOption = new Option<DirectoryInfo>("--output")
        {
            Description = "Directory that receives compare.csv and summary.md."
        };
        outputOption.Required = true;

        var command = new RootCommand("Load run.json files and compute cross-configuration summaries.");
        command.Options.Add(baselineOption);
        command.Options.Add(inputOption);
        command.Options.Add(outputOption);

        command.SetAction(async parseResult =>
        {
            var baselineDirectory = parseResult.GetValue(baselineOption)!;
            var inputDirectories = parseResult.GetValue(inputOption)!;
            var outputDirectory = parseResult.GetValue(outputOption)!;

            var baselineRuns = LoadRuns(baselineDirectory.FullName);
            if (baselineRuns.Count == 0)
            {
                throw new InvalidOperationException($"No run.json files were found under {baselineDirectory.FullName}.");
            }

            var namedRuns = new Dictionary<string, List<RunResult>>(StringComparer.OrdinalIgnoreCase);
            foreach (var inputDirectory in inputDirectories)
            {
                var runs = LoadRuns(inputDirectory.FullName);
                if (runs.Count == 0)
                {
                    Console.WriteLine($"[compare] Skipping empty input directory: {inputDirectory.FullName}");
                    continue;
                }

                var avName = runs[0].AvName;
                if (!namedRuns.TryGetValue(avName, out var existing))
                {
                    existing = [];
                    namedRuns[avName] = existing;
                }

                existing.AddRange(runs);
                Console.WriteLine($"[compare] Loaded {runs.Count} runs for '{avName}' from {inputDirectory.FullName}");
            }

            if (namedRuns.Count == 0)
            {
                throw new InvalidOperationException("No comparison input directories contained any run.json files.");
            }

            var comparisonRows = CompareEngine.Compare(baselineRuns, namedRuns);

            Directory.CreateDirectory(outputDirectory.FullName);
            var csvPath = Path.Combine(outputDirectory.FullName, "compare.csv");
            var summaryPath = Path.Combine(outputDirectory.FullName, "summary.md");

            await CompareCsvWriter.WriteAsync(comparisonRows, csvPath, CancellationToken.None);
            await SummaryRenderer.WriteAsync(comparisonRows, summaryPath, CancellationToken.None);

            Console.WriteLine($"[compare] Wrote {comparisonRows.Count} comparison rows to {outputDirectory.FullName}");
            return 0;
        });

        return command;
    }

    private static List<RunResult> LoadRuns(string rootDirectory)
    {
        var results = new List<RunResult>();

        foreach (var path in Directory.EnumerateFiles(rootDirectory, "run.json", SearchOption.AllDirectories))
        {
            var json = File.ReadAllText(path);
            var run = JsonSerializer.Deserialize(json, AvBenchJsonContext.Default.RunResult);
            if (run is not null)
            {
                results.Add(run);
            }
        }

        return results;
    }
}
