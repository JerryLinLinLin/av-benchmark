using AvBench.Core.Models;
using System.Text;

namespace AvBench.Core.Scenarios;

public static class RipgrepScenarioFactory
{
    private const string IncrementalMarkerPrefix = "// avbench incremental marker: ";

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
                    MutateIncrementalSource(manifest.IncrementalTouchPath);
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

    private static void MutateIncrementalSource(string path)
    {
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Incremental touch target does not exist: {path}");
        }

        var original = File.ReadAllText(path);
        string updated;

        if (original.Contains(IncrementalMarkerPrefix, StringComparison.Ordinal))
        {
            updated = ToggleExistingMarker(original);
        }
        else
        {
            var newline = DetectNewline(original);
            var builder = new StringBuilder(original);
            if (!original.EndsWith("\r\n", StringComparison.Ordinal) && !original.EndsWith("\n", StringComparison.Ordinal))
            {
                builder.Append(newline);
            }

            builder.Append(IncrementalMarkerPrefix);
            builder.Append('1');
            builder.Append(newline);
            updated = builder.ToString();
        }

        File.WriteAllText(path, updated);
    }

    private static string ToggleExistingMarker(string content)
    {
        var lineStart = content.IndexOf(IncrementalMarkerPrefix, StringComparison.Ordinal);
        if (lineStart < 0)
        {
            return content;
        }

        var valueStart = lineStart + IncrementalMarkerPrefix.Length;
        var valueEnd = content.IndexOfAny(['\r', '\n'], valueStart);
        if (valueEnd < 0)
        {
            valueEnd = content.Length;
        }

        var currentValue = content[valueStart..valueEnd].Trim();
        var nextValue = currentValue == "1" ? "0" : "1";
        return string.Concat(content.AsSpan(0, valueStart), nextValue, content.AsSpan(valueEnd));
    }

    private static string DetectNewline(string content)
    {
        return content.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
    }
}
