namespace AvBench.Core.Setup;

public sealed class RustInstaller : ToolInstaller
{
    private const string RustupUrl = "https://win.rustup.rs/x86_64";
    private const string RequiredToolchainVersion = "1.85.0";

    public override string Name => "Rust";

    public override string? Detect()
    {
        KnownToolPaths.EnsureCargoOnPath();
        var version = RunAndCapture("rustc", "--version");
        return version is not null && version.Contains($" {RequiredToolchainVersion} ", StringComparison.OrdinalIgnoreCase)
            ? version
            : null;
    }

    public override async Task InstallAsync(CancellationToken cancellationToken)
    {
        KnownToolPaths.EnsureCargoOnPath();

        if (!string.IsNullOrWhiteSpace(RunAndCapture("rustup", "--version")))
        {
            EnsureRustupSuccess($"toolchain install {RequiredToolchainVersion}");
            EnsureRustupSuccess($"default {RequiredToolchainVersion}");
        }
        else
        {
            var installerPath = Path.Combine(Path.GetTempPath(), "avbench", "rustup-init.exe");
            await DownloadFileAsync(RustupUrl, installerPath, cancellationToken);

            var exitCode = RunProcess(installerPath, $"-y --default-toolchain {RequiredToolchainVersion}");
            if (exitCode != 0)
            {
                throw new InvalidOperationException($"rustup-init exited with code {exitCode}.");
            }
        }

        KnownToolPaths.EnsureCargoOnPath();
    }

    private static void EnsureRustupSuccess(string arguments)
    {
        var exitCode = RunProcess("rustup", arguments);
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"rustup {arguments} exited with code {exitCode}.");
        }
    }
}
