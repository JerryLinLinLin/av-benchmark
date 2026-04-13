# Milestone 4 Implementation

## Scope

- Add `--file-delta` opt-in collector (snapshot build trees before/after)
- Add `--procmon` opt-in collector (Sysinternals Process Monitor automation)
- Add `--build-log` opt-in collector (MSBuild binary log, Nuitka XML report)
- Add `tools-manifest.json` version pinning for full reproducibility

## Prerequisites from Milestone 1–3

All M1/M2/M3 components are assumed working:

- Full tool installer chain (Git, Rust, VS Build Tools, CMake, Ninja, .NET SDK, Python, Nuitka)
- All compile scenarios (ripgrep, Roslyn, LLVM, Files, Black/Nuitka)
- All API microbench families
- Job object runner, AV sampler, output writers
- `avbench-compare` with CSV and markdown output
- `IOptInCollector` interface and existing collectors (WPR, typeperf)

## New Files

```
AvBench.Core/
  Collectors/
    FileDeltaCollector.cs      ← NEW
    ProcMonCollector.cs        ← NEW
    BuildLogCollector.cs       ← NEW
  Setup/
    ProcMonInstaller.cs        ← NEW
  Models/
    ToolsManifest.cs           ← NEW
    FileDeltaReport.cs         ← NEW
```

## File-Delta Collector

Snapshots a build/output tree before and after the workload. Reports files created, deleted, modified, and byte totals grouped by extension. Useful for understanding what the build actually produces and where AV scanning is likely concentrated.

### `FileDeltaReport.cs`

```csharp
namespace AvBench.Core.Models;

public sealed class FileDeltaReport
{
    public required string RootDirectory { get; init; }
    public required DateTime SnapshotBeforeUtc { get; init; }
    public required DateTime SnapshotAfterUtc { get; init; }
    public required List<FileEntry> Created { get; init; }
    public required List<FileEntry> Deleted { get; init; }
    public required List<FileEntry> Modified { get; init; }
    public required Dictionary<string, ExtensionSummary> ByExtension { get; init; }
    public long TotalCreatedBytes { get; init; }
    public long TotalDeletedBytes { get; init; }
    public long TotalModifiedBytes { get; init; }
}

public sealed class FileEntry
{
    public required string RelativePath { get; init; }
    public long SizeBytes { get; init; }
    public DateTime LastWriteUtc { get; init; }
}

public sealed class ExtensionSummary
{
    public required string Extension { get; init; }
    public int CreatedCount { get; init; }
    public int DeletedCount { get; init; }
    public int ModifiedCount { get; init; }
    public long TotalBytes { get; init; }
}
```

### `FileDeltaCollector.cs`

