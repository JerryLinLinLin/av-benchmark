# Antivirus Performance Benchmark Plan

## Goal

Build a Windows VM benchmark suite in C# that answers two questions:

1. How much does antivirus software change build time and resource usage for representative projects?
2. How much does antivirus software change the cost of high-volume Windows API activity?

The suite consists of two separate programs:

1. **`avbench`** — runs inside each Windows VM. Handles environment setup (tool installation, source acquisition, dependency hydration) and benchmark execution. Each VM runs one AV configuration.
2. **`avbench-compare`** — runs on any machine (host, workstation, CI). Collects result directories from multiple VMs and produces cross-configuration comparison output.

This split exists because each VM runs in isolation with its own AV configuration and snapshot. The in-VM program can only see its own results. Cross-configuration comparison must happen outside.

## Key Principles

- Use C# for both programs.
- Keep the benchmark core vendor-neutral. Do not depend on Defender-only tooling.
- Use Windows Job objects via P/Invoke for process-tree accounting (CPU, I/O, memory) as the primary measurement layer.
- Use `typeperf` as an always-on system counter sampler for resource utilization analysis and anomaly diagnosis.
- Compare AV products with VM snapshots, not by uninstalling and reinstalling on the same image.
- `avbench setup` automates everything from tool installation to dependency hydration on a clean Windows VM.
- Separate untimed setup from timed benchmark execution.
- Pin repo SHAs and toolchain versions for each test campaign.
- Default metrics are kept lean: wall time, CPU time, I/O bytes, peak memory. `typeperf` counters are always collected alongside for diagnostic context.
- `avbench` always runs as Administrator. Tool installation requires elevation. Rather than scattering privilege checks, the program validates elevation at startup and exits immediately if not elevated.

## Why C#

- Precise timing with low harness overhead via `Stopwatch` and Job object accounting.
- Win32 interop (P/Invoke) for Job objects, process creation, and API microbench workers is straightforward.
- Structured JSON/CSV output is easier to produce and version than from PowerShell.
- A single compiled runner is easier to pin and execute repeatably inside test VMs.
- `avbench-compare` shares the same data model as `avbench`, so both can live in one solution.

PowerShell remains a helper only (e.g., `setup-test-vm.ps1` for reducing Windows noise before a run).

## High-Level Workflow

```
┌─────────────────────────────────────────┐
│  VM: baseline-os snapshot               │
│  1. avbench setup    (install tools,    │
│                       clone repos,      │
│                       hydrate deps)     │
│  2. avbench run --name baseline-os      │
│         (run benchmarks, write run.json)│
│  3. Copy results/ out to shared storage │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│  VM: defender-default snapshot          │
│  1. avbench setup                       │
│  2. avbench run --name defender-default │
│  3. Copy results/ out to shared storage │
└─────────────────────────────────────────┘

        ... repeat per AV configuration ...

┌─────────────────────────────────────────┐
│  Host / any machine                     │
│  avbench-compare                        │
│    --baseline results/baseline-os/      │
│    --input results/defender-default/    │
│    --input results/vendorA-default/     │
│    → compare.csv, summary.md            │
└─────────────────────────────────────────┘
```

## Workload Selection

### Compile workloads

#### `BurntSushi/ripgrep`

Medium Rust workload. Simple build entry point, fast iteration.

```
cargo build --release
```

Minimum Rust version: 1.85.0 stable. Optional PCRE2 feature build (`cargo build --release --features pcre2`) as a second scenario if desired.

Scenarios:

- `clean-build` (`cargo build --release`)
- `incremental-build` (touch a core source file in `crates/searcher/`, triggering cascade rebuild through printer → core → final binary)

#### `dotnet/roslyn`

Heavy C# workload. Real-world Microsoft compiler repo.

Build on Windows with Visual Studio 2022 17.13+ and the .NET SDK matching `global.json` `sdk.version`:

```
Restore.cmd   (untimed, during suite setup)
dotnet build Roslyn.slnx -c Release /m /nr:false   (timed)
```

Roslyn uses `Roslyn.slnx` as of recent commits. The current repo also builds cleanly with `dotnet build Roslyn.slnx` even when `Build.cmd` expects a newer Visual Studio/MSBuild pairing. Requires .NET Framework 4.7.2 minimum.

Scenarios:

