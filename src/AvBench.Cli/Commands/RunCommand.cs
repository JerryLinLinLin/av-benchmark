using System.CommandLine;
using System.Runtime.Versioning;
using AvBench.Core;
using AvBench.Core.Detection;
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

        var outputOption = new Option<DirectoryInfo>("--output")
        {
            Description = "Directory that receives benchmark result folders.",
            DefaultValueFactory = _ => new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "results"))
        };

        var workloadOption = new Option<string[]>("--workload", ["-w"])
        {
            Description = $"One or more workload family ids or specific microbench scenario ids to run. Defaults to all workload families: {BenchmarkWorkloads.HelpText}.",
            AllowMultipleArgumentsPerToken = true,
            DefaultValueFactory = _ => []
        };

        var avProductOption = new Option<string?>("--av-product")
        {
            Description = "Override the auto-detected AV product name."
        };

        var avVersionOption = new Option<string?>("--av-version")
        {
            Description = "Override the auto-detected AV product version."
        };

        var command = new Command("run", "Execute benchmark scenarios for the configured M1-M4 workload set.");
        command.Options.Add(nameOption);
        command.Options.Add(benchDirOption);
        command.Options.Add(outputOption);
        command.Options.Add(workloadOption);
        command.Options.Add(avProductOption);
        command.Options.Add(avVersionOption);

        command.SetAction(async parseResult =>
        {
            try
            {
                var avName = parseResult.GetValue(nameOption)!;
                var benchDir = parseResult.GetValue(benchDirOption)!;
                var outputRoot = parseResult.GetValue(outputOption)!;
                var avProductOverride = parseResult.GetValue(avProductOption);
                var avVersionOverride = parseResult.GetValue(avVersionOption);
                if (!BenchmarkWorkloads.TryNormalizeRun(parseResult.GetValue(workloadOption), out var selection, out var error))
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

                ValidateRequestedWorkloads(manifest, selection);

                Directory.CreateDirectory(outputRoot.FullName);
                File.Copy(manifestPath, Path.Combine(outputRoot.FullName, SetupService.SuiteManifestFileName), overwrite: true);

                await IdleChecker.VerifyAsync(CancellationToken.None);

                var detectedAv = AvDetector.Detect();
                var effectiveAv = new AvInfo(
                    NormalizeOverride(avProductOverride) ?? detectedAv.ProductName,
                    NormalizeOverride(avVersionOverride) ?? detectedAv.ProductVersion);

                Console.WriteLine($"[run] AV: {effectiveAv.ProductName} v{effectiveAv.ProductVersion}");

                var runner = new ScenarioRunner(
                    avName.Trim(),
                    outputRoot.FullName,
                    SystemInfoProvider.GetRunnerVersion(),
                    SetupService.ComputeManifestSha(manifestPath),
                    effectiveAv);

                var scenarios = new List<ScenarioDefinition>();
                if (selection.IncludesWorkloadFamily(BenchmarkWorkloads.Ripgrep))
                {
                    scenarios.AddRange(RipgrepScenarioFactory.Create(manifest));
                }

                if (selection.IncludesWorkloadFamily(BenchmarkWorkloads.Roslyn))
                {
                    scenarios.AddRange(RoslynScenarioFactory.Create(manifest));
                }

                if (selection.IncludesWorkloadFamily(BenchmarkWorkloads.Microbench) || selection.IncludesAnyMicrobenchScenario())
                {
                    scenarios.AddRange(MicrobenchScenarioFactory.Create(manifest, selection.MicrobenchScenarioIds));
                }

                var orderedScenarios = OrderScenariosForSession(scenarios);
                var results = await runner.ExecuteScenariosAsync(orderedScenarios, CancellationToken.None);

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

    private static void ValidateRequestedWorkloads(SuiteManifest manifest, BenchmarkRunSelection selection)
    {
        var availableWorkloads = manifest.Workloads
            .Select(workload => workload.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var workload in selection.WorkloadFamilies)
        {
            if (string.Equals(workload, BenchmarkWorkloads.Microbench, StringComparison.OrdinalIgnoreCase))
            {
                manifest.GetRequiredMicrobenchSupport();
                continue;
            }

            if (!availableWorkloads.Contains(workload))
            {
                throw new InvalidOperationException(
                    $"The suite manifest does not contain workload '{workload}'. Run `avbench setup --workload {workload}` first, or rerun setup with a broader workload selection.");
            }
        }

        if (selection.IncludesAnyMicrobenchScenario())
        {
            manifest.GetRequiredMicrobenchSupport();
        }
    }

    private static List<ScenarioDefinition> OrderScenariosForSession(IReadOnlyList<ScenarioDefinition> scenarios)
    {
        var groups = new List<List<ScenarioDefinition>>();
        List<ScenarioDefinition>? currentGroup = null;
        string? currentFamily = null;

        foreach (var scenario in scenarios)
        {
            var family = GetScenarioFamily(scenario.Id);
            if (currentGroup is null || !string.Equals(currentFamily, family, StringComparison.OrdinalIgnoreCase))
            {
                currentGroup = [];
                groups.Add(currentGroup);
                currentFamily = family;
            }

            currentGroup.Add(scenario);
        }

        for (var index = groups.Count - 1; index > 0; index--)
        {
            var swapIndex = Random.Shared.Next(index + 1);
            (groups[index], groups[swapIndex]) = (groups[swapIndex], groups[index]);
        }

        return groups.SelectMany(static group => group).ToList();
    }

    private static string GetScenarioFamily(string scenarioId)
    {
        if (scenarioId.StartsWith("ripgrep-", StringComparison.OrdinalIgnoreCase))
        {
            return "ripgrep";
        }

        if (scenarioId.StartsWith("roslyn-", StringComparison.OrdinalIgnoreCase))
        {
            return "roslyn";
        }

        return scenarioId;
    }

    private static string? NormalizeOverride(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
