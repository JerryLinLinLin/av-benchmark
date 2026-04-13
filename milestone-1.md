# Milestone 1 Implementation

## Scope

- `avbench setup` — install Git + Rust, clone ripgrep, run `cargo fetch`
- `avbench run` — ripgrep compile scenarios (clean/incremental/noop) + `file-create-delete` API microbench
- Job object process-tree runner with default metrics
- JSON output (`run.json`) and CSV flattening (`runs.csv`)

## Target Framework and Dependencies

- .NET 8.0 (LTS, ships as a self-contained single-file exe so the VM doesn't need .NET pre-installed)
- `System.CommandLine` 2.0.5 (stable, supports subcommands via `SetAction` + `ParseResult`)
- `System.Text.Json` (built-in, for `run.json` serialization)
- No other NuGet packages

Publish as **self-contained, single-file** for Windows x64:

```
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

This means the VM only needs the `avbench.exe` binary — no .NET runtime pre-installed.

## Solution Structure

```
av-benchmark/
  src/
    AvBench.sln
    AvBench.Cli/
      AvBench.Cli.csproj          → produces avbench.exe
      Program.cs
      Commands/
        SetupCommand.cs
        RunCommand.cs
    AvBench.Core/
      AvBench.Core.csproj         → class library
      Models/
        RunResult.cs              → run.json data model
        SuiteManifest.cs
        AvProfile.cs
        ScenarioDefinition.cs
      Setup/
        ToolInstaller.cs          → base class for tool install logic
        GitInstaller.cs
        RustInstaller.cs
        RepoCloner.cs
      Runner/
        JobObject.cs              → P/Invoke wrapper for Windows Job objects
        ProcessTreeRunner.cs      → launch process under Job, collect metrics
      Scenarios/
        ScenarioRunner.cs         → orchestrate warmup + repetitions
        RipgrepScenario.cs
        FileMicrobenchScenario.cs
      Output/
        JsonResultWriter.cs
        CsvResultWriter.cs
  profiles/
    baseline-os.json
    defender-default.json
  scenarios/
    ripgrep.json
    file-create-delete.json
```

## Project Files

### `AvBench.Cli.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <AssemblyName>avbench</AssemblyName>
    <RootNamespace>AvBench.Cli</RootNamespace>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="System.CommandLine" Version="2.0.5" />
    <ProjectReference Include="..\AvBench.Core\AvBench.Core.csproj" />
  </ItemGroup>
</Project>
```

### `AvBench.Core.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <RootNamespace>AvBench.Core</RootNamespace>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

## CLI Entry Point

### `Program.cs`

```csharp
using System.CommandLine;
using System.Security.Principal;
using AvBench.Cli.Commands;

// Require admin — tool installs and WPR tracing need elevation
using var identity = WindowsIdentity.GetCurrent();
var principal = new WindowsPrincipal(identity);
if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
{
    Console.Error.WriteLine("ERROR: avbench must run as Administrator.");
    return 1;
}

var rootCommand = new RootCommand("AV benchmark suite");

rootCommand.Subcommands.Add(SetupCommand.Create());
rootCommand.Subcommands.Add(RunCommand.Create());

return rootCommand.Parse(args).Invoke();
```

### `SetupCommand.cs`

```csharp
using System.CommandLine;

namespace AvBench.Cli.Commands;

public static class SetupCommand
{
    public static Command Create()
    {
        var benchDirOption = new Option<DirectoryInfo>("--bench-dir")
        {
            Description = "Root directory for repos and builds",
            DefaultValueFactory = _ => new DirectoryInfo(@"C:\bench")
        };

        var command = new Command("setup", "Install tools, clone repos, hydrate dependencies");
        command.Options.Add(benchDirOption);

        command.SetAction(parseResult =>
        {
            var benchDir = parseResult.GetValue(benchDirOption)!;
            // 1. Install Git
            // 2. Install Rust
            // 3. Clone ripgrep
            // 4. cargo fetch
            // 5. Write suite-manifest.json
            return 0;
        });

        return command;
    }
}
```

### `RunCommand.cs`

```csharp
using System.CommandLine;

namespace AvBench.Cli.Commands;

public static class RunCommand
{
    public static Command Create()
    {
        var profileOption = new Option<FileInfo>("--profile")
        {
            Description = "Path to AV profile JSON file",
            Required = true
        };
        var benchDirOption = new Option<DirectoryInfo>("--bench-dir")
        {
            Description = "Root directory for repos and builds",
            DefaultValueFactory = _ => new DirectoryInfo(@"C:\bench")
        };
        var repetitionsOption = new Option<int>("--repetitions", "-n")
        {
            Description = "Number of timed repetitions per scenario",
            DefaultValueFactory = _ => 5
        };
        var outputOption = new Option<DirectoryInfo>("--output")
        {
            Description = "Output directory for results",
            DefaultValueFactory = _ => new DirectoryInfo("results")
        };

        var command = new Command("run", "Execute benchmark scenarios and record metrics");
        command.Options.Add(profileOption);
        command.Options.Add(benchDirOption);
        command.Options.Add(repetitionsOption);
        command.Options.Add(outputOption);

        command.SetAction(parseResult =>
        {
            var profile = parseResult.GetValue(profileOption)!;
            var benchDir = parseResult.GetValue(benchDirOption)!;
            var reps = parseResult.GetValue(repetitionsOption);
            var output = parseResult.GetValue(outputOption)!;
            // 1. Load profile + manifest
            // 2. Idle check
            // 3. For each scenario: warmup + N reps
            // 4. Write runs.csv
            return 0;
        });

        return command;
    }
}
```

## Data Models

### `RunResult.cs`

Maps directly to `run.json`. Use `System.Text.Json` source generators for AOT-safe serialization.

```csharp
using System.Text.Json.Serialization;

namespace AvBench.Core.Models;

public sealed class RunResult
{
    [JsonPropertyName("scenario_id")]
    public string ScenarioId { get; set; } = "";

    [JsonPropertyName("av_profile")]
    public string AvProfile { get; set; } = "";

    [JsonPropertyName("repetition")]
    public int Repetition { get; set; }

    [JsonPropertyName("timestamp_utc")]
    public DateTime TimestampUtc { get; set; }

    [JsonPropertyName("command")]
    public string Command { get; set; } = "";

    [JsonPropertyName("working_dir")]
    public string WorkingDir { get; set; } = "";

    [JsonPropertyName("exit_code")]
    public int ExitCode { get; set; }

    [JsonPropertyName("wall_ms")]
    public long WallMs { get; set; }

    [JsonPropertyName("user_cpu_ms")]
    public long UserCpuMs { get; set; }

    [JsonPropertyName("kernel_cpu_ms")]
    public long KernelCpuMs { get; set; }

    [JsonPropertyName("peak_job_memory_mb")]
    public long PeakJobMemoryMb { get; set; }

    [JsonPropertyName("io_read_bytes")]
    public long IoReadBytes { get; set; }

    [JsonPropertyName("io_write_bytes")]
    public long IoWriteBytes { get; set; }

    [JsonPropertyName("io_read_ops")]
    public long IoReadOps { get; set; }

    [JsonPropertyName("io_write_ops")]
    public long IoWriteOps { get; set; }

    [JsonPropertyName("total_processes")]
    public int TotalProcesses { get; set; }

    [JsonPropertyName("machine")]
    public MachineInfo Machine { get; set; } = new();

    [JsonPropertyName("runner_version")]
    public string RunnerVersion { get; set; } = "";

    [JsonPropertyName("suite_manifest_sha")]
    public string SuiteManifestSha { get; set; } = "";
}

public sealed class MachineInfo
{
    [JsonPropertyName("os")]
    public string Os { get; set; } = "";

    [JsonPropertyName("cpu")]
    public string Cpu { get; set; } = "";

    [JsonPropertyName("ram_gb")]
    public int RamGb { get; set; }

    [JsonPropertyName("storage")]
    public string Storage { get; set; } = "";
}

// Source generator for AOT-safe JSON serialization
[JsonSerializable(typeof(RunResult))]
[JsonSerializable(typeof(List<RunResult>))]
public partial class RunResultContext : JsonSerializerContext { }
```

### `AvProfile.cs`

```csharp
using System.Text.Json.Serialization;

namespace AvBench.Core.Models;

public sealed class AvProfile
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("product")]
    public string Product { get; set; } = "";

    [JsonPropertyName("product_version")]
    public string ProductVersion { get; set; } = "";

    [JsonPropertyName("realtime_protection")]
    public bool RealtimeProtection { get; set; }

    [JsonPropertyName("cloud_features")]
    public bool CloudFeatures { get; set; }

    [JsonPropertyName("exclusion_paths")]
    public List<string> ExclusionPaths { get; set; } = [];

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = "";
}

[JsonSerializable(typeof(AvProfile))]
public partial class AvProfileContext : JsonSerializerContext { }
```

### `SuiteManifest.cs`

```csharp
using System.Text.Json.Serialization;

namespace AvBench.Core.Models;

public sealed class SuiteManifest
{
    [JsonPropertyName("created_utc")]
    public DateTime CreatedUtc { get; set; }

    [JsonPropertyName("repos")]
    public List<RepoEntry> Repos { get; set; } = [];

    [JsonPropertyName("tools")]
    public Dictionary<string, string> Tools { get; set; } = new();
}

public sealed class RepoEntry
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("sha")]
    public string Sha { get; set; } = "";

    [JsonPropertyName("local_path")]
    public string LocalPath { get; set; } = "";
}

[JsonSerializable(typeof(SuiteManifest))]
public partial class SuiteManifestContext : JsonSerializerContext { }
```

## Tool Installation

### Design

Each installer follows the same pattern:

1. **Detect** — run a command to check if the tool exists and get its version.
2. **Download** — fetch the installer from a known URL to a temp directory.
3. **Install** — run the installer silently.
4. **Verify** — re-run the detect command.

All download URLs are configurable via `tools-manifest.json` (optional, hardcoded defaults for M1).

### `ToolInstaller.cs`

```csharp
namespace AvBench.Core.Setup;

public abstract class ToolInstaller
{
    public abstract string Name { get; }

    /// <summary>Returns the installed version, or null if not found.</summary>
    public abstract string? Detect();

    /// <summary>Downloads and installs the tool silently. Throws on failure.</summary>
    public abstract Task InstallAsync(CancellationToken ct = default);

    public async Task EnsureInstalledAsync(CancellationToken ct = default)
    {
        var version = Detect();
        if (version is not null)
        {
            Console.WriteLine($"[setup] {Name} already installed: {version}");
            return;
        }

        Console.WriteLine($"[setup] Installing {Name}...");
        await InstallAsync(ct);

        version = Detect() ?? throw new InvalidOperationException(
            $"{Name} installation completed but detection still fails");
        Console.WriteLine($"[setup] {Name} installed: {version}");
    }
}
```

### `GitInstaller.cs`

```csharp
using System.Diagnostics;
using System.Net.Http;

namespace AvBench.Core.Setup;

public sealed class GitInstaller : ToolInstaller
{
    public override string Name => "Git";

    // Default URL for Git for Windows 64-bit. Override via tools-manifest.json later.
    private const string DefaultUrl =
        "https://github.com/git-for-windows/git/releases/download/v2.47.1.windows.1/Git-2.47.1-64-bit.exe";

    public override string? Detect()
    {
        return RunAndCapture("git", "--version");
    }

    public override async Task InstallAsync(CancellationToken ct = default)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "git-installer.exe");
        await DownloadFileAsync(DefaultUrl, tempPath, ct);

        // Silent install: /VERYSILENT /NORESTART /NOCANCEL /SP-
        // /COMPONENTS="" installs only core git, no GUI extras
        var exitCode = RunProcess(tempPath,
            "/VERYSILENT /NORESTART /NOCANCEL /SP- " +
            "/COMPONENTS=\"icons,ext,ext\\shellhere,ext\\guihere,assoc,assoc_sh\"");

        if (exitCode != 0)
            throw new InvalidOperationException($"Git installer exited with code {exitCode}");

        // Git installs to C:\Program Files\Git\cmd — add to PATH for this process
        var gitPath = @"C:\Program Files\Git\cmd";
        if (Directory.Exists(gitPath))
            Environment.SetEnvironmentVariable("PATH",
                gitPath + ";" + Environment.GetEnvironmentVariable("PATH"));
    }

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

### `RustInstaller.cs`

```csharp
using System.Diagnostics;
using System.Net.Http;

namespace AvBench.Core.Setup;

public sealed class RustInstaller : ToolInstaller
{
    public override string Name => "Rust";

    private const string RustupUrl =
        "https://static.rust-lang.org/rustup/dist/x86_64-pc-windows-msvc/rustup-init.exe";

    public override string? Detect()
    {
        return RunAndCapture("rustc", "--version");
    }

    public override async Task InstallAsync(CancellationToken ct = default)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "rustup-init.exe");
        await DownloadFileAsync(RustupUrl, tempPath, ct);

        // -y = accept defaults, no prompts
        // --default-toolchain stable
        var exitCode = RunProcess(tempPath, "-y --default-toolchain stable");

        if (exitCode != 0)
            throw new InvalidOperationException($"rustup-init exited with code {exitCode}");

        // rustup installs to %USERPROFILE%\.cargo\bin — add to PATH for this process
        var cargoPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".cargo", "bin");
        if (Directory.Exists(cargoPath))
            Environment.SetEnvironmentVariable("PATH",
                cargoPath + ";" + Environment.GetEnvironmentVariable("PATH"));
    }

    // Same helpers as GitInstaller — factor into base class or static helper in real code
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

