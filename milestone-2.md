# Milestone 2 Implementation

## Scope

- Extend `avbench setup` to install Visual Studio/MSBuild prerequisites, CMake, Ninja, Python, and .NET SDKs
- Add Roslyn compile scenarios (clean/incremental/noop)
- Add LLVM compile scenarios (configure, clean/incremental/noop)
- Add Files (WinUI 3) compile scenarios (clean/incremental/noop)
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
        CmakeInstaller.cs           ← NEW
        NinjaInstaller.cs            ← NEW
        DotNetSdkInstaller.cs        ← NEW
      Scenarios/
        RoslynScenario.cs            ← NEW
        LlvmScenario.cs             ← NEW
        FilesScenario.cs             ← NEW
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

Visual Studio/MSBuild is the heaviest install. Live verification against the current Roslyn and Files repos showed that milestone 2 needs:
- `Microsoft.VisualStudio.Workload.VCTools`
- `Microsoft.VisualStudio.Workload.ManagedDesktopBuildTools`
- `Microsoft.VisualStudio.Workload.UniversalBuildTools`
- `Microsoft.VisualStudio.Component.Windows11SDK.26100`
- `Microsoft.VisualStudio.ComponentGroup.WindowsAppSDK.Cs`
- `Microsoft.VisualStudio.Component.VC.ATL`

Detection: `vswhere.exe -latest -products * -requires Microsoft.Component.MSBuild -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64`.

Important behavior:
- If Windows already has a pending restart, setup should stop before attempting Visual Studio install.
- If the installer prints a restart-required message or leaves pending restart state behind, `avbench setup` should stop and tell the user to restart Windows and rerun setup.

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
            "--add Microsoft.VisualStudio.ComponentGroup.WindowsAppSDK.Cs",
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

### `CmakeInstaller.cs`

CMake ships with VS Build Tools via the "C++ CMake tools" component, but we can also install standalone for a guaranteed version.

```csharp
using System.Diagnostics;
using System.Net.Http;

namespace AvBench.Core.Setup;

public sealed class CmakeInstaller : ToolInstaller
{
    public override string Name => "CMake";

    // CMake MSI installer URL — pin a specific version
    private const string DefaultUrl =
        "https://github.com/Kitware/CMake/releases/download/v3.31.4/cmake-3.31.4-windows-x86_64.msi";

    public override string? Detect()
    {
        return RunAndCapture("cmake", "--version");
    }

    public override async Task InstallAsync(CancellationToken ct = default)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "cmake-installer.msi");
        await DownloadFileAsync(DefaultUrl, tempPath, ct);

        // MSI silent install with PATH update
        var exitCode = RunProcess("msiexec.exe",
            $"/i \"{tempPath}\" /quiet /norestart ADD_CMAKE_TO_PATH=System");

        if (exitCode != 0)
            throw new InvalidOperationException($"CMake MSI install exited with code {exitCode}");

        // Add to PATH for current process
        var cmakePath = @"C:\Program Files\CMake\bin";
        if (Directory.Exists(cmakePath))
            Environment.SetEnvironmentVariable("PATH",
                cmakePath + ";" + Environment.GetEnvironmentVariable("PATH"));
    }

    private static async Task DownloadFileAsync(string url, string dest, CancellationToken ct)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
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

### `NinjaInstaller.cs`

Ninja is a single-binary download — no installer needed.

```csharp
using System.IO.Compression;
using System.Net.Http;

namespace AvBench.Core.Setup;

public sealed class NinjaInstaller : ToolInstaller
{
    public override string Name => "Ninja";

    private const string DefaultUrl =
        "https://github.com/ninja-build/ninja/releases/download/v1.12.1/ninja-win.zip";

    // Install to a known location on PATH
    private static readonly string InstallDir = @"C:\Tools\ninja";

    public override string? Detect()
    {
        return RunAndCapture("ninja", "--version");
    }

