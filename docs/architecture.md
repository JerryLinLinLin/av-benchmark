# Architecture & Design

## Purpose

`avbench` measures the performance cost that antivirus / endpoint-security software adds to everyday Windows workloads. It runs identical work inside **two or more VM snapshots** — one with no AV (baseline) and one with each AV product under test — then compares the results.

The suite produces **machine-readable JSON/CSV** and a **Markdown comparison report**, so teams can quantify AV overhead before deploying a product across their fleet.

## High-level workflow

```
┌─────────────────────────────────────────────────────────────────────┐
│                         Host machine                                │
│                                                                     │
│   VM-1 (baseline-os)         VM-2 (defender)        VM-N (…)       │
│   ┌─────────────────┐       ┌─────────────────┐                    │
│   │ avbench setup    │       │ avbench setup    │                    │
│   │ avbench run      │       │ avbench run      │                    │
│   │  → results/      │       │  → results/      │                    │
│   └─────────────────┘       └─────────────────┘                    │
│                                                                     │
│   Host: copy all results/ folders locally, then:                    │
│   avbench-compare --baseline baseline/ --input defender/ --output . │
│     → compare.csv + summary.md                                      │
└─────────────────────────────────────────────────────────────────────┘
```

**One run per VM session.** The external orchestrator (you, or a CI script) restores a clean VM snapshot before each session and collects results afterwards. Repeat measurements by restoring the snapshot and running again — there is no `--repetitions` flag.

## Two executables

| Executable | Project | Purpose |
|---|---|---|
| `avbench.exe` | `AvBench.Cli` | Runs inside the VM. Installs toolchains (`setup`), executes benchmarks (`run`). |
| `avbench-compare.exe` | `AvBench.Compare` | Runs on the host (or anywhere). Loads `run.json` files from multiple runs and produces comparison outputs. |

Both are [.NET 8](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) self-contained single-file executables targeting `win-x64`.

## Solution structure

```
src/
├── AvBench.sln
├── Directory.Build.props              # Shared: net8.0, C# 12, nullable
│
├── AvBench.Cli/                       # CLI entry point (avbench.exe)
│   ├── Program.cs                     # Admin check, subcommand registration
│   └── Commands/
│       ├── SetupCommand.cs            # `avbench setup`
│       └── RunCommand.cs              # `avbench run`
│
├── AvBench.Core/                      # Shared library
│   ├── BenchmarkWorkloads.cs          # Workload ID registry (ripgrep, roslyn, microbench)
│   ├── Collectors/
│   │   └── TypeperfCollector.cs       # Always-on perf counter sampling
│   ├── Detection/
│   │   ├── AvDetector.cs              # WMI SecurityCenter2 query
│   │   └── AvInfo.cs                  # Product name + version record
│   ├── Environment/
│   │   ├── IdleChecker.cs             # Pre-run CPU < 20% gate
│   │   └── SystemInfoProvider.cs      # OS, CPU, RAM collection
│   ├── Internal/
│   │   ├── FileSystemUtil.cs          # Robust recursive delete
│   │   ├── LatencyHistogram.cs        # QPC-based percentile tracker
│   │   └── ProcessUtil.cs             # Async process runner helper
│   ├── Microbench/
│   │   ├── MicrobenchRequest.cs       # Unified parameter object
│   │   ├── MicrobenchSupport.cs       # Setup: archive zip, unsigned exe
│   │   ├── MicrobenchWorker.cs        # Main routing switch (partial)
│   │   ├── MicrobenchWorker.FileSystem.cs
│   │   ├── MicrobenchWorker.Management.cs
│   │   └── MicrobenchWorker.System.cs
│   ├── Models/
│   │   ├── RunResult.cs               # Per-scenario result JSON model
│   │   ├── ScenarioDefinition.cs      # What/how to run a scenario
│   │   └── SuiteManifest.cs           # Setup output: repos, tools, paths
│   ├── Output/
│   │   ├── CsvResultWriter.cs         # runs.csv (25 columns)
│   │   └── JsonResultWriter.cs        # run.json per scenario
│   ├── Runner/
│   │   ├── JobObject.cs               # Win32 Job Object wrapper
│   │   ├── ProcessTreeRunner.cs       # Spawn + measure in Job Object
│   │   └── ProcessTreeRunResult.cs    # Exit code + accounting snapshot
│   ├── Scenarios/
│   │   ├── ScenarioRunner.cs          # Orchestrator: prepare → run → record
│   │   ├── MicrobenchScenarioFactory.cs  # Creates 27 ScenarioDefinitions
│   │   ├── RipgrepScenarioFactory.cs  # clean-build + incremental-build
│   │   ├── RoslynScenarioFactory.cs   # clean-build + incremental-build
│   │   ├── ScenarioSupport.cs         # File/dir validation helpers
│   │   └── SourceFileToucher.cs       # Touch file for incremental builds
│   ├── Serialization/
│   │   └── AvBenchJsonContext.cs      # Source-generated JSON context
│   └── Setup/
│       ├── SetupService.cs            # Orchestrates full environment setup
│       ├── DotNetSdkInstaller.cs
│       ├── GitInstaller.cs
│       ├── RustInstaller.cs           # Pins to 1.85.0
│       ├── VsBuildToolsInstaller.cs   # winget + VS component workloads
│       ├── KnownToolPaths.cs          # PATH management
│       ├── RepoCloner.cs             # GitHub archive download + hydrate
│       ├── ToolInstaller.cs           # Base class for tool installers
│       └── ...
│
└── AvBench.Compare/                   # Comparison tool (avbench-compare.exe)
    ├── Program.cs
    ├── CompareCommand.cs              # CLI: --baseline, --input, --output
    ├── CompareEngine.cs               # Stat computation + slowdown %
    ├── CompareCsvWriter.cs            # compare.csv (16 columns)
    └── SummaryRenderer.cs             # summary.md (Markdown tables)
```

