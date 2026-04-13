using System.Diagnostics;
using AvBench.Core.Models;

namespace AvBench.Core.Setup;

public static class RepoCloner
{
    public static async Task<RepoEntry> CloneRipgrepAsync(string benchDirectory, string? revision, CancellationToken cancellationToken)
    {
        var repoUrl = "https://github.com/BurntSushi/ripgrep.git";
        var targetDirectory = Path.Combine(benchDirectory, "ripgrep");

        if (!Directory.Exists(targetDirectory))
        {
            Console.WriteLine($"[setup] Cloning {repoUrl} into {targetDirectory}");
            await RunGitAsync($"clone --config core.autocrlf=false {repoUrl} \"{targetDirectory}\"", benchDirectory, cancellationToken);
        }
        else
        {
            Console.WriteLine($"[setup] Reusing existing repo: {targetDirectory}");
        }

        await RunGitAsync("-C \"" + targetDirectory + "\" fetch --all --tags --prune", benchDirectory, cancellationToken);

        if (!string.IsNullOrWhiteSpace(revision))
        {
            await RunGitAsync("-C \"" + targetDirectory + "\" checkout " + revision, benchDirectory, cancellationToken);
        }

        var sha = ToolInstaller.RunAndCapture("git", $"-C \"{targetDirectory}\" rev-parse HEAD", benchDirectory)
            ?? throw new InvalidOperationException("Unable to resolve ripgrep HEAD SHA.");

        return new RepoEntry
        {
            Name = "ripgrep",
            Url = repoUrl,
            Sha = sha,
            LocalPath = targetDirectory
        };
    }

    public static async Task CargoFetchAsync(string repoDirectory, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[setup] Running cargo fetch in {repoDirectory}");
        using var process = Process.Start(new ProcessStartInfo("cargo", "fetch")
        {
            WorkingDirectory = repoDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        });

        if (process is null)
        {
            throw new InvalidOperationException("Failed to start cargo fetch.");
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);
        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"cargo fetch failed with exit code {process.ExitCode}:{System.Environment.NewLine}{stdout}{System.Environment.NewLine}{stderr}");
        }
    }

    public static string ResolveIncrementalTouchPath(string repoDirectory)
    {
        var preferredCandidates = new[]
        {
            Path.Combine(repoDirectory, "crates", "core", "main.rs"),
            Path.Combine(repoDirectory, "crates", "core", "lib.rs"),
            Path.Combine(repoDirectory, "src", "main.rs")
        };

        foreach (var candidate in preferredCandidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        var fallback = Directory
            .EnumerateFiles(repoDirectory, "*.rs", SearchOption.AllDirectories)
            .Where(path => !path.Contains(Path.DirectorySeparatorChar + "target" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path.Length)
            .FirstOrDefault();

        return fallback ?? throw new InvalidOperationException("Unable to locate a Rust source file for the incremental ripgrep scenario.");
    }

    private static async Task RunGitAsync(string arguments, string workingDirectory, CancellationToken cancellationToken)
    {
        using var process = Process.Start(new ProcessStartInfo("git", arguments)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        });

        if (process is null)
        {
            throw new InvalidOperationException("Failed to start git.");
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);
        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"git {arguments} failed with exit code {process.ExitCode}:{System.Environment.NewLine}{stdout}{System.Environment.NewLine}{stderr}");
        }
    }
}
