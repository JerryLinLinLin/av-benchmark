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
    LatencyHistogram.cs            ← NEW  (shared percentile helper)
    ArchiveExtractBench.cs         ← NEW
    ProcessCreateBench.cs          ← NEW
    ExtensionSensitivityBench.cs   ← NEW  (replaces RegistryBench)
    DllLoadBench.cs                ← NEW
    FileWriteContentBench.cs       ← NEW
    MotwBench.cs                   ← NEW
  Collectors/
    TypeperfCollector.cs           ← NEW
```

## Remaining API Microbench Families

All microbench families follow the same pattern as `FileMicrobenchScenario` from M1: fixed-iteration measurement, batch timing. No warmup run is performed — every iteration is measured cold, because AV cache priming from a discarded warmup would hide the real overhead that developers experience on first build, package restore, or branch switch. Each is an in-process benchmark wrapped in a `ProcessTreeRunner` Job for consistent measurement.

All microbench timing uses `Stopwatch` (QPC-based, sub-microsecond resolution) for wall time measurements. Job object CPU accounting (~15.625ms granularity) is too coarse for individual operations but the aggregate wall time over thousands of ops yields reliable ops/sec and mean latency figures.

Every bench records per-operation QPC ticks in a pre-allocated `long[]` and computes **p50 / p95 / p99 / max** latency percentiles at the end. This captures AV-induced tail latency that mean alone would hide. The shared `LatencyHistogram` helper handles recording and percentile computation.

### `LatencyHistogram.cs` (shared helper)

```csharp
using System.Diagnostics;

namespace AvBench.Core.Scenarios;

/// <summary>
/// Pre-allocated array for per-op latency recording. Computes percentiles after the run.
/// Overhead: one Stopwatch.GetTimestamp() call per op (~10–20 ns), negligible for μs-scale operations.
/// </summary>
public sealed class LatencyHistogram
{
    private readonly long[] _ticks;
    private int _count;

    public LatencyHistogram(int capacity) => _ticks = new long[capacity];

    /// <summary>Record one operation's elapsed Stopwatch ticks.</summary>
    public void Record(long elapsedTicks) => _ticks[_count++] = elapsedTicks;

    /// <summary>Sort and return p50/p95/p99/max in microseconds.</summary>
    public string Summarize()
    {
        var span = _ticks.AsSpan(0, _count);
        span.Sort();
        double freq = Stopwatch.Frequency;
        P50Us = span[(int)(span.Length * 0.50)] / freq * 1_000_000;
        P95Us = span[(int)(span.Length * 0.95)] / freq * 1_000_000;
        P99Us = span[(int)(span.Length * 0.99)] / freq * 1_000_000;
        MaxUs = span[^1] / freq * 1_000_000;
        return $"p50_us={P50Us:F1} p95_us={P95Us:F1} p99_us={P99Us:F1} max_us={MaxUs:F1}";
    }

    public double P50Us { get; private set; }
    public double P95Us { get; private set; }
    public double P99Us { get; private set; }
    public double MaxUs { get; private set; }
}
```

### `ArchiveExtractBench.cs`

The single highest-impact AV operation for developers. `npm install` extracts 10,000–50,000 files into `node_modules`, `NuGet restore` extracts hundreds of `.nupkg` (zip) packages, `pip install` extracts wheels. ClamAV’s own docs note that on-access scanning during package installation can cause **1000× slowdowns**. Chromium’s build docs explicitly recommend excluding build directories from antivirus. AV-Comparatives’ performance methodology includes archiving/unarchiving as a distinct test category.

This bench extracts a pre-built zip containing ~2,000 heterogeneous files (mixed .cs/.js/.dll/.json/.xml/.exe at varying sizes from 64B to 64KB) into a temp directory, then deletes the tree. Each file triggers `IRP_MJ_CREATE` + `IRP_MJ_WRITE` + content scan + extension-based dispatch — a burst workload that AV can’t cache.

```csharp
using System.Diagnostics;
using System.IO.Compression;
using AvBench.Core.Models;

namespace AvBench.Core.Scenarios;

