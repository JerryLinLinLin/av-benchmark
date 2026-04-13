using AvBench.Core.Models;
using AvBench.Core.Setup;

namespace AvBench.Core.Scenarios;

public static class FilesScenarioFactory
{
    public static IReadOnlyList<ScenarioDefinition> Create(SuiteManifest manifest)
    {
        var workload = manifest.GetRequiredWorkload("files");
        var repoDirectory = workload.WorkingDirectory;
        var solutionPath = Path.Combine(repoDirectory, "Files.slnx");
        var msbuildPath = VsBuildToolsInstaller.FindMsBuildPath()
            ?? throw new InvalidOperationException("MSBuild.exe could not be located. Visual Studio Build Tools are required.");
        var outputDirectory = Path.Combine(repoDirectory, "src", "Files.App", "bin");

        if (!File.Exists(solutionPath))
        {
            throw new InvalidOperationException($"Files solution was not found at {solutionPath}.");
        }

        var buildArguments = $"\"{solutionPath}\" /p:Configuration=Release /p:Platform=x64 /m /nr:false /p:UseSharedCompilation=false";

        return
        [
            new ScenarioDefinition
            {
                Id = "files-clean-build",
                FileName = msbuildPath,
                Arguments = buildArguments,
                WorkingDirectory = repoDirectory,
                PrepareAsync = _ =>
                {
                    DeleteCommonBuildOutputs(repoDirectory);
                    return Task.CompletedTask;
                },
                ValidateAsync = _ => ScenarioSupport.EnsureDirectoryHasFilesAsync(outputDirectory, "Files build output")
            },
            new ScenarioDefinition
            {
                Id = "files-incremental-build",
                FileName = msbuildPath,
                Arguments = buildArguments,
                WorkingDirectory = repoDirectory,
                PrepareAsync = async cancellationToken =>
                {
                    await ScenarioSupport.EnsureBuildOutputsExistAsync(
                        [outputDirectory],
                        ct => RunUntimedBuildAsync(msbuildPath, buildArguments, repoDirectory, ct),
                        cancellationToken);
                    SourceFileToucher.Touch(workload.IncrementalTouchPath!);
                },
                ValidateAsync = _ => ScenarioSupport.EnsureDirectoryHasFilesAsync(outputDirectory, "Files build output")
            },
            new ScenarioDefinition
            {
                Id = "files-noop-build",
                FileName = msbuildPath,
                Arguments = buildArguments,
                WorkingDirectory = repoDirectory,
                PrepareAsync = cancellationToken => ScenarioSupport.EnsureBuildOutputsExistAsync(
                    [outputDirectory],
                    ct => RunUntimedBuildAsync(msbuildPath, buildArguments, repoDirectory, ct),
                    cancellationToken),
                ValidateAsync = _ => ScenarioSupport.EnsureDirectoryHasFilesAsync(outputDirectory, "Files build output")
            }
        ];
    }

    private static void DeleteCommonBuildOutputs(string repoDirectory)
    {
        foreach (var directory in Directory.EnumerateDirectories(repoDirectory, "bin", SearchOption.AllDirectories))
        {
            ScenarioSupport.DeletePathIfExists(directory);
        }

        foreach (var directory in Directory.EnumerateDirectories(repoDirectory, "obj", SearchOption.AllDirectories))
        {
            ScenarioSupport.DeletePathIfExists(directory);
        }

        foreach (var directory in Directory.EnumerateDirectories(repoDirectory, "AppPackages", SearchOption.AllDirectories))
        {
            ScenarioSupport.DeletePathIfExists(directory);
        }
    }

    private static Task RunUntimedBuildAsync(
        string msbuildPath,
        string buildArguments,
        string repoDirectory,
        CancellationToken cancellationToken)
    {
        return ScenarioSupport.RunProcessAsync(
            msbuildPath,
            buildArguments,
            repoDirectory,
            "Files untimed prerequisite build",
            cancellationToken);
    }
}
