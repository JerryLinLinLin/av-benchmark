# Milestone 2 Implementation

## Scope

- Extend `avbench setup` to install Visual Studio/MSBuild prerequisites and .NET SDKs
- Add Roslyn compile scenarios (clean/incremental/noop)
- Build `avbench-compare` — reads results from multiple VMs, produces `compare.csv` and `summary.md`

## Prerequisites from Milestone 1

All M1 components are assumed working:

- Solution structure (`AvBench.Cli`, `AvBench.Core`)
- Job object process-tree runner (`ProcessTreeRunner`, `JobObject`)\n- Data models (`RunResult`, `SuiteManifest`, `ScenarioDefinition`)
- Output writers (`JsonResultWriter`, `CsvResultWriter`)
- CLI framework (`SetupCommand`, `RunCommand`)
- Tool installers (`ToolInstaller` base, `GitInstaller`, `RustInstaller`, `RepoCloner`)

## New Solution Structure

```
av-benchmark/
  src/
    AvBench.sln
    AvBench.Cli/          (existing from M1)
    AvBench.Core/         (extended)
      Setup/
        VsBuildToolsInstaller.cs    ← NEW
        DotNetSdkInstaller.cs        ← NEW
      Scenarios/
        RoslynScenario.cs            ← NEW
    AvBench.Compare/       ← NEW project
      AvBench.Compare.csproj
      Program.cs
      CompareCommand.cs
      CompareEngine.cs
      SummaryRenderer.cs
```

## New Project: `AvBench.Compare`

### `AvBench.Compare.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <AssemblyName>avbench-compare</AssemblyName>
    <RootNamespace>AvBench.Compare</RootNamespace>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="System.CommandLine" Version="2.0.5" />
    <ProjectReference Include="..\AvBench.Core\AvBench.Core.csproj" />
  </ItemGroup>
</Project>
```

Add to solution:

```powershell
cd src
dotnet new console -n AvBench.Compare --framework net8.0
dotnet sln add AvBench.Compare
cd AvBench.Compare
dotnet add reference ..\AvBench.Core
dotnet add package System.CommandLine --version 2.0.5
```

## Tool Installation

### `VsBuildToolsInstaller.cs`

Visual Studio/MSBuild is the heaviest install. Live verification against the current Roslyn repo showed that milestone 2 needs:
- `Microsoft.VisualStudio.Workload.VCTools`
- `Microsoft.VisualStudio.Workload.ManagedDesktopBuildTools`
- `Microsoft.VisualStudio.Workload.UniversalBuildTools`
- `Microsoft.VisualStudio.Component.Windows11SDK.26100`

Detection: `vswhere.exe -latest -products * -requires Microsoft.Component.MSBuild -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64`, plus file-level verification that Windows SDK `10.0.26100.0` headers are present.

Important behavior:
- If Windows already has a pending restart, setup should stop before attempting Visual Studio install.
- If the installer prints a restart-required message or leaves a real pending restart state behind, `avbench setup` should stop and tell the user to restart Windows and rerun setup.
- Ignore the Visual Studio bootstrapper cleanup delete under `C:\ProgramData\Microsoft\VisualStudio\Packages\_bootstrapper\vs_setup_bootstrapper_*.json`; current VS 2026 installs can leave that queued in `PendingFileRenameOperations` even when `vswhere` reports `isRebootRequired=false`.

```csharp
using System.Diagnostics;
using System.Net.Http;

namespace AvBench.Core.Setup;

public sealed class VsBuildToolsInstaller : ToolInstaller
{
    public override string Name => "Visual Studio Build Tools";

    // Latest VS 2022 Build Tools bootstrapper
    private const string BootstrapperUrl =
        "https://aka.ms/vs/17/release/vs_buildtools.exe";

