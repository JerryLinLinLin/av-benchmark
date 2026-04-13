using System.CommandLine;
using System.Text.Json;
using AvBench.Core.Environment;
using AvBench.Core.Models;
using AvBench.Core.Output;
using AvBench.Core.Scenarios;
using AvBench.Core.Serialization;
using AvBench.Core.Setup;

namespace AvBench.Cli.Commands;

public static class RunCommand
{
    public static Command Create()
    {
        var profileOption = new Option<FileInfo>("--profile")
        {
            Description = "Path to an AV profile JSON file."
        };
        profileOption.Required = true;

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

        var command = new Command("run", "Execute milestone 1 benchmark scenarios.");
        command.Options.Add(profileOption);
        command.Options.Add(benchDirOption);
        command.Options.Add(repetitionsOption);
        command.Options.Add(outputOption);

        command.SetAction(async parseResult =>
        {
            var profilePath = parseResult.GetValue(profileOption)!;
            var benchDir = parseResult.GetValue(benchDirOption)!;
            var repetitions = parseResult.GetValue(repetitionsOption);
            var outputRoot = parseResult.GetValue(outputOption)!;

            var manifestPath = Path.Combine(benchDir.FullName, SetupService.SuiteManifestFileName);
            if (!File.Exists(manifestPath))
            {
                throw new FileNotFoundException($"Suite manifest not found at {manifestPath}. Run `avbench setup` first.");
            }

            if (!profilePath.Exists)
            {
                throw new FileNotFoundException($"Profile not found: {profilePath.FullName}");
            }

            var manifest = JsonSerializer.Deserialize(
                await File.ReadAllTextAsync(manifestPath, CancellationToken.None),
                AvBenchJsonContext.Default.SuiteManifest)
                ?? throw new InvalidOperationException("Suite manifest could not be parsed.");

            var profile = JsonSerializer.Deserialize(
                await File.ReadAllTextAsync(profilePath.FullName, CancellationToken.None),
                AvBenchJsonContext.Default.AvProfile)
                ?? throw new InvalidOperationException("AV profile could not be parsed.");

            Directory.CreateDirectory(outputRoot.FullName);
            var runDirectory = Path.Combine(outputRoot.FullName, DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
            Directory.CreateDirectory(runDirectory);

            File.Copy(manifestPath, Path.Combine(runDirectory, SetupService.SuiteManifestFileName), overwrite: true);

            Console.WriteLine("[run] Idle check: not yet enforced in milestone 1; proceeding with benchmark execution.");

            var runner = new ScenarioRunner(
                profile,
                runDirectory,
                SystemInfoProvider.GetRunnerVersion(),
                SetupService.ComputeManifestSha(manifestPath));

            var results = new List<RunResult>();
            foreach (var scenario in RipgrepScenarioFactory.Create(manifest))
            {
                results.AddRange(await runner.ExecuteScenarioAsync(scenario, repetitions, CancellationToken.None));
            }

            var executablePath = Environment.ProcessPath
                ?? throw new InvalidOperationException("Unable to resolve the current executable path.");
            var fileMicrobench = FileMicrobenchScenarioFactory.Create(executablePath, benchDir.FullName);
            results.AddRange(await runner.ExecuteScenarioAsync(fileMicrobench, repetitions, CancellationToken.None));

            var csvPath = Path.Combine(runDirectory, "runs.csv");
            await CsvResultWriter.WriteAsync(results, csvPath, CancellationToken.None);

            Console.WriteLine($"[run] Wrote {results.Count} run records to {runDirectory}");
            return 0;
        });

        return command;
    }
}