- `clean-build` (`dotnet build Roslyn.slnx -c Release /m /nr:false`)
- `incremental-build` (touch a core source file in `src/Compilers/Core/Portable/`, triggering cascade rebuild through CSharp, Workspaces, and downstream assemblies)

Restore is always untimed and belongs in suite setup.

### API microbench workloads

Split by behavior, not collapsed into one mega-loop.

First families:

- `file-create-delete` — create and delete small temp files (M1)
- `archive-extract` — extract ~2K-file zip with mixed extensions/sizes (simulates NuGet/npm/pip restore)
- `ext-sensitivity` — create+write+delete with .exe / .dll / .js / .ps1 extensions (same content)
- `process-create-wait` — spawn an unsigned noop.exe (forces full AV scan, no trust-cache)
- `dll-load-unique` — copy system DLL to unique temp path then load (bypasses section cache)
- `file-write-content` — create→write→close→delete with pool of unique-hash unsigned PEs (forces full AV content inspection every iteration)
- `motw` — copy + execute real unsigned exe, with vs without Zone.Identifier ADS (ZoneId=3 Internet origin)

Later additions (not in v1):

- named pipe operations
- memory-mapped file operations

## Benchmark Matrix

### AV identification

`avbench run` requires a `--name <label>` parameter that identifies this VM's AV configuration (e.g., `baseline-os`, `defender-default`, `eset-default`). This label is stamped into every `run.json` and used by `avbench-compare` to group results.

In Milestone 4, `avbench` will auto-detect the installed AV product and version by querying Windows Security Center (`root\SecurityCenter2\AntiVirusProduct`). This works for any AV that registers with WSC — no hardcoded per-product logic. The product version is read from the exe's `FileVersionInfo`. Both fields can be manually overridden via `--av-product` and `--av-version` CLI flags.

Suggested `--name` labels:

- `baseline-os` — AV real-time protection off (or no AV installed)
- `defender-default`
- `defender-exclusions` — with build/output paths excluded
- `eset-default`
- `bitdefender-default`
- `huorong-default`
- `trendmicro-default`

### Compile phases

Every compile workload is measured in these phases:

- `prepare` — untimed, dependency hydration (NuGet restore, cargo fetch, etc.)
- `clean-build` — first timed phase
- `incremental-build` — touch a core source file, cascade rebuild

### Execution model — one session, one rep

`avbench run` executes each scenario **exactly once** per invocation. Repetition is achieved by restoring the VM snapshot and re-running `avbench run` in a fresh session. This guarantees every rep starts from identical OS/AV state — no cached trust decisions, filesystem cache, or Prefetch data from prior runs.

External orchestration (host-side script) handles the snapshot-restore loop:

```
for rep in 1..N:
    restore VM snapshot
    avbench run --name defender-default
    copy results/ to shared/<av-name>/session-<rep>/
```

Per invocation:

1. Idle check (refuse to start if CPU > threshold)
2. Run each scenario once
3. Quick validation (check exit code and expected output artifacts)

Within one session for compile workloads:

1. Clean (delete build artifacts)
2. Build
3. Touch one core source file, rebuild (incremental)

## What To Measure

### Default metrics (every run)

Collected from Windows Job object accounting (`JOBOBJECT_BASIC_AND_IO_ACCOUNTING_INFORMATION` via `QueryInformationJobObject`):

| Metric | Source |
|---|---|
| Wall-clock duration | `Stopwatch` start/stop |
| Total user CPU time | `JOBOBJECT_BASIC_ACCOUNTING_INFORMATION.TotalUserTime` |
| Total kernel CPU time | `JOBOBJECT_BASIC_ACCOUNTING_INFORMATION.TotalKernelTime` |
| Total processes spawned | `JOBOBJECT_BASIC_ACCOUNTING_INFORMATION.TotalProcesses` |
| Peak job memory | `JOBOBJECT_EXTENDED_LIMIT_INFORMATION.PeakJobMemoryUsed` |
| I/O read bytes | `IO_COUNTERS.ReadTransferCount` |
| I/O write bytes | `IO_COUNTERS.WriteTransferCount` |
| I/O read operations | `IO_COUNTERS.ReadOperationCount` |
| I/O write operations | `IO_COUNTERS.WriteOperationCount` |
| Exit code | Process exit code |

Also collected per run:

