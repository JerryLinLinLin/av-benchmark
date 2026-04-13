using System.Security.Cryptography;
using System.Text.Json;
using AvBench.Core.Environment;
using AvBench.Core.Models;
using AvBench.Core.Serialization;

namespace AvBench.Core.Setup;

public sealed class SetupService
{
    public const string SuiteManifestFileName = "suite-manifest.json";

    public async Task<SuiteManifest> ExecuteAsync(string benchDirectory, string? ripgrepRevision, CancellationToken cancellationToken)
    {
        KnownToolPaths.EnsureCommonToolPaths();
        Directory.CreateDirectory(benchDirectory);

        var gitVersion = await new GitInstaller().EnsureInstalledAsync(cancellationToken);
        var rustVersion = await new RustInstaller().EnsureInstalledAsync(cancellationToken);

        var ripgrep = await RepoCloner.CloneRipgrepAsync(benchDirectory, ripgrepRevision, cancellationToken);
        await RepoCloner.CargoFetchAsync(ripgrep.LocalPath, cancellationToken);

        var manifest = new SuiteManifest
        {
            CreatedUtc = DateTime.UtcNow,
            BenchDirectory = benchDirectory,
            RunnerVersion = SystemInfoProvider.GetRunnerVersion(),
            IncrementalTouchPath = RepoCloner.ResolveIncrementalTouchPath(ripgrep.LocalPath),
            Repos = [ripgrep],
            Tools = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["git"] = gitVersion,
                ["rustc"] = rustVersion
            }
        };

        var manifestPath = Path.Combine(benchDirectory, SuiteManifestFileName);
        var json = JsonSerializer.Serialize(manifest, AvBenchJsonContext.Default.SuiteManifest);
        await File.WriteAllTextAsync(manifestPath, json, cancellationToken);
        Console.WriteLine($"[setup] Wrote manifest: {manifestPath}");

        return manifest;
    }

    public static string ComputeManifestSha(string manifestPath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(manifestPath);
        var hash = sha256.ComputeHash(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