public static class ArchiveExtractBench
{
    /// <summary>
    /// One-time setup: create a zip archive with ~2,000 heterogeneous files.
    /// File mix simulates a developer package restore (NuGet, npm, pip).
    /// </summary>
    public static string Setup(string tempRoot)
    {
        var stageDir = Path.Combine(tempRoot, "archive_stage");
        Directory.CreateDirectory(stageDir);

        var rng = new Random(42); // deterministic for reproducibility
        string[] extensions = [".cs", ".js", ".json", ".xml", ".dll", ".exe", ".txt", ".md"];
        int[] sizes = [64, 256, 1024, 4096, 16384, 65536];

        for (int i = 0; i < 2000; i++)
        {
            string ext = extensions[i % extensions.Length];
            int size = sizes[i % sizes.Length];
            string subDir = Path.Combine(stageDir, $"pkg_{i / 100}");
            Directory.CreateDirectory(subDir);

            var content = new byte[size];
            rng.NextBytes(content);

            // DLL/EXE files get MZ header to trigger PE content scanning
            if ((ext == ".dll" || ext == ".exe") && size >= 2)
            {
                content[0] = 0x4D; // 'M'
                content[1] = 0x5A; // 'Z'
            }

            File.WriteAllBytes(Path.Combine(subDir, $"file_{i}{ext}"), content);
        }

        var zipPath = Path.Combine(tempRoot, "bench_archive.zip");
        if (File.Exists(zipPath)) File.Delete(zipPath);
        ZipFile.CreateFromDirectory(stageDir, zipPath);

        // Clean up staging dir
        Directory.Delete(stageDir, recursive: true);
        return zipPath;
    }

    public static RunResult Execute(string tempRoot, string zipPath, int iterations, string avName)
    {
        var hist = new LatencyHistogram(iterations);
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var extractDir = Path.Combine(tempRoot, $"archive_run_{i}");
            long t0 = Stopwatch.GetTimestamp();
            ZipFile.ExtractToDirectory(zipPath, extractDir);
            Directory.Delete(extractDir, recursive: true);
            hist.Record(Stopwatch.GetTimestamp() - t0);
        }
        sw.Stop();

        double opsPerSec = iterations / sw.Elapsed.TotalSeconds;
        double meanLatencyMs = sw.Elapsed.TotalMilliseconds / iterations;

        return new RunResult
        {
            ScenarioId = "archive-extract",
            AvName = avName,
            TimestampUtc = DateTime.UtcNow,
            Command = $"archive-extract iters={iterations} files_per_iter=2000 ops_sec={opsPerSec:F2} mean_latency_ms={meanLatencyMs:F0} {hist.Summarize()}",
            WorkingDir = tempRoot,
            ExitCode = 0,
            WallMs = sw.ElapsedMilliseconds,
            P50Us = hist.P50Us,
            P95Us = hist.P95Us,
            P99Us = hist.P99Us,
            MaxUs = hist.MaxUs
        };
    }
}
```

### `ProcessCreateBench.cs`

Spawns an **unsigned** noop.exe instead of `cmd.exe`. AV trust-caches skip scanning for Microsoft-signed binaries; an unsigned exe forces a full on-execute scan every time — revealing the true AV overhead for process creation.

A one-time `Setup()` call compiles a trivial `return 0;` console app via `dotnet build`. The resulting apphost exe is unsigned (no Authenticode signature).

```csharp
using System.Diagnostics;
using AvBench.Core.Models;

namespace AvBench.Core.Scenarios;