- Start/end timestamps (UTC)
- Command line and working directory
- Runner version and scenario version

#### Timing accuracy and granularity

**Wall-clock (`Stopwatch`):** Based on `QueryPerformanceCounter` (QPC), which uses the TSC register on modern processors. Resolution is ~333ns on a 3 GHz TSC; sub-microsecond in practice. Not affected by power management or Turbo Boost on invariant-TSC hardware (all modern x64 processors). Suitable for timing individual operations in microbench families.

**CPU time (Job object `TotalUserTime` / `TotalKernelTime`):** Stored in 100-nanosecond ticks, but **actual charging granularity is the scheduler clock interrupt — typically 15.625ms (64 Hz)**. The kernel charges CPU time to whichever thread is running when the clock tick fires. A thread running for 14ms may get charged 0ms or 15.6ms. Implications:

- For compile workloads (30s+), this is negligible: ±15ms on a 30s measurement is <0.05% error, further reduced by averaging 5+ reps.
- For API microbench totals (1–10s), the error averages out across many scheduler ticks.
- For single-op latency (<1ms), Job object CPU accounting is useless — only `Stopwatch` should be used.
- The kernel/user CPU ratio is statistically reliable for multi-second workloads because charging converges to the true distribution over thousands of scheduler ticks.

### System counters (always-on)

`typeperf` samples 6 performance counters at 1-second intervals for every scenario run. Overhead is negligible (~0.01% CPU). The resulting `counters.csv` is the primary diagnostic tool for explaining noisy runs — a CPU or disk spike at a specific timestamp pinpoints exactly when background activity interfered.

| Counter | Purpose |
|---|---|
| `\Processor(_Total)\% Processor Time` | Total CPU utilization |
| `\PhysicalDisk(_Total)\Disk Bytes/sec` | Total disk throughput |
| `\PhysicalDisk(_Total)\Disk Read Bytes/sec` | Disk read throughput |
| `\PhysicalDisk(_Total)\Disk Write Bytes/sec` | Disk write throughput |
| `\Memory\Available MBytes` | Free physical memory |
| `\Memory\Pages/sec` | Page faults (memory pressure indicator) |

### API microbench metrics

Per benchmark family:

| Metric | Notes |
|---|---|
| Operation name | e.g., `file-create-delete` |
| Batch size | Operations per timed batch |
| Total operations | Across all batches |
| Ops/sec | Total operations / wall time |
| Mean latency (us) | Wall time / total operations |
| p50 / p95 / p99 / max latency (us) | Per-op QPC recording via `LatencyHistogram`, sorted percentiles |
| Total wall time (ms) | End-to-end |

## What To Output

### `avbench` output (per VM)

Each VM produces a results directory that can be copied to shared storage.

```
results/
  suite-manifest.json
  <scenario>/
    run.json
    stdout.log
    stderr.log
    counters.csv
```

The AV configuration name is recorded inside each `run.json`, not as a directory level, since each VM runs only one configuration.

Also produces:

- `runs.csv` — one row per run, flat columns matching `run.json` fields. Useful for quick inspection without leaving the VM.

### `run.json`

```json
{
  "scenario_id": "ripgrep-clean-build",
  "av_name": "defender-default",
  "av_product": "Microsoft Defender Antivirus",
  "av_version": "4.18.24090.11",
  "timestamp_utc": "2026-04-13T15:30:00Z",
  "command": "cargo build --release",
  "working_dir": "C:\\bench\\ripgrep",
  "exit_code": 0,
  "wall_ms": 42350,
  "user_cpu_ms": 38100,
  "kernel_cpu_ms": 4250,
  "peak_job_memory_mb": 1240,
  "io_read_bytes": 523000000,
  "io_write_bytes": 312000000,
  "io_read_ops": 84000,
  "io_write_ops": 51000,
  "total_processes": 47,
  "p50_us": null,
  "p95_us": null,
  "p99_us": null,
  "max_us": null,
  "machine": {
    "os": "Windows Server 2022",
    "cpu": "4 vCPU",
    "ram_gb": 16,
    "storage": "SSD"
  },
  "runner_version": "0.1.0",
  "suite_manifest_sha": "abc123"
}
```

### `avbench-compare` output (cross-VM)

`avbench-compare` reads result directories from multiple VM sessions (copied to shared storage or a local folder) and produces:

