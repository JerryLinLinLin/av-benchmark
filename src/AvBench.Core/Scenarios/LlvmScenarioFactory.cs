using AvBench.Core.Models;
using AvBench.Core.Setup;

namespace AvBench.Core.Scenarios;

public static class LlvmScenarioFactory
{
    public static IReadOnlyList<ScenarioDefinition> Create(SuiteManifest manifest)
    {
        var workload = manifest.GetRequiredWorkload("llvm");
        var repoDirectory = workload.WorkingDirectory;
        var buildDirectory = workload.BuildDirectory
            ?? throw new InvalidOperationException("LLVM workload is missing its build directory in the suite manifest.");
        var clangPath = Path.Combine(buildDirectory, "bin", "clang.exe");
        var buildNinjaPath = Path.Combine(buildDirectory, "build.ninja");

        return
        [
            new ScenarioDefinition
            {
                Id = "llvm-configure",
                FileName = "cmd.exe",
                Arguments = $"/d /c {RepoCloner.BuildLlvmConfigureCommand(repoDirectory, buildDirectory)}",
                WorkingDirectory = repoDirectory,
                PrepareAsync = _ =>
                {
                    ScenarioSupport.DeletePathIfExists(buildDirectory);
                    Directory.CreateDirectory(buildDirectory);
                    return Task.CompletedTask;
                },
                ValidateAsync = _ => ScenarioSupport.EnsureFileExistsAsync(buildNinjaPath, "LLVM build.ninja")
            },
            new ScenarioDefinition
            {
                Id = "llvm-clean-build",
                FileName = "cmd.exe",
                Arguments = $"/d /c {BuildNinjaCommand(buildDirectory)}",
                WorkingDirectory = repoDirectory,
                PrepareAsync = async cancellationToken =>
                {
                    await EnsureConfiguredAsync(repoDirectory, buildDirectory, buildNinjaPath, cancellationToken);
                    await ScenarioSupport.RunDeveloperShellProcessAsync(
                        $"ninja -C \"{buildDirectory}\" -t clean",
                        repoDirectory,
                        "LLVM ninja clean",
                        cancellationToken);
                },
                ValidateAsync = _ => ScenarioSupport.EnsureFileExistsAsync(clangPath, "LLVM clang artifact")
            },
            new ScenarioDefinition
            {
                Id = "llvm-incremental-build",
                FileName = "cmd.exe",
                Arguments = $"/d /c {BuildNinjaCommand(buildDirectory)}",
                WorkingDirectory = repoDirectory,
                PrepareAsync = async cancellationToken =>
                {
                    await EnsureBuildOutputsReadyAsync(repoDirectory, buildDirectory, buildNinjaPath, clangPath, cancellationToken);
                    SourceFileToucher.Touch(workload.IncrementalTouchPath!);
                },
                ValidateAsync = _ => ScenarioSupport.EnsureFileExistsAsync(clangPath, "LLVM clang artifact")
            },
            new ScenarioDefinition
            {
                Id = "llvm-noop-build",
                FileName = "cmd.exe",
                Arguments = $"/d /c {BuildNinjaCommand(buildDirectory)}",
                WorkingDirectory = repoDirectory,
                PrepareAsync = cancellationToken => EnsureBuildOutputsReadyAsync(repoDirectory, buildDirectory, buildNinjaPath, clangPath, cancellationToken),
                ValidateAsync = _ => ScenarioSupport.EnsureFileExistsAsync(clangPath, "LLVM clang artifact")
            }
        ];
    }

    private static string BuildNinjaCommand(string buildDirectory)
        => $"ninja -C \"{buildDirectory}\"";

    private static async Task EnsureConfiguredAsync(
        string repoDirectory,
        string buildDirectory,
        string buildNinjaPath,
        CancellationToken cancellationToken)
    {
        await ScenarioSupport.EnsureBuildOutputsExistAsync(
            [buildNinjaPath],
            ct => ScenarioSupport.RunDeveloperShellProcessAsync(
                RepoCloner.BuildLlvmConfigureCommand(repoDirectory, buildDirectory),
                repoDirectory,
                "LLVM untimed prerequisite configure",
                ct),
            cancellationToken);
    }

    private static async Task EnsureBuildOutputsReadyAsync(
        string repoDirectory,
        string buildDirectory,
        string buildNinjaPath,
        string clangPath,
        CancellationToken cancellationToken)
    {
        await EnsureConfiguredAsync(repoDirectory, buildDirectory, buildNinjaPath, cancellationToken);
        await ScenarioSupport.EnsureBuildOutputsExistAsync(
            [clangPath],
            ct => ScenarioSupport.RunDeveloperShellProcessAsync(
                BuildNinjaCommand(buildDirectory),
                repoDirectory,
                "LLVM untimed prerequisite build",
                ct),
            cancellationToken);
    }
}
