using System.CommandLine;
using AvBench.Core.Setup;

namespace AvBench.Cli.Commands;

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

        var command = new Command("setup", "Install milestone 2 toolchains, clone benchmark repos, hydrate dependencies, and write suite-manifest.json.");
        command.Options.Add(benchDirOption);
        command.Options.Add(ripgrepRefOption);

        command.SetAction(async parseResult =>
        {
            var benchDir = parseResult.GetValue(benchDirOption)!;
            var ripgrepRef = parseResult.GetValue(ripgrepRefOption);

            var service = new SetupService();
            await service.ExecuteAsync(benchDir.FullName, ripgrepRef, CancellationToken.None);
            return 0;
        });

        return command;
    }
}