    private static readonly string VswherePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
        @"Microsoft Visual Studio\Installer\vswhere.exe");

    public override string? Detect()
    {
        if (!File.Exists(VswherePath))
            return null;

        var output = RunAndCapture(VswherePath,
            "-products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 " +
            "-property installationVersion -format value");
        return output;
    }

    public override async Task InstallAsync(CancellationToken ct = default)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "vs_buildtools.exe");
        await DownloadFileAsync(BootstrapperUrl, tempPath, ct);

        // --quiet: no UI
        // --wait: block until install completes
        // --norestart: don't reboot
        // --add: workloads
        // --includeRecommended: include recommended components for each workload
        var args = string.Join(" ",
            "--quiet", "--wait", "--norestart",
            "--add Microsoft.VisualStudio.Workload.VCTools",
            "--add Microsoft.VisualStudio.Workload.ManagedDesktopBuildTools",
            "--add Microsoft.VisualStudio.Workload.UniversalBuildTools",
            "--add Microsoft.VisualStudio.Component.Windows11SDK.26100",
            "--includeRecommended");

        var exitCode = RunProcess(tempPath, args);

        // Exit codes: 0 = success, 3010 = success but reboot needed
        if (exitCode != 0 && exitCode != 3010)
            throw new InvalidOperationException(
                $"VS Build Tools installer exited with code {exitCode}");
    }

    /// <summary>
    /// Returns the MSBuild.exe path from the VS installation.
    /// </summary>
    public static string? FindMsBuildPath()
    {
        var vswhere = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
            @"Microsoft Visual Studio\Installer\vswhere.exe");

        if (!File.Exists(vswhere))
            return null;

        var output = RunAndCaptureStatic(vswhere,
            "-latest -requires Microsoft.Component.MSBuild " +
            "-find MSBuild\\**\\Bin\\MSBuild.exe");

        return string.IsNullOrWhiteSpace(output) ? null : output.Split('\n')[0].Trim();
    }

    private static async Task DownloadFileAsync(string url, string dest, CancellationToken ct)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        await using var fs = File.Create(dest);
        await response.Content.CopyToAsync(fs, ct);
    }

    private static int RunProcess(string fileName, string arguments)
    {
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };
        var proc = Process.Start(psi)!;
        proc.WaitForExit();
        return proc.ExitCode;
    }

    private static string? RunAndCapture(string fileName, string arguments)
        => RunAndCaptureStatic(fileName, arguments);

    private static string? RunAndCaptureStatic(string fileName, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo(fileName, arguments)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var proc = Process.Start(psi);
            if (proc is null) return null;
            var output = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit();
            return proc.ExitCode == 0 && !string.IsNullOrWhiteSpace(output) ? output : null;
        }
        catch { return null; }
    }
}
```

### `DotNetSdkInstaller.cs`

Installs .NET SDK. Milestone 2 needs the version pinned by Roslyn's `global.json`.

The installer handles one or more pinned SDK installs. Use the official `dotnet-install.ps1` script for each version.

```csharp
using System.Diagnostics;
using System.Net.Http;

namespace AvBench.Core.Setup;

public sealed class DotNetSdkInstaller : ToolInstaller
{
    public override string Name => ".NET SDK";

    // Versions to install — pin to match each project's global.json
    private static readonly string[] RequiredVersions = ["8.0.404"];

    private const string InstallScriptUrl =
        "https://dot.net/v1/dotnet-install.ps1";

    public override string? Detect()
    {
        var output = RunAndCapture("dotnet", "--list-sdks");
        if (output is null) return null;

        // Check each required version is present
        foreach (var version in RequiredVersions)
        {
            // Match major.minor — not exact patch
            var majorMinor = version[..version.LastIndexOf('.')];
            if (!output.Contains(majorMinor))
                return null; // missing this SDK
        }

        return output.Split('\n').LastOrDefault()?.Trim();
    }