```csharp
using System.Text.Json;
using AvBench.Core.Models;

namespace AvBench.Core.Collectors;

public sealed class FileDeltaCollector : IOptInCollector
{
    private readonly string[] _watchDirs;
    private Dictionary<string, SnapshotEntry> _beforeSnapshot = new();
    private string _outputDir = "";
    private DateTime _snapshotBeforeUtc;

    /// <param name="watchDirs">
    /// Directories to snapshot (e.g., build output dirs, source tree).
    /// Typically: the repo root + any out-of-tree build directories.
    /// </param>
    public FileDeltaCollector(params string[] watchDirs)
    {
        _watchDirs = watchDirs;
    }

    public void Start(string outputDir)
    {
        _outputDir = outputDir;
        _snapshotBeforeUtc = DateTime.UtcNow;

        _beforeSnapshot = TakeSnapshot();
        Console.WriteLine($"[file-delta] Snapshot before: {_beforeSnapshot.Count} files across {_watchDirs.Length} dirs");
    }

    public void Stop()
    {
        var afterSnapshot = TakeSnapshot();
        var afterUtc = DateTime.UtcNow;

        var report = ComputeDelta(_beforeSnapshot, afterSnapshot, _snapshotBeforeUtc, afterUtc);

        var outputPath = Path.Combine(_outputDir, "file-delta.json");
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });
        File.WriteAllText(outputPath, json);

        Console.WriteLine($"[file-delta] Created={report.Created.Count} Deleted={report.Deleted.Count} Modified={report.Modified.Count}");
        Console.WriteLine($"[file-delta] Report saved: {outputPath}");
    }

    public void Dispose() { }

    private Dictionary<string, SnapshotEntry> TakeSnapshot()
    {
        var snapshot = new Dictionary<string, SnapshotEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (var dir in _watchDirs)
        {
            if (!Directory.Exists(dir)) continue;

            foreach (var file in Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories))
            {
                try
                {
                    var info = new FileInfo(file);
                    snapshot[file] = new SnapshotEntry
                    {
                        FullPath = file,
                        SizeBytes = info.Length,
                        LastWriteUtc = info.LastWriteTimeUtc
                    };
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }

        return snapshot;
    }

    private FileDeltaReport ComputeDelta(
        Dictionary<string, SnapshotEntry> before,
        Dictionary<string, SnapshotEntry> after,
        DateTime beforeUtc,
        DateTime afterUtc)
    {
        var created = new List<FileEntry>();
        var deleted = new List<FileEntry>();
        var modified = new List<FileEntry>();
        var byExtension = new Dictionary<string, ExtensionSummary>(StringComparer.OrdinalIgnoreCase);

        // Find created and modified
        foreach (var (path, afterEntry) in after)
        {
            var relPath = GetRelativePath(path);
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext)) ext = "(none)";

            if (!before.TryGetValue(path, out var beforeEntry))
            {
                created.Add(new FileEntry
                {
                    RelativePath = relPath,
                    SizeBytes = afterEntry.SizeBytes,
                    LastWriteUtc = afterEntry.LastWriteUtc
                });
                AddToExtSummary(byExtension, ext, created: 1, bytes: afterEntry.SizeBytes);
            }
            else if (beforeEntry.LastWriteUtc != afterEntry.LastWriteUtc ||
                     beforeEntry.SizeBytes != afterEntry.SizeBytes)
            {
                modified.Add(new FileEntry
                {
                    RelativePath = relPath,
                    SizeBytes = afterEntry.SizeBytes,
                    LastWriteUtc = afterEntry.LastWriteUtc
                });
                AddToExtSummary(byExtension, ext, modified: 1, bytes: afterEntry.SizeBytes);
            }
        }

        // Find deleted
        foreach (var (path, beforeEntry) in before)
        {
            if (!after.ContainsKey(path))
            {
                var relPath = GetRelativePath(path);
                var ext = Path.GetExtension(path).ToLowerInvariant();
                if (string.IsNullOrEmpty(ext)) ext = "(none)";

                deleted.Add(new FileEntry
                {
                    RelativePath = relPath,
                    SizeBytes = beforeEntry.SizeBytes,
                    LastWriteUtc = beforeEntry.LastWriteUtc
                });
                AddToExtSummary(byExtension, ext, deleted: 1, bytes: beforeEntry.SizeBytes);
            }
        }

        return new FileDeltaReport
        {
            RootDirectory = _watchDirs.Length == 1 ? _watchDirs[0] : string.Join(";", _watchDirs),
            SnapshotBeforeUtc = beforeUtc,
            SnapshotAfterUtc = afterUtc,
            Created = created,
            Deleted = deleted,
            Modified = modified,
            ByExtension = byExtension,
            TotalCreatedBytes = created.Sum(f => f.SizeBytes),
            TotalDeletedBytes = deleted.Sum(f => f.SizeBytes),
            TotalModifiedBytes = modified.Sum(f => f.SizeBytes)
        };
    }

    private string GetRelativePath(string fullPath)
    {
        foreach (var dir in _watchDirs)
        {
            if (fullPath.StartsWith(dir, StringComparison.OrdinalIgnoreCase))
                return Path.GetRelativePath(dir, fullPath);
        }
        return fullPath;
    }

    private static void AddToExtSummary(
        Dictionary<string, ExtensionSummary> dict, string ext,
        int created = 0, int deleted = 0, int modified = 0, long bytes = 0)
    {
        if (!dict.TryGetValue(ext, out var summary))
        {
            summary = new ExtensionSummary { Extension = ext };
            dict[ext] = summary;
        }

        // ExtensionSummary uses init properties — rebuild with updated counts
        dict[ext] = new ExtensionSummary
        {
            Extension = ext,
            CreatedCount = summary.CreatedCount + created,
            DeletedCount = summary.DeletedCount + deleted,
            ModifiedCount = summary.ModifiedCount + modified,
            TotalBytes = summary.TotalBytes + bytes
        };
    }

    private sealed class SnapshotEntry
    {
        public required string FullPath { get; init; }
        public long SizeBytes { get; init; }
        public DateTime LastWriteUtc { get; init; }
    }
}
```

## ProcMon Collector

Sysinternals Process Monitor captures detailed file, registry, network, and process activity. Best for root-cause investigation — not for every run due to overhead and large log size.

