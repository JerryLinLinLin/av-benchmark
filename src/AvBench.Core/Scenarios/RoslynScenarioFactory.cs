using AvBench.Core.Models;

namespace AvBench.Core.Scenarios;

public static class RoslynScenarioFactory
{
    public static IReadOnlyList<ScenarioDefinition> Create(SuiteManifest manifest)
    {
        var workload = manifest.GetRequiredWorkload("roslyn");
        var repoDirectory = workload.WorkingDirectory;
        var artifactsDirectory = Path.Combine(repoDirectory, "artifacts", "bin");
        var buildScriptPath = Path.Combine(repoDirectory, "Build.cmd");

        if (!File.Exists(buildScriptPath))
        {
            throw new InvalidOperationException($"Roslyn build script was not found at {buildScriptPath}.");
        }

        return
        [
            new ScenarioDefinition
            {
                Id = "roslyn-clean-build",
                FileName = "cmd.exe",
                Arguments = "/d /c Build.cmd -configuration Release",
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
                FileName = "cmd.exe",
                Arguments = "/d /c Build.cmd -configuration Release",
                WorkingDirectory = repoDirectory,
                PrepareAsync = async cancellationToken =>
                {
                    await ScenarioSupport.EnsureBuildOutputsExistAsync(
                        [artifactsDirectory],
                        ct => RunUntimedBuildAsync(repoDirectory, ct),
                        cancellationToken);
                    SourceFileToucher.Touch(workload.IncrementalTouchPath!);
                },
                ValidateAsync = _ => ScenarioSupport.EnsureDirectoryHasFilesAsync(artifactsDirectory, "Roslyn artifacts")
            },
            new ScenarioDefinition
            {
                Id = "roslyn-noop-build",
                FileName = "cmd.exe",
                Arguments = "/d /c Build.cmd -configuration Release",
                WorkingDirectory = repoDirectory,
                PrepareAsync = cancellationToken => ScenarioSupport.EnsureBuildOutputsExistAsync(
                    [artifactsDirectory],
                    ct => RunUntimedBuildAsync(repoDirectory, ct),
                    cancellationToken),
                ValidateAsync = _ => ScenarioSupport.EnsureDirectoryHasFilesAsync(artifactsDirectory, "Roslyn artifacts")
            }
        ];
    }

    private static Task RunUntimedBuildAsync(string repoDirectory, CancellationToken cancellationToken)
    {
        return ScenarioSupport.RunProcessAsync(
            "cmd.exe",
            "/d /c Build.cmd -configuration Release",
            repoDirectory,
            "Roslyn untimed prerequisite build",
            cancellationToken);
    }
}