    public override async Task InstallAsync(CancellationToken ct = default)
    {
        var scriptPath = Path.Combine(Path.GetTempPath(), "dotnet-install.ps1");
        await DownloadFileAsync(InstallScriptUrl, scriptPath, ct);

        foreach (var version in RequiredVersions)
        {
            Console.WriteLine($"[setup] Installing .NET SDK {version}...");

            // dotnet-install.ps1 -Version <ver> -InstallDir "C:\Program Files\dotnet"
            var exitCode = RunProcess("powershell.exe",
                $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" " +
                $"-Version {version} " +
                $"-InstallDir \"{GetDotnetInstallDir()}\"");

            if (exitCode != 0)
                throw new InvalidOperationException(
                    $"dotnet-install.ps1 for SDK {version} exited with code {exitCode}");
        }

        // Ensure dotnet is on PATH
        var dotnetDir = GetDotnetInstallDir();
        var currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
        if (!currentPath.Contains(dotnetDir, StringComparison.OrdinalIgnoreCase))
            Environment.SetEnvironmentVariable("PATH", dotnetDir + ";" + currentPath);
    }

    private static string GetDotnetInstallDir()
        => @"C:\Program Files\dotnet";

    private static async Task DownloadFileAsync(string url, string dest, CancellationToken ct)
    {
        using var http = new HttpClient();
        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        await using var fs = File.Create(dest);
        await response.Content.CopyToAsync(fs, ct);
    }

    private static int RunProcess(string fileName, string arguments)
    {
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };
        var proc = Process.Start(psi)!;
        proc.WaitForExit();
        return proc.ExitCode;
    }

    private static string? RunAndCapture(string fileName, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo(fileName, arguments)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var proc = Process.Start(psi);
            if (proc is null) return null;
            var output = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit();
            return proc.ExitCode == 0 ? output : null;
        }
        catch { return null; }
    }
}
```

## Repo Acquisition and Dependency Hydration

Extend `RepoCloner` to acquire benchmark source trees without paying the full `git clone` cost for every workload. The current implementation should:

- Prefer GitHub source archives over full clones
- Resolve an exact commit SHA first, then download the matching archive
- Use the latest release tag archive for ripgrep
- Use the default-branch head archive for Roslyn, because milestone 2 tracks current upstream build layout
- Use a requested ripgrep ref by resolving it to an exact commit SHA and downloading that archive
- Record source metadata in `suite-manifest.json` (`sha`, `source_kind`, `source_reference`, `archive_url`)

Each repo still has specific hydration steps after source acquisition.

### Roslyn

Hydration remains `Restore.cmd`, but the timed benchmark command is not `Build.cmd` anymore. Live verification showed current Roslyn builds reliably with:

```powershell
dotnet build Roslyn.slnx -c Release /m /nr:false
```

This avoids current `Build.cmd`/MSBuild runtime mismatches on newer repos.

## Scenario Definitions

### `RoslynScenario.cs`

```csharp
namespace AvBench.Core.Scenarios;

public static class RoslynScenario
{
    public static List<ScenarioDefinition> Create(string repoDir)
    {
        var solutionPath = Path.Combine(repoDir, "Roslyn.slnx");

        return
        [
            new ScenarioDefinition
            {
                Id = "roslyn-clean-build",
                FileName = "dotnet",
                Arguments = $"build \"{solutionPath}\" -c Release /m /nr:false",
                WorkingDirectory = repoDir,
                PreActions =
                [
                    // Clean: delete artifacts directory
                    $"if exist \"{Path.Combine(repoDir, "artifacts")}\" rmdir /s /q \"{Path.Combine(repoDir, "artifacts")}\""
                ]
            },
            new ScenarioDefinition
            {
                Id = "roslyn-incremental-build",
                FileName = "dotnet",
                Arguments = $"build \"{solutionPath}\" -c Release /m /nr:false",
                WorkingDirectory = repoDir,
                PreActions = [TouchFileCommand(repoDir)]
            },
            new ScenarioDefinition
            {
                Id = "roslyn-noop-build",
                FileName = "dotnet",
                Arguments = $"build \"{solutionPath}\" -c Release /m /nr:false",
                WorkingDirectory = repoDir,
                PreActions = []
            }
        ];
    }