### `RepoCloner.cs`

```csharp
using System.Diagnostics;

namespace AvBench.Core.Setup;

public static class RepoCloner
{
    /// <summary>
    /// Clones a repo if the target directory doesn't exist, then checks out the pinned SHA.
    /// Returns the resolved HEAD SHA.
    /// </summary>
    public static string CloneAndPin(string repoUrl, string targetDir, string? pinnedSha = null)
    {
        if (!Directory.Exists(targetDir))
        {
            Console.WriteLine($"[setup] Cloning {repoUrl} → {targetDir}");
            RunGit($"clone --config core.autocrlf=false {repoUrl} {targetDir}");
        }
        else
        {
            Console.WriteLine($"[setup] Repo already exists: {targetDir}");
        }

        if (pinnedSha is not null)
        {
            Console.WriteLine($"[setup] Checking out {pinnedSha}");
            RunGit($"-C {targetDir} checkout {pinnedSha}");
        }

        // Read current HEAD SHA
        var sha = RunGitCapture($"-C {targetDir} rev-parse HEAD");
        Console.WriteLine($"[setup] HEAD: {sha}");
        return sha;
    }

    /// <summary>Run cargo fetch in a repo directory.</summary>
    public static void CargoFetch(string repoDir)
    {
        Console.WriteLine($"[setup] Running cargo fetch in {repoDir}");
        var psi = new ProcessStartInfo("cargo", "fetch")
        {
            WorkingDirectory = repoDir,
            UseShellExecute = false
        };
        var proc = Process.Start(psi)!;
        proc.WaitForExit();
        if (proc.ExitCode != 0)
            throw new InvalidOperationException($"cargo fetch failed with exit code {proc.ExitCode}");
    }

    private static void RunGit(string arguments)
    {
        var psi = new ProcessStartInfo("git", arguments)
        {
            UseShellExecute = false
        };
        var proc = Process.Start(psi)!;
        proc.WaitForExit();
        if (proc.ExitCode != 0)
            throw new InvalidOperationException($"git {arguments} failed with exit code {proc.ExitCode}");
    }

    private static string RunGitCapture(string arguments)
    {
        var psi = new ProcessStartInfo("git", arguments)
        {
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        var proc = Process.Start(psi)!;
        var output = proc.StandardOutput.ReadToEnd().Trim();
        proc.WaitForExit();
        return output;
    }
}
```

