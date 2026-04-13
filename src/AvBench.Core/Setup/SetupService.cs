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
        var roslyn = await RepoCloner.CloneRoslynAsync(benchDirectory, cancellationToken);
        var llvm = await RepoCloner.CloneLlvmAsync(benchDirectory, cancellationToken);
        var files = await RepoCloner.CloneFilesAsync(benchDirectory, cancellationToken);

        var llvmBuildDirectory = Path.Combine(benchDirectory, "llvm-build");
        var requiredVsVersion = RepoCloner.ResolveVisualStudioVersion(roslyn.LocalPath);

        var vsVersion = await new VsBuildToolsInstaller(requiredVsVersion).EnsureInstalledAsync(cancellationToken);
        var cmakeVersion = await new CmakeInstaller().EnsureInstalledAsync(cancellationToken);
        var ninjaVersion = await new NinjaInstaller().EnsureInstalledAsync(cancellationToken);
        var dotnetInstaller = new DotNetSdkInstaller(
        [
            RepoCloner.ResolveDotNetSdkVersion(roslyn.LocalPath),
            RepoCloner.ResolveDotNetSdkVersion(files.LocalPath)
        ]);
        var dotnetSdkVersions = await dotnetInstaller.EnsureInstalledAsync(cancellationToken);

        await RepoCloner.CargoFetchAsync(ripgrep.LocalPath, cancellationToken);
        await RepoCloner.HydrateRoslynAsync(roslyn.LocalPath, cancellationToken);
        await RepoCloner.HydrateLlvmAsync(llvm.LocalPath, llvmBuildDirectory, cancellationToken);
        await RepoCloner.HydrateFilesAsync(files.LocalPath, cancellationToken);

        var manifest = new SuiteManifest
        {
            CreatedUtc = DateTime.UtcNow,
            BenchDirectory = benchDirectory,
            RunnerVersion = SystemInfoProvider.GetRunnerVersion(),
            Repos =
            [
                ripgrep,
                roslyn,
                llvm,
                files
            ],
            Workloads =
            [
                new WorkloadEntry
                {
                    Name = "ripgrep",
                    RepoName = ripgrep.Name,
                    WorkingDirectory = ripgrep.LocalPath,
                    IncrementalTouchPath = RepoCloner.ResolveRipgrepTouchPath(ripgrep.LocalPath)
                },
                new WorkloadEntry
                {
                    Name = "roslyn",
                    RepoName = roslyn.Name,
                    WorkingDirectory = roslyn.LocalPath,
                    IncrementalTouchPath = RepoCloner.ResolveRoslynTouchPath(roslyn.LocalPath)
                },
                new WorkloadEntry
                {
                    Name = "llvm",
                    RepoName = llvm.Name,
                    WorkingDirectory = llvm.LocalPath,
                    BuildDirectory = llvmBuildDirectory,
                    IncrementalTouchPath = RepoCloner.ResolveLlvmTouchPath(llvm.LocalPath)
                },
                new WorkloadEntry
                {
                    Name = "files",
                    RepoName = files.Name,
                    WorkingDirectory = files.LocalPath,
                    IncrementalTouchPath = RepoCloner.ResolveFilesTouchPath(files.LocalPath)
                }
            ],
            Tools = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["git"] = gitVersion,
                ["rustc"] = rustVersion,
                ["visual_studio"] = vsVersion,
                ["cmake"] = cmakeVersion,
                ["ninja"] = ninjaVersion,
                ["dotnet_sdks"] = dotnetSdkVersions
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
