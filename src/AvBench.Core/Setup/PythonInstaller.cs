namespace AvBench.Core.Setup;

public sealed class PythonInstaller : ToolInstaller
{
    private const string WingetPackageId = "Python.Python.3.14";

    public override string Name => "Python";

    public override string? Detect()
    {
        KnownToolPaths.EnsurePythonOnPath();
        return RunAndCapture("python", "--version");
    }

    public override async Task InstallAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();

        var exitCode = RunProcess(
            "winget",
            $"install -e --id {WingetPackageId} --source winget --accept-package-agreements --accept-source-agreements --silent");

        if (exitCode != 0)
        {
            throw new InvalidOperationException($"winget failed to install {WingetPackageId} (exit code {exitCode}).");
        }

        KnownToolPaths.EnsurePythonOnPath();
    }
}