## Windows Job Object P/Invoke

### `JobObject.cs`

This is the core measurement component. It wraps the Win32 Job Object API for process-tree accounting.

```csharp
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace AvBench.Core.Runner;

/// <summary>
/// Wraps a Windows Job Object for process-tree creation and accounting.
/// </summary>
public sealed class JobObject : IDisposable
{
    private readonly SafeFileHandle _handle;

    public JobObject()
    {
        _handle = CreateJobObject(IntPtr.Zero, null);
        if (_handle.IsInvalid)
            throw new InvalidOperationException(
                $"CreateJobObject failed: {Marshal.GetLastWin32Error()}");

        // Configure: kill all processes when the job handle is closed
        var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
            {
                LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
            }
        };
        int size = Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();
        if (!SetInformationJobObject(_handle, JobObjectInfoClass.ExtendedLimitInformation,
                ref info, size))
            throw new InvalidOperationException(
                $"SetInformationJobObject failed: {Marshal.GetLastWin32Error()}");
    }

    public void AssignProcess(IntPtr processHandle)
    {
        if (!AssignProcessToJobObject(_handle, processHandle))
            throw new InvalidOperationException(
                $"AssignProcessToJobObject failed: {Marshal.GetLastWin32Error()}");
    }

    /// <summary>Query basic + I/O accounting after all processes have exited.</summary>
    public JobAccountingInfo QueryAccounting()
    {
        var info = new JOBOBJECT_BASIC_AND_IO_ACCOUNTING_INFORMATION();
        int size = Marshal.SizeOf<JOBOBJECT_BASIC_AND_IO_ACCOUNTING_INFORMATION>();
        if (!QueryInformationJobObject(_handle,
                JobObjectInfoClass.BasicAndIoAccountingInformation,
                out info, size, out _))
            throw new InvalidOperationException(
                $"QueryInformationJobObject (accounting) failed: {Marshal.GetLastWin32Error()}");

        var extInfo = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
        int extSize = Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();
        if (!QueryInformationJobObject(_handle,
                JobObjectInfoClass.ExtendedLimitInformation,
                out extInfo, extSize, out _))
            throw new InvalidOperationException(
                $"QueryInformationJobObject (extended) failed: {Marshal.GetLastWin32Error()}");

        return new JobAccountingInfo
        {
            TotalUserTimeMs = info.BasicInfo.TotalUserTime / 10_000,     // 100ns → ms
            TotalKernelTimeMs = info.BasicInfo.TotalKernelTime / 10_000,
            TotalProcesses = (int)info.BasicInfo.TotalProcesses,
            IoReadBytes = (long)info.IoInfo.ReadTransferCount,
            IoWriteBytes = (long)info.IoInfo.WriteTransferCount,
            IoReadOps = (long)info.IoInfo.ReadOperationCount,
            IoWriteOps = (long)info.IoInfo.WriteOperationCount,
            PeakJobMemoryBytes = (long)extInfo.PeakJobMemoryUsed
        };
    }

    public void Dispose() => _handle.Dispose();

    // --- P/Invoke declarations ---

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern SafeFileHandle CreateJobObject(IntPtr lpJobAttributes, string? lpName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool AssignProcessToJobObject(SafeFileHandle hJob, IntPtr hProcess);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetInformationJobObject(
        SafeFileHandle hJob,
        JobObjectInfoClass informationClass,
        ref JOBOBJECT_EXTENDED_LIMIT_INFORMATION info,
        int cbInfoLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool QueryInformationJobObject(
        SafeFileHandle hJob,
        JobObjectInfoClass informationClass,
        out JOBOBJECT_BASIC_AND_IO_ACCOUNTING_INFORMATION info,
        int cbInfoLength,
        out int returnLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool QueryInformationJobObject(
        SafeFileHandle hJob,
        JobObjectInfoClass informationClass,
        out JOBOBJECT_EXTENDED_LIMIT_INFORMATION info,
        int cbInfoLength,
        out int returnLength);

    private enum JobObjectInfoClass
    {
        BasicAccountingInformation = 1,
        BasicAndIoAccountingInformation = 8,
        ExtendedLimitInformation = 9,
    }

    private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000;

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public UIntPtr MinimumWorkingSetSize;
        public UIntPtr MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public UIntPtr Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IO_COUNTERS
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_BASIC_ACCOUNTING_INFORMATION
    {
        public long TotalUserTime;          // 100-nanosecond ticks
        public long TotalKernelTime;
        public long ThisPeriodTotalUserTime;
        public long ThisPeriodTotalKernelTime;
        public uint TotalPageFaultCount;
        public uint TotalProcesses;
        public uint ActiveProcesses;
        public uint TotalTerminatedProcesses;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_BASIC_AND_IO_ACCOUNTING_INFORMATION
    {
        public JOBOBJECT_BASIC_ACCOUNTING_INFORMATION BasicInfo;
        public IO_COUNTERS IoInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;          // Reserved
        public UIntPtr ProcessMemoryLimit;
        public UIntPtr JobMemoryLimit;
        public UIntPtr PeakProcessMemoryUsed;
        public UIntPtr PeakJobMemoryUsed;
    }
}

/// <summary>Metrics collected from the Job Object after process-tree completion.</summary>
public sealed class JobAccountingInfo
{
    public long TotalUserTimeMs { get; init; }
    public long TotalKernelTimeMs { get; init; }
    public int TotalProcesses { get; init; }
    public long IoReadBytes { get; init; }
    public long IoWriteBytes { get; init; }
    public long IoReadOps { get; init; }
    public long IoWriteOps { get; init; }
    public long PeakJobMemoryBytes { get; init; }
}
```

