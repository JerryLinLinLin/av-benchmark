using System.Security.Cryptography;
using System.Text.Json;
using AvBench.Core.Environment;
using AvBench.Core.Models;
using AvBench.Core.Serialization;
using System.Runtime.Versioning;

namespace AvBench.Core.Setup;

[SupportedOSPlatform("windows")]
public sealed class SetupService
{
    public const string SuiteManifestFileName = "suite-manifest.json";
    private static readonly Version MinimumVisualStudioVersion = new(17, 0, 0);

    public async Task<SuiteManifest> ExecuteAsync(
        string benchDirectory,
        string? ripgrepRevision,
        IReadOnlyCollection<string> selectedWorkloads,
        CancellationToken cancellationToken)
    {
        KnownToolPaths.EnsureCommonToolPaths();
        Directory.CreateDirectory(benchDirectory);

        RepoEntry? ripgrep = null;
        RepoEntry? roslyn = null;
        RepoEntry? llvm = null;

        var llvmBuildDirectory = Path.Combine(benchDirectory, "llvm-build");
        var repos = new List<RepoEntry>();
        var workloads = new List<WorkloadEntry>();
        var tools = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (BenchmarkWorkloads.Contains(selectedWorkloads, BenchmarkWorkloads.Ripgrep))
        {
            ripgrep = await RepoCloner.CloneRipgrepAsync(benchDirectory, ripgrepRevision, cancellationToken);
            repos.Add(ripgrep);
        }

        if (BenchmarkWorkloads.Contains(selectedWorkloads, BenchmarkWorkloads.Roslyn))
        {
            roslyn = await RepoCloner.CloneRoslynAsync(benchDirectory, cancellationToken);
            repos.Add(roslyn);
        }

        if (BenchmarkWorkloads.Contains(selectedWorkloads, BenchmarkWorkloads.Llvm))
        {
            llvm = await RepoCloner.CloneLlvmAsync(benchDirectory, cancellationToken);
            repos.Add(llvm);
        }

        if (BenchmarkWorkloads.RequiresRust(selectedWorkloads))
        {
            tools["rustc"] = await new RustInstaller().EnsureInstalledAsync(cancellationToken);
        }

        if (BenchmarkWorkloads.RequiresVisualStudio(selectedWorkloads))
        {
            var roslynVersion = roslyn is null ? null : RepoCloner.ResolveVisualStudioVersion(roslyn.LocalPath);
            tools["visual_studio"] = await new VsBuildToolsInstaller(DetermineRequiredVisualStudioVersion(roslynVersion))
                .EnsureInstalledAsync(cancellationToken);

            if (WindowsRestartDetector.IsRestartPending())
            {
                throw SetupRestartRequiredException.PendingVisualStudioFinalize();
            }
        }

        if (BenchmarkWorkloads.RequiresCmake(selectedWorkloads))
        {
            tools["cmake"] = await new CmakeInstaller().EnsureInstalledAsync(cancellationToken);
        }

        if (BenchmarkWorkloads.RequiresNinja(selectedWorkloads))
        {
            tools["ninja"] = await new NinjaInstaller().EnsureInstalledAsync(cancellationToken);
        }

        if (BenchmarkWorkloads.RequiresPython(selectedWorkloads))
        {
            tools["python"] = await new PythonInstaller().EnsureInstalledAsync(cancellationToken);
        }

        if (BenchmarkWorkloads.RequiresDotNetSdk(selectedWorkloads))
        {
            var sdkVersions = new List<string>();
            if (roslyn is not null)
            {
                sdkVersions.Add(RepoCloner.ResolveDotNetSdkVersion(roslyn.LocalPath));
            }

            tools["dotnet_sdks"] = await new DotNetSdkInstaller(
                    sdkVersions.Distinct(StringComparer.OrdinalIgnoreCase).ToArray())
                .EnsureInstalledAsync(cancellationToken);
        }

        if (ripgrep is not null)
        {
            await RepoCloner.CargoFetchAsync(ripgrep.LocalPath, cancellationToken);
            workloads.Add(new WorkloadEntry
            {
                Name = BenchmarkWorkloads.Ripgrep,
                RepoName = ripgrep.Name,
                WorkingDirectory = ripgrep.LocalPath,
                IncrementalTouchPath = RepoCloner.ResolveRipgrepTouchPath(ripgrep.LocalPath)
            });
        }

        if (roslyn is not null)
        {
            await RepoCloner.HydrateRoslynAsync(roslyn.LocalPath, cancellationToken);
            workloads.Add(new WorkloadEntry
            {
                Name = BenchmarkWorkloads.Roslyn,
                RepoName = roslyn.Name,
                WorkingDirectory = roslyn.LocalPath,
                IncrementalTouchPath = RepoCloner.ResolveRoslynTouchPath(roslyn.LocalPath)
            });
        }

        if (llvm is not null)
        {
            await RepoCloner.HydrateLlvmAsync(llvm.LocalPath, llvmBuildDirectory, cancellationToken);
            workloads.Add(new WorkloadEntry
            {
                Name = BenchmarkWorkloads.Llvm,
                RepoName = llvm.Name,
                WorkingDirectory = llvm.LocalPath,
                BuildDirectory = llvmBuildDirectory,
                IncrementalTouchPath = RepoCloner.ResolveLlvmTouchPath(llvm.LocalPath)
            });
        }

        var manifest = new SuiteManifest
        {
            CreatedUtc = DateTime.UtcNow,
            BenchDirectory = benchDirectory,
            RunnerVersion = SystemInfoProvider.GetRunnerVersion(),
            Repos = repos,
            Workloads = workloads,
            Tools = tools
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

    private static string DetermineRequiredVisualStudioVersion(string? roslynVersion)
    {
        if (Version.TryParse(roslynVersion, out var roslynParsed) && roslynParsed > MinimumVisualStudioVersion)
        {
            return roslynParsed.ToString();
        }

        return MinimumVisualStudioVersion.ToString();
    }
}