- `compare.csv` — aggregated comparison across profiles
- `summary.md` — human-readable report

Usage:

```
avbench-compare ^
  --baseline C:\results\baseline-os\ ^
  --input C:\results\defender-default\ ^
  --input C:\results\vendorA-default\ ^
  --output C:\results\comparison\
```

### `compare.csv`

| Column | Description |
|---|---|
| `scenario_id` | e.g., `ripgrep-clean-build` |
| `av_name` | e.g., `defender-default` |
| `av_product` | e.g., `Microsoft Defender Antivirus` (auto-detected or overridden, M4) |
| `av_version` | e.g., `4.18.24090.11` (auto-detected or overridden, M4) |
| `baseline_name` | e.g., `baseline-os` |
| `sessions` | Number of VM sessions (snapshot-restored runs) collected for this scenario+config |
| `mean_wall_ms` | Mean wall-clock time |
| `median_wall_ms` | Median wall-clock time |
| `mean_cpu_ms` | Mean total CPU time (user + kernel) |
| `kernel_cpu_pct` | Mean kernel CPU as percentage of total CPU. AV minifilter overhead lands in kernel mode, so this ratio shifting upward vs. baseline is the most direct signal of AV scanning impact. |
| `baseline_kernel_cpu_pct` | Baseline's kernel CPU percentage for the same scenario. Shown side-by-side for easy comparison. |
| `kernel_cpu_slowdown_pct` | `(kernel_cpu_pct - baseline_kernel_cpu_pct)` in percentage points. Positive = AV added kernel-mode overhead. |
| `peak_memory_mb` | Max peak job memory across sessions |
| `slowdown_pct` | `(mean_wall - baseline_mean_wall) / baseline_mean_wall * 100` |
| `cv_pct` | Coefficient of variation for wall time |
| `status` | `ok`, `noisy`, or `failed` |

### `summary.md`

Answers:

- Which AV configuration slowed which workload the most?
- Was the slowdown CPU-bound or I/O-bound? (The kernel CPU % shift between baseline and AV-enabled runs directly answers this: AV minifilter scanning executes in kernel mode within the build process tree, so a kernel CPU ratio increase pinpoints AV overhead.)
- Which runs were too noisy to trust (CV > threshold)?

## C# System Design

### Solution layout

One solution, three projects:

- **`AvBench.Cli`** — in-VM program. Entry point for `avbench setup` and `avbench run`.
- **`AvBench.Compare`** — separate program. Entry point for `avbench-compare`. No Windows-specific dependencies beyond .NET runtime. Can run on the host or any machine.
- **`AvBench.Core`** — shared library. Data model (`run.json` schema), Job runner, collectors, scenario engine, API microbench worker, CSV/JSON serialization.

`AvBench.Cli` and `AvBench.Compare` both reference `AvBench.Core`. Split further only if/when the codebase justifies it.

### `avbench` CLI commands (in-VM program)

- `avbench setup` — install tools, fetch benchmark source trees, hydrate dependencies, write manifests
- `avbench run` — execute scenarios, collect metrics, write `run.json` and `runs.csv`. Requires `--name` to label this VM's AV configuration.

### `avbench-compare` CLI (host program)

- `avbench-compare --baseline <dir> --input <dir> [--input <dir> ...] --output <dir>`

### `setup` — automated environment provisioning

`avbench setup` takes a clean Windows VM (Server 2022, Windows 11, etc.) and makes it ready to run benchmarks. It installs all required tools, fetches benchmark source trees, and hydrates dependencies.

The setup is idempotent — re-running it skips already-installed tools and reuses existing source trees when their recorded commit SHA still matches the resolved source snapshot.

#### Tool installation

Each tool is installed silently using its official unattended installer. The setup checks whether the tool is already present before downloading.

| Tool | Install method | Detection |
|---|---|---|
| Git for Windows | `Git-*-64-bit.exe /VERYSILENT /NORESTART` | `git --version` |
| Visual Studio Build Tools / MSBuild | `winget install Microsoft.VisualStudio.BuildTools` with VCTools, managed desktop build tools, WinUI/Windows SDK, and C++ ATL components | `vswhere -products *` |
| .NET SDK | `dotnet-sdk-*-win-x64.exe /quiet /norestart` | `dotnet --list-sdks` |
| Rust (rustup) | `rustup-init.exe -y --default-toolchain 1.85.0` | `rustc --version` |

