using System.CommandLine;
using System.Runtime.Versioning;
using AvBench.Core;
using AvBench.Core.Environment;
using AvBench.Core.Models;
using AvBench.Core.Output;
using AvBench.Core.Scenarios;
using AvBench.Core.Serialization;
using AvBench.Core.Setup;

namespace AvBench.Cli.Commands;

[SupportedOSPlatform("windows")]
public static class RunCommand
{
    public static Command Create()
    {
        var nameOption = new Option<string>("--name")
        {
            Description = "Label for this VM's AV configuration, such as baseline-os or defender-default."
        };
        nameOption.Required = true;

        var benchDirOption = new Option<DirectoryInfo>("--bench-dir")
        {
            Description = "Root directory where setup stored repos and the suite manifest.",
            DefaultValueFactory = _ => new DirectoryInfo(@"C:\bench")
        };

        var repetitionsOption = new Option<int>("--repetitions", ["-n"])
        {
            Description = "Number of measured repetitions per scenario.",
            DefaultValueFactory = _ => 5
        };

        var outputOption = new Option<DirectoryInfo>("--output")
        {
            Description = "Directory that receives benchmark result folders.",
            DefaultValueFactory = _ => new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "results"))
        };

        var workloadOption = new Option<string[]>("--workload", ["-w"])
        {
            Description = $"One or more workload ids to run. Defaults to all workloads present in the manifest plus {BenchmarkWorkloads.FileCreateDelete}: {BenchmarkWorkloads.HelpText}.",
            AllowMultipleArgumentsPerToken = true,
            DefaultValueFactory = _ => []
        };

        var command = new Command("run", "Execute milestone 2 benchmark scenarios.");
        command.Options.Add(nameOption);
        command.Options.Add(benchDirOption);
        command.Options.Add(repetitionsOption);
        command.Options.Add(outputOption);
        command.Options.Add(workloadOption);

        command.SetAction(async parseResult =>
        {
            try
            {
                var avName = parseResult.GetValue(nameOption)!;
                var benchDir = parseResult.GetValue(benchDirOption)!;
                var repetitions = parseResult.GetValue(repetitionsOption);
                var outputRoot = parseResult.GetValue(outputOption)!;
                if (!BenchmarkWorkloads.TryNormalize(parseResult.GetValue(workloadOption), out var selectedWorkloads, out var error))
                {
                    Console.Error.WriteLine($"ERROR: {error}");
                    return 1;
                }

                var manifestPath = Path.Combine(benchDir.FullName, SetupService.SuiteManifestFileName);
                if (!File.Exists(manifestPath))
                {
                    throw new FileNotFoundException($"Suite manifest not found at {manifestPath}. Run `avbench setup` first.");
                }

                if (string.IsNullOrWhiteSpace(avName))
                {
                    throw new InvalidOperationException("`--name` must be a non-empty AV configuration label.");
                }

                var manifest = System.Text.Json.JsonSerializer.Deserialize(
                    await File.ReadAllTextAsync(manifestPath, CancellationToken.None),
                    AvBenchJsonContext.Default.SuiteManifest)
                    ?? throw new InvalidOperationException("Suite manifest could not be parsed.");

                ValidateRequestedWorkloads(manifest, selectedWorkloads);

                Directory.CreateDirectory(outputRoot.FullName);
                File.Copy(manifestPath, Path.Combine(outputRoot.FullName, SetupService.SuiteManifestFileName), overwrite: true);

                Console.WriteLine("[run] Idle check: not yet enforced in milestone 1; proceeding with benchmark execution.");

                var runner = new ScenarioRunner(
                    avName.Trim(),
                    outputRoot.FullName,
                    SystemInfoProvider.GetRunnerVersion(),
                    SetupService.ComputeManifestSha(manifestPath));

                var scenarios = new List<ScenarioDefinition>();
                if (BenchmarkWorkloads.Contains(selectedWorkloads, BenchmarkWorkloads.Ripgrep))
                {
                    scenarios.AddRange(RipgrepScenarioFactory.Create(manifest));
                }

                if (BenchmarkWorkloads.Contains(selectedWorkloads, BenchmarkWorkloads.Roslyn))
                {
                    scenarios.AddRange(RoslynScenarioFactory.Create(manifest));
                }

                var executablePath = Environment.ProcessPath
                    ?? throw new InvalidOperationException("Unable to resolve the current executable path.");
                if (BenchmarkWorkloads.Contains(selectedWorkloads, BenchmarkWorkloads.FileCreateDelete))
                {
                    var fileMicrobench = FileMicrobenchScenarioFactory.Create(executablePath, benchDir.FullName);
                    scenarios.Add(fileMicrobench);
                }

                var results = new List<RunResult>();
                foreach (var scenario in scenarios)
                {
                    results.AddRange(await runner.ExecuteScenarioAsync(scenario, repetitions, CancellationToken.None));
                }

                var csvPath = Path.Combine(outputRoot.FullName, "runs.csv");
                await CsvResultWriter.WriteAsync(results, csvPath, CancellationToken.None);

                Console.WriteLine($"[run] Wrote {results.Count} run records to {outputRoot.FullName}");
                return 0;
            }
            catch (InvalidOperationException ex)
            {
                Console.Error.WriteLine($"ERROR: {ex.Message}");
                return 1;
            }
            catch (FileNotFoundException ex)
            {
                Console.Error.WriteLine($"ERROR: {ex.Message}");
                return 1;
            }
        });

        return command;
    }

    private static void ValidateRequestedWorkloads(SuiteManifest manifest, IReadOnlyCollection<string> selectedWorkloads)
    {
        var availableWorkloads = manifest.Workloads
            .Select(workload => workload.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var workload in selectedWorkloads)
        {
            if (string.Equals(workload, BenchmarkWorkloads.FileCreateDelete, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!availableWorkloads.Contains(workload))
            {
                throw new InvalidOperationException(
                    $"The suite manifest does not contain workload '{workload}'. Run `avbench setup --workload {workload}` first, or rerun setup with a broader workload selection.");
            }
        }
    }
}