    private static string TouchFileCommand(string repoDir)
    {
        // Touch a stable C# file to trigger incremental rebuild
        // CSharpSyntaxGenerator is small, stable, and doesn't break the build
        var target = Path.Combine(repoDir,
            "src", "Compilers", "CSharp", "Portable",
            "CSharpResources.Designer.cs");
        return $"copy /b \"{target}\"+,, \"{target}\"";
    }
}
```

## `avbench-compare` Implementation

### `Program.cs`

```csharp
using System.CommandLine;
using AvBench.Compare;

var rootCommand = new RootCommand("AV benchmark cross-configuration comparison tool");

var baselineOption = new Option<DirectoryInfo>("--baseline")
{
    Description = "Path to baseline results directory",
    Required = true
};
var inputOption = new Option<DirectoryInfo[]>("--input")
{
    Description = "Paths to AV configuration results directories",
    Required = true,
    AllowMultipleArgumentsPerToken = true
};
var outputOption = new Option<DirectoryInfo>("--output")
{
    Description = "Output directory for comparison results",
    DefaultValueFactory = _ => new DirectoryInfo("comparison")
};

rootCommand.Options.Add(baselineOption);
rootCommand.Options.Add(inputOption);
rootCommand.Options.Add(outputOption);

rootCommand.SetAction(parseResult =>
{
    var baseline = parseResult.GetValue(baselineOption)!;
    var inputs = parseResult.GetValue(inputOption)!;
    var output = parseResult.GetValue(outputOption)!;

    return CompareCommand.Execute(baseline, inputs, output);
});

return rootCommand.Parse(args).Invoke();
```

### `CompareCommand.cs`

```csharp
using System.Text.Json;
using AvBench.Core.Models;
using AvBench.Core.Output;

namespace AvBench.Compare;

public static class CompareCommand
{
    public static int Execute(DirectoryInfo baseline, DirectoryInfo[] inputs, DirectoryInfo output)
    {
        Console.WriteLine($"[compare] Baseline: {baseline.FullName}");

        // 1. Load all run.json files from baseline
        var baselineRuns = LoadRuns(baseline);
        Console.WriteLine($"[compare] Loaded {baselineRuns.Count} baseline runs");

        // 2. Load all run.json files from each input
        var allNamedRuns = new Dictionary<string, List<RunResult>>();
        foreach (var input in inputs)
        {
            var runs = LoadRuns(input);
            if (runs.Count > 0)
            {
                var avName = runs[0].AvName;
                allNamedRuns[avName] = runs;
                Console.WriteLine($"[compare] Loaded {runs.Count} runs for '{avName}'");
            }
        }

        // 3. Compute comparisons
        var comparisons = CompareEngine.Compare(baselineRuns, allNamedRuns);

        // 4. Write output
        output.Create();
        CompareCsvWriter.Write(comparisons, Path.Combine(output.FullName, "compare.csv"));
        SummaryRenderer.Write(comparisons, Path.Combine(output.FullName, "summary.md"));

        Console.WriteLine($"[compare] Results written to {output.FullName}");
        return 0;
    }

    private static List<RunResult> LoadRuns(DirectoryInfo dir)
    {
        var results = new List<RunResult>();
        // Recursively find all run.json files
        foreach (var file in dir.EnumerateFiles("run.json", SearchOption.AllDirectories))
        {
            var json = File.ReadAllText(file.FullName);
            var result = JsonSerializer.Deserialize(json, RunResultContext.Default.RunResult);
            if (result is not null)
                results.Add(result);
        }
        return results;
    }
}
```

### `CompareEngine.cs`

```csharp
namespace AvBench.Compare;