ProcMon command-line flags:
- `/Quiet` — no UI
- `/Minimized` — start minimized (alternative to Quiet)
- `/BackingFile <path>` — write to PML file instead of virtual memory
- `/LoadConfig <path>` — load filter configuration
- `/Runtime <seconds>` — auto-stop after N seconds (optional)
- `/Terminate` — tell running instance to stop

Download: `https://download.sysinternals.com/files/ProcessMonitor.zip`

### `ProcMonInstaller.cs`

ProcMon is a standalone executable (no install needed). Download and extract.

```csharp
using System.IO.Compression;
using System.Net.Http;

namespace AvBench.Core.Setup;

public sealed class ProcMonInstaller : ToolInstaller
{
    public override string Name => "Process Monitor";

    private const string DownloadUrl =
        "https://download.sysinternals.com/files/ProcessMonitor.zip";
    private const string InstallDir = @"C:\Tools\procmon";
    private const string ExeName = "Procmon64.exe";

    public static string ExePath => Path.Combine(InstallDir, ExeName);

    public override string? Detect()
    {
        return File.Exists(ExePath) ? ExePath : null;
    }

    public override async Task InstallAsync(CancellationToken ct = default)
    {
        var zipPath = Path.Combine(Path.GetTempPath(), "ProcessMonitor.zip");
        await DownloadFileAsync(DownloadUrl, zipPath, ct);

        Directory.CreateDirectory(InstallDir);
        ZipFile.ExtractToDirectory(zipPath, InstallDir, overwriteFiles: true);

        File.Delete(zipPath);

        if (!File.Exists(ExePath))
            throw new FileNotFoundException($"ProcMon not found after extraction: {ExePath}");

        Console.WriteLine($"[setup] Process Monitor installed: {ExePath}");
    }

    private static async Task DownloadFileAsync(string url, string dest, CancellationToken ct)
    {
        using var http = new HttpClient();
        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        await using var fs = File.Create(dest);
        await response.Content.CopyToAsync(fs, ct);
    }
}
```

### `ProcMonCollector.cs`

```csharp
using System.Diagnostics;

namespace AvBench.Core.Collectors;

public sealed class ProcMonCollector : IOptInCollector
{
    private string _outputPath = "";
    private Process? _process;

    /// <summary>
    /// Creates a ProcMon filter config that captures only the processes
    /// likely relevant to a build workload. Without filtering, ProcMon
    /// captures everything on the system and produces enormous logs.
    /// </summary>
    private static string CreateFilterConfig(string configPath)
    {
        // ProcMon uses XML config files. A minimal filter that excludes
        // common noise processes while keeping build tools and AV.
        // Users can customize by providing their own config.
        var config = """
            <?xml version="1.0" encoding="UTF-8"?>
            <procmon>
              <DestructiveFilter>0</DestructiveFilter>
              <FilterRules>
                <Rule>
                  <Column>Process Name</Column>
                  <Relation>is</Relation>
                  <Action>Exclude</Action>
                  <Value>Procmon64.exe</Value>
                </Rule>
                <Rule>
                  <Column>Process Name</Column>
                  <Relation>is</Relation>
                  <Action>Exclude</Action>
                  <Value>Procmon.exe</Value>
                </Rule>
                <Rule>
                  <Column>Process Name</Column>
                  <Relation>is</Relation>
                  <Action>Exclude</Action>
                  <Value>System</Value>
                </Rule>
              </FilterRules>
              <HistoryDepth>0</HistoryDepth>
              <Profiling>0</Profiling>
            </procmon>
            """;

        File.WriteAllText(configPath, config);
        return configPath;
    }

    public void Start(string outputDir)
    {
        var procmonExe = Setup.ProcMonInstaller.ExePath;
        if (!File.Exists(procmonExe))
        {
            Console.WriteLine("[procmon] WARNING: Procmon64.exe not found. Run 'avbench setup' first.");
            return;
        }

        _outputPath = Path.Combine(outputDir, "procmon.pml");
        var configPath = Path.Combine(outputDir, "procmon-config.pmc");

        // Create filter config
        CreateFilterConfig(configPath);

        // Start ProcMon in quiet mode with backing file
        // /Quiet — no UI, runs as background process
        // /BackingFile — write captured events to PML file
        // /LoadConfig — apply filter configuration
        var psi = new ProcessStartInfo(procmonExe,
            $"/Quiet /BackingFile \"{_outputPath}\" /LoadConfig \"{configPath}\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        };

        try
        {
            _process = Process.Start(psi);
            Console.WriteLine($"[procmon] Recording started: {_outputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[procmon] WARNING: Failed to start ProcMon: {ex.Message}");
        }
    }

    public void Stop()
    {
        if (_process is null || _process.HasExited) return;

        var procmonExe = Setup.ProcMonInstaller.ExePath;

        try
        {
            // /Terminate tells the running ProcMon instance to stop and save
            var psi = new ProcessStartInfo(procmonExe, "/Terminate")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };
            var termProc = Process.Start(psi);
            termProc?.WaitForExit(TimeSpan.FromSeconds(30));

            // Wait for the original process to exit
            if (!_process.WaitForExit(TimeSpan.FromSeconds(30)))
            {
                _process.Kill();
                Console.WriteLine("[procmon] WARNING: ProcMon did not exit gracefully, killed.");
            }

            Console.WriteLine($"[procmon] Capture saved: {_outputPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[procmon] WARNING: Failed to stop ProcMon: {ex.Message}");
        }
    }

    public void Dispose()
    {
        Stop();
        _process?.Dispose();
    }
}
```

