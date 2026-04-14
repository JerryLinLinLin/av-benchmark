# Milestone 3 Implementation

## Scope

- Add remaining API microbench families
- Add `--counters` (typeperf) opt-in collector

## Prerequisites from Milestone 1–2

All M1/M2 components are assumed working:

- Full tool installer chain (Git, Rust, VS Build Tools, .NET SDK)
- All compile scenarios (ripgrep, Roslyn)
- Job object runner, output writers
- `avbench-compare` with CSV and markdown output

## New Files

```
AvBench.Core/
  Scenarios/
    FileOpenCloseBench.cs      ← NEW
    DirEnumerateBench.cs       ← NEW
    CopyRenameMoveBench.cs     ← NEW
    ProcessCreateBench.cs      ← NEW
    RegistryBench.cs           ← NEW
    DllLoadBench.cs            ← NEW
  Collectors/
    TypeperfCollector.cs       ← NEW
```

## Remaining API Microbench Families

All microbench families follow the same pattern as `FileMicrobenchScenario` from M1: warmup, fixed-iteration measurement, batch timing. Each is an in-process benchmark wrapped in a `ProcessTreeRunner` Job for consistent measurement.

All microbench timing uses `Stopwatch` (QPC-based, sub-microsecond resolution) for wall time measurements. Job object CPU accounting (~15.625ms granularity) is too coarse for individual operations but the aggregate wall time over thousands of ops yields reliable ops/sec and mean latency figures.

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
var countersOption = new Option<bool>("--counters")
{
    Description = "Enable typeperf performance counter sampling (opt-in)",
    DefaultValueFactory = _ => false
};

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

No additional tool installation needed for M3. Setup is unchanged from M2.

## Extending RunCommand

```csharp
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

### Step 1: Build remaining API microbench families

Create each bench file. Test individually:
- `file-open-close`: opsPerSec in plausible range (100K+)
- `dir-enumerate`: measure a repo dir (~10K entries), completes in seconds
- `copy-rename-move`: 5K ops completes without leftover temp files
- `process-create-wait`: 500 `cmd.exe` spawns, verify total time
- `registry-open-query`: 100K queries, verify no exceptions
- `dll-load-unload`: 10K loads of urlmon.dll, verify handle cleanup

### Step 2: Build `IOptInCollector` interface

Create the interface in `AvBench.Core/Collectors/`.

### Step 3: Build typeperf collector

Create `TypeperfCollector.cs`. Test:
1. Start typeperf, wait 5 seconds, stop
2. Verify `counters.csv` has header row + data rows
3. Verify all 6 counter columns present

### Step 4: Integrate collector into ScenarioRunner

Update `ScenarioRunner` to accept `--counters` flag. Wire collector into `RunOnce()`.

### Step 5: Wire up extended run command

Update `RunCommand.cs` to add `--counters` option and register M3 microbench scenarios.

### Step 6: End-to-end test

```powershell
# Full run with opt-in counter collector
avbench run --name defender-default --bench-dir C:\bench --output results -n 3 --counters
```

Expected additional output per rep:
```
results/
  <scenario>/
    rep-01/
      run.json
      stdout.log
      stderr.log
      counters.csv     ← from --counters
```

## Key Risks and Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| typeperf counter names may differ by Windows version | Missing columns in CSV | Use well-known counters (`\Processor(_Total)\% Processor Time` etc.) that exist on all Windows versions. |
| `urlmon.dll` for DLL load bench may not exist on Server Core | Bench fails | Fallback to `ntdll.dll` or `kernel32.dll` which are always loaded. |

## Testing Strategy

Manual verification:

1. All 7 API microbench families produce plausible ops/sec values
2. `--counters` produces `counters.csv` with 6 counter columns
3. Collector doesn't perturb timing by more than ~2% (compare runs with/without `--counters`)
4. Full suite run completes end-to-end with all scenarios active
