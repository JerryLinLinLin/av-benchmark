using AvBench.Core.Models;

namespace AvBench.Core.Scenarios;

public static class RoslynScenarioFactory
{
    public static IReadOnlyList<ScenarioDefinition> Create(SuiteManifest manifest)
    {
        var workload = manifest.GetRequiredWorkload("roslyn");
        var repoDirectory = workload.WorkingDirectory;
        var artifactsDirectory = Path.Combine(repoDirectory, "artifacts", "bin");
        var solutionPath = Path.Combine(repoDirectory, "Roslyn.slnx");

        if (!File.Exists(solutionPath))
        {
            throw new InvalidOperationException($"Roslyn solution was not found at {solutionPath}.");
        }

        var buildArguments = $"build \"{solutionPath}\" -c Release /m /nr:false";

        return
        [
            new ScenarioDefinition
            {
                Id = "roslyn-clean-build",
                FileName = "dotnet",
                Arguments = buildArguments,
                WorkingDirectory = repoDirectory,
                PrepareAsync = _ =>
                {
                    ScenarioSupport.DeletePathIfExists(Path.Combine(repoDirectory, "artifacts", "bin"));
                    ScenarioSupport.DeletePathIfExists(Path.Combine(repoDirectory, "artifacts", "obj"));
                    return Task.CompletedTask;
                },
                ValidateAsync = _ => ScenarioSupport.EnsureDirectoryHasFilesAsync(artifactsDirectory, "Roslyn artifacts")
            },
            new ScenarioDefinition
            {
                Id = "roslyn-incremental-build",
                FileName = "dotnet",
                Arguments = buildArguments,
                WorkingDirectory = repoDirectory,
                PrepareAsync = async cancellationToken =>
                {
                    await ScenarioSupport.EnsureBuildOutputsExistAsync(
                        [artifactsDirectory],
                        ct => RunUntimedBuildAsync(repoDirectory, buildArguments, ct),
                        cancellationToken);
                    SourceFileToucher.Touch(workload.IncrementalTouchPath!);
                },
                ValidateAsync = _ => ScenarioSupport.EnsureDirectoryHasFilesAsync(artifactsDirectory, "Roslyn artifacts")
            },
            new ScenarioDefinition
            {
                Id = "roslyn-noop-build",
                FileName = "dotnet",
                Arguments = buildArguments,
                WorkingDirectory = repoDirectory,
                PrepareAsync = cancellationToken => ScenarioSupport.EnsureBuildOutputsExistAsync(
                    [artifactsDirectory],
                    ct => RunUntimedBuildAsync(repoDirectory, buildArguments, ct),
                    cancellationToken),
                ValidateAsync = _ => ScenarioSupport.EnsureDirectoryHasFilesAsync(artifactsDirectory, "Roslyn artifacts")
            }
        ];
    }

    private static Task RunUntimedBuildAsync(string repoDirectory, string buildArguments, CancellationToken cancellationToken)
    {
        return ScenarioSupport.RunProcessAsync(
            "dotnet",
            buildArguments,
            repoDirectory,
            "Roslyn untimed prerequisite build",
            cancellationToken);
    }
}