## Build Log Collector

Captures build system output that aids root-cause analysis:
- **MSBuild binary log** (`.binlog`) for Roslyn and Files builds — recorded via `/bl` flag
- **Nuitka XML report** for Black/Nuitka compiles — recorded via `--xml` flag

Unlike other collectors that wrap external tools, the build log collector modifies the scenario command line to enable logging.

### `BuildLogCollector.cs`

```csharp
namespace AvBench.Core.Collectors;

/// <summary>
/// Modifies scenario commands to capture build logs.
/// Unlike WPR/typeperf/ProcMon collectors that wrap external tools,
/// this collector injects flags into the build command itself.
/// </summary>
public sealed class BuildLogCollector : IOptInCollector
{
    private string _outputDir = "";
    private readonly string _scenarioId;

    public BuildLogCollector(string scenarioId)
    {
        _scenarioId = scenarioId;
    }

    public void Start(string outputDir)
    {
        _outputDir = outputDir;
    }

    public void Stop()
    {
        // No-op. Log files are written by the build itself.
    }

    public void Dispose() { }

    /// <summary>
    /// Returns additional arguments to append to the build command
    /// to enable build logging.
    /// </summary>
    public string GetExtraArguments()
    {
        return _scenarioId switch
        {
            // MSBuild binary log for Roslyn and Files
            // /bl creates msbuild.binlog in the working directory
            // Can be analyzed with MSBuild Structured Log Viewer
            "roslyn-clean-build" or "roslyn-incremental-build" or "roslyn-noop-build"
                => $"/bl:\"{Path.Combine(_outputDir, "msbuild.binlog")}\"",

            "files-clean-build" or "files-incremental-build" or "files-noop-build"
                => $"/bl:\"{Path.Combine(_outputDir, "msbuild.binlog")}\"",

            // Nuitka XML compilation report
            // --xml=<path> writes compilation details as XML
            "nuitka-standalone" or "nuitka-onefile"
                => $"--xml=\"{Path.Combine(_outputDir, "nuitka-report.xml")}\"",

            _ => "" // No build log for this scenario
        };
    }
}
```

### Integration: Modifying Scenario Commands

The build log collector works differently from other collectors. Instead of starting/stopping an external tool, it injects arguments into the build command. This is handled in the ScenarioRunner:

```csharp
private RunResult RunOnce(ScenarioDefinition scenario, bool isWarmup, string repDir)
{
    var effectiveArguments = scenario.Arguments;

    // Start opt-in collectors
    var collectors = new List<IOptInCollector>();

    // ... existing WPR and typeperf collectors from M3 ...

    if (_enableFileDelta && !isWarmup)
    {
        var delta = new FileDeltaCollector(scenario.WorkingDirectory);
        delta.Start(repDir);
        collectors.Add(delta);
    }

    if (_enableProcMon && !isWarmup)
    {
        var procmon = new ProcMonCollector();
        procmon.Start(repDir);
        collectors.Add(procmon);
    }

    if (_enableBuildLog && !isWarmup)
    {
        var buildLog = new BuildLogCollector(scenario.Id);
        buildLog.Start(repDir);
        collectors.Add(buildLog);

        // Inject build log flags into the command
        var extra = buildLog.GetExtraArguments();
        if (!string.IsNullOrEmpty(extra))
            effectiveArguments = $"{effectiveArguments} {extra}";
    }

    // ... run the workload with effectiveArguments ...

    // Stop opt-in collectors
    foreach (var collector in collectors)
    {
        collector.Stop();
        collector.Dispose();
    }

    // ... build RunResult ...
}
```