## Process-Tree Runner

### `ProcessTreeRunner.cs`

Launches a workload command under a Job Object, streams stdout/stderr to files, waits for completion, then queries accounting.

```csharp
using System.Diagnostics;

namespace AvBench.Core.Runner;

public sealed class ProcessTreeRunResult
{
    public int ExitCode { get; init; }
    public long WallMs { get; init; }
    public JobAccountingInfo Accounting { get; init; } = new();
}

public static class ProcessTreeRunner
{
    /// <summary>
    /// Run a command under a Windows Job object and collect metrics.
    /// </summary>
    public static ProcessTreeRunResult Run(
        string fileName,
        string arguments,
        string workingDirectory,
        string stdoutLogPath,
        string stderrLogPath,
        TimeSpan? timeout = null)
    {
        using var job = new JobObject();

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        var sw = Stopwatch.StartNew();
        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start: {fileName} {arguments}");

        // Assign to job immediately after start.
        // Note: Process.Start doesn't use CREATE_SUSPENDED, but on modern Windows
        // (8+/Server 2012+) nested jobs work and child processes inherit the job.
        // The window between Start and AssignProcess is tiny; for compile workloads
        // that spawn many children over seconds/minutes, this is acceptable.
        job.AssignProcess(proc.Handle);

        // Stream stdout/stderr to log files asynchronously
        var stdoutTask = StreamToFileAsync(proc.StandardOutput, stdoutLogPath);
        var stderrTask = StreamToFileAsync(proc.StandardError, stderrLogPath);

        var timeoutMs = timeout.HasValue ? (int)timeout.Value.TotalMilliseconds : -1;
        if (!proc.WaitForExit(timeoutMs))
        {
            proc.Kill(entireProcessTree: true);
            proc.WaitForExit();
        }

        sw.Stop();

        // Ensure streams are fully drained
        stdoutTask.GetAwaiter().GetResult();
        stderrTask.GetAwaiter().GetResult();

        var accounting = job.QueryAccounting();

        return new ProcessTreeRunResult
        {
            ExitCode = proc.ExitCode,
            WallMs = sw.ElapsedMilliseconds,
            Accounting = accounting
        };
    }

    private static async Task StreamToFileAsync(StreamReader reader, string path)
    {
        await using var writer = new StreamWriter(path, append: false);
        string? line;
        while ((line = await reader.ReadLineAsync()) is not null)
        {
            await writer.WriteLineAsync(line);
        }
    }
}
```