public sealed class ComparisonRow
{
    public string ScenarioId { get; init; } = "";
    public string AvName { get; init; } = "";
    public string BaselineName { get; init; } = "";
    public int Sessions { get; init; }
    public double MeanWallMs { get; init; }
    public double MedianWallMs { get; init; }
    public double MeanCpuMs { get; init; }
    public double KernelCpuPct { get; init; }
    public double BaselineKernelCpuPct { get; init; }
    public double KernelCpuSlowdownPct { get; init; }
    public double PeakMemoryMb { get; init; }
    public double SlowdownPct { get; init; }
    public double CvPct { get; init; }
    public string Status { get; init; } = "ok";
}

public static class CompareEngine
{
    private const double NoisyThreshold = 10.0; // CV > 10% = noisy

    public static List<ComparisonRow> Compare(
        List<RunResult> baselineRuns,
        Dictionary<string, List<RunResult>> namedRuns)
    {
        var rows = new List<ComparisonRow>();

        // Group baseline runs by scenario
        var baselineByScenario = baselineRuns
            .Where(r => r.ExitCode == 0)
            .GroupBy(r => r.ScenarioId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var baselineName = baselineRuns.FirstOrDefault()?.AvName ?? "baseline-os";

        foreach (var (avName, runs) in namedRuns)
        {
            var byScenario = runs
                .Where(r => r.ExitCode == 0)
                .GroupBy(r => r.ScenarioId);

            foreach (var group in byScenario)
            {
                var scenarioId = group.Key;
                var scenarioRuns = group.ToList();
                var wallTimes = scenarioRuns.Select(r => (double)r.WallMs).ToList();
                var cpuTimes = scenarioRuns.Select(r => (double)(r.UserCpuMs + r.KernelCpuMs)).ToList();

                var meanWall = wallTimes.Average();
                var medianWall = Median(wallTimes);
                var meanCpu = cpuTimes.Average();
                var peakMem = scenarioRuns.Max(r => r.PeakJobMemoryMb);
                var cv = wallTimes.Count > 1
                    ? StdDev(wallTimes) / meanWall * 100
                    : 0;

                // Kernel/user CPU ratio — AV minifilter overhead lands in kernel mode
                var kernelTimes = scenarioRuns.Select(r => (double)r.KernelCpuMs).ToList();
                var meanKernel = kernelTimes.Average();
                var kernelCpuPct = meanCpu > 0 ? meanKernel / meanCpu * 100 : 0;

                double baselineKernelCpuPct = 0;
                double slowdown = 0;
                if (baselineByScenario.TryGetValue(scenarioId, out var baseRuns))
                {
                    var baselineMean = baseRuns.Average(r => (double)r.WallMs);
                    slowdown = baselineMean > 0
                        ? (meanWall - baselineMean) / baselineMean * 100
                        : 0;

                    var baseKernelMs = baseRuns.Average(r => (double)r.KernelCpuMs);
                    var baseCpuMs = baseRuns.Average(r => (double)(r.UserCpuMs + r.KernelCpuMs));
                    baselineKernelCpuPct = baseCpuMs > 0 ? baseKernelMs / baseCpuMs * 100 : 0;
                }

                var status = scenarioRuns.Any(r => r.ExitCode != 0) ? "failed"
                    : cv > NoisyThreshold ? "noisy"
                    : "ok";

                rows.Add(new ComparisonRow
                {
                    ScenarioId = scenarioId,
                    AvName = avName,
                    BaselineName = baselineName,
                    Sessions = scenarioRuns.Count,
                    MeanWallMs = Math.Round(meanWall, 1),
                    MedianWallMs = Math.Round(medianWall, 1),
                    MeanCpuMs = Math.Round(meanCpu, 1),
                    KernelCpuPct = Math.Round(kernelCpuPct, 1),
                    BaselineKernelCpuPct = Math.Round(baselineKernelCpuPct, 1),
                    KernelCpuSlowdownPct = Math.Round(kernelCpuPct - baselineKernelCpuPct, 1),
                    PeakMemoryMb = peakMem,
                    SlowdownPct = Math.Round(slowdown, 1),
                    CvPct = Math.Round(cv, 1),
                    Status = status
                });
            }
        }

        return rows.OrderBy(r => r.ScenarioId).ThenBy(r => r.AvName).ToList();
    }

    private static double Median(List<double> values)
    {
        var sorted = values.OrderBy(v => v).ToList();
        int n = sorted.Count;
        if (n == 0) return 0;
        return n % 2 == 0
            ? (sorted[n / 2 - 1] + sorted[n / 2]) / 2.0
            : sorted[n / 2];
    }

    private static double StdDev(List<double> values)
    {
        var mean = values.Average();
        var sumSqDiff = values.Sum(v => (v - mean) * (v - mean));
        return Math.Sqrt(sumSqDiff / (values.Count - 1));
    }
}
```

### `CompareCsvWriter.cs`

```csharp
using System.Globalization;
using System.Text;

namespace AvBench.Compare;

public static class CompareCsvWriter
{
    private static readonly string[] Headers =
    [
        "scenario_id", "av_name", "baseline_name", "sessions",
        "mean_wall_ms", "median_wall_ms", "mean_cpu_ms",
        "kernel_cpu_pct", "baseline_kernel_cpu_pct", "kernel_cpu_slowdown_pct",
        "peak_memory_mb", "slowdown_pct", "cv_pct", "status"
    ];

