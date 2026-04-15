# Milestone 3 Implementation

## Scope

- Add all API microbench families (23 benches across 9 tiers / 11 categories)
- Integrate `TypeperfCollector` (always-on system counter sampling)

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
    ArchiveExtractBench.cs         ← NEW  (Tier 1 — File I/O)
    FileEnumLargeDirBench.cs       ← NEW  (Tier 1 — File I/O)
    FileCopyLargeBench.cs          ← NEW  (Tier 1 — File I/O)
    HardlinkJunctionBench.cs       ← NEW  (Tier 1 — File I/O)
    ProcessCreateBench.cs          ← NEW  (Tier 2 — Process)
    ExtensionSensitivityBench.cs   ← NEW  (Tier 1 — File I/O)
    DllLoadBench.cs                ← NEW  (Tier 2 — DLL/Image)
    FileWriteContentBench.cs       ← NEW  (Tier 1 — File I/O)
    MotwBench.cs                   ← NEW  (Tier 2 — MOTW)
    ThreadCreateBench.cs           ← NEW  (Tier 2 — Thread)
    MemAllocProtectBench.cs        ← NEW  (Tier 3 — Memory)
    MemMapFileBench.cs             ← NEW  (Tier 3 — Memory)
    NetConnectLoopbackBench.cs     ← NEW  (Tier 4 — Network)
    DnsResolveBench.cs             ← NEW  (Tier 4 — Network)
    RegistryCrudBench.cs           ← NEW  (Tier 5 — Registry)
    PipeRoundtripBench.cs          ← NEW  (Tier 6 — IPC)
    TokenQueryBench.cs             ← NEW  (Tier 7 — Security)
    CryptoHashVerifyBench.cs       ← NEW  (Tier 7 — Crypto)
    ComActivationBench.cs          ← NEW  (Tier 8 — COM)
    WmiQueryBench.cs               ← NEW  (Tier 8 — WMI)
    FsWatcherBench.cs              ← NEW  (Tier 9 — FS notify)
  Collectors/
    TypeperfCollector.cs           ← NEW
```

## All API Microbench Families

All microbench families follow the same pattern as `FileMicrobenchScenario` from M1: fixed-iteration measurement, batch timing. No warmup run is performed — every iteration is measured cold, because AV cache priming from a discarded warmup would hide the real overhead that developers experience on first build, package restore, or branch switch. Each is an in-process benchmark wrapped in a `ProcessTreeRunner` Job for consistent measurement.

All microbench timing uses `Stopwatch` (QPC-based, sub-microsecond resolution) for wall time measurements. Job object CPU accounting (~15.625ms granularity) is too coarse for individual operations but the aggregate wall time over thousands of ops yields reliable ops/sec and mean latency figures.

Every bench records per-operation QPC ticks in a pre-allocated `long[]` and computes **p50 / p95 / p99 / max** latency percentiles at the end. This captures AV-induced tail latency that mean alone would hide. The shared `LatencyHistogram` helper handles recording and percentile computation.

The bench list covers 11 categories of commonly-used Windows APIs — file system operations, process/thread management, memory mapping, networking, registry, IPC, security tokens, cryptography, COM, WMI, and file-system notifications. Some of these APIs are known to be sensitive to security-software monitoring ([ref](https://github.com/Mr-Un1k0d3r/EDRs)), but the primary selection criterion is how commonly Windows applications use them, not which ones are intercepted by any particular product.

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

Security software uses file extensions for dispatch: `.exe`/`.dll` trigger PE header parsing and signature checks, `.js` triggers script-content heuristic scanning, `.ps1` triggers heuristic scanning for malicious patterns. This bench creates+writes+deletes files with **identical random content** across four extensions, isolating the extension-based dispatch cost.

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

Measures the AV cost of **Mark of the Web** (`Zone.Identifier` NTFS alternate data stream) on **actual process execution**. The real MOTW escalation — SmartScreen reputation lookup, cloud query, signature verification — fires when you **execute** a MOTW-tagged binary, not merely when you create it. Writing a fake `.exe` only tests the minifilter file-create path, which ExtensionSensitivityBench already covers.

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

---

### Tier 1 additions — File I/O

### `FileEnumLargeDirBench.cs`

Enumerates a pre-created directory with ~10K files. Exercises `NtQueryDirectoryFile` through the minifilter. IDE file indexing, `git status`, `Find-ChildItem`, and `dir /s` all hit this path constantly. The minifilter sees every directory enumeration request and can add latency per-entry or per-batch.

```csharp
using System.Diagnostics;
using AvBench.Core.Models;

namespace AvBench.Core.Scenarios;

public static class FileEnumLargeDirBench
{
    /// <summary>
    /// One-time setup: create a directory with ~10K small files (mixed extensions).
    /// </summary>
    public static string Setup(string tempRoot)
    {
        var dir = Path.Combine(tempRoot, "enum_bench");
        Directory.CreateDirectory(dir);

        string[] extensions = [".cs", ".js", ".json", ".xml", ".dll", ".exe", ".txt", ".md", ".config", ".props"];
        var rng = new Random(42);
        var content = new byte[256];

        for (int i = 0; i < 10_000; i++)
        {
            rng.NextBytes(content);
            var ext = extensions[i % extensions.Length];
            File.WriteAllBytes(Path.Combine(dir, $"file_{i:D5}{ext}"), content);
        }
        return dir;
    }

    public static RunResult Execute(string enumDir, int iterations, string avName)
    {
        var hist = new LatencyHistogram(iterations);
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            long t0 = Stopwatch.GetTimestamp();
            int count = 0;
            foreach (var _ in Directory.EnumerateFiles(enumDir))
                count++;
            hist.Record(Stopwatch.GetTimestamp() - t0);
        }
        sw.Stop();

        double opsPerSec = iterations / sw.Elapsed.TotalSeconds;
        double meanLatencyMs = sw.Elapsed.TotalMilliseconds / iterations;

