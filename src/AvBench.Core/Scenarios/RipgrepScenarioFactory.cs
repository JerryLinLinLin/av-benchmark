using AvBench.Core.Models;

namespace AvBench.Core.Scenarios;

public static class RipgrepScenarioFactory
{
    public static IReadOnlyList<ScenarioDefinition> Create(SuiteManifest manifest)
    {
        var repo = manifest.Repos.SingleOrDefault(entry => string.Equals(entry.Name, "ripgrep", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException("Suite manifest does not contain the ripgrep repo.");

        var repoDirectory = repo.LocalPath;
        var artifactPath = Path.Combine(repoDirectory, "target", "release", "rg.exe");

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
                    var targetDirectory = Path.Combine(repoDirectory, "target");
                    if (Directory.Exists(targetDirectory))
                    {
                        Directory.Delete(targetDirectory, recursive: true);
                    }

                    return Task.CompletedTask;
                },
                ValidateAsync = _ => EnsureArtifactExistsAsync(artifactPath)
            },
            new ScenarioDefinition
            {
                Id = "ripgrep-incremental-build",
                FileName = "cargo",
                Arguments = "build --release",
                WorkingDirectory = repoDirectory,
                PrepareAsync = _ =>
                {
                    Touch(manifest.IncrementalTouchPath);
                    return Task.CompletedTask;
                },
                ValidateAsync = _ => EnsureArtifactExistsAsync(artifactPath)
            },
            new ScenarioDefinition
            {
                Id = "ripgrep-noop-build",
                FileName = "cargo",
                Arguments = "build --release",
                WorkingDirectory = repoDirectory,
                PrepareAsync = _ => Task.CompletedTask,
                ValidateAsync = _ => EnsureArtifactExistsAsync(artifactPath)
            }
        ];
    }

    private static Task EnsureArtifactExistsAsync(string artifactPath)
    {
        if (!File.Exists(artifactPath))
        {
            throw new InvalidOperationException($"Expected ripgrep artifact was not produced: {artifactPath}");
        }

        return Task.CompletedTask;
    }

    private static void Touch(string path)
    {
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Incremental touch target does not exist: {path}");
        }

        File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
    }
}