## Tools Manifest

### Purpose

The `tools-manifest.json` file pins exact versions of all tools and repos used in a benchmark campaign. This ensures full reproducibility — two VMs using the same manifest will have identical toolchains and source code.

The manifest is written by `avbench setup` and validated by `avbench run` before starting benchmarks. If any version has drifted, `run` refuses to start.

### `ToolsManifest.cs`

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AvBench.Core.Models;

public sealed class ToolsManifest
{
    [JsonPropertyName("schema_version")]
    public int SchemaVersion { get; init; } = 1;

    [JsonPropertyName("created_utc")]
    public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;

    [JsonPropertyName("tools")]
    public required Dictionary<string, ToolEntry> Tools { get; init; }

    [JsonPropertyName("repos")]
    public required Dictionary<string, RepoEntry> Repos { get; init; }

    public sealed class ToolEntry
    {
        [JsonPropertyName("version")]
        public required string Version { get; init; }

        [JsonPropertyName("install_url")]
        public string? InstallUrl { get; init; }

        [JsonPropertyName("sha256")]
        public string? Sha256 { get; init; }
    }

    public sealed class RepoEntry
    {
        [JsonPropertyName("url")]
        public required string Url { get; init; }

        [JsonPropertyName("commit_sha")]
        public required string CommitSha { get; init; }

        [JsonPropertyName("branch")]
        public string? Branch { get; init; }
    }

    /// <summary>Write manifest to disk as JSON.</summary>
    public void Save(string path)
    {
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        File.WriteAllText(path, json);
    }

    /// <summary>Load manifest from disk.</summary>
    public static ToolsManifest Load(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<ToolsManifest>(json)
            ?? throw new InvalidOperationException("Failed to deserialize tools-manifest.json");
    }
}
```

### Example `tools-manifest.json`

```json
{
  "schema_version": 1,
  "created_utc": "2026-04-15T10:00:00Z",
  "tools": {
    "git": {
      "version": "2.47.1",
      "install_url": "https://github.com/git-for-windows/git/releases/download/v2.47.1.windows.1/Git-2.47.1-64-bit.exe",
      "sha256": "abc123..."
    },
    "rust": {
      "version": "1.85.0"
    },
    "vs_build_tools": {
      "version": "17.12",
      "install_url": "https://aka.ms/vs/17/release/vs_buildtools.exe"
    },
    "cmake": {
      "version": "3.31.4",
      "install_url": "https://github.com/Kitware/CMake/releases/download/v3.31.4/cmake-3.31.4-windows-x86_64.msi",
      "sha256": "def456..."
    },
    "ninja": {
      "version": "1.12.1",
      "install_url": "https://github.com/ninja-build/ninja/releases/download/v1.12.1/ninja-win.zip",
      "sha256": "ghi789..."
    },
    "dotnet_sdk_8": {
      "version": "8.0.404"
    },
    "dotnet_sdk_10": {
      "version": "10.0.102"
    },
    "python": {
      "version": "3.12.8",
      "install_url": "https://www.python.org/ftp/python/3.12.8/python-3.12.8-amd64.exe",
      "sha256": "jkl012..."
    },
    "nuitka": {
      "version": "2.5.8"
    },
    "procmon": {
      "version": "4.01",
      "install_url": "https://download.sysinternals.com/files/ProcessMonitor.zip"
    }
  },
  "repos": {
    "ripgrep": {
      "url": "https://github.com/BurntSushi/ripgrep",
      "commit_sha": "4649aa9700619f94cf9c66876e9549d83420e16c",
      "branch": "master"
    },
    "roslyn": {
      "url": "https://github.com/dotnet/roslyn",
      "commit_sha": "aabbccdd...",
      "branch": "main"
    },
    "llvm-project": {
      "url": "https://github.com/llvm/llvm-project",
      "commit_sha": "eeff0011...",
      "branch": "main"
    },
    "black": {
      "url": "https://github.com/psf/black",
      "commit_sha": "22334455...",
      "branch": "main"
    },
    "Files": {
      "url": "https://github.com/files-community/Files",
      "commit_sha": "66778899...",
      "branch": "main"
    }
  }
}
```

### Manifest Generation in Setup

Update `SetupCommand` to generate the manifest after all tools and repos are installed:

```csharp
private static ToolsManifest BuildManifest(string benchDir)
{
    var tools = new Dictionary<string, ToolsManifest.ToolEntry>();
    var repos = new Dictionary<string, ToolsManifest.RepoEntry>();

    // Detect installed tool versions
    tools["git"] = new ToolsManifest.ToolEntry
    {
        Version = CaptureVersion("git", "--version") ?? "unknown"
    };
    tools["rust"] = new ToolsManifest.ToolEntry
    {
        Version = CaptureVersion("rustc", "--version") ?? "unknown"
    };
    tools["cmake"] = new ToolsManifest.ToolEntry
    {
        Version = CaptureVersion("cmake", "--version") ?? "unknown"
    };
    tools["ninja"] = new ToolsManifest.ToolEntry
    {
        Version = CaptureVersion("ninja", "--version") ?? "unknown"
    };
    tools["python"] = new ToolsManifest.ToolEntry
    {
        Version = CaptureVersion("python", "--version") ?? "unknown"
    };

    // Detect .NET SDK versions
    var sdkOutput = CaptureVersion("dotnet", "--list-sdks") ?? "";
    foreach (var line in sdkOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries))
    {
        var ver = line.Split(' ')[0];
        if (ver.StartsWith("8.0"))
            tools["dotnet_sdk_8"] = new ToolsManifest.ToolEntry { Version = ver };
        else if (ver.StartsWith("10.0"))
            tools["dotnet_sdk_10"] = new ToolsManifest.ToolEntry { Version = ver };
    }

    // Detect Nuitka version
    tools["nuitka"] = new ToolsManifest.ToolEntry
    {
        Version = CaptureVersion("python", "-m nuitka --version") ?? "unknown"
    };

    // Record repo SHAs
    var repoNames = new[] { "ripgrep", "roslyn", "llvm-project", "black", "Files" };
    foreach (var repo in repoNames)
    {
        var repoDir = Path.Combine(benchDir, repo);
        if (!Directory.Exists(repoDir)) continue;

        var sha = CaptureVersion("git", $"-C \"{repoDir}\" rev-parse HEAD")?.Trim();
        var remoteUrl = CaptureVersion("git", $"-C \"{repoDir}\" remote get-url origin")?.Trim();

        if (sha is not null && remoteUrl is not null)
        {
            repos[repo] = new ToolsManifest.RepoEntry
            {
                Url = remoteUrl,
                CommitSha = sha
            };
        }
    }

    return new ToolsManifest
    {
        Tools = tools,
        Repos = repos
    };
}

