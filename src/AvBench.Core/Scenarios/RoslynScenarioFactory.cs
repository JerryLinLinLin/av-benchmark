using AvBench.Core.Models;
using System.Runtime.Versioning;

namespace AvBench.Core.Scenarios;

[SupportedOSPlatform("windows")]
public static class RoslynScenarioFactory
{
    public static IReadOnlyList<ScenarioDefinition> Create(SuiteManifest manifest)
    {
        var workload = manifest.GetRequiredWorkload("roslyn");
        var repo = manifest.GetRequiredRepo("roslyn");
        var repoDirectory = workload.WorkingDirectory;
        var artifactsDirectory = Path.Combine(repoDirectory, "artifacts", "bin");
        var solutionPath = Path.Combine(repoDirectory, "Roslyn.slnx");

        if (!File.Exists(solutionPath))
        {
            throw new InvalidOperationException($"Roslyn solution was not found at {solutionPath}.");
        }

        var buildArguments = BuildArguments(solutionPath, repo);

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

    private static string BuildArguments(string solutionPath, RepoEntry repo)
    {
        var arguments = new List<string>
        {
            "build",
            $"\"{solutionPath}\"",
            "-c Release",
            "/m",
            "/nr:false",
            $"/p:RepositoryUrl={repo.Url}",
            $"/p:RepositoryCommit={repo.Sha}"
        };

        if (!string.IsNullOrWhiteSpace(repo.SourceReference))
        {
            arguments.Add($"/p:RepositoryBranch={repo.SourceReference}");
        }

        return string.Join(" ", arguments);
    }
}
