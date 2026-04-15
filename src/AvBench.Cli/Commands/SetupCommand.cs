using System.CommandLine;
using System.Net.Http;
using System.Runtime.Versioning;
using AvBench.Core;
using AvBench.Core.Setup;

namespace AvBench.Cli.Commands;

[SupportedOSPlatform("windows")]
public static class SetupCommand
{
    public static Command Create()
    {
        var benchDirOption = new Option<DirectoryInfo>("--bench-dir")
        {
            Description = "Root directory for benchmark repos and manifests.",
            DefaultValueFactory = _ => new DirectoryInfo(@"C:\bench")
        };

        var ripgrepRefOption = new Option<string?>("--ripgrep-ref")
        {
            Description = "Optional ripgrep branch, tag, or SHA to check out before hydrating dependencies."
        };

        var workloadOption = new Option<string[]>("--workload", ["-w"])
        {
            Description = $"One or more workload ids to set up. Defaults to all: {BenchmarkWorkloads.HelpText}.",
            AllowMultipleArgumentsPerToken = true,
            DefaultValueFactory = _ => []
        };

        var command = new Command("setup", "Install toolchains, fetch selected benchmark source trees, hydrate dependencies, and write suite-manifest.json.");
        command.Options.Add(benchDirOption);
        command.Options.Add(ripgrepRefOption);
        command.Options.Add(workloadOption);

        command.SetAction(async parseResult =>
        {
            try
            {
                var benchDir = parseResult.GetValue(benchDirOption)!;
                var ripgrepRef = parseResult.GetValue(ripgrepRefOption);
                if (!BenchmarkWorkloads.TryNormalize(parseResult.GetValue(workloadOption), out var workloads, out var error))
                {
                    Console.Error.WriteLine($"ERROR: {error}");
                    return 1;
                }

                var service = new SetupService();
                await service.ExecuteAsync(benchDir.FullName, ripgrepRef, workloads, CancellationToken.None);
                return 0;
            }
            catch (SetupRestartRequiredException ex)
            {
                Console.WriteLine($"[setup] {ex.Message}");
                return 2;
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
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"ERROR: Setup could not download required sources or tools. {ex.Message}");
                return 1;
            }
        });

        return command;
    }
}