private static string? CaptureVersion(string fileName, string arguments)
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
```

### Manifest Validation in Run

```csharp
/// <summary>
/// Verify that the current environment matches the pinned manifest.
/// Called at the start of `avbench run`.
/// </summary>
public static List<string> ValidateManifest(ToolsManifest manifest, string benchDir)
{
    var warnings = new List<string>();

    // Check tool versions
    foreach (var (name, entry) in manifest.Tools)
    {
        var (cmd, args) = name switch
        {
            "git" => ("git", "--version"),
            "rust" => ("rustc", "--version"),
            "cmake" => ("cmake", "--version"),
            "ninja" => ("ninja", "--version"),
            "python" => ("python", "--version"),
            "nuitka" => ("python", "-m nuitka --version"),
            _ => ((string?)null, (string?)null)
        };

        if (cmd is null) continue;

        var currentVersion = CaptureVersion(cmd, args!);
        if (currentVersion is null)
        {
            warnings.Add($"[manifest] {name}: not found (expected {entry.Version})");
        }
        else if (!currentVersion.Contains(entry.Version))
        {
            warnings.Add($"[manifest] {name}: version drift — expected {entry.Version}, got {currentVersion}");
        }
    }

    // Check repo SHAs
    foreach (var (name, entry) in manifest.Repos)
    {
        var repoDir = Path.Combine(benchDir, name);
        if (!Directory.Exists(repoDir))
        {
            warnings.Add($"[manifest] repo {name}: directory not found");
            continue;
        }

        var currentSha = CaptureVersion("git", $"-C \"{repoDir}\" rev-parse HEAD")?.Trim();
        if (currentSha != entry.CommitSha)
        {
            warnings.Add($"[manifest] repo {name}: SHA drift — expected {entry.CommitSha[..12]}, got {currentSha?[..12] ?? "unknown"}");
        }
    }

    return warnings;
}
```

### Run Command Integration

```csharp
// At the start of RunCommand execution:
var manifestPath = Path.Combine(benchDir, "tools-manifest.json");
if (File.Exists(manifestPath))
{
    var manifest = ToolsManifest.Load(manifestPath);
    var warnings = ValidateManifest(manifest, benchDir);
    if (warnings.Count > 0)
    {
        Console.WriteLine("[run] WARNING: Environment does not match tools-manifest.json:");
        foreach (var w in warnings)
            Console.WriteLine($"  {w}");
        Console.WriteLine("[run] Results may not be comparable to other VMs. Continue? (y/N)");

        // In batch/unattended mode, fail hard
        if (!Console.IsInputRedirected)
        {
            var response = Console.ReadLine()?.Trim().ToLowerInvariant();
            if (response != "y") return;
        }
        else
        {
            Console.WriteLine("[run] ABORT: Manifest validation failed in unattended mode.");
            return;
        }
    }
    else
    {
        Console.WriteLine("[run] Manifest validation passed.");
    }
}
```

## New CLI Options

```csharp
// In RunCommand.cs — add M4 opt-in flags
var fileDeltaOption = new Option<bool>("--file-delta")
{
    Description = "Enable file-system delta snapshots (opt-in)",
    DefaultValueFactory = _ => false
};
var procmonOption = new Option<bool>("--procmon")
{
    Description = "Enable Process Monitor capture (opt-in, requires admin)",
    DefaultValueFactory = _ => false
};
var buildLogOption = new Option<bool>("--build-log")
{
    Description = "Enable build system log capture (opt-in)",
    DefaultValueFactory = _ => false
};

