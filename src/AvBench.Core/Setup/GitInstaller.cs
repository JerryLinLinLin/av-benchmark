namespace AvBench.Core.Setup;

public sealed class GitInstaller : ToolInstaller
{
    private const string GitDownloadUrl = "https://github.com/git-for-windows/git/releases/latest/download/Git-64-bit.exe";

    public override string Name => "Git";

    public override string? Detect()
    {
        return RunAndCapture("git", "--version");
    }

    public override async Task InstallAsync(CancellationToken cancellationToken)
    {
        var installerPath = Path.Combine(Path.GetTempPath(), "avbench", "git-installer.exe");
        await DownloadFileAsync(GitDownloadUrl, installerPath, cancellationToken);

        var exitCode = RunProcess(
            installerPath,
            "/VERYSILENT /NORESTART /NOCANCEL /SP-",
            useShellExecute: true);

        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Git installer exited with code {exitCode}.");
        }

        KnownToolPaths.EnsureGitOnPath();
    }
}

