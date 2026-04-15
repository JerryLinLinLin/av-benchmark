using AvBench.Core.Internal;
using AvBench.Core.Setup;
using System.Runtime.Versioning;

namespace AvBench.Core.Scenarios;

[SupportedOSPlatform("windows")]
public static class ScenarioSupport
{
    public const int MicrobenchCooldownMilliseconds = 10_000;
    public const int CompileCooldownMilliseconds = 20_000;

    public static void DeletePathIfExists(string path)
    {
        FileSystemUtil.DeletePathIfExists(path);
    }

    public static async Task EnsureFileExistsAsync(string path, string description)
    {
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Expected {description} was not produced: {path}");
        }

        await Task.CompletedTask;
    }

    public static async Task EnsureDirectoryHasFilesAsync(string path, string description)
    {
        if (!Directory.Exists(path) || !Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Any())
        {
            throw new InvalidOperationException($"Expected {description} directory to contain output files: {path}");
        }

        await Task.CompletedTask;
    }

    public static async Task EnsureBuildOutputsExistAsync(
        IReadOnlyCollection<string> expectedPaths,
        Func<CancellationToken, Task> prerequisiteBuildAsync,
        CancellationToken cancellationToken)
    {
        if (expectedPaths.Any(static path => File.Exists(path) || Directory.Exists(path)))
        {
            return;
        }

        await prerequisiteBuildAsync(cancellationToken);
    }

    public static Task RunProcessAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        string operationName,
        CancellationToken cancellationToken)
        => ProcessUtil.EnsureSuccessAsync(fileName, arguments, workingDirectory, operationName, cancellationToken);

    public static Task RunDeveloperShellProcessAsync(
        string commandLine,
        string workingDirectory,
        string operationName,
        CancellationToken cancellationToken)
        => VsBuildToolsInstaller.EnsureSuccessInDeveloperShellAsync(commandLine, workingDirectory, operationName, cancellationToken);

    public static string GetScenarioFamily(string scenarioId)
    {
        if (scenarioId.StartsWith("ripgrep-", StringComparison.OrdinalIgnoreCase))
        {
            return "ripgrep";
        }

        if (scenarioId.StartsWith("roslyn-", StringComparison.OrdinalIgnoreCase))
        {
            return "roslyn";
        }

        return scenarioId;
    }

    public static bool IsCompilationFamily(string family)
        => string.Equals(family, "ripgrep", StringComparison.OrdinalIgnoreCase)
            || string.Equals(family, "roslyn", StringComparison.OrdinalIgnoreCase);

    public static bool IsCompilationScenario(string scenarioId)
        => IsCompilationFamily(GetScenarioFamily(scenarioId));

    public static int GetCooldownMilliseconds(string currentScenarioId, string nextScenarioId)
    {
        var currentIsCompilation = IsCompilationScenario(currentScenarioId);
        var nextIsCompilation = IsCompilationScenario(nextScenarioId);

        if (currentIsCompilation && nextIsCompilation)
        {
            return CompileCooldownMilliseconds;
        }

        return MicrobenchCooldownMilliseconds;
    }
}