command.Options.Add(fileDeltaOption);
command.Options.Add(procmonOption);
command.Options.Add(buildLogOption);
```

## Complete CLI Summary (All Milestones)

```
avbench setup
    --bench-dir <path>      Working directory for repos and builds (default: C:\bench)
    --manifest <path>       Path to tools-manifest.json for pinned versions (optional)

avbench run
    --profile <path>        AV profile JSON file
    --bench-dir <path>      Working directory
    --output <path>         Results output directory
    -n <count>              Repetitions per scenario (default: 5)
    --warmup <count>        Warmup iterations (default: 1)
    --idle-threshold <pct>  Max CPU% before starting (default: 5)
    --trace                 [M3] Enable WPR ETL trace capture
    --counters              [M3] Enable typeperf counter sampling
    --file-delta            [M4] Enable file-system delta snapshots
    --procmon               [M4] Enable Process Monitor capture
    --build-log             [M4] Enable build system log capture

avbench-compare
    --baseline <dir>        Baseline results directory
    --input <dir>           Input results directory (repeatable)
    --output <dir>          Comparison output directory
```

## Expected Output with All Opt-Ins

```
results/
  <campaign-timestamp>/
    suite-manifest.json
    tools-manifest.json          ← M4
    <scenario>/
      rep-01/
        run.json                 ← M1
        stdout.log               ← M1
        stderr.log               ← M1
        trace.etl                ← M3 (--trace)
        counters.csv             ← M3 (--counters)
        file-delta.json          ← M4 (--file-delta)
        procmon.pml              ← M4 (--procmon)
        procmon-config.pmc       ← M4 (--procmon)
        msbuild.binlog           ← M4 (--build-log, Roslyn/Files)
        nuitka-report.xml        ← M4 (--build-log, Nuitka)