        return new RunResult
        {
            ScenarioId = "file-enum-large-dir",
            AvName = avName,
            TimestampUtc = DateTime.UtcNow,
            Command = $"file-enum-large-dir iters={iterations} files=10000 ops_sec={opsPerSec:F2} mean_latency_ms={meanLatencyMs:F1} {hist.Summarize()}",
            WorkingDir = enumDir,
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

### `FileCopyLargeBench.cs`

Copies a single ~100 MB file. Measures sustained minifilter read+write overhead on bulk data transfer. Build tools, package managers, and CI pipelines frequently copy large artifacts (`.nupkg`, `.tar.gz`, build outputs). The minifilter scans both the source read and destination write sides.

```csharp
using System.Diagnostics;
using AvBench.Core.Models;

namespace AvBench.Core.Scenarios;

public static class FileCopyLargeBench
{
    /// <summary>
    /// One-time setup: create a ~100MB file with random content.
    /// </summary>
    public static string Setup(string tempRoot)
    {
        var srcPath = Path.Combine(tempRoot, "large_file_src.bin");
        var rng = new Random(42);
        var buffer = new byte[1024 * 1024]; // 1 MB chunks
        using var fs = File.Create(srcPath);
        for (int i = 0; i < 100; i++)
        {
            rng.NextBytes(buffer);
            fs.Write(buffer);
        }
        return srcPath;
    }

    public static RunResult Execute(string tempRoot, string srcPath, int iterations, string avName)
    {
        var hist = new LatencyHistogram(iterations);
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            var destPath = Path.Combine(tempRoot, $"large_copy_{i}.bin");
            long t0 = Stopwatch.GetTimestamp();
            File.Copy(srcPath, destPath, overwrite: true);
            File.Delete(destPath);
            hist.Record(Stopwatch.GetTimestamp() - t0);
        }
        sw.Stop();

        double opsPerSec = iterations / sw.Elapsed.TotalSeconds;
        double meanLatencyMs = sw.Elapsed.TotalMilliseconds / iterations;

        return new RunResult
        {
            ScenarioId = "file-copy-large",
            AvName = avName,
            TimestampUtc = DateTime.UtcNow,
            Command = $"file-copy-large iters={iterations} size_mb=100 ops_sec={opsPerSec:F2} mean_latency_ms={meanLatencyMs:F1} {hist.Summarize()}",
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

### `HardlinkJunctionBench.cs`

Creates hard links and directory junctions in a loop. npm/pnpm use hard links for package deduplication across projects. Junctions (reparse points) are used for `node_modules` hoisting and monorepo workspace linking. Each operation transits the minifilter via `NtSetInformationFile(FileLinkInformation)` for hard links and `DeviceIoControl(FSCTL_SET_REPARSE_POINT)` for junctions.

```csharp
using System.Diagnostics;
using System.Runtime.InteropServices;
using AvBench.Core.Models;

namespace AvBench.Core.Scenarios;

public static class HardlinkJunctionBench
{
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);

    private const int SYMBOLIC_LINK_FLAG_DIRECTORY = 0x1;

    /// <summary>Hard link bench: create hard links to a source file, then delete them.</summary>
    public static RunResult ExecuteHardlinks(string tempRoot, int totalOps, string avName)
    {
        Directory.CreateDirectory(tempRoot);
        var srcFile = Path.Combine(tempRoot, "hardlink_source.dat");
        File.WriteAllBytes(srcFile, new byte[4096]);

        var hist = new LatencyHistogram(totalOps);
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < totalOps; i++)
        {
            var linkPath = Path.Combine(tempRoot, $"hlink_{i}.dat");
            long t0 = Stopwatch.GetTimestamp();
            CreateHardLink(linkPath, srcFile, IntPtr.Zero);
            File.Delete(linkPath);
            hist.Record(Stopwatch.GetTimestamp() - t0);
        }
        sw.Stop();

        File.Delete(srcFile);
        double opsPerSec = totalOps / sw.Elapsed.TotalSeconds;
        double meanLatencyUs = sw.Elapsed.TotalMicroseconds / totalOps;

        return new RunResult
        {
            ScenarioId = "hardlink-create",
            AvName = avName,
            TimestampUtc = DateTime.UtcNow,
            Command = $"hardlink-create ops={totalOps} ops_sec={opsPerSec:F0} mean_latency_us={meanLatencyUs:F1} {hist.Summarize()}",
            WorkingDir = tempRoot,
            ExitCode = 0,
            WallMs = sw.ElapsedMilliseconds,
            P50Us = hist.P50Us,
            P95Us = hist.P95Us,
            P99Us = hist.P99Us,
            MaxUs = hist.MaxUs
        };
    }

    /// <summary>Junction bench: create directory junctions, then remove them.</summary>
    public static RunResult ExecuteJunctions(string tempRoot, int totalOps, string avName)
    {
        Directory.CreateDirectory(tempRoot);
        var targetDir = Path.Combine(tempRoot, "junction_target");
        Directory.CreateDirectory(targetDir);

        var hist = new LatencyHistogram(totalOps);
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < totalOps; i++)
        {
            var junctionPath = Path.Combine(tempRoot, $"junc_{i}");
            long t0 = Stopwatch.GetTimestamp();
            CreateSymbolicLink(junctionPath, targetDir, SYMBOLIC_LINK_FLAG_DIRECTORY);
            Directory.Delete(junctionPath);
            hist.Record(Stopwatch.GetTimestamp() - t0);
        }
        sw.Stop();

        Directory.Delete(targetDir);
        double opsPerSec = totalOps / sw.Elapsed.TotalSeconds;
        double meanLatencyUs = sw.Elapsed.TotalMicroseconds / totalOps;