#### Repo acquisition and dependency hydration

After tools are installed:

1. Resolve an exact source snapshot for each repo and fetch it into a pinned location (e.g., `C:\bench\<repo>`).
2. Prefer GitHub source archives over full `git clone`:
   - ripgrep uses the latest release tag archive by default
   - Roslyn uses the default branch head archive because milestone 2 tracks the current upstream build layout
   - `--ripgrep-ref` resolves the requested ref to an exact commit SHA and downloads that archive
3. Record the exact commit SHA plus source metadata (`source_kind`, `source_reference`, `archive_url`) in `suite-manifest.json`.
4. Read repo manifests where possible:
   - `global.json` (Roslyn .NET SDK version)
   - `Cargo.toml` / `rust-toolchain.toml` (ripgrep Rust version)
5. Hydrate dependencies (untimed):
   - Roslyn: `Restore.cmd`
   - ripgrep: `cargo fetch`
6. Write `suite-manifest.json` (repos, SHAs, source metadata, tool versions).

If Visual Studio installation leaves Windows in a real pending-restart state, `avbench setup` should stop with a clear message telling the user to restart the PC and rerun setup. Ignore the Visual Studio bootstrapper's own queued cleanup JSON delete under `C:\ProgramData\Microsoft\VisualStudio\Packages\_bootstrapper\`, because current VS 2026 installs can leave that behind even when `vswhere` reports `isRebootRequired=false`.

`setup` must not silently upgrade toolchains once a campaign has started. Re-running `setup` after a campaign starts should detect and warn about version drift.

#### VM noise reduction

Optionally run `setup-test-vm.ps1` to disable Windows Update, Superfetch, search indexing, and other background services that add measurement noise.

### `run`

Responsibilities:

1. Load suite manifest.
2. Validate setup is complete (tools present, source trees fetched, deps hydrated).
3. Idle check: refuse if system CPU > threshold.
4. For each scenario:
      - Start `typeperf` counter sampling.
      - Create Windows Job object (`CreateJobObject`).
      - Launch workload process, assign to Job (`AssignProcessToJobObject`).
      - Stream stdout/stderr to log files.
      - On completion, query `JOBOBJECT_BASIC_AND_IO_ACCOUNTING_INFORMATION` and `JOBOBJECT_EXTENDED_LIMIT_INFORMATION` for metrics.
      - Stop `typeperf`.
      - Validate exit code and expected artifacts.
      - Write `run.json` and `counters.csv`.
5. Flatten all `run.json` files into `runs.csv`.

### `compare` (in `avbench-compare`, the host program)

Responsibilities:

1. Accept `--baseline <dir>` and one or more `--input <dir>` arguments pointing to result directories from different VMs.
2. Read all `run.json` files from each directory.
3. Validate that scenarios match across directories (warn on mismatches).
4. Group runs by scenario and AV profile.
5. Compute mean, median, stdev, and CV for wall time and CPU time.
6. Compute slowdown vs. baseline profile.
7. Flag runs where CV > threshold as `noisy`.
8. Write `compare.csv` and `summary.md` to the output directory.

### Process-tree runner (core component)

The runner launches workloads under a Windows Job object so accounting covers the whole process tree (builds spawn many child processes).

Key Win32 APIs via P/Invoke:

- `CreateJobObject` — create the job
- `SetInformationJobObject` — configure `JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE`
- `Process.Start()` — create the child (no `CREATE_SUSPENDED`; the window between start and job assignment is negligible for multi-second compile workloads)
- `AssignProcessToJobObject` — attach child to job immediately after start
- `WaitForExit` — wait for child exit
- `QueryInformationJobObject` with `JobObjectBasicAndIoAccountingInformation` and `JobObjectExtendedLimitInformation` — get CPU, I/O, memory metrics

Child processes inherit the job assignment by default (nested jobs work on Windows 8+/Server 2012+), so the entire build process tree is captured.

### API microbench worker

In-process static methods called directly within the `avbench.exe` process. Each bench family is a `static Execute()` method that uses `Stopwatch` (QPC) for wall-time measurement and returns a `RunResult`. Job object CPU/IO accounting is not used for microbenchs — the per-operation granularity is too fine for scheduler-tick-based CPU charging.