Why not `CREATE_SUSPENDED`? `Process.Start()` in .NET doesn't expose `CREATE_SUSPENDED` directly. We would need raw `CreateProcess` P/Invoke. For M1, the `Process.Start` then immediate `AssignProcess` approach is sufficient — compile workloads run for seconds to minutes, and the process is assigned to the job before it spawns any children. If we need exact coverage from process start, we can add raw `CreateProcess` P/Invoke in M2.

## Scenario Orchestration

### `ScenarioRunner.cs`

Orchestrates warmup, repetitions, and cleanup for a scenario.

```csharp
using AvBench.Core.Models;
using AvBench.Core.Runner;
using AvBench.Core.Output;

namespace AvBench.Core.Scenarios;

public sealed class ScenarioRunner
{
    private readonly AvProfile _profile;
    private readonly string _outputRoot;
    private readonly int _repetitions;
    private readonly string _runnerVersion;

    public ScenarioRunner(AvProfile profile, string outputRoot, int repetitions, string runnerVersion)
    {
        _profile = profile;
        _outputRoot = outputRoot;
        _repetitions = repetitions;
        _runnerVersion = runnerVersion;
    }

    /// <summary>
    /// Run a full scenario block: idle check, warmup, N repetitions.
    /// Returns all RunResults.
    /// </summary>
    public List<RunResult> Execute(ScenarioDefinition scenario)
    {
        IdleCheck();

        Console.WriteLine($"[run] Warmup: {scenario.Id}");
        RunOnce(scenario, isWarmup: true);

        var results = new List<RunResult>();
        for (int rep = 1; rep <= _repetitions; rep++)
        {
            Console.WriteLine($"[run] {scenario.Id} rep {rep}/{_repetitions}");
            var result = RunOnce(scenario, isWarmup: false);
            result.Repetition = rep;
            results.Add(result);

            // Write run.json immediately
            var repDir = Path.Combine(_outputRoot, scenario.Id, $"rep-{rep:D2}");
            Directory.CreateDirectory(repDir);

            // Move log files into rep dir
            var stdoutDest = Path.Combine(repDir, "stdout.log");
            var stderrDest = Path.Combine(repDir, "stderr.log");
            if (File.Exists(result.Command + ".stdout.tmp"))
                File.Move(result.Command + ".stdout.tmp", stdoutDest, overwrite: true);
            if (File.Exists(result.Command + ".stderr.tmp"))
                File.Move(result.Command + ".stderr.tmp", stderrDest, overwrite: true);

            JsonResultWriter.Write(result, Path.Combine(repDir, "run.json"));
        }

        return results;
    }

    private RunResult RunOnce(ScenarioDefinition scenario, bool isWarmup)
    {
        // Run pre-actions (e.g., clean build dir)
        foreach (var action in scenario.PreActions)
            RunShell(action, scenario.WorkingDirectory);

        var stdoutLog = Path.GetTempFileName();
        var stderrLog = Path.GetTempFileName();

        var treeResult = ProcessTreeRunner.Run(
            fileName: scenario.FileName,
            arguments: scenario.Arguments,
            workingDirectory: scenario.WorkingDirectory,
            stdoutLogPath: stdoutLog,
            stderrLogPath: stderrLog,
            timeout: TimeSpan.FromHours(2));

        if (!isWarmup)
        {
            return new RunResult
            {
                ScenarioId = scenario.Id,
                AvProfile = _profile.Name,
                TimestampUtc = DateTime.UtcNow,
                Command = $"{scenario.FileName} {scenario.Arguments}",
                WorkingDir = scenario.WorkingDirectory,
                ExitCode = treeResult.ExitCode,
                WallMs = treeResult.WallMs,
                UserCpuMs = treeResult.Accounting.TotalUserTimeMs,
                KernelCpuMs = treeResult.Accounting.TotalKernelTimeMs,
                PeakJobMemoryMb = treeResult.Accounting.PeakJobMemoryBytes / (1024 * 1024),
                IoReadBytes = treeResult.Accounting.IoReadBytes,
                IoWriteBytes = treeResult.Accounting.IoWriteBytes,
                IoReadOps = treeResult.Accounting.IoReadOps,
                IoWriteOps = treeResult.Accounting.IoWriteOps,
                TotalProcesses = treeResult.Accounting.TotalProcesses,
                Machine = CollectMachineInfo(),
                RunnerVersion = _runnerVersion
            };
        }

        return new RunResult(); // warmup, discarded
    }

    private static void IdleCheck()
    {
        // Simple heuristic: check CPU usage over 3 seconds
        // Refuse to run if average > 20%
        // Implementation: use PerformanceCounter or /proc/stat equivalent
        // For M1, just print a warning
        Console.WriteLine("[run] Idle check: OK (stub)");
    }

    private static MachineInfo CollectMachineInfo()
    {
        return new MachineInfo
        {
            Os = Environment.OSVersion.ToString(),
            Cpu = $"{Environment.ProcessorCount} vCPU",
            RamGb = (int)(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024L * 1024 * 1024)),
            Storage = "SSD" // assume for now
        };
    }

    private static void RunShell(string command, string workingDir)
    {
        var psi = new ProcessStartInfo("cmd.exe", $"/c {command}")
        {
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        var proc = Process.Start(psi)!;
        proc.WaitForExit();
    }
}
```