public static class ProcessCreateBench
{
    /// <summary>
    /// One-time setup: build a trivial unsigned exe via `dotnet build`.
    /// Returns the path to the built noop.exe.
    /// </summary>
    public static string Setup(string tempRoot)
    {
        var projDir = Path.Combine(tempRoot, "procbench");
        Directory.CreateDirectory(projDir);
        File.WriteAllText(Path.Combine(projDir, "Program.cs"), "return 0;");
        File.WriteAllText(Path.Combine(projDir, "noop.csproj"),
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """);

        var psi = new ProcessStartInfo("dotnet", "build -c Release -o .")
        {
            WorkingDirectory = projDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        using var build = Process.Start(psi)!;
        build.WaitForExit();

        if (build.ExitCode != 0)
            throw new InvalidOperationException("Failed to build unsigned noop.exe for ProcessCreateBench");

        return Path.Combine(projDir, "noop.exe");
    }

    public static RunResult Execute(string unsignedExePath, int totalOps, string avName)
    {
        var hist = new LatencyHistogram(totalOps);
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < totalOps; i++)
        {
            long t0 = Stopwatch.GetTimestamp();
            RunExe(unsignedExePath);
            hist.Record(Stopwatch.GetTimestamp() - t0);
        }
        sw.Stop();

        double opsPerSec = totalOps / sw.Elapsed.TotalSeconds;
        double meanLatencyUs = sw.Elapsed.TotalMicroseconds / totalOps;

        return new RunResult
        {
            ScenarioId = "process-create-wait",
            AvName = avName,
            TimestampUtc = DateTime.UtcNow,
            Command = $"process-create-wait ops={totalOps} unsigned_exe ops_sec={opsPerSec:F0} mean_latency_us={meanLatencyUs:F1} {hist.Summarize()}",
            WorkingDir = Path.GetDirectoryName(unsignedExePath)!,
            ExitCode = 0,
            WallMs = sw.ElapsedMilliseconds,
            P50Us = hist.P50Us,
            P95Us = hist.P95Us,
            P99Us = hist.P99Us,
            MaxUs = hist.MaxUs
        };
    }

    private static void RunExe(string exePath)
    {
        var psi = new ProcessStartInfo(exePath)
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

### `ExtensionSensitivityBench.cs`

Replaces `RegistryBench` (registry monitoring is EDR/HIPS, not standard AV minifilter — low signal for AV benchmarking).

AV products use file extensions for dispatch: `.exe`/`.dll` trigger PE header parsing and signature checks, `.js` triggers script-content heuristic scanning, `.ps1` triggers heuristic scanning for malicious PowerShell patterns. (Note: AMSI integration fires when a script *engine* executes a script, not on file write — this bench measures the minifilter-level extension dispatch cost, not AMSI.) This bench creates+writes+deletes files with **identical random content** across four extensions, isolating the extension-based dispatch cost.

```csharp
using System.Diagnostics;
using AvBench.Core.Models;

namespace AvBench.Core.Scenarios;

/// <summary>
/// Measures AV extension-based dispatch cost: same random content,
/// different file extensions (.exe, .dll, .js, .ps1).
/// </summary>
public static class ExtensionSensitivityBench
{
    public static RunResult Execute(string tempRoot, int opsPerExtension, string ext, string avName)
    {
        Directory.CreateDirectory(tempRoot);
        var content = new byte[4096]; // 4KB — above typical AV content-scan threshold
        Random.Shared.NextBytes(content);

        var hist = new LatencyHistogram(opsPerExtension);
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < opsPerExtension; i++)
        {
            var p = Path.Combine(tempRoot, $"bench_{i}{ext}");
            long t0 = Stopwatch.GetTimestamp();
            File.WriteAllBytes(p, content);
            File.Delete(p);
            hist.Record(Stopwatch.GetTimestamp() - t0);
        }
        sw.Stop();

        double opsPerSec = opsPerExtension / sw.Elapsed.TotalSeconds;
        double meanLatencyUs = sw.Elapsed.TotalMicroseconds / opsPerExtension;

        return new RunResult
        {
            ScenarioId = $"ext-sensitivity-{ext.TrimStart('.')}",
            AvName = avName,
            TimestampUtc = DateTime.UtcNow,
            Command = $"ext-sensitivity ext={ext} ops={opsPerExtension} ops_sec={opsPerSec:F0} mean_latency_us={meanLatencyUs:F1} {hist.Summarize()}",
            WorkingDir = tempRoot,
            ExitCode = 0,
            WallMs = sw.ElapsedMilliseconds,
            P50Us = hist.P50Us,
            P95Us = hist.P95Us,
            P99Us = hist.P99Us,
            MaxUs = hist.MaxUs
        };
    }
}
```

### `DllLoadBench.cs`

Copies a system DLL to a unique temp path each iteration, then loads and unloads it. This bypasses the Windows section cache, forcing AV to scan each copy as a "new" DLL — one of the strongest AV signals for developer workloads that generate fresh binaries.

The cached variant (`dll-load-cached`) was removed because it only measures the OS loader fast-path after AV has already cached the verdict — no unique AV signal.

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

    /// <summary>
    /// Copy a system DLL to a unique temp path each iteration, then load/unload.
    /// Forces AV to scan each copy (no section-cache hit).
    /// </summary>
    public static RunResult Execute(string tempRoot, int totalOps, string avName)
    {
        Directory.CreateDirectory(tempRoot);
        var systemDir = Environment.GetFolderPath(Environment.SpecialFolder.System);
        var sourceDll = Path.Combine(systemDir, "urlmon.dll");

        var hist = new LatencyHistogram(totalOps);
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < totalOps; i++)
        {
            var uniquePath = Path.Combine(tempRoot, $"bench_{i}.dll");

            long t0 = Stopwatch.GetTimestamp();
            File.Copy(sourceDll, uniquePath, overwrite: true);
            LoadUnload(uniquePath);
            File.Delete(uniquePath);
            hist.Record(Stopwatch.GetTimestamp() - t0);
        }
        sw.Stop();

        double opsPerSec = totalOps / sw.Elapsed.TotalSeconds;
        double meanLatencyUs = sw.Elapsed.TotalMicroseconds / totalOps;

        return new RunResult
        {
            ScenarioId = "dll-load-unique",
            AvName = avName,
            TimestampUtc = DateTime.UtcNow,
            Command = $"dll-load-unique ops={totalOps} ops_sec={opsPerSec:F0} mean_latency_us={meanLatencyUs:F1} {hist.Summarize()}",
            WorkingDir = tempRoot,
            ExitCode = 0,
            WallMs = sw.ElapsedMilliseconds,
            P50Us = hist.P50Us,
            P95Us = hist.P95Us,
            P99Us = hist.P99Us,
            MaxUs = hist.MaxUs
        };
    }

    private static void LoadUnload(string dllPath)
    {
        var handle = LoadLibrary(dllPath);
        if (handle != IntPtr.Zero)
            FreeLibrary(handle);
    }
}
```

### `FileWriteContentBench.cs`

Measures AV **content-inspection cost** by writing **real, unique-hash unsigned PE files** and deleting them. Each iteration clones the unsigned `noop.exe` template (read once at setup) and patches 4 bytes in the DOS stub padding area (offsets `0x40`–`0x43`) with the loop counter. This produces a structurally valid PE with a **unique file hash on every single write** — the AV engine cannot cache a verdict and must perform full PE inspection (header validation, section enumeration, hash computation, unsigned-signature check) every time. The ~1 μs cost of a 65 KB `Buffer.BlockCopy` + 4-byte patch is <1% of the AV file-write overhead (typically 100–10,000 μs), so the measurement noise is negligible.

Alternates `.exe` and `.dll` extensions across iterations to exercise both PE extension paths.

```csharp
using System.Diagnostics;
using AvBench.Core.Models;

namespace AvBench.Core.Scenarios;

/// <summary>
/// Create→write→close→delete with real unsigned PE content.
/// Clones + patches 4 bytes in-loop so every iteration writes a unique-hash
/// unsigned PE that the AV engine has never seen. Alternates .exe/.dll extensions.
/// </summary>
public static class FileWriteContentBench
{
    public static RunResult Execute(
        string tempRoot, string noopExePath, int totalOps, string avName)
    {
        Directory.CreateDirectory(tempRoot);
        var template = File.ReadAllBytes(noopExePath);
        var content = new byte[template.Length];
        string[] extensions = [".exe", ".dll"];

        var hist = new LatencyHistogram(totalOps);
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < totalOps; i++)
        {
            // Clone template and patch 4 bytes → unique hash every iteration
            Buffer.BlockCopy(template, 0, content, 0, template.Length);
            content[0x40] = (byte)(i);
            content[0x41] = (byte)(i >> 8);
            content[0x42] = (byte)(i >> 16);
            content[0x43] = (byte)(i >> 24);

            var ext = extensions[i % extensions.Length];
            var p = Path.Combine(tempRoot, $"bench_{i}{ext}");
            long t0 = Stopwatch.GetTimestamp();
            File.WriteAllBytes(p, content);
            File.Delete(p);
            hist.Record(Stopwatch.GetTimestamp() - t0);
        }
        sw.Stop();

        double opsPerSec = totalOps / sw.Elapsed.TotalSeconds;
        double meanLatencyUs = sw.Elapsed.TotalMicroseconds / totalOps;

        return new RunResult
        {
            ScenarioId = "file-write-pe",
            AvName = avName,
            TimestampUtc = DateTime.UtcNow,
            Command = $"file-write-pe ops={totalOps} pe_size={template.Length} ops_sec={opsPerSec:F0} mean_latency_us={meanLatencyUs:F1} {hist.Summarize()}",
            WorkingDir = tempRoot,
            ExitCode = 0,
            WallMs = sw.ElapsedMilliseconds,
            P50Us = hist.P50Us,
            P95Us = hist.P95Us,
            P99Us = hist.P99Us,
            MaxUs = hist.MaxUs
        };
    }
}
```

### `MotwBench.cs`

Measures the AV cost of **Mark of the Web** (`Zone.Identifier` NTFS alternate data stream) on **actual process execution**. The real MOTW escalation — SmartScreen reputation lookup, cloud query, AMSI scan, signature verification — fires when you **execute** a MOTW-tagged binary, not merely when you create it. Writing a fake `.exe` only tests the minifilter file-create path, which ExtensionSensitivityBench already covers.

This bench reuses the real unsigned `noop.exe` from `ProcessCreateBench.Setup()`. Each iteration copies it to a unique temp path, optionally stamps `Zone.Identifier` ADS (`ZoneId=3`, Internet), then **launches and waits for it**. The delta between no-motw and motw-zone3 isolates the full AV execution-time overhead for downloaded binaries — exactly what happens when developers run tools pulled from CI, NuGet native packages, or GitHub Releases.

```csharp
using System.Diagnostics;
using AvBench.Core.Models;

namespace AvBench.Core.Scenarios;

/// <summary>
/// Measures AV overhead from executing a MOTW-tagged binary.
/// Two variants:
///   no-motw  — copy + execute unsigned noop.exe (locally-built binary)
///   motw-zone3 — copy + stamp Zone.Identifier ZoneId=3 + execute (downloaded binary)
/// The exe is real (built by ProcessCreateBench.Setup), not a fake MZ stub.
/// </summary>
public static class MotwBench
{
    /// <param name="unsignedExePath">Path to the real unsigned noop.exe built by ProcessCreateBench.Setup().</param>
    public static RunResult Execute(
        string tempRoot, string unsignedExePath, int totalOps, bool applyMotw, string avName)
    {
        Directory.CreateDirectory(tempRoot);
        string variant = applyMotw ? "motw-zone3" : "no-motw";

        var hist = new LatencyHistogram(totalOps);
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < totalOps; i++)
        {
            var copyPath = Path.Combine(tempRoot, $"bench_{i}.exe");
            long t0 = Stopwatch.GetTimestamp();
            CopyAndRun(unsignedExePath, copyPath, applyMotw);
            hist.Record(Stopwatch.GetTimestamp() - t0);
        }
        sw.Stop();

        double opsPerSec = totalOps / sw.Elapsed.TotalSeconds;
        double meanLatencyUs = sw.Elapsed.TotalMicroseconds / totalOps;

        return new RunResult
        {
            ScenarioId = $"motw-exe-{variant}",
            AvName = avName,
            TimestampUtc = DateTime.UtcNow,
            Command = $"motw-exe variant={variant} ops={totalOps} ops_sec={opsPerSec:F0} mean_latency_us={meanLatencyUs:F1} {hist.Summarize()}",
            WorkingDir = tempRoot,
            ExitCode = 0,
            WallMs = sw.ElapsedMilliseconds,
            P50Us = hist.P50Us,
            P95Us = hist.P95Us,
            P99Us = hist.P99Us,
            MaxUs = hist.MaxUs
        };
    }

    private static void CopyAndRun(string srcExe, string destExe, bool applyMotw)
    {
        File.Copy(srcExe, destExe, overwrite: true);

        if (applyMotw)
        {
            File.WriteAllText(
                destExe + ":Zone.Identifier",
                "[ZoneTransfer]\r\nZoneId=3\r\n");
        }

        var psi = new ProcessStartInfo(destExe)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true
        };
        using var proc = Process.Start(psi)!;
        proc.StandardOutput.ReadToEnd();
        proc.WaitForExit();

        File.Delete(destExe);
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
private RunResult RunOnce(ScenarioDefinition scenario, string repDir)
{
    foreach (var action in scenario.PreActions)
        RunShell(action, scenario.WorkingDirectory);

    var stdoutLog = Path.GetTempFileName();
    var stderrLog = Path.GetTempFileName();

    // Start opt-in collectors
    var collectors = new List<IOptInCollector>();
    if (_enableCounters)
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

// Archive extraction (highest-impact AV operation for developers)
var benchZip = ArchiveExtractBench.Setup(tempMicro);
microbenchResults.Add(ArchiveExtractBench.Execute(tempMicro, benchZip, iterations: 10, avName));

// Process creation with unsigned exe (forces full AV scan)
var unsignedExe = ProcessCreateBench.Setup(tempMicro);
microbenchResults.Add(ProcessCreateBench.Execute(unsignedExe, totalOps: 500, avName));

// Extension sensitivity — same content, different extensions
foreach (string ext in new[] { ".exe", ".dll", ".js", ".ps1" })
    microbenchResults.Add(ExtensionSensitivityBench.Execute(tempMicro, opsPerExtension: 10_000, ext, avName));

// DLL load: copy to unique temp path each iteration (bypasses section cache)
microbenchResults.Add(DllLoadBench.Execute(tempMicro, totalOps: 2_000, avName));

// File-write content: in-loop clone+patch of noop.exe → unique-hash PE every iteration
microbenchResults.Add(FileWriteContentBench.Execute(tempMicro, unsignedExe, totalOps: 10_000, avName));

// MOTW (Mark of the Web): copy + execute real unsigned exe, no-motw vs Zone.Identifier ZoneId=3
// Reuses the same unsigned noop.exe built above by ProcessCreateBench.Setup()
microbenchResults.Add(MotwBench.Execute(tempMicro, unsignedExe, totalOps: 500, applyMotw: false, avName));
microbenchResults.Add(MotwBench.Execute(tempMicro, unsignedExe, totalOps: 500, applyMotw: true, avName));
```

## Implementation Steps (ordered)

### Step 1: Build LatencyHistogram helper and remaining API microbench families

Create `LatencyHistogram.cs` (shared percentile helper), then each bench file. Test individually:
- `archive-extract`: setup creates ~2K-file zip, 10 extract iterations, verify all files extracted, verify cleanup
- `process-create-wait`: build unsigned noop.exe via `dotnet build`, 500 spawns, verify total time
- `ext-sensitivity`: 10K ops per extension (.exe/.dll/.js/.ps1), verify measurable latency difference across extensions
- `dll-load-unique`: 2K loads of copied DLL with unique paths, verify no leaked temp files
- `file-write-content`: 10K ops, each writes a unique-hash unsigned PE (clone+patch noop.exe in-loop), alternating .exe/.dll, verify each file deleted
- `motw`: 500 ops no-motw vs motw-zone3, copy+execute real unsigned noop.exe, verify NTFS ADS written correctly, verify measurable latency difference

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
| `dotnet build` for unsigned noop.exe fails or is slow | ProcessCreateBench setup fails | Detect failure early, skip bench with warning. Build happens once per run, not per-op. |
| Archive zip creation fills disk | Setup creates ~50MB zip + ~100MB staging dir | Stage dir is deleted after zip creation. Total temp space ~150MB. |
| `.exe` extension files trigger Windows SmartScreen prompts | ExtensionSensitivity bench hangs | ExtensionSensitivity uses `File.WriteAllBytes` (not `Process.Start`), so SmartScreen is not invoked. MotwBench uses `UseShellExecute = false` which bypasses Explorer-level SmartScreen UI. |
| MotwBench execution slower than expected | 500 ops × process spawn is several minutes | 500 ops is intentionally lower than other benches. Copy+execute+delete per op is ~10–100ms, so total is ~5–50s. |
| Zone.Identifier ADS not supported on non-NTFS volumes | MotwBench fails silently | Bench temp dir is always on the system drive (NTFS). Log a warning if ADS write throws. |

## Testing Strategy

Manual verification:

1. All API microbench families produce plausible ops/sec values and p50/p95/p99/max latency percentiles
2. Archive extraction shows measurable slowdown with AV enabled; ~2K files extracted per iteration with mixed extensions and sizes
3. Extension sensitivity shows measurable latency difference between `.exe`/`.dll` (PE dispatch) and `.js`/`.ps1` (script dispatch)
4. `dll-load-unique` latency is measurably higher than baseline filesystem copy (AV scans each fresh DLL)
5. `file-write-content` with real unique-hash unsigned PEs shows higher latency than same-size random-content file creation (AV performs full PE inspection on every write)
6. ProcessCreateBench unsigned exe produces higher latency than a cached signed-exe baseline
7. MOTW bench: `motw-exe-motw-zone3` shows higher latency than `motw-exe-no-motw`; the real unsigned exe actually executes (exit code 0) in both variants
8. `--counters` produces `counters.csv` with 6 counter columns
9. Collector doesn't perturb timing by more than ~2% (compare runs with/without `--counters`)
10. Full suite run completes end-to-end with all scenarios active
