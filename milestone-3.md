# Milestone 3 Implementation

## Scope

- Extend `avbench setup` to install Python 3.x and Nuitka
- Add Black + Nuitka compile scenarios (`nuitka-standalone`, `nuitka-onefile`)
- Add remaining API microbench families
- Add `--trace` (WPR) opt-in collector
- Add `--counters` (typeperf) opt-in collector

## Prerequisites from Milestone 1–2

All M1/M2 components are assumed working:

- Full tool installer chain (Git, Rust, VS Build Tools, CMake, Ninja, .NET SDK)
- All compile scenarios (ripgrep, Roslyn, LLVM)
- Job object runner, output writers
- `avbench-compare` with CSV and markdown output

## New Files

```
AvBench.Core/
  Setup/
    PythonInstaller.cs         ← NEW
    NuitkaInstaller.cs         ← NEW
  Scenarios/
    BlackNuitkaScenario.cs     ← NEW
    FileOpenCloseBench.cs      ← NEW
    DirEnumerateBench.cs       ← NEW
    CopyRenameMoveBench.cs     ← NEW
    ProcessCreateBench.cs      ← NEW
    RegistryBench.cs           ← NEW
    DllLoadBench.cs            ← NEW
  Collectors/
    WprCollector.cs            ← NEW
    TypeperfCollector.cs       ← NEW
```

## Tool Installation

### `PythonInstaller.cs`

Python 3.x installation on Windows. The legacy full installer (deprecated as of 3.14, but still available for 3.12/3.13 LTS) supports silent unattended install via `/quiet` with property flags.

For Windows Server VMs where MSIX may not work, use the legacy installer:

```csharp
using System.Diagnostics;
using System.Net.Http;

namespace AvBench.Core.Setup;

public sealed class PythonInstaller : ToolInstaller
{
    public override string Name => "Python";

    // Pin a specific Python version — 3.12.x LTS is safer for Nuitka compatibility
    private const string DefaultUrl =
        "https://www.python.org/ftp/python/3.12.8/python-3.12.8-amd64.exe";

    public override string? Detect()
    {
        return RunAndCapture("python", "--version");
    }

    public override async Task InstallAsync(CancellationToken ct = default)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "python-installer.exe");
        await DownloadFileAsync(DefaultUrl, tempPath, ct);

        // Silent install flags (legacy installer, not the new Install Manager):
        // /quiet           — no UI
        // InstallAllUsers=1 — system-wide install
        // PrependPath=1     — add Python to PATH
        // Include_test=0    — skip test suite (saves space)
        // Include_tcltk=0   — skip Tcl/Tk (don't need GUI)
        var exitCode = RunProcess(tempPath,
            "/quiet InstallAllUsers=1 PrependPath=1 Include_test=0 Include_tcltk=0");

        if (exitCode != 0)
            throw new InvalidOperationException($"Python installer exited with code {exitCode}");

        // Python installs to C:\Program Files\Python3xx — add to PATH
        var pythonDir = FindPythonDir();
        if (pythonDir is not null)
        {
            var scriptsDir = Path.Combine(pythonDir, "Scripts");
            var currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
            Environment.SetEnvironmentVariable("PATH",
                $"{pythonDir};{scriptsDir};{currentPath}");
        }
    }

    private static string? FindPythonDir()
    {
        // Check common install locations
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        foreach (var dir in Directory.GetDirectories(programFiles, "Python*"))
        {
            if (File.Exists(Path.Combine(dir, "python.exe")))
                return dir;
        }
        return null;
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

### `NuitkaInstaller.cs`

Nuitka installs via pip. Requires Python to be installed first.

```csharp
using System.Diagnostics;

namespace AvBench.Core.Setup;

public sealed class NuitkaInstaller : ToolInstaller
{
    public override string Name => "Nuitka";

    public override string? Detect()
    {
        return RunAndCapture("python", "-m nuitka --version");
    }