### `ScenarioDefinition.cs`

```csharp
namespace AvBench.Core.Models;

public sealed class ScenarioDefinition
{
    public string Id { get; set; } = "";
    public string FileName { get; set; } = "";
    public string Arguments { get; set; } = "";
    public string WorkingDirectory { get; set; } = "";
    public List<string> PreActions { get; set; } = [];   // shell commands to run before each rep
    public List<string> PostActions { get; set; } = [];
}
```

### Ripgrep Scenario Definitions

Built in code for M1 (no JSON scenario file needed yet):

```csharp
namespace AvBench.Core.Scenarios;

public static class RipgrepScenario
{
    public static List<ScenarioDefinition> Create(string repoDir)
    {
        var buildDir = Path.Combine(repoDir, "target");

        return
        [
            new ScenarioDefinition
            {
                Id = "ripgrep-clean-build",
                FileName = "cargo",
                Arguments = "build --release",
                WorkingDirectory = repoDir,
                PreActions = [$"if exist \"{buildDir}\" rmdir /s /q \"{buildDir}\""]
            },
            new ScenarioDefinition
            {
                Id = "ripgrep-incremental-build",
                FileName = "cargo",
                Arguments = "build --release",
                WorkingDirectory = repoDir,
                PreActions = [MutateIncrementalSourceCommand(repoDir)]
            },
            new ScenarioDefinition
            {
                Id = "ripgrep-noop-build",
                FileName = "cargo",
                Arguments = "build --release",
                WorkingDirectory = repoDir,
                PreActions = []  // no changes, just rebuild
            }
        ];
    }

    private static string MutateIncrementalSourceCommand(string repoDir)
    {
        // Make a harmless source edit to trigger a real incremental rebuild
        var target = Path.Combine(repoDir, "crates", "core", "main.rs");
        return $"powershell -NoProfile -Command " +
               "\"$p='{target}'; " +
               "$marker='// avbench incremental marker: '; " +
               "$content = Get-Content -Raw $p; " +
               "if ($content -match [regex]::Escape($marker)) { " +
               "  $content = $content -replace '([01])(?=[\\r\\n]*$)', { if ($args[0].Value -eq '1') { '0' } else { '1' } }; " +
               "} else { " +
               "  if (-not $content.EndsWith([Environment]::NewLine)) { $content += [Environment]::NewLine }; " +
               "  $content += $marker + '1' + [Environment]::NewLine; " +
               "} " +
               "Set-Content -NoNewline -Path $p -Value $content\"";
    }
}
```

