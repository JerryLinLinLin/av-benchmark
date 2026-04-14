using AvBench.Core.Internal;
using AvBench.Core.Setup;
using System.Runtime.Versioning;

namespace AvBench.Core.Scenarios;

[SupportedOSPlatform("windows")]
internal static class ScenarioSupport
{
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
}