## Key design decisions

### Windows Job Objects for measurement

Every out-of-process scenario (compile builds) runs inside a [Win32 Job Object](https://learn.microsoft.com/en-us/windows/win32/procthread/job-objects) with `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE`. The Job Object provides:

- **User CPU time** and **kernel CPU time** (summed across all processes in the tree)
- **Peak memory** of the entire process tree
- **I/O counters** — bytes and operations for read/write
- **Total process count** spawned

This captures the full cost of multi-process build tools (MSBuild, cargo) and their child processes. CPU time accounting granularity defaults to the system timer tick (~15.6 ms), though modern Windows versions can use cycle-based accounting for finer precision. At the whole-build level this granularity is immaterial.

### In-process microbench execution

API microbenchmarks run **in-process** (not spawned as child processes) via `MicrobenchWorker.Execute()`. Each bench uses [`Stopwatch`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.stopwatch) (QPC-backed, sub-microsecond resolution) for wall time and a `LatencyHistogram` to record per-operation tick counts for percentile computation (p50/p95/p99/max).

### TypeperfCollector (always-on)

Every scenario gets a [`typeperf`](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/typeperf) process that samples 6 counters at 1-second intervals into a `counters.csv` file:

- `\Processor(_Total)\% Processor Time`
- `\PhysicalDisk(_Total)\Disk Bytes/sec`
- `\PhysicalDisk(_Total)\Disk Read Bytes/sec`
- `\PhysicalDisk(_Total)\Disk Write Bytes/sec`
- `\Memory\Available MBytes`
- `\Memory\Pages/sec`

These are diagnostic — useful for explaining noisy runs or identifying thermal throttling.

### Idle check before run

Before running any scenarios, `IdleChecker.VerifyAsync()` samples CPU via `typeperf` for 3 seconds and aborts if average CPU usage exceeds 20 %. This prevents AV background scans or system tasks from contaminating results.

### AV auto-detection

`AvDetector` queries [`root\SecurityCenter2`](https://learn.microsoft.com/en-us/windows/win32/wmisdk/--securitycenter2) `AntiVirusProduct` via WMI. When multiple products are registered (e.g., Defender + a third-party product), it selects the **non-Defender** product. Product version is read from `FileVersionInfo` of the reported exe path. Both can be overridden with `--av-product` and `--av-version`.

### Scenario ordering

`RunCommand` uses a **Fisher-Yates shuffle** on scenario family groups (ripgrep, roslyn, microbench families) at the start of each session. This distributes systematic ordering effects (AV cache warm-up, thermal drift) across multiple sessions rather than letting them accumulate in a fixed order.

### Suite manifest

`avbench setup` produces a `suite-manifest.json` that records every repo SHA, tool version, and file path. `avbench run` reads this manifest and stamps its SHA-256 hash into every `run.json`, enabling reproducibility verification downstream.

## Output structure

After a run:

```
results/
├── suite-manifest.json                 # Copy of the manifest used
├── runs.csv                            # Aggregated CSV of all scenario results
├── ripgrep-clean-build/
│   ├── run.json                        # Full result record
│   ├── stdout.log
│   ├── stderr.log
│   └── counters.csv                    # typeperf samples
├── ripgrep-incremental-build/
│   └── ...
├── roslyn-clean-build/
│   └── ...
├── file-create-delete/
│   └── ...
├── mem-alloc-protect/
│   └── ...
└── ... (one folder per scenario)
```

After comparison:

```
compare-out/
├── compare.csv                         # 16-column comparison spreadsheet
└── summary.md                          # Markdown report with per-AV tables
```