```

## Implementation Steps (ordered)

### Step 1: Build `FileDeltaReport` model

Create `FileDeltaReport.cs` with `FileEntry` and `ExtensionSummary` types.

### Step 2: Build `FileDeltaCollector`

Create `FileDeltaCollector.cs`. Test:
1. Create a temp dir with known files
2. Start collector, add/modify/delete files, stop
3. Verify `file-delta.json` accurately reports changes
4. Verify extension summary is correct

### Step 3: Build `ProcMonInstaller`

Create `ProcMonInstaller.cs`. Test:
1. `EnsureInstalledAsync()` downloads and extracts ProcMon
2. `Procmon64.exe` exists at expected path

### Step 4: Build `ProcMonCollector`

Create `ProcMonCollector.cs`. Test:
1. Start collector, run a short workload, stop
2. Verify `procmon.pml` is created and non-empty
3. Can open PML file in ProcMon GUI for verification
4. Test graceful shutdown via `/Terminate`

Note: ProcMon requires admin privileges. Collector should print a clear warning if it fails due to insufficient privileges.

### Step 5: Build `BuildLogCollector`

Create `BuildLogCollector.cs`. Test:
1. For Roslyn scenario → `GetExtraArguments()` returns `/bl:"<path>/msbuild.binlog"`
2. For Files scenario → same MSBuild `/bl` flag
3. For Nuitka scenario → returns `--xml="<path>/nuitka-report.xml"`
4. For ripgrep/LLVM → returns empty string
5. Actually run a Roslyn build with `/bl` and verify `.binlog` is created

### Step 6: Build `ToolsManifest` model

Create `ToolsManifest.cs`. Test:
1. Serialize/deserialize round-trip
2. JSON output matches expected schema

### Step 7: Build manifest generation in SetupCommand

Update setup to call `BuildManifest()` and save `tools-manifest.json`. Test:
1. Run `avbench setup` on a prepared VM
2. Verify `tools-manifest.json` contains correct versions
3. Verify all repo SHAs are recorded

### Step 8: Build manifest validation in RunCommand

Update run to load and validate manifest. Test:
1. Run with matching manifest → "validation passed"
2. Modify a tool version → warning printed, user prompted
3. Unattended mode with drift → hard abort

### Step 9: Integrate all M4 collectors into ScenarioRunner

Wire `--file-delta`, `--procmon`, `--build-log` flags into `ScenarioRunner.RunOnce()`.

### Step 10: End-to-end test

```powershell
# Full run with all opt-ins
avbench run --profile profiles\defender-default.json --bench-dir C:\bench --output results -n 3 ^
  --trace --counters --file-delta --procmon --build-log
```

Verify:
- All result files produced for each rep
- `tools-manifest.json` present and valid
- `file-delta.json` has plausible created/modified counts
- `procmon.pml` non-empty
- `msbuild.binlog` present for Roslyn/Files scenarios
- `nuitka-report.xml` present for Nuitka scenarios
- Timing results are not wildly different from runs without opt-ins (some overhead expected)

## Key Risks and Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| File-delta snapshot on large trees is slow | Delays between stop-workload and save-result | Limit snapshot scope to build output dirs only, not entire source tree. Use concurrent enumeration. |
| ProcMon requires admin privileges | Collector fails on non-admin VMs | Print clear warning. Document that `--procmon` requires elevated shell. Check admin before starting. |
| ProcMon PML files can be huge (GBs) | Disk space exhaustion | Apply filters via config. Consider `/Runtime <seconds>` to auto-stop. Warn user about expected file size. |
| ProcMon captures system-wide activity | Noise from unrelated processes | Use filter config to exclude known noise. Post-processing can further filter by PID tree. |
| MSBuild binary log contains full source paths | Privacy concern if shared | Document that `.binlog` files contain full paths. Not included in `compare.csv`. |
| Nuitka `--xml` flag may change between versions | Report format breaks | Pin Nuitka version in manifest. Treat XML as opaque — don't parse it, just collect it. |
| Manifest SHA256 hashes require pre-computed values | Manual step to update manifest | SHA256 is optional field. Generate for downloads during setup. Validate on subsequent runs only if present. |
| Clock drift between VMs | Timestamps don't align for cross-VM comparison | Use NTP sync as part of VM prep. Timestamps are for human reference, not measurement. |

## Testing Strategy

Manual verification:

1. **File-delta**: Run on ripgrep clean build — expect `created` list includes `.exe`, `.d`, `.rlib` files in `target/release/`
2. **ProcMon**: Run on a short scenario — open PML in ProcMon GUI, verify build process activity visible
3. **Build-log**: Run Roslyn build with `--build-log` — open `.binlog` in MSBuild Structured Log Viewer, verify all projects
4. **Manifest generation**: Run setup, inspect JSON — all versions and SHAs present
5. **Manifest validation**: Mutate one version in manifest, run → warning printed
6. **Full suite**: All opt-ins active simultaneously on a real workload, no crashes or corrupt output
7. **Disk space**: Check total output size with all opt-ins for one scenario, extrapolate for full suite

## Project Completion Checklist

With M4 complete, the full `avbench` suite delivers:

- [x] **M1**: Core runner, ripgrep + file-create-delete, JSON/CSV output
- [x] **M2**: VS Build Tools chain, Roslyn + LLVM + Files scenarios, `avbench-compare`
- [x] **M3**: Python + Nuitka, Black scenarios, all API microbench families, WPR + typeperf collectors
- [x] **M4**: file-delta, ProcMon, build-log collectors, tools-manifest.json

All plan.md deliverables are covered. Future extensions (named pipes, memory-mapped files, latency percentiles) can be added as M5+.
