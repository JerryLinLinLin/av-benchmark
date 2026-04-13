using System.Net.Http.Headers;
using System.Text.Json;

namespace AvBench.Core.Setup;

public sealed class CmakeInstaller : ToolInstaller
{
    private const string ReleaseApiUrl = "https://api.github.com/repos/Kitware/CMake/releases/latest";

    public override string Name => "CMake";

    public override string? Detect()
    {
        KnownToolPaths.EnsureCmakeOnPath();
        return RunAndCapture("cmake", "--version");
    }

    public override async Task InstallAsync(CancellationToken cancellationToken)
    {
        var downloadUrl = await ResolveLatestMsiUrlAsync(cancellationToken);
        var installerPath = Path.Combine(Path.GetTempPath(), "avbench", "cmake-installer.msi");

        await DownloadFileAsync(downloadUrl, installerPath, cancellationToken);

        var exitCode = RunProcess(
            "msiexec.exe",
            $"/i \"{installerPath}\" /quiet /norestart ADD_CMAKE_TO_PATH=System");

        if (exitCode != 0)
        {
            throw new InvalidOperationException($"CMake installer exited with code {exitCode}.");
        }

        KnownToolPaths.EnsureCmakeOnPath();
    }

    private static async Task<string> ResolveLatestMsiUrlAsync(CancellationToken cancellationToken)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("avbench", "0.2.0"));

        using var response = await client.GetAsync(ReleaseApiUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var release = await JsonSerializer.DeserializeAsync(stream, GitHubReleaseContext.Default.GitHubRelease, cancellationToken)
            ?? throw new InvalidOperationException("Unable to parse the latest CMake release metadata.");

        var asset = release.Assets.FirstOrDefault(static item =>
            item.BrowserDownloadUrl.EndsWith("-windows-x86_64.msi", StringComparison.OrdinalIgnoreCase));

        return asset?.BrowserDownloadUrl
            ?? throw new InvalidOperationException("Unable to locate a Windows x86_64 CMake MSI asset in the latest release.");
    }
}