        return new RunResult
        {
            ScenarioId = "junction-create",
            AvName = avName,
            TimestampUtc = DateTime.UtcNow,
            Command = $"junction-create ops={totalOps} ops_sec={opsPerSec:F0} mean_latency_us={meanLatencyUs:F1} {hist.Summarize()}",
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

---

### Tier 2 addition — Thread

### `ThreadCreateBench.cs`

Rapid thread create → start → join cycle. Exercises `NtCreateThreadEx` + the kernel `PsSetCreateThreadNotifyRoutine` callback. Every multithreaded application — compilers, servers, IDEs, databases — creates threads. The .NET runtime itself creates thread pool threads via `NtCreateThreadEx`.

```csharp
using System.Diagnostics;
using AvBench.Core.Models;

namespace AvBench.Core.Scenarios;

public static class ThreadCreateBench
{
    public static RunResult Execute(int totalOps, string avName)
    {
        var hist = new LatencyHistogram(totalOps);
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < totalOps; i++)
        {
            long t0 = Stopwatch.GetTimestamp();
            var t = new Thread(() => { }) { IsBackground = true };
            t.Start();
            t.Join();
            hist.Record(Stopwatch.GetTimestamp() - t0);
        }
        sw.Stop();

        double opsPerSec = totalOps / sw.Elapsed.TotalSeconds;
        double meanLatencyUs = sw.Elapsed.TotalMicroseconds / totalOps;

        return new RunResult
        {
            ScenarioId = "thread-create",
            AvName = avName,
            TimestampUtc = DateTime.UtcNow,
            Command = $"thread-create ops={totalOps} ops_sec={opsPerSec:F0} mean_latency_us={meanLatencyUs:F1} {hist.Summarize()}",
            WorkingDir = Environment.CurrentDirectory,
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

---

### Tier 3 — Memory

### `MemAllocProtectBench.cs`

`VirtualAlloc(MEM_COMMIT|MEM_RESERVE, PAGE_READWRITE)` → `VirtualProtect(PAGE_EXECUTE_READ)` → `VirtualFree` loop. The RW→RX transition is a well-known sensitive pattern for security software. The .NET JIT, V8 JIT, and any code-generation tool do this on every method compilation. ETW Threat Intelligence provider also fires on RX protection changes.

```csharp
using System.Diagnostics;
using System.Runtime.InteropServices;
using AvBench.Core.Models;

namespace AvBench.Core.Scenarios;

public static class MemAllocProtectBench
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr VirtualAlloc(IntPtr lpAddress, nuint dwSize, uint flAllocationType, uint flProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool VirtualProtect(IntPtr lpAddress, nuint dwSize, uint flNewProtect, out uint lpflOldProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool VirtualFree(IntPtr lpAddress, nuint dwSize, uint dwFreeType);

    private const uint MEM_COMMIT = 0x1000;
    private const uint MEM_RESERVE = 0x2000;
    private const uint MEM_RELEASE = 0x8000;
    private const uint PAGE_READWRITE = 0x04;
    private const uint PAGE_EXECUTE_READ = 0x20;

    public static RunResult Execute(int totalOps, string avName)
    {
        const nuint allocSize = 4096; // one page
        var hist = new LatencyHistogram(totalOps);
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < totalOps; i++)
        {
            long t0 = Stopwatch.GetTimestamp();
            var ptr = VirtualAlloc(IntPtr.Zero, allocSize, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
            VirtualProtect(ptr, allocSize, PAGE_EXECUTE_READ, out _);
            VirtualFree(ptr, 0, MEM_RELEASE);
            hist.Record(Stopwatch.GetTimestamp() - t0);
        }
        sw.Stop();

        double opsPerSec = totalOps / sw.Elapsed.TotalSeconds;
        double meanLatencyUs = sw.Elapsed.TotalMicroseconds / totalOps;

        return new RunResult
        {
            ScenarioId = "mem-alloc-protect",
            AvName = avName,
            TimestampUtc = DateTime.UtcNow,
            Command = $"mem-alloc-protect ops={totalOps} page_size=4096 rw_to_rx ops_sec={opsPerSec:F0} mean_latency_us={meanLatencyUs:F1} {hist.Summarize()}",
            WorkingDir = Environment.CurrentDirectory,
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

### `MemMapFileBench.cs`

`CreateFileMapping` → `MapViewOfFile` → `UnmapViewOfFile` loop. Exercises `NtMapViewOfSection` — one of the most widely monitored APIs by security software. This is how DLLs are loaded, PE images are mapped, and memory-mapped files work. Databases (SQLite), package managers, and applications that use shared memory all use file mappings.

```csharp
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using AvBench.Core.Models;

namespace AvBench.Core.Scenarios;

public static class MemMapFileBench
{
    public static RunResult Execute(string tempRoot, int totalOps, string avName)
    {
        Directory.CreateDirectory(tempRoot);
        var filePath = Path.Combine(tempRoot, "mmap_bench.dat");
        var data = new byte[65536]; // 64KB
        Random.Shared.NextBytes(data);
        File.WriteAllBytes(filePath, data);

        var hist = new LatencyHistogram(totalOps);
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < totalOps; i++)
        {
            long t0 = Stopwatch.GetTimestamp();
            using var mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
            using var accessor = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            accessor.ReadByte(0); // realize the mapping
            hist.Record(Stopwatch.GetTimestamp() - t0);
        }
        sw.Stop();

        File.Delete(filePath);
        double opsPerSec = totalOps / sw.Elapsed.TotalSeconds;
        double meanLatencyUs = sw.Elapsed.TotalMicroseconds / totalOps;

        return new RunResult
        {
            ScenarioId = "mem-map-file",
            AvName = avName,
            TimestampUtc = DateTime.UtcNow,
            Command = $"mem-map-file ops={totalOps} file_size=65536 ops_sec={opsPerSec:F0} mean_latency_us={meanLatencyUs:F1} {hist.Summarize()}",
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

---

### Tier 4 — Network

### `NetConnectLoopbackBench.cs`

TCP connect → send 1 KB → recv → close against a local echo server. Each connection triggers the WFP `ALE_AUTH_CONNECT` callout that security software with network inspection registers. This is the hot path for every HTTP request, package download, API call, and network communication.

```csharp
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using AvBench.Core.Models;

namespace AvBench.Core.Scenarios;

public static class NetConnectLoopbackBench
{
    public static RunResult Execute(int totalOps, string avName)
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var payload = new byte[1024]; // 1 KB
        Random.Shared.NextBytes(payload);
        var recvBuf = new byte[1024];

        var cts = new CancellationTokenSource();
        var serverTask = Task.Run(() =>
        {
            while (!cts.IsCancellationRequested)
            {
                try
                {
                    if (!listener.Pending()) { Thread.Sleep(1); continue; }
                    using var client = listener.AcceptTcpClient();
                    using var stream = client.GetStream();
                    int read = stream.Read(recvBuf, 0, recvBuf.Length);
                    if (read > 0) stream.Write(recvBuf, 0, read);
                }
                catch (SocketException) { }
            }
        });

        var hist = new LatencyHistogram(totalOps);
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < totalOps; i++)
        {
            long t0 = Stopwatch.GetTimestamp();
            using var tcp = new TcpClient();
            tcp.Connect(IPAddress.Loopback, port);
            using var ns = tcp.GetStream();
            ns.Write(payload, 0, payload.Length);
            ns.Read(recvBuf, 0, recvBuf.Length);
            hist.Record(Stopwatch.GetTimestamp() - t0);
        }
        sw.Stop();

        cts.Cancel();
        listener.Stop();
        serverTask.Wait(TimeSpan.FromSeconds(3));

        double opsPerSec = totalOps / sw.Elapsed.TotalSeconds;
        double meanLatencyUs = sw.Elapsed.TotalMicroseconds / totalOps;

        return new RunResult
        {
            ScenarioId = "net-connect-loopback",
            AvName = avName,
            TimestampUtc = DateTime.UtcNow,
            Command = $"net-connect-loopback ops={totalOps} payload=1024 ops_sec={opsPerSec:F0} mean_latency_us={meanLatencyUs:F1} {hist.Summarize()}",
            WorkingDir = Environment.CurrentDirectory,
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

### `DnsResolveBench.cs`

`Dns.GetHostEntry` loop. Security software with DNS filtering inspects queries for domain blocking. Even without DNS filtering, network-inspection software intercepts at the WFP layer which DNS traverses. Every application that connects to remote services triggers DNS resolution.

```csharp
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using AvBench.Core.Models;

namespace AvBench.Core.Scenarios;

public static class DnsResolveBench
{
    public static RunResult Execute(int totalOps, string avName)
    {
        var hist = new LatencyHistogram(totalOps);
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < totalOps; i++)
        {
            long t0 = Stopwatch.GetTimestamp();
            try { Dns.GetHostEntry("localhost"); }
            catch (SocketException) { }
            hist.Record(Stopwatch.GetTimestamp() - t0);
        }
        sw.Stop();

        double opsPerSec = totalOps / sw.Elapsed.TotalSeconds;
        double meanLatencyUs = sw.Elapsed.TotalMicroseconds / totalOps;

        return new RunResult
        {
            ScenarioId = "net-dns-resolve",
            AvName = avName,
            TimestampUtc = DateTime.UtcNow,
            Command = $"net-dns-resolve ops={totalOps} target=localhost ops_sec={opsPerSec:F0} mean_latency_us={meanLatencyUs:F1} {hist.Summarize()}",
            WorkingDir = Environment.CurrentDirectory,
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

---

### Tier 5 — Registry

### `RegistryCrudBench.cs`

Create key → set 5 values (REG_SZ, REG_DWORD, REG_BINARY, REG_MULTI_SZ, REG_EXPAND_SZ) → query each → enumerate → delete. All operations go through `CmRegisterCallbackEx` kernel callbacks. Security software monitors registry operations for persistence and configuration changes. Installers, application settings, COM lookups, and system tools perform heavy registry I/O.

```csharp
using System.Diagnostics;
using Microsoft.Win32;
using AvBench.Core.Models;

namespace AvBench.Core.Scenarios;

public static class RegistryCrudBench
{
    private const string BasePath = @"Software\AvBench\Temp";

    public static RunResult Execute(int totalOps, string avName)
    {
        var hist = new LatencyHistogram(totalOps);
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < totalOps; i++)
        {
            long t0 = Stopwatch.GetTimestamp();

            using var key = Registry.CurrentUser.CreateSubKey($@"{BasePath}\key_{i}");
            key.SetValue("str_val", $"value_{i}", RegistryValueKind.String);
            key.SetValue("dword_val", i, RegistryValueKind.DWord);
            key.SetValue("bin_val", new byte[] { 0x01, 0x02, 0x03, 0x04 }, RegistryValueKind.Binary);
            key.SetValue("multi_val", new[] { "a", "b", "c" }, RegistryValueKind.MultiString);
            key.SetValue("expand_val", "%TEMP%\\test", RegistryValueKind.ExpandString);

            _ = key.GetValue("str_val");
            _ = key.GetValue("dword_val");
            _ = key.GetValue("bin_val");
            _ = key.GetValue("multi_val");
            _ = key.GetValue("expand_val");
            _ = key.GetValueNames();

            hist.Record(Stopwatch.GetTimestamp() - t0);
        }
        sw.Stop();

        try { Registry.CurrentUser.DeleteSubKeyTree($@"{BasePath}", throwOnMissingSubKey: false); } catch { }

        double opsPerSec = totalOps / sw.Elapsed.TotalSeconds;
        double meanLatencyUs = sw.Elapsed.TotalMicroseconds / totalOps;

        return new RunResult
        {
            ScenarioId = "registry-crud",
            AvName = avName,
            TimestampUtc = DateTime.UtcNow,
            Command = $"registry-crud ops={totalOps} values_per_key=5 ops_sec={opsPerSec:F0} mean_latency_us={meanLatencyUs:F1} {hist.Summarize()}",
            WorkingDir = Environment.CurrentDirectory,
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

---

### Tier 6 — IPC

### `PipeRoundtripBench.cs`

Create named pipe server → client connect → write 4 KB → read → disconnect. Exercises `NtCreateFile` (pipe) + `NtWriteFile` + `NtReadFile` through the minifilter, since named pipes are file system objects (`\Device\NamedPipe\`). Build tools, database servers, Docker, and many Windows services use pipes for IPC.

```csharp
using System.Diagnostics;
using System.IO.Pipes;
using AvBench.Core.Models;

namespace AvBench.Core.Scenarios;

public static class PipeRoundtripBench
{
    public static RunResult Execute(int totalOps, string avName)
    {
        var payload = new byte[4096]; // 4 KB
        Random.Shared.NextBytes(payload);
        var recvBuf = new byte[4096];

        var hist = new LatencyHistogram(totalOps);
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < totalOps; i++)
        {
            var pipeName = $"avbench_pipe_{i}";
            long t0 = Stopwatch.GetTimestamp();

            using var server = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            var connectTask = Task.Run(() =>
            {
                using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
                client.Connect(5000);
                client.Write(payload, 0, payload.Length);
                client.Read(recvBuf, 0, recvBuf.Length);
            });

            server.WaitForConnection();
            int read = server.Read(recvBuf, 0, recvBuf.Length);
            server.Write(payload, 0, read);
            connectTask.Wait();

            hist.Record(Stopwatch.GetTimestamp() - t0);
        }
        sw.Stop();

        double opsPerSec = totalOps / sw.Elapsed.TotalSeconds;
        double meanLatencyUs = sw.Elapsed.TotalMicroseconds / totalOps;

        return new RunResult
        {
            ScenarioId = "pipe-roundtrip",
            AvName = avName,
            TimestampUtc = DateTime.UtcNow,
            Command = $"pipe-roundtrip ops={totalOps} payload=4096 ops_sec={opsPerSec:F0} mean_latency_us={meanLatencyUs:F1} {hist.Summarize()}",
            WorkingDir = Environment.CurrentDirectory,
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

---

### Tier 7 — Security & Crypto

### `TokenQueryBench.cs`

Open process token → query token privileges → close handle. Exercises `NtOpenProcessToken` and `ObRegisterCallbacks` (handle operations are monitored by security software). Every elevated application, installer, service, and UAC-aware tool queries token privileges.

```csharp
using System.Diagnostics;
using System.Runtime.InteropServices;
using AvBench.Core.Models;

namespace AvBench.Core.Scenarios;

public static class TokenQueryBench
{
    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

    [DllImport("advapi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetTokenInformation(IntPtr TokenHandle, int TokenInformationClass,
        IntPtr TokenInformation, int TokenInformationLength, out int ReturnLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);

    private const uint TOKEN_QUERY = 0x0008;
    private const int TokenPrivileges = 3;

    public static RunResult Execute(int totalOps, string avName)
    {
        var currentProcess = Process.GetCurrentProcess().Handle;
        var hist = new LatencyHistogram(totalOps);
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < totalOps; i++)
        {
            long t0 = Stopwatch.GetTimestamp();

            if (OpenProcessToken(currentProcess, TOKEN_QUERY, out var tokenHandle))
            {
                GetTokenInformation(tokenHandle, TokenPrivileges, IntPtr.Zero, 0, out int needed);
                if (needed > 0)
                {
                    var buffer = Marshal.AllocHGlobal(needed);
                    GetTokenInformation(tokenHandle, TokenPrivileges, buffer, needed, out _);
                    Marshal.FreeHGlobal(buffer);
                }
                CloseHandle(tokenHandle);
            }

            hist.Record(Stopwatch.GetTimestamp() - t0);
        }
        sw.Stop();

        double opsPerSec = totalOps / sw.Elapsed.TotalSeconds;
        double meanLatencyUs = sw.Elapsed.TotalMicroseconds / totalOps;

        return new RunResult
        {
            ScenarioId = "token-query",
            AvName = avName,
            TimestampUtc = DateTime.UtcNow,
            Command = $"token-query ops={totalOps} ops_sec={opsPerSec:F0} mean_latency_us={meanLatencyUs:F1} {hist.Summarize()}",
            WorkingDir = Environment.CurrentDirectory,
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

### `CryptoHashVerifyBench.cs`

SHA-256 hash a 64 KB buffer + RSA-2048 signature verification. Simulates package signature verification, Authenticode checks, and HTTPS handshake crypto. Not directly hooked by security software, but AV's own concurrent signature verification shares CPU/cache resources.

```csharp
using System.Diagnostics;
using System.Security.Cryptography;
using AvBench.Core.Models;

namespace AvBench.Core.Scenarios;

public static class CryptoHashVerifyBench
{
    public static RunResult Execute(int totalOps, string avName)
    {
        var payload = new byte[65536]; // 64 KB
        Random.Shared.NextBytes(payload);

        using var rsa = RSA.Create(2048);
        byte[] signature = rsa.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        var hist = new LatencyHistogram(totalOps);
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < totalOps; i++)
        {
            long t0 = Stopwatch.GetTimestamp();
            byte[] hash = SHA256.HashData(payload);
            bool valid = rsa.VerifyData(payload, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            hist.Record(Stopwatch.GetTimestamp() - t0);
        }
        sw.Stop();

        double opsPerSec = totalOps / sw.Elapsed.TotalSeconds;
        double meanLatencyUs = sw.Elapsed.TotalMicroseconds / totalOps;

        return new RunResult
        {
            ScenarioId = "crypto-hash-verify",
            AvName = avName,
            TimestampUtc = DateTime.UtcNow,
            Command = $"crypto-hash-verify ops={totalOps} payload=65536 rsa_2048 ops_sec={opsPerSec:F0} mean_latency_us={meanLatencyUs:F1} {hist.Summarize()}",
            WorkingDir = Environment.CurrentDirectory,
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

---

### Tier 8 — COM & WMI

### `ComActivationBench.cs`

Create a COM object (`Scripting.FileSystemObject`) in a loop via `Activator.CreateInstance`. Exercises COM class factory, DLL loading (image-load notify), and registry lookup (`HKCR\CLSID\{...}` — registry callbacks). Office applications, shell extensions, management consoles, and many Windows tools use COM activation heavily.

```csharp
using System.Diagnostics;
using AvBench.Core.Models;

namespace AvBench.Core.Scenarios;

public static class ComActivationBench
{
    public static RunResult Execute(int totalOps, string avName)
    {
        var fsoType = Type.GetTypeFromProgID("Scripting.FileSystemObject");
        if (fsoType == null)
        {
            return new RunResult
            {
                ScenarioId = "com-create-instance",
                AvName = avName,
                TimestampUtc = DateTime.UtcNow,
                Command = "com-create-instance SKIPPED — Scripting.FileSystemObject not registered",
                WorkingDir = Environment.CurrentDirectory,
                ExitCode = -1,
                WallMs = 0
            };
        }

        var hist = new LatencyHistogram(totalOps);
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < totalOps; i++)
        {
            long t0 = Stopwatch.GetTimestamp();
            var obj = Activator.CreateInstance(fsoType);
            if (obj != null)
                System.Runtime.InteropServices.Marshal.ReleaseComObject(obj);
            hist.Record(Stopwatch.GetTimestamp() - t0);
        }
        sw.Stop();

        double opsPerSec = totalOps / sw.Elapsed.TotalSeconds;
        double meanLatencyUs = sw.Elapsed.TotalMicroseconds / totalOps;

        return new RunResult
        {
            ScenarioId = "com-create-instance",
            AvName = avName,
            TimestampUtc = DateTime.UtcNow,
            Command = $"com-create-instance ops={totalOps} progid=Scripting.FileSystemObject ops_sec={opsPerSec:F0} mean_latency_us={meanLatencyUs:F1} {hist.Summarize()}",
            WorkingDir = Environment.CurrentDirectory,
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

### `WmiQueryBench.cs`

WMI query loop. Exercises WMI provider infrastructure (COM + named pipes via DCOM + registry + process enumeration). System monitoring, hardware inventory, management tools, and many Windows applications use WMI. Requires `System.Management` NuGet package.

```csharp
using System.Diagnostics;
using System.Management;
using AvBench.Core.Models;

namespace AvBench.Core.Scenarios;

public static class WmiQueryBench
{
    public static RunResult Execute(int totalOps, string avName)
    {
        int pid = Environment.ProcessId;
        var hist = new LatencyHistogram(totalOps);
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < totalOps; i++)
        {
            long t0 = Stopwatch.GetTimestamp();
            using var searcher = new ManagementObjectSearcher(
                $"SELECT ProcessId, Name FROM Win32_Process WHERE ProcessId = {pid}");
            using var results = searcher.Get();
            foreach (var obj in results) { _ = obj["Name"]; obj.Dispose(); }
            hist.Record(Stopwatch.GetTimestamp() - t0);
        }
        sw.Stop();

        double opsPerSec = totalOps / sw.Elapsed.TotalSeconds;
        double meanLatencyUs = sw.Elapsed.TotalMicroseconds / totalOps;

        return new RunResult
        {
            ScenarioId = "wmi-query",
            AvName = avName,
            TimestampUtc = DateTime.UtcNow,
            Command = $"wmi-query ops={totalOps} class=Win32_Process ops_sec={opsPerSec:F0} mean_latency_us={meanLatencyUs:F1} {hist.Summarize()}",
            WorkingDir = Environment.CurrentDirectory,
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

---

### Tier 9 — File System Notifications

### `FsWatcherBench.cs`

Set up `FileSystemWatcher` on a directory, create+modify+delete files rapidly, measure the combined create+delete latency under watcher pressure. IDEs, file sync tools, cloud storage clients, and build systems use `ReadDirectoryChangesW`. The minifilter sits in the notification delivery path.

```csharp
using System.Diagnostics;
using AvBench.Core.Models;

namespace AvBench.Core.Scenarios;

public static class FsWatcherBench
{
    public static RunResult Execute(string tempRoot, int totalOps, string avName)
    {
        var watchDir = Path.Combine(tempRoot, "fswatcher_bench");
        Directory.CreateDirectory(watchDir);

        int notificationsReceived = 0;
        using var watcher = new FileSystemWatcher(watchDir)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
            Filter = "*.*",
            EnableRaisingEvents = true
        };
        watcher.Created += (_, _) => Interlocked.Increment(ref notificationsReceived);
        watcher.Deleted += (_, _) => Interlocked.Increment(ref notificationsReceived);

        var hist = new LatencyHistogram(totalOps);
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < totalOps; i++)
        {
            var filePath = Path.Combine(watchDir, $"watch_{i}.tmp");
            long t0 = Stopwatch.GetTimestamp();
            File.WriteAllBytes(filePath, new byte[64]);
            File.Delete(filePath);
            hist.Record(Stopwatch.GetTimestamp() - t0);
        }
        sw.Stop();

        Thread.Sleep(100); // let pending notifications drain
        Directory.Delete(watchDir, recursive: true);

        double opsPerSec = totalOps / sw.Elapsed.TotalSeconds;
        double meanLatencyUs = sw.Elapsed.TotalMicroseconds / totalOps;

        return new RunResult
        {
            ScenarioId = "fs-watcher",
            AvName = avName,
            TimestampUtc = DateTime.UtcNow,
            Command = $"fs-watcher ops={totalOps} notifications={notificationsReceived} ops_sec={opsPerSec:F0} mean_latency_us={meanLatencyUs:F1} {hist.Summarize()}",
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

## TypeperfCollector (always-on)

### Architecture

`TypeperfCollector` is started unconditionally before every scenario run and stopped after. It produces a `counters.csv` alongside each `run.json`. Overhead is negligible (~0.01% CPU at 1-second sample intervals). The CSV is the primary diagnostic tool for explaining noisy runs.

No `IOptInCollector` interface or `--counters` flag — typeperf always runs.

### `TypeperfCollector.cs`

`typeperf` samples performance counters and writes to CSV. We collect CPU%, disk bytes/sec, and available memory.

Key commands:
- `typeperf "counter1" "counter2" -si 1 -o output.csv` — sample every 1 second to CSV
- `typeperf` runs until Ctrl+C or the process is killed
- `-sc N` — collect N samples then stop

```csharp
using System.Diagnostics;

namespace AvBench.Core.Collectors;

public sealed class TypeperfCollector : IDisposable
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

`TypeperfCollector` is already wired into `ScenarioRunner.RunOnce()` from M1 (always-on). No additional CLI flags needed.

The M3 `RunCommand` extension adds all microbench families (23 benches):

```csharp
// M3 API microbench — run after compile scenarios
var tempMicro = Path.Combine(benchDir, "microbench_temp");

// === Tier 1: File I/O (minifilter) ===

// Archive extraction (highest-impact AV operation for developers)
var benchZip = ArchiveExtractBench.Setup(tempMicro);
microbenchResults.Add(ArchiveExtractBench.Execute(tempMicro, benchZip, iterations: 10, avName));

// Extension sensitivity — same content, different extensions
foreach (string ext in new[] { ".exe", ".dll", ".js", ".ps1" })
    microbenchResults.Add(ExtensionSensitivityBench.Execute(tempMicro, opsPerExtension: 10_000, ext, avName));

// File-write content: in-loop clone+patch of noop.exe → unique-hash PE every iteration
var unsignedExe = ProcessCreateBench.Setup(tempMicro); // reused by multiple benches
microbenchResults.Add(FileWriteContentBench.Execute(tempMicro, unsignedExe, totalOps: 10_000, avName));

// File enumerate large directory (~10K files)
var enumDir = FileEnumLargeDirBench.Setup(tempMicro);
microbenchResults.Add(FileEnumLargeDirBench.Execute(enumDir, iterations: 50, avName));

// File copy large (~100 MB)
var largeSrc = FileCopyLargeBench.Setup(tempMicro);
microbenchResults.Add(FileCopyLargeBench.Execute(tempMicro, largeSrc, iterations: 10, avName));

// Hard links and junctions (npm/pnpm deduplication)
microbenchResults.Add(HardlinkJunctionBench.ExecuteHardlinks(tempMicro, totalOps: 5_000, avName));
microbenchResults.Add(HardlinkJunctionBench.ExecuteJunctions(tempMicro, totalOps: 2_000, avName));

// === Tier 2: Process / Thread / DLL (kernel notify callbacks) ===

// Process creation with unsigned exe (forces full AV scan)
microbenchResults.Add(ProcessCreateBench.Execute(unsignedExe, totalOps: 500, avName));

// DLL load: copy to unique temp path each iteration (bypasses section cache)
microbenchResults.Add(DllLoadBench.Execute(tempMicro, totalOps: 2_000, avName));

// MOTW: copy + execute real unsigned exe, no-motw vs Zone.Identifier ZoneId=3
microbenchResults.Add(MotwBench.Execute(tempMicro, unsignedExe, totalOps: 500, applyMotw: false, avName));
microbenchResults.Add(MotwBench.Execute(tempMicro, unsignedExe, totalOps: 500, applyMotw: true, avName));

// Thread create/join cycle
microbenchResults.Add(ThreadCreateBench.Execute(totalOps: 5_000, avName));

// === Tier 3: Memory (user-mode hooks — near-universal) ===

// VirtualAlloc RW → VirtualProtect RX → VirtualFree
microbenchResults.Add(MemAllocProtectBench.Execute(totalOps: 50_000, avName));

// CreateFileMapping → MapViewOfFile → UnmapViewOfFile
microbenchResults.Add(MemMapFileBench.Execute(tempMicro, totalOps: 10_000, avName));

// === Tier 4: Network (WFP callout drivers) ===

// TCP connect→send→recv→close against local echo server
microbenchResults.Add(NetConnectLoopbackBench.Execute(totalOps: 2_000, avName));

// DNS resolve loop
microbenchResults.Add(DnsResolveBench.Execute(totalOps: 5_000, avName));

// === Tier 5: Registry (kernel CmRegisterCallbackEx) ===

// Registry CRUD: create key, set 5 values, query, enumerate, delete
microbenchResults.Add(RegistryCrudBench.Execute(totalOps: 5_000, avName));

// === Tier 6: IPC (named pipes — minifilter) ===

// Named pipe roundtrip: server→client→write→read→disconnect
microbenchResults.Add(PipeRoundtripBench.Execute(totalOps: 2_000, avName));

// === Tier 7: Security & Crypto ===

// Token query: OpenProcessToken → GetTokenInformation → CloseHandle
microbenchResults.Add(TokenQueryBench.Execute(totalOps: 50_000, avName));

// Crypto: SHA-256 + RSA-2048 verify (CPU contention with AV's own crypto)
microbenchResults.Add(CryptoHashVerifyBench.Execute(totalOps: 5_000, avName));

// === Tier 8: COM & WMI ===

// COM activation: Scripting.FileSystemObject
microbenchResults.Add(ComActivationBench.Execute(totalOps: 5_000, avName));

// WMI query: Win32_Process
microbenchResults.Add(WmiQueryBench.Execute(totalOps: 500, avName));

// === Tier 9: FS Notifications ===

// FileSystemWatcher under file churn
microbenchResults.Add(FsWatcherBench.Execute(tempMicro, totalOps: 5_000, avName));
```

## Implementation Steps (ordered)

### Step 1: Build LatencyHistogram helper and all API microbench families

Create `LatencyHistogram.cs` (shared percentile helper), then each bench file. Test individually:
- `archive-extract`: setup creates ~2K-file zip, 10 extract iterations, verify all files extracted, verify cleanup
- `ext-sensitivity`: 10K ops per extension (.exe/.dll/.js/.ps1), verify measurable latency difference across extensions
- `file-write-content`: 10K ops, each writes a unique-hash unsigned PE, alternating .exe/.dll
- `file-enum-large-dir`: setup creates ~10K files, 50 enumeration iterations, verify count = 10K each time
- `file-copy-large`: setup creates ~100MB file, 10 copy+delete iterations
- `hardlink-create`: 5K hard link create+delete, verify no leaked files
- `junction-create`: 2K junction create+delete, verify no leaked directories
- `process-create-wait`: build unsigned noop.exe via `dotnet build`, 500 spawns
- `dll-load-unique`: 2K loads of copied DLL with unique paths
- `motw`: 500 ops no-motw vs motw-zone3, copy+execute real unsigned noop.exe
- `thread-create`: 5K thread create+join cycles
- `mem-alloc-protect`: 50K VirtualAlloc RW→RX→Free cycles
- `mem-map-file`: 10K CreateFileMapping→MapViewOfFile→UnmapViewOfFile cycles
- `net-connect-loopback`: 2K TCP connect→send→recv→close against local echo server
- `net-dns-resolve`: 5K Dns.GetHostEntry("localhost") calls
- `registry-crud`: 5K create key + 5 values + query + enumerate + delete
- `pipe-roundtrip`: 2K named pipe server-client roundtrips
- `token-query`: 50K OpenProcessToken→GetTokenInformation→Close
- `crypto-hash-verify`: 5K SHA-256 + RSA-2048 verify
- `com-create-instance`: 5K CoCreateInstance for Scripting.FileSystemObject
- `wmi-query`: 500 WMI Win32_Process queries
- `fs-watcher`: 5K file create+delete under active FileSystemWatcher

### Step 2: Build `TypeperfCollector`

Create `TypeperfCollector.cs` in `AvBench.Core/Collectors/`. Test:
1. Start typeperf, wait 5 seconds, stop
2. Verify `counters.csv` has header row + data rows
3. Verify all 6 counter columns present

### Step 3: Wire up extended run command

Update `RunCommand.cs` to register all M3 microbench scenarios (23 benches). No new CLI flags needed — `TypeperfCollector` is already wired into `ScenarioRunner.RunOnce()` from M1.

### Step 4: End-to-end test

```powershell
avbench run --name defender-default --bench-dir C:\bench --output results
```

Expected output per scenario:
```
results/
  <scenario>/
    run.json
    stdout.log
    stderr.log
    counters.csv
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
| Large file copy fills disk for SSD-constrained VMs | 100MB × 10 iters = ~1GB I/O | Each copy is deleted immediately. Only 2× source size needed at peak. |
| Loopback TCP connections exhausting ephemeral ports | `net-connect-loopback` fails after ~16K ops | 2K ops is well under the 16K ephemeral port default. Connections are fully closed each iteration. |
| Named pipe name conflicts across concurrent runs | `pipe-roundtrip` fails | Pipe names include iteration counter; each name used exactly once. |
| `Scripting.FileSystemObject` may not be registered | `com-create-instance` fails | COM bench checks `Type.GetTypeFromProgID` and returns SKIPPED result if null. |
| `System.Management` NuGet package needed for WMI bench | Build fails | Add `<PackageReference Include="System.Management">` to `AvBench.Core.csproj`. |
| `CreateSymbolicLink` requires SeCreateSymbolicLinkPrivilege | Junction bench fails for non-admin | `avbench` always runs as Administrator (checked at startup). |
| FileSystemWatcher buffer overflow under high file churn | `fs-watcher` misses notifications | Notification count is informational, not a pass/fail criterion. Primary measurement is per-op latency. |
| 23 benches total runtime may exceed reasonable session window | Runs too long for automated orchestration | Each bench has conservative iteration counts. Total estimated runtime: ~5–10 minutes for all benches. |

## Testing Strategy

Manual verification:

1. All 23 API microbench families produce plausible ops/sec values and p50/p95/p99/max latency percentiles
2. Archive extraction shows measurable slowdown with AV enabled; ~2K files extracted per iteration with mixed extensions and sizes
3. Extension sensitivity shows measurable latency difference between `.exe`/`.dll` (PE dispatch) and `.js`/`.ps1` (script dispatch)
4. `dll-load-unique` latency is measurably higher than baseline filesystem copy (AV scans each fresh DLL)
5. `file-write-content` with real unique-hash unsigned PEs shows higher latency than same-size random-content file creation (AV performs full PE inspection on every write)
6. ProcessCreateBench unsigned exe produces higher latency than a cached signed-exe baseline
7. MOTW bench: `motw-exe-motw-zone3` shows higher latency than `motw-exe-no-motw`; the real unsigned exe actually executes (exit code 0) in both variants
8. `file-enum-large-dir` completes 50 iterations enumerating 10K files each with plausible timing
9. `file-copy-large` copies ~100MB per iteration with measurable AV-enabled slowdown
10. `hardlink-create` and `junction-create` produce valid link operations with no leaked files/dirs
11. `thread-create` shows measurable per-thread overhead from kernel notify callbacks
12. `mem-alloc-protect` RW→RX transition shows higher latency than RW-only allocation on AV-enabled systems
13. `mem-map-file` MapViewOfFile shows measurable latency (exercises the widely-monitored NtMapViewOfSection)
14. `net-connect-loopback` completes 2K TCP roundtrips against local echo server without port exhaustion
15. `net-dns-resolve` resolves "localhost" with consistent per-operation timing
16. `registry-crud` creates+queries+deletes 5K HKCU keys with cleanup
17. `pipe-roundtrip` completes 2K named pipe server-client roundtrips
18. `token-query` opens+queries+closes process token 50K times
19. `crypto-hash-verify` completes SHA-256+RSA-2048 verify with consistent per-op timing
20. `com-create-instance` creates 5K COM objects (or gracefully skips if not registered)
21. `wmi-query` completes 500 WMI queries with plausible latency
22. `fs-watcher` runs under active FileSystemWatcher with notification tracking
23. `counters.csv` always produced with 6 counter columns for every scenario
24. TypeperfCollector doesn't perturb timing by more than ~2%
25. Full suite run completes end-to-end with all 23 bench scenarios active (estimated ~5–10 min)