Behavior:

- One benchmark family per `Execute()` call.
- Fixed-iteration measurement window.
- Per-op QPC recording for latency percentiles (p50/p95/p99/max).
- Returns `RunResult` directly (no stdout/JSON round-trip).

### Collector layer

Default:

- **`JobAccountingCollector`** — queries Job object accounting on process exit.

Always-on:

- **`TypeperfCollector`** — starts/stops `typeperf.exe` for sampled system counters. Runs unconditionally for every scenario.

## VM Image Preparation

The only things that must exist before running `avbench setup` on a VM:

1. A clean Windows install (Server 2022 or Windows 11), fully updated.
2. The AV product installed and signature-updated (or no AV for baseline).
3. The `avbench` binary itself (copied in or available on a share).
4. Network access for downloading tools and cloning repos (during setup only).
5. An elevated (Administrator) terminal. `avbench` checks for admin on startup and refuses to run without it.

After running `avbench setup` + `avbench run`, copy the `results/` directory to shared storage, then restore the VM snapshot for the next profile.

Create separate snapshots for:

- Baseline image (no AV or real-time protection off)
- Each AV product / tuning configuration

## Statistical Rules

Simple, defensible rules for v1:

- No warmup runs — every run is a cold start from a freshly restored VM snapshot.
- One run per VM session. Repetitions are achieved by restoring the snapshot and re-running. This eliminates OS/AV cache contamination between reps.
- At least 5 sessions (reps) per AV configuration.
- Report mean, median, stdev, and coefficient of variation (CV) across sessions.
- Mark a scenario as `noisy` if CV > 10%.
- Randomize scenario order within a session when possible.
- Never compare a single best run against another single best run.

## Workload-Specific Notes

### ripgrep

- Best candidate for early runner development — fast builds, simple toolchain.
- Use as the first end-to-end scenario in Milestone 1.
- `cargo build --release` is the baseline; `--features pcre2` is optional.
- Minimum Rust: 1.85.0 stable.

### Roslyn

- Separate `Restore.cmd` (untimed) from the timed build.
- Requires Visual Studio/MSBuild plus the .NET SDK matching `global.json`.
- Solution file is `Roslyn.slnx`.
- Timed benchmark command is `dotnet build Roslyn.slnx -c Release /m /nr:false`.
- Watch for compiler server (`VBCSCompiler.exe`) behavior — it stays resident and may affect subsequent runs.

## What To Build First

### Milestone 1

Deliverables:

- `avbench setup` — automated tool installation (Git, Rust) + ripgrep source fetch + `cargo fetch`
- `avbench run` — ripgrep compile scenarios + one API microbench (`file-create-delete`)
- Job object process-tree runner with default metrics
- JSON output (`run.json`)
- CSV flattening (`runs.csv`)

Target workloads:

- ripgrep clean/incremental build
- `file-create-delete` API microbench

Why: ripgrep needs only Git + Rust, so setup automation is minimal. One API microbench gives immediate AV overhead signal.

### Milestone 2

- Extend `avbench setup` to install Visual Studio/MSBuild prerequisites and the .NET SDKs needed by Roslyn
- Add Roslyn compile scenarios
- Build `avbench-compare` — reads results from multiple directories, produces `compare.csv` and `summary.md`

### Milestone 3

- Add remaining API microbench families
- Integrate `TypeperfCollector` (always-on system counter sampling)

### Milestone 4

- Auto-detect installed AV product and version via Windows Security Center (`root\SecurityCenter2`)
- Record detected `av_product` and `av_version` in `run.json`
- `--av-product` and `--av-version` CLI overrides

## References

- Windows Job Objects: https://learn.microsoft.com/en-us/windows/win32/procthread/job-objects
- `JOBOBJECT_BASIC_AND_IO_ACCOUNTING_INFORMATION`: https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_basic_and_io_accounting_information
- `JOBOBJECT_BASIC_ACCOUNTING_INFORMATION`: https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_basic_accounting_information
- `typeperf`: https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/typeperf
- ripgrep README: https://github.com/BurntSushi/ripgrep/blob/master/README.md
- Roslyn Windows build guide: https://github.com/dotnet/roslyn/blob/main/docs/contributing/Building%2C%20Debugging%2C%20and%20Testing%20on%20Windows.md