    public static void Write(IReadOnlyList<ComparisonRow> rows, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", Headers));

        foreach (var r in rows)
        {
            sb.AppendLine(string.Join(",",
                r.ScenarioId,
                r.AvName,
                r.BaselineName,
                r.Sessions.ToString(CultureInfo.InvariantCulture),
                r.MeanWallMs.ToString("F1", CultureInfo.InvariantCulture),
                r.MedianWallMs.ToString("F1", CultureInfo.InvariantCulture),
                r.MeanCpuMs.ToString("F1", CultureInfo.InvariantCulture),
                r.KernelCpuPct.ToString("F1", CultureInfo.InvariantCulture),
                r.BaselineKernelCpuPct.ToString("F1", CultureInfo.InvariantCulture),
                r.KernelCpuSlowdownPct.ToString("F1", CultureInfo.InvariantCulture),
                r.PeakMemoryMb.ToString(CultureInfo.InvariantCulture),
                r.SlowdownPct.ToString("F1", CultureInfo.InvariantCulture),
                r.CvPct.ToString("F1", CultureInfo.InvariantCulture),
                r.Status
            ));
        }

        File.WriteAllText(path, sb.ToString());
    }
}
```

### `SummaryRenderer.cs`

```csharp
using System.Globalization;
using System.Text;

namespace AvBench.Compare;

public static class SummaryRenderer
{
    public static void Write(IReadOnlyList<ComparisonRow> rows, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# AV Benchmark Comparison Report");
        sb.AppendLine();
        sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();

        var byName = rows.GroupBy(r => r.AvName);

        foreach (var nameGroup in byName)
        {
            sb.AppendLine($"## {nameGroup.Key} vs {nameGroup.First().BaselineName}");
            sb.AppendLine();
            sb.AppendLine("| Scenario | Mean Wall (ms) | Slowdown | Kernel CPU % | Baseline Kernel % | Kernel Shift | CV% | Status |");
            sb.AppendLine("|---|---:|---:|---:|---:|---:|---:|---|");

            foreach (var row in nameGroup.OrderBy(r => r.ScenarioId))
            {
                var slowdownStr = row.SlowdownPct >= 0
                    ? $"+{row.SlowdownPct:F1}%"
                    : $"{row.SlowdownPct:F1}%";
                var kernelShift = row.KernelCpuSlowdownPct >= 0
                    ? $"+{row.KernelCpuSlowdownPct:F1}pp"
                    : $"{row.KernelCpuSlowdownPct:F1}pp";
                sb.AppendLine($"| {row.ScenarioId} | {row.MeanWallMs:F0} | {slowdownStr} | {row.KernelCpuPct:F1}% | {row.BaselineKernelCpuPct:F1}% | {kernelShift} | {row.CvPct:F1}% | {row.Status} |");
            }

            sb.AppendLine();

            // Summary insights
            var worst = nameGroup.Where(r => r.Status == "ok")
                .OrderByDescending(r => r.SlowdownPct).FirstOrDefault();
            if (worst is not null)
                sb.AppendLine($"**Highest slowdown**: {worst.ScenarioId} at +{worst.SlowdownPct:F1}%");

            var worstKernel = nameGroup.Where(r => r.Status == "ok")
                .OrderByDescending(r => r.KernelCpuSlowdownPct).FirstOrDefault();
            if (worstKernel is not null && worstKernel.KernelCpuSlowdownPct > 0)
                sb.AppendLine($"**Largest kernel CPU shift**: {worstKernel.ScenarioId} at +{worstKernel.KernelCpuSlowdownPct:F1}pp ({worstKernel.BaselineKernelCpuPct:F1}% → {worstKernel.KernelCpuPct:F1}%)");

            var noisy = nameGroup.Where(r => r.Status == "noisy").ToList();
            if (noisy.Count > 0)
                sb.AppendLine($"**Noisy runs** (CV > 10%): {string.Join(", ", noisy.Select(r => r.ScenarioId))}");

            var failed = nameGroup.Where(r => r.Status == "failed").ToList();
            if (failed.Count > 0)
                sb.AppendLine($"**Failed**: {string.Join(", ", failed.Select(r => r.ScenarioId))}");

            sb.AppendLine();
        }

        File.WriteAllText(path, sb.ToString());
    }
}
```

## Extending `SetupCommand.cs`

The M1 setup command is extended to install all M2 tools and fetch all benchmark source trees:

```csharp
command.SetAction(async parseResult =>
{
    var benchDir = parseResult.GetValue(benchDirOption)!;

    // M1 tools
    await new GitInstaller().EnsureInstalledAsync();
    await new RustInstaller().EnsureInstalledAsync();

    // M2 tools
    await new VsBuildToolsInstaller().EnsureInstalledAsync();
    await new DotNetSdkInstaller().EnsureInstalledAsync();

    // M1 repos
    var rgDir = Path.Combine(benchDir.FullName, "ripgrep");
    await RepoCloner.CloneRipgrepAsync(benchDir.FullName, ripgrepRef, CancellationToken.None);
    RepoCloner.CargoFetch(rgDir);

    // M2 repos
    var roslynDir = Path.Combine(benchDir.FullName, "roslyn");
    await RepoCloner.CloneRoslynAsync(benchDir.FullName, CancellationToken.None);
    RepoCloner.HydrateRoslyn(roslynDir);

    // Write suite-manifest.json
    // ...

    return 0;
});
```

## Extending `RunCommand.cs`

Register all new scenarios:

```csharp
// Build scenario list
var scenarios = new List<ScenarioDefinition>();

// M1 scenarios
scenarios.AddRange(RipgrepScenario.Create(rgDir));

// M2 scenarios
scenarios.AddRange(RoslynScenario.Create(roslynDir));

// API microbench (M1)
// file-create-delete handled separately...

// Execute all scenarios
var runner = new ScenarioRunner(profile, outputRoot, runnerVersion);
var allResults = new List<RunResult>();
foreach (var scenario in scenarios)
{
    var results = runner.Execute(scenarios);
    allResults.AddRange(results);
}
```

## Implementation Steps (ordered)

### Step 1: Create `AvBench.Compare` project

```powershell
cd c:\projects\av-benchmark\src
dotnet new console -n AvBench.Compare --framework net8.0
dotnet sln add AvBench.Compare
cd AvBench.Compare
dotnet add reference ..\AvBench.Core
dotnet add package System.CommandLine --version 2.0.5
```

### Step 2: Build new tool installers

Create `VsBuildToolsInstaller.cs`, `DotNetSdkInstaller.cs` in `AvBench.Core/Setup/`.

Test each on a clean VM:
1. `VsBuildToolsInstaller.EnsureInstalledAsync()` → `vswhere` detects install or setup exits with a restart-required message
2. `DotNetSdkInstaller.EnsureInstalledAsync()` → `dotnet --list-sdks` shows both versions

Note: VS Build Tools install takes 10-30 minutes. Plan accordingly.

### Step 3: Add MSBuild path helper

Add `VsBuildToolsInstaller.FindMsBuildPath()` — uses vswhere to locate MSBuild.exe for installer verification.

### Step 4: Add repo hydration methods

Extend `RepoCloner` with `HydrateRoslyn()`.

Test:
- Roslyn: `Restore.cmd` completes without errors

### Step 5: Build scenario definitions

Create `RoslynScenario.cs`.

Test each scenario standalone:
- Roslyn clean build via `dotnet build Roslyn.slnx -c Release /m /nr:false` produces output in `artifacts/`

### Step 6: Wire up extended setup/run commands

Update `SetupCommand.cs` to install M2 tools and fetch/hydrate all benchmark source trees.
Update `RunCommand.cs` to register all M2 scenarios.

### Step 7: Build `CompareEngine`

Create `CompareEngine.cs` with statistics (mean, median, stdev, CV, slowdown).

Test with synthetic `RunResult` data:
- Verify slowdown calculation against known values
- Verify CV > 10% flags as `noisy`
- Verify failed exit codes produce `failed` status

### Step 8: Build compare output writers

Create `CompareCsvWriter.cs` and `SummaryRenderer.cs`.

Test by feeding `CompareEngine` results → verify CSV has correct columns and markdown renders properly.

### Step 9: Wire up `avbench-compare` CLI

Create `Program.cs` and `CompareCommand.cs` for the Compare project.

### Step 10: End-to-end test

All testing happens inside the same VM. Run `avbench` twice with different names (e.g., toggle Defender real-time protection between runs), then run `avbench-compare` locally against both result directories.

```powershell
# Run 1: with current AV (e.g., defender-default)
avbench setup --bench-dir C:\bench
avbench run --name defender-default --bench-dir C:\bench --output C:\results\defender-default

# Run 2: disable real-time protection, run again as baseline
# (manually toggle Defender off)
avbench run --name baseline-os --bench-dir C:\bench --output C:\results\baseline-os

# Compare both runs locally
avbench-compare --baseline C:\results\baseline-os --input C:\results\defender-default --output C:\results\comparison
```

If only one AV configuration is available during development, test `avbench-compare` with synthetic data: duplicate a results directory, rename the `av_name` in the copied `run.json` files, and run compare against both. This validates the comparison pipeline without needing a second AV configuration.

Expected output:
```
comparison/
  compare.csv    (rows for all scenarios × profiles)
  summary.md     (markdown report with slowdown table)
```

## Key Risks and Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| VS Build Tools install takes 30+ minutes | Setup is very slow | Install once per VM snapshot. Document expected time. |
| Visual Studio install may require reboot | Setup cannot finish in one pass | Detect real pending restart state, tell the user to restart Windows, and rerun `avbench setup`. Ignore the Visual Studio bootstrapper cleanup JSON delete that can remain queued after a successful install. |
| MSBuild path varies by VS version | Scenario fails to find MSBuild | Use vswhere with `-find MSBuild\**\Bin\MSBuild.exe` to discover dynamically. |
| Comparison across VMs with different hardware | Invalid slowdown numbers | `compare.csv` includes `machine` info from `run.json`. Document: all VMs must use identical hardware specs. |

## Testing Strategy

Manual verification on test VMs:

1. `avbench setup` on clean VM installs all tools (VS Build Tools, .NET SDK, Git, Rust)
2. All benchmark source trees fetched and hydrated (Roslyn Restore.cmd)
3. Each scenario produces valid `run.json` with non-zero metrics
4. Roslyn clean build produces compiler output in `artifacts/`
5. `avbench-compare` produces `compare.csv` with correct columns and plausible slowdown values
6. `summary.md` renders correctly and identifies highest-slowdown scenarios
