using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text.Json;

namespace AvBench.Core.Setup;

public sealed class NinjaInstaller : ToolInstaller
{
    private const string ReleaseApiUrl = "https://api.github.com/repos/ninja-build/ninja/releases/latest";
    private const string InstallDirectory = @"C:\Tools\ninja";

    public override string Name => "Ninja";

    public override string? Detect()
    {
        KnownToolPaths.EnsureNinjaOnPath();
        return RunAndCapture("ninja", "--version");
    }

    public override async Task InstallAsync(CancellationToken cancellationToken)
    {
        var downloadUrl = await ResolveLatestZipUrlAsync(cancellationToken);
        var zipPath = Path.Combine(Path.GetTempPath(), "avbench", "ninja-win.zip");

        await DownloadFileAsync(downloadUrl, zipPath, cancellationToken);

        Directory.CreateDirectory(InstallDirectory);
        ZipFile.ExtractToDirectory(zipPath, InstallDirectory, overwriteFiles: true);

        KnownToolPaths.EnsureNinjaOnPath();
    }

    private static async Task<string> ResolveLatestZipUrlAsync(CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("avbench", "0.2.0"));

        using var response = await client.GetAsync(ReleaseApiUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var release = await JsonSerializer.DeserializeAsync(stream, GitHubReleaseContext.Default.GitHubRelease, cancellationToken)
            ?? throw new InvalidOperationException("Unable to parse the latest Ninja release metadata.");

        var asset = release.Assets.FirstOrDefault(static item =>
            string.Equals(item.Name, "ninja-win.zip", StringComparison.OrdinalIgnoreCase));

        return asset?.BrowserDownloadUrl
            ?? throw new InvalidOperationException("Unable to locate the ninja-win.zip asset in the latest release.");
    }
}
