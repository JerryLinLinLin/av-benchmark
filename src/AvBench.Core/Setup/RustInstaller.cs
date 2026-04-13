namespace AvBench.Core.Setup;

public sealed class RustInstaller : ToolInstaller
{
    private const string RustupUrl = "https://win.rustup.rs/x86_64";

    public override string Name => "Rust";

    public override string? Detect()
    {
        KnownToolPaths.EnsureCargoOnPath();
        return RunAndCapture("rustc", "--version");
    }

    public override async Task InstallAsync(CancellationToken cancellationToken)
    {
        var installerPath = Path.Combine(Path.GetTempPath(), "avbench", "rustup-init.exe");
        await DownloadFileAsync(RustupUrl, installerPath, cancellationToken);

        var exitCode = RunProcess(installerPath, "-y --default-toolchain stable");
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"rustup-init exited with code {exitCode}.");
        }

        KnownToolPaths.EnsureCargoOnPath();
    }
}