### File Microbench Scenario

```csharp
using System.Diagnostics;
using AvBench.Core.Models;

namespace AvBench.Core.Scenarios;

public static class FileMicrobenchScenario
{
    /// <summary>
    /// Create and delete small temp files in a loop.
    /// Returns a RunResult with ops/sec in the command field for now.
    /// </summary>
    public static RunResult Execute(string tempRoot, int totalOps, int batchSize, string avProfile)
    {
        Directory.CreateDirectory(tempRoot);

        // Warmup
        RunBatch(tempRoot, batchSize);

        var sw = Stopwatch.StartNew();
        int completed = 0;

        while (completed < totalOps)
        {
            int ops = Math.Min(batchSize, totalOps - completed);
            RunBatch(tempRoot, ops);
            completed += ops;
        }

        sw.Stop();

        double opsPerSec = totalOps / sw.Elapsed.TotalSeconds;
        double meanLatencyUs = sw.Elapsed.TotalMicroseconds / totalOps;

        return new RunResult
        {
            ScenarioId = "file-create-delete",
            AvProfile = avProfile,
            TimestampUtc = DateTime.UtcNow,
            Command = $"file-create-delete ops={totalOps} batch={batchSize} ops_sec={opsPerSec:F0} mean_latency_us={meanLatencyUs:F1}",
            WorkingDir = tempRoot,
            ExitCode = 0,
            WallMs = sw.ElapsedMilliseconds,
            UserCpuMs = 0,  // not measured via Job for in-process microbench
            KernelCpuMs = 0,
            Machine = new MachineInfo
            {
                Os = Environment.OSVersion.ToString(),
                Cpu = $"{Environment.ProcessorCount} vCPU",
                RamGb = (int)(GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024L * 1024 * 1024)),
                Storage = "SSD"
            }
        };
    }

    private static void RunBatch(string tempRoot, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var path = Path.Combine(tempRoot, $"bench_{i}.tmp");
            // Create a small file (64 bytes)
            using (var fs = File.Create(path))
            {
                Span<byte> data = stackalloc byte[64];
                fs.Write(data);
            }
            File.Delete(path);
        }
    }
}
```

## Output

### `JsonResultWriter.cs`

```csharp
using System.Text.Json;
using AvBench.Core.Models;

namespace AvBench.Core.Output;

public static class JsonResultWriter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        TypeInfoResolver = RunResultContext.Default
    };

    public static void Write(RunResult result, string path)
    {
        var json = JsonSerializer.Serialize(result, Options);
        File.WriteAllText(path, json);
    }
}
```

### `CsvResultWriter.cs`

```csharp
using System.Globalization;
using System.Text;
using AvBench.Core.Models;

namespace AvBench.Core.Output;

public static class CsvResultWriter
{
    private static readonly string[] Headers =
    [
        "scenario_id", "av_profile", "repetition", "timestamp_utc",
        "exit_code", "wall_ms", "user_cpu_ms", "kernel_cpu_ms",
        "peak_job_memory_mb", "io_read_bytes", "io_write_bytes",
        "io_read_ops", "io_write_ops", "total_processes"
    ];

    public static void Write(IReadOnlyList<RunResult> results, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(",", Headers));

        foreach (var r in results)
        {
            sb.AppendLine(string.Join(",",
                Escape(r.ScenarioId),
                Escape(r.AvProfile),
                r.Repetition.ToString(CultureInfo.InvariantCulture),
                r.TimestampUtc.ToString("o"),
                r.ExitCode.ToString(CultureInfo.InvariantCulture),
                r.WallMs.ToString(CultureInfo.InvariantCulture),
                r.UserCpuMs.ToString(CultureInfo.InvariantCulture),
                r.KernelCpuMs.ToString(CultureInfo.InvariantCulture),
                r.PeakJobMemoryMb.ToString(CultureInfo.InvariantCulture),
                r.IoReadBytes.ToString(CultureInfo.InvariantCulture),
                r.IoWriteBytes.ToString(CultureInfo.InvariantCulture),
                r.IoReadOps.ToString(CultureInfo.InvariantCulture),
                r.IoWriteOps.ToString(CultureInfo.InvariantCulture),
                r.TotalProcesses.ToString(CultureInfo.InvariantCulture)
            ));
        }

        File.WriteAllText(path, sb.ToString());
    }

    private static string Escape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
```

## Example Profile Files

### `profiles/baseline-os.json`

```json
{
  "name": "baseline-os",
  "product": "none",
  "product_version": "",
  "realtime_protection": false,
  "cloud_features": false,
  "exclusion_paths": [],
  "notes": "AV real-time protection disabled or no AV installed"
}
```

### `profiles/defender-default.json`

