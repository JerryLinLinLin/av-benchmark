namespace AvBench.Core.Setup;

public sealed class DotNetSdkInstaller(IReadOnlyCollection<string> requiredVersions) : ToolInstaller
{
    private const string InstallScriptUrl = "https://dot.net/v1/dotnet-install.ps1";
    private readonly string[] _requiredVersions = requiredVersions
        .Where(static version => !string.IsNullOrWhiteSpace(version))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(static version => version, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public override string Name => ".NET SDK";

    public override string? Detect()
    {
        KnownToolPaths.EnsureDotNetOnPath();

        if (_requiredVersions.Length == 0)
        {
            return RunAndCapture("dotnet", "--version");
        }

        var installed = RunAndCapture("dotnet", "--list-sdks");
        if (string.IsNullOrWhiteSpace(installed))
        {
            return null;
        }

        foreach (var version in _requiredVersions)
        {
            if (!installed.Contains(version, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
        }

        return string.Join(", ", _requiredVersions);
    }

    public override async Task InstallAsync(CancellationToken cancellationToken)
    {
        var scriptPath = Path.Combine(Path.GetTempPath(), "avbench", "dotnet-install.ps1");
        await DownloadFileAsync(InstallScriptUrl, scriptPath, cancellationToken);

        foreach (var version in _requiredVersions)
        {
            Console.WriteLine($"[setup] Installing .NET SDK {version}...");
            var exitCode = RunProcess(
                "powershell.exe",
                $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -Version {version} -InstallDir \"{KnownToolPaths.DotNetInstallDirectory}\"");

            if (exitCode != 0)
            {
                throw new InvalidOperationException($"dotnet-install.ps1 exited with code {exitCode} while installing SDK {version}.");
            }
        }

        KnownToolPaths.EnsureDotNetOnPath();
    }
}