    public override async Task InstallAsync(CancellationToken ct = default)
    {
        var tempZip = Path.Combine(Path.GetTempPath(), "ninja-win.zip");

        using var http = new HttpClient();
        using var response = await http.GetAsync(DefaultUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        await using var fs = File.Create(tempZip);
        await response.Content.CopyToAsync(fs, ct);
        fs.Close();

        Directory.CreateDirectory(InstallDir);
        ZipFile.ExtractToDirectory(tempZip, InstallDir, overwriteFiles: true);

        // Add to PATH for current process
        Environment.SetEnvironmentVariable("PATH",
            InstallDir + ";" + Environment.GetEnvironmentVariable("PATH"));
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

### `DotNetSdkInstaller.cs`

Installs .NET SDK. We need two SDK versions:
- .NET 8 SDK (for Roslyn, matching its `global.json`)
- .NET 10 SDK 10.0.102 (for Files, matching its `global.json`)

The installer handles side-by-side SDK installs. Use the official `dotnet-install.ps1` script for each version.

```csharp
using System.Diagnostics;
using System.Net.Http;

namespace AvBench.Core.Setup;

public sealed class DotNetSdkInstaller : ToolInstaller
{
    public override string Name => ".NET SDK";

    // Versions to install — pin to match each project's global.json
    private static readonly string[] RequiredVersions = ["8.0.404", "10.0.102"];

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

## Repo Clone and Dependency Hydration

Extend `RepoCloner` to support all three new repos. Each repo has specific hydration steps.

### Roslyn

Hydration remains `Restore.cmd`, but the timed benchmark command is not `Build.cmd` anymore. Live verification showed current Roslyn builds reliably with:

```powershell
dotnet build Roslyn.slnx -c Release /m /nr:false
```

This avoids current `Build.cmd`/MSBuild runtime mismatches on newer repos.

### LLVM

CMake configure is untimed setup. It generates Ninja build files and currently requires Python to be available on PATH. Run configure inside the VS developer shell.

```csharp
public static void HydrateLlvm(string repoDir, string buildDir)
{
    Directory.CreateDirectory(buildDir);
    Console.WriteLine($"[setup] CMake configure LLVM → {buildDir}");

    var args = string.Join(" ",
        $"-S \"{Path.Combine(repoDir, "llvm", "llvm")}\"",
        $"-B \"{buildDir}\"",
        "-G Ninja",
        "-DLLVM_ENABLE_PROJECTS=clang",
        "-DLLVM_TARGETS_TO_BUILD=X86",
        "-DCMAKE_BUILD_TYPE=Release");

    var psi = new ProcessStartInfo("cmake", args)
    {
        UseShellExecute = false
    };
    var proc = Process.Start(psi)!;
    proc.WaitForExit();
    if (proc.ExitCode != 0)
        throw new InvalidOperationException($"CMake configure failed: exit {proc.ExitCode}");
}
```

### Files

The current Files build guide requires Visual Studio 2022 17.13+ with .NET 10.0.102, Windows 11 SDK 10.0.26100.0, MSVC v145, and C++ ATL, plus Windows App SDK 1.8. For `avbench setup` we automate the equivalent MSBuild/Build Tools prerequisites.

MSBuild restore is untimed setup and must include `RestorePackagesConfig=true`.

```csharp
public static void HydrateFiles(string repoDir)
{
    var msbuild = VsBuildToolsInstaller.FindMsBuildPath()
        ?? throw new InvalidOperationException("MSBuild not found — install VS Build Tools first");

    Console.WriteLine($"[setup] MSBuild restore in {repoDir}");
    var psi = new ProcessStartInfo(msbuild,
        $"Files.slnx /t:Restore /p:Platform=x64 /p:Configuration=Release /p:RestorePackagesConfig=true /nr:false")
    {
        WorkingDirectory = repoDir,
        UseShellExecute = false
    };
    var proc = Process.Start(psi)!;
    proc.WaitForExit();
    if (proc.ExitCode != 0)
        throw new InvalidOperationException($"MSBuild restore failed: exit {proc.ExitCode}");
}
```

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
                FileName = "cmd.exe",
                Arguments = $"/c \"{buildCmd}\"",
                WorkingDirectory = repoDir,
                PreActions = [TouchFileCommand(repoDir)]
            },
            new ScenarioDefinition
            {
                Id = "roslyn-noop-build",
                FileName = "cmd.exe",
                Arguments = $"/c \"{buildCmd}\"",
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

### `LlvmScenario.cs`

```csharp
namespace AvBench.Core.Scenarios;

public static class LlvmScenario
{
    public static List<ScenarioDefinition> Create(string repoDir, string buildDir)
    {
        return
        [
            new ScenarioDefinition
            {
                Id = "llvm-clean-build",
                FileName = "ninja",
                Arguments = $"-C \"{buildDir}\"",
                WorkingDirectory = buildDir,
                PreActions =
                [
                    // Clean: ninja clean removes build artifacts but keeps build.ninja
                    $"ninja -C \"{buildDir}\" clean"
                ]
            },
            new ScenarioDefinition
            {
                Id = "llvm-incremental-build",
                FileName = "ninja",
                Arguments = $"-C \"{buildDir}\"",
                WorkingDirectory = buildDir,
                PreActions = [TouchFileCommand(repoDir)]
            },
            new ScenarioDefinition
            {
                Id = "llvm-noop-build",
                FileName = "ninja",
                Arguments = $"-C \"{buildDir}\"",
                WorkingDirectory = buildDir,
                PreActions = []
            }
        ];
    }

    private static string TouchFileCommand(string repoDir)
    {
        // Touch a Clang source file to trigger incremental rebuild
        var target = Path.Combine(repoDir,
            "llvm", "clang", "lib", "Basic", "Version.cpp");
        return $"copy /b \"{target}\"+,, \"{target}\"";
    }
}
```

### `FilesScenario.cs`

```csharp
namespace AvBench.Core.Scenarios;

public static class FilesScenario
{
    public static List<ScenarioDefinition> Create(string repoDir)
    {
        var msbuild = VsBuildToolsInstaller.FindMsBuildPath()
            ?? throw new InvalidOperationException("MSBuild not found");

        return
        [
            new ScenarioDefinition
            {
                Id = "files-clean-build",
                FileName = msbuild,
                Arguments = "Files.slnx /t:Build /p:Configuration=Release /p:Platform=x64 /m",
                WorkingDirectory = repoDir,
                PreActions =
                [
                    // Clean: delete bin/obj/AppPackages across all projects
                    $"for /d /r \"{Path.Combine(repoDir, "src")}\" %d in (bin obj) do @if exist \"%d\" rmdir /s /q \"%d\"",
                    $"if exist \"{Path.Combine(repoDir, "tests")}\" for /d /r \"{Path.Combine(repoDir, "tests")}\" %d in (bin obj) do @if exist \"%d\" rmdir /s /q \"%d\""
                ]
            },
            new ScenarioDefinition
            {
                Id = "files-incremental-build",
                FileName = msbuild,
                Arguments = "Files.slnx /t:Build /p:Configuration=Release /p:Platform=x64 /m",
                WorkingDirectory = repoDir,
                PreActions = [TouchFileCommand(repoDir)]
            },
            new ScenarioDefinition
            {
                Id = "files-noop-build",
                FileName = msbuild,
                Arguments = "Files.slnx /t:Build /p:Configuration=Release /p:Platform=x64 /m",
                WorkingDirectory = repoDir,
                PreActions = []
            }
        ];
    }

    private static string TouchFileCommand(string repoDir)
    {
        // Touch a ViewModel file in Files.App to trigger incremental rebuild
        // This exercises XAML compilation + C# compilation
        var target = Path.Combine(repoDir,
            "src", "Files.App", "App.xaml.cs");
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
    public int Repetitions { get; init; }
    public double MeanWallMs { get; init; }
    public double MedianWallMs { get; init; }
    public double MeanCpuMs { get; init; }
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

                double slowdown = 0;
                if (baselineByScenario.TryGetValue(scenarioId, out var baseRuns))
                {
                    var baselineMean = baseRuns.Average(r => (double)r.WallMs);
                    slowdown = baselineMean > 0
                        ? (meanWall - baselineMean) / baselineMean * 100
                        : 0;
                }

                var status = scenarioRuns.Any(r => r.ExitCode != 0) ? "failed"
                    : cv > NoisyThreshold ? "noisy"
                    : "ok";

                rows.Add(new ComparisonRow
                {
                    ScenarioId = scenarioId,
                    AvName = avName,
                    BaselineName = baselineName,
                    Repetitions = scenarioRuns.Count,
                    MeanWallMs = Math.Round(meanWall, 1),
                    MedianWallMs = Math.Round(medianWall, 1),
                    MeanCpuMs = Math.Round(meanCpu, 1),
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
        "scenario_id", "av_name", "baseline_name", "repetitions",
        "mean_wall_ms", "median_wall_ms", "mean_cpu_ms", "peak_memory_mb",
        "slowdown_pct", "cv_pct", "status"
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
                r.Repetitions.ToString(CultureInfo.InvariantCulture),
                r.MeanWallMs.ToString("F1", CultureInfo.InvariantCulture),
                r.MedianWallMs.ToString("F1", CultureInfo.InvariantCulture),
                r.MeanCpuMs.ToString("F1", CultureInfo.InvariantCulture),
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
            sb.AppendLine("| Scenario | Mean Wall (ms) | Slowdown | CV% | Status |");
            sb.AppendLine("|---|---:|---:|---:|---|");

            foreach (var row in nameGroup.OrderBy(r => r.ScenarioId))
            {
                var slowdownStr = row.SlowdownPct >= 0
                    ? $"+{row.SlowdownPct:F1}%"
                    : $"{row.SlowdownPct:F1}%";
                sb.AppendLine($"| {row.ScenarioId} | {row.MeanWallMs:F0} | {slowdownStr} | {row.CvPct:F1}% | {row.Status} |");
            }

            sb.AppendLine();

            // Summary insights
            var worst = nameGroup.Where(r => r.Status == "ok")
                .OrderByDescending(r => r.SlowdownPct).FirstOrDefault();
            if (worst is not null)
                sb.AppendLine($"**Highest slowdown**: {worst.ScenarioId} at +{worst.SlowdownPct:F1}%");

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

The M1 setup command is extended to install all M2 tools and clone all repos:

```csharp
command.SetAction(async parseResult =>
{
    var benchDir = parseResult.GetValue(benchDirOption)!;

    // M1 tools
    await new GitInstaller().EnsureInstalledAsync();
    await new RustInstaller().EnsureInstalledAsync();

    // M2 tools
    await new VsBuildToolsInstaller().EnsureInstalledAsync();
    await new CmakeInstaller().EnsureInstalledAsync();
    await new NinjaInstaller().EnsureInstalledAsync();
    await new DotNetSdkInstaller().EnsureInstalledAsync();

    // M1 repos
    var rgDir = Path.Combine(benchDir.FullName, "ripgrep");
    RepoCloner.CloneAndPin("https://github.com/BurntSushi/ripgrep", rgDir, pinnedSha: null);
    RepoCloner.CargoFetch(rgDir);

    // M2 repos
    var roslynDir = Path.Combine(benchDir.FullName, "roslyn");
    RepoCloner.CloneAndPin("https://github.com/dotnet/roslyn", roslynDir, pinnedSha: null);
    RepoCloner.HydrateRoslyn(roslynDir);

    var llvmDir = Path.Combine(benchDir.FullName, "llvm-project");
    var llvmBuildDir = Path.Combine(benchDir.FullName, "llvm-build");
    RepoCloner.CloneAndPin("https://github.com/llvm/llvm-project", llvmDir, pinnedSha: null);
    RepoCloner.HydrateLlvm(llvmDir, llvmBuildDir);

    var filesDir = Path.Combine(benchDir.FullName, "Files");
    RepoCloner.CloneAndPin("https://github.com/files-community/Files", filesDir, pinnedSha: null);
    RepoCloner.HydrateFiles(filesDir);

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
scenarios.AddRange(LlvmScenario.Create(llvmDir, llvmBuildDir));
scenarios.AddRange(FilesScenario.Create(filesDir));

// API microbench (M1)
// file-create-delete handled separately...

// Execute all scenarios
var runner = new ScenarioRunner(profile, outputRoot, repetitions, runnerVersion);
var allResults = new List<RunResult>();
foreach (var scenario in scenarios)
{
    var results = runner.Execute(scenario);
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

Create `VsBuildToolsInstaller.cs`, `CmakeInstaller.cs`, `NinjaInstaller.cs`, `DotNetSdkInstaller.cs` in `AvBench.Core/Setup/`.

Test each on a clean VM:
1. `VsBuildToolsInstaller.EnsureInstalledAsync()` → `vswhere` detects install or setup exits with a restart-required message
2. `CmakeInstaller.EnsureInstalledAsync()` → `cmake --version` works
3. `NinjaInstaller.EnsureInstalledAsync()` → `ninja --version` works
4. `DotNetSdkInstaller.EnsureInstalledAsync()` → `dotnet --list-sdks` shows both versions

Note: VS Build Tools install takes 10-30 minutes. Plan accordingly.

### Step 3: Add MSBuild path helper

Add `VsBuildToolsInstaller.FindMsBuildPath()` — uses vswhere to locate MSBuild.exe. This is used by Files and Roslyn scenarios.

### Step 4: Add repo hydration methods

Extend `RepoCloner` with `HydrateRoslyn()`, `HydrateLlvm()`, `HydrateFiles()`.

Test:
- Roslyn: `Restore.cmd` completes without errors
- LLVM: `cmake` configure generates `build.ninja` in build dir and succeeds with Python available
- Files: `msbuild /t:Restore /p:RestorePackagesConfig=true /nr:false` restores NuGet and native packages

### Step 5: Build scenario definitions

Create `RoslynScenario.cs`, `LlvmScenario.cs`, `FilesScenario.cs`.

Test each scenario standalone:
- Roslyn clean build via `dotnet build Roslyn.slnx -c Release /m /nr:false` produces output in `artifacts/`
- LLVM clean build via `ninja` produces clang binary
- Files clean build via `msbuild` produces output in `src/Files.App/bin/`

### Step 6: Wire up extended setup/run commands

Update `SetupCommand.cs` to install M2 tools and clone/hydrate all repos.
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
avbench run --name defender-default --bench-dir C:\bench --output C:\results\defender-default -n 3

# Run 2: disable real-time protection, run again as baseline
# (manually toggle Defender off)
avbench run --name baseline-os --bench-dir C:\bench --output C:\results\baseline-os -n 3

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
| LLVM clean build takes 30-60 minutes | Fewer repetitions practical | Use N=3 for LLVM; N=5 for faster workloads. Allow per-scenario rep count override. |
| Files requires .NET 10 SDK (preview) | SDK availability may vary | Pin exact version in installer code. Use `dotnet-install.ps1` to fetch exact version. |
| Files has C++ projects requiring MSVC | Incremental MSVC builds may create noise | Measure full solution build. The C++ projects are small (dialog helpers). |
| Visual Studio install may require reboot | Setup cannot finish in one pass | Detect pending restart, tell the user to restart Windows, and rerun `avbench setup`. |
| MSBuild path varies by VS version | Scenario fails to find MSBuild | Use vswhere with `-find MSBuild\**\Bin\MSBuild.exe` to discover dynamically. |
| Comparison across VMs with different hardware | Invalid slowdown numbers | `compare.csv` includes `machine` info from `run.json`. Document: all VMs must use identical hardware specs. |
| LLVM clone is 1.5+ GB | Setup slow on limited bandwidth | Use `--depth 1` for shallow clone when not pinning specific SHA. Add `--filter=blob:none` for partial clone. |

## Testing Strategy

Manual verification on test VMs:

1. `avbench setup` on clean VM installs all tools (VS Build Tools, CMake, Ninja, .NET SDK, Git, Rust)
2. All repos cloned and hydrated (Roslyn Restore.cmd, LLVM cmake configure, Files msbuild restore)
3. Each scenario produces valid `run.json` with non-zero metrics
4. LLVM clean build produces clang binary
5. Roslyn clean build produces compiler output in `artifacts/`
6. Files clean build produces output in respective `bin/` dirs
7. `avbench-compare` produces `compare.csv` with correct columns and plausible slowdown values
8. `summary.md` renders correctly and identifies highest-slowdown scenarios
