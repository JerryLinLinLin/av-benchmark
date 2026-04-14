using AvBench.Core.Models;
using System.Runtime.Versioning;

namespace AvBench.Core.Scenarios;

[SupportedOSPlatform("windows")]
public static class RipgrepScenarioFactory
{
    public static IReadOnlyList<ScenarioDefinition> Create(SuiteManifest manifest)
    {
        var workload = manifest.GetRequiredWorkload("ripgrep");
        var repoDirectory = workload.WorkingDirectory;
        var artifactPath = Path.Combine(repoDirectory, "target", "release", "rg.exe");
        var targetDirectory = Path.Combine(repoDirectory, "target");

        return
        [
            new ScenarioDefinition
            {
                Id = "ripgrep-clean-build",
                FileName = "cargo",
                Arguments = "build --release",
                WorkingDirectory = repoDirectory,
                PrepareAsync = _ =>
                {
                    ScenarioSupport.DeletePathIfExists(targetDirectory);
                    return Task.CompletedTask;
                },
                ValidateAsync = _ => ScenarioSupport.EnsureFileExistsAsync(artifactPath, "ripgrep artifact")
            },
            new ScenarioDefinition
            {
                Id = "ripgrep-incremental-build",
                FileName = "cargo",
                Arguments = "build --release",
                WorkingDirectory = repoDirectory,
                PrepareAsync = async cancellationToken =>
                {
                    await ScenarioSupport.EnsureBuildOutputsExistAsync(
                        [artifactPath],
                        ct => RunUntimedBuildAsync(repoDirectory, ct),
                        cancellationToken);
                    SourceFileToucher.Touch(workload.IncrementalTouchPath!);
                },
                ValidateAsync = _ => ScenarioSupport.EnsureFileExistsAsync(artifactPath, "ripgrep artifact")
            },
            new ScenarioDefinition
            {
                Id = "ripgrep-noop-build",
                FileName = "cargo",
                Arguments = "build --release",
                WorkingDirectory = repoDirectory,
                PrepareAsync = cancellationToken => ScenarioSupport.EnsureBuildOutputsExistAsync(
                    [artifactPath],
                    ct => RunUntimedBuildAsync(repoDirectory, ct),
                    cancellationToken),
                ValidateAsync = _ => ScenarioSupport.EnsureFileExistsAsync(artifactPath, "ripgrep artifact")
            }
        ];
    }

    private static Task RunUntimedBuildAsync(string repoDirectory, CancellationToken cancellationToken)
    {
        return ScenarioSupport.RunProcessAsync(
            "cargo",
            "build --release",
            repoDirectory,
            "ripgrep untimed prerequisite build",
            cancellationToken);
    }
}