    public override Task InstallAsync(CancellationToken ct = default)
    {
        // Install Nuitka and its recommended dependency (ordered-set)
        var exitCode = RunProcess("python",
            "-m pip install nuitka ordered-set --quiet");

        if (exitCode != 0)
            throw new InvalidOperationException($"pip install nuitka exited with code {exitCode}");

        return Task.CompletedTask;
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

## Repo Setup for Black

```csharp
public static void HydrateBlack(string benchDir)
{
    var blackDir = Path.Combine(benchDir, "black");
    RepoCloner.CloneAndPin("https://github.com/psf/black", blackDir, pinnedSha: null);

    // Create a venv for isolation
    var venvDir = Path.Combine(blackDir, ".venv");
    Console.WriteLine($"[setup] Creating Python venv in {venvDir}");
    RunProcess("python", $"-m venv \"{venvDir}\"");

    // Install Black into the venv
    var pip = Path.Combine(venvDir, "Scripts", "pip.exe");
    RunProcess(pip, $"install -e \"{blackDir}\" --quiet");

    // Create black_entry.py wrapper for Nuitka
    var entryPath = Path.Combine(blackDir, "black_entry.py");
    if (!File.Exists(entryPath))
    {
        File.WriteAllText(entryPath, """
            import sys
            from black import patched_main
            patched_main()
            """);
        Console.WriteLine($"[setup] Created {entryPath}");
    }
}
```

## Scenario Definitions

### `BlackNuitkaScenario.cs`

```csharp
namespace AvBench.Core.Scenarios;

public static class BlackNuitkaScenario
{
    public static List<ScenarioDefinition> Create(string blackDir)
    {
        var venvPython = Path.Combine(blackDir, ".venv", "Scripts", "python.exe");
        var entryPoint = Path.Combine(blackDir, "black_entry.py");
        var outputDir = Path.Combine(blackDir, "nuitka_output");

        return
        [
            new ScenarioDefinition
            {
                Id = "nuitka-standalone",
                FileName = venvPython,
                Arguments = $"-m nuitka --standalone " +
                            $"--output-dir=\"{outputDir}\" " +
                            $"\"{entryPoint}\"",
                WorkingDirectory = blackDir,
                PreActions =
                [
                    // Clean previous build output
                    $"if exist \"{outputDir}\" rmdir /s /q \"{outputDir}\""
                ],
                PostActions =
                [
                    // Smoke test: run the compiled binary
                    $"\"{Path.Combine(outputDir, "black_entry.dist", "black_entry.exe")}\" --version"
                ]
            },
            new ScenarioDefinition
            {
                Id = "nuitka-onefile",
                FileName = venvPython,
                Arguments = $"-m nuitka --onefile " +
                            $"--output-dir=\"{outputDir}\" " +
                            $"\"{entryPoint}\"",
                WorkingDirectory = blackDir,
                PreActions =
                [
                    $"if exist \"{outputDir}\" rmdir /s /q \"{outputDir}\""
                ],
                PostActions =
                [
                    $"\"{Path.Combine(outputDir, "black_entry.exe")}\" --version"
                ]
            }
        ];
    }
}
```

## Remaining API Microbench Families

All microbench families follow the same pattern as `FileMicrobenchScenario` from M1: warmup, fixed-iteration measurement, batch timing. Each is an in-process benchmark wrapped in a `ProcessTreeRunner` Job for consistent measurement.

### `FileOpenCloseBench.cs`

```csharp
using System.Diagnostics;
using AvBench.Core.Models;

namespace AvBench.Core.Scenarios;

public static class FileOpenCloseBench
{
    public static RunResult Execute(string tempRoot, int totalOps, string avName)
    {
        // Create one target file to repeatedly open/close
        Directory.CreateDirectory(tempRoot);
        var targetPath = Path.Combine(tempRoot, "bench_target.tmp");
        File.WriteAllBytes(targetPath, new byte[64]);

        // Warmup
        for (int i = 0; i < 100; i++)
        {
            using var fs = File.OpenRead(targetPath);
        }

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < totalOps; i++)
        {
            using var fs = File.OpenRead(targetPath);
        }
        sw.Stop();

        double opsPerSec = totalOps / sw.Elapsed.TotalSeconds;
        double meanLatencyUs = sw.Elapsed.TotalMicroseconds / totalOps;

        File.Delete(targetPath);

        return new RunResult
        {
            ScenarioId = "file-open-close",
            AvName = avName,
            TimestampUtc = DateTime.UtcNow,
            Command = $"file-open-close ops={totalOps} ops_sec={opsPerSec:F0} mean_latency_us={meanLatencyUs:F1}",
            WorkingDir = tempRoot,
            ExitCode = 0,
            WallMs = sw.ElapsedMilliseconds
        };
    }
}
```

### `DirEnumerateBench.cs`

```csharp
using System.Diagnostics;
using AvBench.Core.Models;

namespace AvBench.Core.Scenarios;

public static class DirEnumerateBench
{
    public static RunResult Execute(string targetDir, int iterations, string avName)
    {
        // targetDir should be a sizable directory tree (e.g., a cloned repo)
        if (!Directory.Exists(targetDir))
            throw new DirectoryNotFoundException($"Target dir not found: {targetDir}");

        // Warmup
        _ = Directory.EnumerateFileSystemEntries(targetDir, "*", SearchOption.AllDirectories).Count();

        var sw = Stopwatch.StartNew();
        long totalEntries = 0;
        for (int i = 0; i < iterations; i++)
        {
            totalEntries += Directory.EnumerateFileSystemEntries(
                targetDir, "*", SearchOption.AllDirectories).Count();
        }
        sw.Stop();

        double opsPerSec = iterations / sw.Elapsed.TotalSeconds;

        return new RunResult
        {
            ScenarioId = "dir-enumerate",
            AvName = avName,
            TimestampUtc = DateTime.UtcNow,
            Command = $"dir-enumerate iters={iterations} entries_per_iter={totalEntries / iterations} ops_sec={opsPerSec:F1}",
            WorkingDir = targetDir,
            ExitCode = 0,
            WallMs = sw.ElapsedMilliseconds
        };
    }
}
```

### `CopyRenameMoveBench.cs`

```csharp
using System.Diagnostics;
using AvBench.Core.Models;

namespace AvBench.Core.Scenarios;

public static class CopyRenameMoveBench
{
    public static RunResult Execute(string tempRoot, int totalOps, string avName)
    {
        Directory.CreateDirectory(tempRoot);
        var sourceData = new byte[1024]; // 1KB files

        // Warmup
        RunBatch(tempRoot, sourceData, 50);

        var sw = Stopwatch.StartNew();
        RunBatch(tempRoot, sourceData, totalOps);
        sw.Stop();

        double opsPerSec = totalOps / sw.Elapsed.TotalSeconds;
        double meanLatencyUs = sw.Elapsed.TotalMicroseconds / totalOps;

        return new RunResult
        {
            ScenarioId = "copy-rename-move",
            AvName = avName,
            TimestampUtc = DateTime.UtcNow,
            Command = $"copy-rename-move ops={totalOps} ops_sec={opsPerSec:F0} mean_latency_us={meanLatencyUs:F1}",
            WorkingDir = tempRoot,
            ExitCode = 0,
            WallMs = sw.ElapsedMilliseconds
        };
    }

    private static void RunBatch(string tempRoot, byte[] data, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var src = Path.Combine(tempRoot, $"src_{i}.tmp");
            var dst = Path.Combine(tempRoot, $"dst_{i}.tmp");
            var moved = Path.Combine(tempRoot, $"moved_{i}.tmp");

            File.WriteAllBytes(src, data);
            File.Copy(src, dst, overwrite: true);
            File.Move(dst, moved, overwrite: true);

            File.Delete(src);
            File.Delete(moved);
        }
    }
}
```

### `ProcessCreateBench.cs`

```csharp
using System.Diagnostics;
using AvBench.Core.Models;

namespace AvBench.Core.Scenarios;

public static class ProcessCreateBench
{
    public static RunResult Execute(int totalOps, string avName)
    {
        // Warmup
        for (int i = 0; i < 10; i++)
            RunCmd();

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < totalOps; i++)
            RunCmd();
        sw.Stop();

        double opsPerSec = totalOps / sw.Elapsed.TotalSeconds;
        double meanLatencyUs = sw.Elapsed.TotalMicroseconds / totalOps;

        return new RunResult
        {
            ScenarioId = "process-create-wait",
            AvName = avName,
            TimestampUtc = DateTime.UtcNow,
            Command = $"process-create-wait ops={totalOps} ops_sec={opsPerSec:F0} mean_latency_us={meanLatencyUs:F1}",
            WorkingDir = Environment.CurrentDirectory,
            ExitCode = 0,
            WallMs = sw.ElapsedMilliseconds
        };
    }

    private static void RunCmd()
    {
        var psi = new ProcessStartInfo("cmd.exe", "/c echo.")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true
        };
        using var proc = Process.Start(psi)!;
        proc.StandardOutput.ReadToEnd();
        proc.WaitForExit();
    }
}
```

### `RegistryBench.cs`

```csharp
using System.Diagnostics;
using Microsoft.Win32;
using AvBench.Core.Models;

namespace AvBench.Core.Scenarios;

public static class RegistryBench
{
    public static RunResult Execute(int totalOps, string avName)
    {
        // Open and query a well-known readonly registry key
        const string keyPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion";

        // Warmup
        for (int i = 0; i < 100; i++)
            QueryRegistry(keyPath);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < totalOps; i++)
            QueryRegistry(keyPath);
        sw.Stop();

        double opsPerSec = totalOps / sw.Elapsed.TotalSeconds;
        double meanLatencyUs = sw.Elapsed.TotalMicroseconds / totalOps;

        return new RunResult
        {
            ScenarioId = "registry-open-query",
            AvName = avName,
            TimestampUtc = DateTime.UtcNow,
            Command = $"registry-open-query ops={totalOps} ops_sec={opsPerSec:F0} mean_latency_us={meanLatencyUs:F1}",
            WorkingDir = Environment.CurrentDirectory,
            ExitCode = 0,
            WallMs = sw.ElapsedMilliseconds
        };
    }

    private static void QueryRegistry(string keyPath)
    {
        using var key = Registry.LocalMachine.OpenSubKey(keyPath);
        _ = key?.GetValue("ProductName");
    }
}
```

### `DllLoadBench.cs`

```csharp
using System.Diagnostics;
using System.Runtime.InteropServices;
using AvBench.Core.Models;

namespace AvBench.Core.Scenarios;

public static class DllLoadBench
{
    // P/Invoke for LoadLibrary / FreeLibrary
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FreeLibrary(IntPtr hModule);

    public static RunResult Execute(int totalOps, string avName)
    {
        // Use a DLL that's always present on Windows
        const string dllName = "urlmon.dll";

        // Warmup
        for (int i = 0; i < 10; i++)
            LoadUnload(dllName);

        var sw = Stopwatch.StartNew();
        for (int i = 0; i < totalOps; i++)
            LoadUnload(dllName);
        sw.Stop();

        double opsPerSec = totalOps / sw.Elapsed.TotalSeconds;
        double meanLatencyUs = sw.Elapsed.TotalMicroseconds / totalOps;

        return new RunResult
        {
            ScenarioId = "dll-load-unload",
            AvName = avName,
            TimestampUtc = DateTime.UtcNow,
            Command = $"dll-load-unload ops={totalOps} ops_sec={opsPerSec:F0} mean_latency_us={meanLatencyUs:F1}",
            WorkingDir = Environment.CurrentDirectory,
            ExitCode = 0,
            WallMs = sw.ElapsedMilliseconds
        };
    }

    private static void LoadUnload(string dllName)
    {
        var handle = LoadLibrary(dllName);
        if (handle != IntPtr.Zero)
            FreeLibrary(handle);
    }
}
```

## Opt-in Collectors

### Architecture

Opt-in collectors are started before the workload and stopped after. They produce additional output files alongside the standard `run.json`.

```csharp
namespace AvBench.Core.Collectors;

/// <summary>
/// Interface for opt-in collectors that run alongside the benchmark.
/// </summary>
public interface IOptInCollector : IDisposable
{
    /// <summary>Start collecting before the workload begins.</summary>
    void Start(string outputDir);

    /// <summary>Stop collecting after the workload completes.</summary>
    void Stop();
}
```

### `WprCollector.cs`

Windows Performance Recorder (WPR) captures ETW traces. Requires Windows ADK or built-in WPR on Windows 10+.

Key commands:
- `wpr -start GeneralProfile` — start recording with built-in GeneralProfile (verbose, memory mode)
- `wpr -stop <file.etl>` — stop and save trace
- `wpr -cancel` — cancel without saving

```csharp
using System.Diagnostics;

namespace AvBench.Core.Collectors;

public sealed class WprCollector : IOptInCollector
{
    private string _outputPath = "";
    private bool _started;

    /// <summary>
    /// Start WPR recording with the GeneralProfile in memory mode.
    /// Memory mode records to a circular buffer; file mode records unbounded.
    /// Memory mode is preferred for benchmarks — it captures the last N seconds
    /// without unbounded disk I/O that would perturb the measurement.
    /// </summary>
    public void Start(string outputDir)
    {
        _outputPath = Path.Combine(outputDir, "trace.etl");

        // Cancel any existing session first (safe if none running)
        RunWpr("-cancel");

        // Start: GeneralProfile captures CPU sampling, disk I/O, memory
        // .verbose gives full detail (vs .light which is lower overhead)
        var exitCode = RunWpr("-start GeneralProfile.Verbose");
        if (exitCode != 0)
        {
            Console.WriteLine($"[trace] WARNING: wpr -start failed with code {exitCode}");
            return;
        }

        _started = true;
        Console.WriteLine("[trace] WPR recording started (GeneralProfile.Verbose)");
    }

    public void Stop()
    {
        if (!_started) return;

        var exitCode = RunWpr($"-stop \"{_outputPath}\"");
        _started = false;

        if (exitCode != 0)
            Console.WriteLine($"[trace] WARNING: wpr -stop failed with code {exitCode}");
        else
            Console.WriteLine($"[trace] Trace saved: {_outputPath}");
    }

    public void Dispose()
    {
        if (_started)
        {
            // Cancel if stop wasn't called
            RunWpr("-cancel");
            _started = false;
        }
    }

    private static int RunWpr(string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo("wpr", arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            };
            var proc = Process.Start(psi);
            if (proc is null) return -1;
            proc.WaitForExit(TimeSpan.FromMinutes(5));
            return proc.ExitCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[trace] WPR not available: {ex.Message}");
            return -1;
        }
    }
}
```

### `TypeperfCollector.cs`

`typeperf` samples performance counters and writes to CSV. We collect CPU%, disk bytes/sec, and available memory.

Key commands:
- `typeperf "counter1" "counter2" -si 1 -o output.csv` — sample every 1 second to CSV
- `typeperf` runs until Ctrl+C or the process is killed
- `-sc N` — collect N samples then stop

```csharp
using System.Diagnostics;

namespace AvBench.Core.Collectors;

public sealed class TypeperfCollector : IOptInCollector
{
    private Process? _process;
    private string _outputPath = "";

    // Performance counters to sample
    private static readonly string[] Counters =
    [
        @"\Processor(_Total)\% Processor Time",
        @"\PhysicalDisk(_Total)\Disk Bytes/sec",
        @"\PhysicalDisk(_Total)\Disk Read Bytes/sec",
        @"\PhysicalDisk(_Total)\Disk Write Bytes/sec",
        @"\Memory\Available MBytes",
        @"\Memory\Pages/sec"
    ];

    private const int SampleIntervalSeconds = 1;

    public void Start(string outputDir)
    {
        _outputPath = Path.Combine(outputDir, "counters.csv");

        // Build counter arguments
        var counterArgs = string.Join(" ", Counters.Select(c => $"\"{c}\""));

        var psi = new ProcessStartInfo("typeperf",
            $"{counterArgs} -si {SampleIntervalSeconds} -o \"{_outputPath}\" -f CSV")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true  // so we can send 'q' to stop gracefully
        };

        try
        {
            _process = Process.Start(psi);
            Console.WriteLine($"[counters] typeperf started, sampling every {SampleIntervalSeconds}s");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[counters] WARNING: typeperf not available: {ex.Message}");
        }
    }

    public void Stop()
    {
        if (_process is null || _process.HasExited) return;

        try
        {
            // typeperf stops on Ctrl+C or when stdin is closed
            // Closing StandardInput signals end
            _process.StandardInput.Close();

            if (!_process.WaitForExit(TimeSpan.FromSeconds(5)))
            {
                _process.Kill();
                _process.WaitForExit();
            }

            Console.WriteLine($"[counters] Counters saved: {_outputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[counters] WARNING: stopping typeperf failed: {ex.Message}");
        }
    }

    public void Dispose()
    {
        Stop();
        _process?.Dispose();
    }
}
```

## Integration with ScenarioRunner

Extend `ScenarioRunner` to accept opt-in collectors via CLI flags.

### New CLI options

```csharp
// In RunCommand.cs
var traceOption = new Option<bool>("--trace")
{
    Description = "Enable WPR ETL trace capture (opt-in)",
    DefaultValueFactory = _ => false
};
var countersOption = new Option<bool>("--counters")
{
    Description = "Enable typeperf performance counter sampling (opt-in)",
    DefaultValueFactory = _ => false
};

command.Options.Add(traceOption);
command.Options.Add(countersOption);
```

### Updated `ScenarioRunner.RunOnce()`

```csharp
private RunResult RunOnce(ScenarioDefinition scenario, bool isWarmup, string repDir)
{
    foreach (var action in scenario.PreActions)
        RunShell(action, scenario.WorkingDirectory);

    var stdoutLog = Path.GetTempFileName();
    var stderrLog = Path.GetTempFileName();

    // Start opt-in collectors
    var collectors = new List<IOptInCollector>();
    if (_enableTrace && !isWarmup)
    {
        var wpr = new WprCollector();
        wpr.Start(repDir);
        collectors.Add(wpr);
    }
    if (_enableCounters && !isWarmup)
    {
        var typeperf = new TypeperfCollector();
        typeperf.Start(repDir);
        collectors.Add(typeperf);
    }

    var treeResult = ProcessTreeRunner.Run(
        fileName: scenario.FileName,
        arguments: scenario.Arguments,
        workingDirectory: scenario.WorkingDirectory,
        stdoutLogPath: stdoutLog,
        stderrLogPath: stderrLog,
        timeout: TimeSpan.FromHours(2));

    // Stop opt-in collectors
    foreach (var collector in collectors)
    {
        collector.Stop();
        collector.Dispose();
    }

    // ... build RunResult (same as M1) ...
}
```

## Extending SetupCommand

```csharp
// After M2 tools...

// M3 tools
await new PythonInstaller().EnsureInstalledAsync();
await new NuitkaInstaller().EnsureInstalledAsync();

// M3 repos
RepoCloner.HydrateBlack(benchDir.FullName);
```

## Extending RunCommand

```csharp
// M3 scenarios
var blackDir = Path.Combine(benchDir, "black");
scenarios.AddRange(BlackNuitkaScenario.Create(blackDir));

// M3 API microbench — run after compile scenarios
var tempMicro = Path.Combine(benchDir, "microbench_temp");
microbenchResults.Add(FileOpenCloseBench.Execute(tempMicro, totalOps: 50_000, avName));
microbenchResults.Add(DirEnumerateBench.Execute(rgDir, iterations: 20, avName));
microbenchResults.Add(CopyRenameMoveBench.Execute(tempMicro, totalOps: 5_000, avName));
microbenchResults.Add(ProcessCreateBench.Execute(totalOps: 500, avName));
microbenchResults.Add(RegistryBench.Execute(totalOps: 100_000, avName));
microbenchResults.Add(DllLoadBench.Execute(totalOps: 10_000, avName));
```

## Implementation Steps (ordered)

### Step 1: Build Python and Nuitka installers

Create `PythonInstaller.cs` and `NuitkaInstaller.cs`.

Test on a clean VM:
1. `PythonInstaller.EnsureInstalledAsync()` → `python --version` works
2. `NuitkaInstaller.EnsureInstalledAsync()` → `python -m nuitka --version` works

### Step 2: Build Black repo hydration

Add `HydrateBlack()` to `RepoCloner`. Test:
- venv created, Black installed, `black_entry.py` exists

### Step 3: Build Nuitka scenario definitions

Create `BlackNuitkaScenario.cs`. Test both scenarios standalone:
- `nuitka-standalone` produces `black_entry.dist/black_entry.exe`
- `nuitka-onefile` produces `black_entry.exe`
- Smoke test: compiled binary runs `black --version` successfully

### Step 4: Build remaining API microbench families

Create each bench file. Test individually:
- `file-open-close`: opsPerSec in plausible range (100K+)
- `dir-enumerate`: measure a repo dir (~10K entries), completes in seconds
- `copy-rename-move`: 5K ops completes without leftover temp files
- `process-create-wait`: 500 `cmd.exe` spawns, verify total time
- `registry-open-query`: 100K queries, verify no exceptions
- `dll-load-unload`: 10K loads of urlmon.dll, verify handle cleanup

### Step 5: Build `IOptInCollector` interface

Create the interface in `AvBench.Core/Collectors/`.

### Step 6: Build WPR collector

Create `WprCollector.cs`. Test:
1. Verify `wpr -start GeneralProfile.Verbose` succeeds
2. Run a short workload
3. `wpr -stop trace.etl` saves file
4. File is valid (open in WPA to verify)

Note: WPR requires Windows ADK or is built-in on Windows 10/11. For Server 2022, install ADK separately.

### Step 7: Build typeperf collector

Create `TypeperfCollector.cs`. Test:
1. Start typeperf, wait 5 seconds, stop
2. Verify `counters.csv` has header row + data rows
3. Verify all 6 counter columns present

### Step 8: Integrate collectors into ScenarioRunner

Update `ScenarioRunner` to accept `--trace` and `--counters` flags. Wire collectors into `RunOnce()`.

### Step 9: Wire up extended setup/run commands

Update `SetupCommand.cs` to install Python, Nuitka, and hydrate Black.
Update `RunCommand.cs` to add `--trace` and `--counters` options and register M3 scenarios.

### Step 10: End-to-end test

```powershell
# Full run with opt-in collectors
avbench run --name defender-default --bench-dir C:\bench --output results -n 3 --trace --counters
```

Expected additional output per rep:
```
results/
  <scenario>/
    rep-01/
      run.json
      stdout.log
      stderr.log
      trace.etl        ← from --trace
      counters.csv     ← from --counters
```

## Key Risks and Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Python installer version pinned to 3.12.x | Newer Nuitka may require newer Python | Pin compatible versions in installer constants. Test compatibility. |
| Nuitka standalone build is slow (5-15 min) | Fewer repetitions practical | Use N=3 for Nuitka scenarios, same as LLVM. |
| Nuitka requires MSVC compiler (via VS Build Tools) | Already installed in M2 | Verify Nuitka can find MSVC. Set `CC` env var if needed. |
| WPR not available on Windows Server without ADK | `--trace` fails silently | Print clear warning. Add Windows ADK to optional install table. |
| WPR trace adds disk I/O overhead | Perturbs measurement | Use memory mode (default). Document: `--trace` runs should not be used for timing comparisons, only root-cause analysis. |
| typeperf counter names may differ by Windows version | Missing columns in CSV | Use well-known counters (`\Processor(_Total)\% Processor Time` etc.) that exist on all Windows versions. |
| `urlmon.dll` for DLL load bench may not exist on Server Core | Bench fails | Fallback to `ntdll.dll` or `kernel32.dll` which are always loaded. |
| Legacy Python installer deprecated in 3.14+ | Must use older installer or new Install Manager | Pin Python 3.12.x LTS. For future, add Install Manager support via `winget install 9NQ7512CXL7T`. |

## Testing Strategy

Manual verification:

1. `avbench setup` installs Python + Nuitka on clean VM
2. Black venv created, `black_entry.py` exists
3. `nuitka-standalone` and `nuitka-onefile` produce working binaries
4. All 7 API microbench families produce plausible ops/sec values
5. `--trace` produces valid `.etl` file (can be opened in WPA)
6. `--counters` produces `counters.csv` with 6 counter columns
7. Collectors don't perturb timing by more than ~2% (compare runs with/without `--counters`)
8. Full suite run completes end-to-end with all scenarios active