```json
{
  "name": "defender-default",
  "product": "Microsoft Defender",
  "product_version": "",
  "realtime_protection": true,
  "cloud_features": true,
  "exclusion_paths": [],
  "notes": "Default Defender settings, no exclusions"
}
```

## Implementation Steps (ordered)

### Step 1: Create solution and projects

```powershell
cd c:\projects\av-benchmark
mkdir src
cd src
dotnet new sln -n AvBench
dotnet new console -n AvBench.Cli --framework net8.0
dotnet new classlib -n AvBench.Core --framework net8.0
dotnet sln add AvBench.Cli AvBench.Core
cd AvBench.Cli
dotnet add reference ..\AvBench.Core
dotnet add package System.CommandLine --version 2.0.5
```

### Step 2: Build data models

Create `RunResult.cs`, `AvProfile.cs`, `SuiteManifest.cs`, `ScenarioDefinition.cs` in `AvBench.Core/Models/`. These are plain POCOs with `System.Text.Json` attributes and source generators.

### Step 3: Build Job Object P/Invoke wrapper

Create `JobObject.cs` in `AvBench.Core/Runner/`. This is the trickiest piece — test it standalone first:

```csharp
// Quick smoke test: create a job, run cmd /c echo hello, query accounting
using var job = new JobObject();
var psi = new ProcessStartInfo("cmd.exe", "/c echo hello") { UseShellExecute = false };
var proc = Process.Start(psi)!;
job.AssignProcess(proc.Handle);
proc.WaitForExit();
var info = job.QueryAccounting();
Console.WriteLine($"Processes: {info.TotalProcesses}, CPU: {info.TotalUserTimeMs}ms");
```

Verify that `TotalProcesses >= 1` and `TotalUserTimeMs` is non-zero for a real workload.

### Step 4: Build ProcessTreeRunner

Create `ProcessTreeRunner.cs`. This combines `JobObject`, `Process.Start`, `Stopwatch`, and stdout/stderr streaming. Test with `cargo build --release` in any small Rust project.

### Step 5: Build tool installers

Create `ToolInstaller.cs`, `GitInstaller.cs`, `RustInstaller.cs`, `RepoCloner.cs`. Test each on a clean VM:

1. `GitInstaller.EnsureInstalledAsync()` → `git --version` works
2. `RustInstaller.EnsureInstalledAsync()` → `rustc --version` works
3. `RepoCloner.CloneAndPin("https://github.com/BurntSushi/ripgrep", @"C:\bench\ripgrep")` → repo exists
4. `RepoCloner.CargoFetch(@"C:\bench\ripgrep")` → dependencies cached

### Step 6: Build scenario definitions

Create `RipgrepScenario.cs` and `FileMicrobenchScenario.cs`. These produce `ScenarioDefinition` objects or run in-process.

### Step 7: Build ScenarioRunner

Create `ScenarioRunner.cs` that ties together ProcessTreeRunner and the scenario definitions.

### Step 8: Build output writers

Create `JsonResultWriter.cs` and `CsvResultWriter.cs`. Test by writing a fake `RunResult` and verifying the output files.

### Step 9: Wire up CLI commands

Create `SetupCommand.cs` and `RunCommand.cs` that call the above components.

### Step 10: End-to-end test

On a VM with Defender enabled:

```powershell
avbench setup --bench-dir C:\bench
avbench run --profile profiles\defender-default.json --bench-dir C:\bench --output results -n 3
```

Expected output:

```
results/
  20260413-153000/
    suite-manifest.json
    ripgrep-clean-build/
      rep-01/run.json, stdout.log, stderr.log, combined.log
      rep-02/...
      rep-03/...
    ripgrep-incremental-build/
      rep-01/...
    ripgrep-noop-build/
      rep-01/...
    file-create-delete/
      rep-01/run.json
    runs.csv
```

## Key Risks and Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| `AssignProcessToJobObject` after `Process.Start` misses early children | Under-count metrics for first few ms | Acceptable for compile workloads (seconds+). Add raw `CreateProcess` with `CREATE_SUSPENDED` in M2 if needed. |
| Git/Rust installer URLs become stale | Setup fails on new VMs | Use `tools-manifest.json` to override URLs. Pin known-good versions. |
| AV blocks installer downloads | Setup fails | Document: whitelist download URLs in AV profile if needed, or pre-stage installers on a network share. |
| ripgrep `crates/core/main.rs` path may change across versions | Incremental scenario breaks | Pin ripgrep to a specific SHA. Validate the incremental source target path in `setup`. |
| Job object accounting on nested jobs | Double-counting on older Windows | Target Windows Server 2022+ / Windows 11+ where nested jobs are fully supported. |

## Testing Strategy

No formal test framework in M1 — manual verification on a test VM.

Verification checklist:

1. `avbench setup` on a clean Windows Server 2022 VM installs Git + Rust and clones ripgrep
2. Re-running `avbench setup` is idempotent (skips installed tools)
3. `avbench run` with baseline profile produces `run.json` files with valid metrics
4. `avbench run` with defender-default profile produces higher wall times than baseline
5. `run.json` wall_ms, user_cpu_ms, io_read_bytes are non-zero for clean builds
6. `runs.csv` has the correct number of rows (scenarios × repetitions)
7. File microbench reports ops/sec in a plausible range (thousands to hundreds of thousands)
