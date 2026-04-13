# Antivirus Performance Benchmark Plan

## Goal

Build a Windows VM benchmark suite in C# that answers two questions:

1. How much does antivirus software change build time and resource usage for representative projects?
2. How much does antivirus software change the cost of high-volume Windows API activity?

The suite consists of two separate programs:

1. **`avbench`** — runs inside each Windows VM. Handles environment setup (tool installation, repo cloning, dependency hydration) and benchmark execution. Each VM runs one AV configuration.
2. **`avbench-compare`** — runs on any machine (host, workstation, CI). Collects result directories from multiple VMs and produces cross-configuration comparison output.

This split exists because each VM runs in isolation with its own AV configuration and snapshot. The in-VM program can only see its own results. Cross-configuration comparison must happen outside.

## Key Principles

- Use C# for both programs.
- Keep the benchmark core vendor-neutral. Do not depend on Defender-only tooling.
- Use Windows Job objects via P/Invoke for process-tree accounting (CPU, I/O, memory) as the primary measurement layer.
- Use `WPR` (Windows Performance Recorder) as an opt-in detailed trace layer for root-cause investigation.
- Compare AV products with VM snapshots, not by uninstalling and reinstalling on the same image.
- `avbench setup` automates everything from tool installation to dependency hydration on a clean Windows VM.
- Separate untimed setup from timed benchmark execution.
- Pin repo SHAs and toolchain versions for each test campaign.
- Default metrics are kept lean: wall time, CPU time, I/O bytes, peak memory. Everything else is opt-in.
- `avbench` always runs as Administrator. Tool installation and WPR tracing require elevation. Rather than scattering privilege checks, the program validates elevation at startup and exits immediately if not elevated.

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

#### `llvm/llvm-project`

Heavy C/C++ workload. Large source tree, high compile volume, high file churn.

Use on Windows with Visual Studio Build Tools + Ninja + CMake:

```
cmake -S llvm\llvm -B build -G Ninja ^
  -DLLVM_ENABLE_PROJECTS=clang ^
  -DLLVM_TARGETS_TO_BUILD=X86 ^
  -DCMAKE_BUILD_TYPE=Release
ninja -C build
```

Keep the build scope fixed (LLVM + Clang, X86 target only). Building LLVM + Clang in Release mode needs ~15-20 GB disk. An SSD-backed VM is assumed.

Scenarios:

- `configure` (CMake generation, separate from compile)
- `clean-build` (`ninja -C build`)
- `incremental-build` (touch one `.cpp`, rebuild)
- `noop-build` (rebuild with no changes)

#### `BurntSushi/ripgrep`

Medium Rust workload. Simple build entry point, fast iteration.

```
cargo build --release
```

Minimum Rust version: 1.85.0 stable. Optional PCRE2 feature build (`cargo build --release --features pcre2`) as a second scenario if desired.

Scenarios:

- `clean-build` (`cargo build --release`)
- `incremental-build` (make a small, harmless edit to one stable `.rs` file, rebuild)
- `noop-build`

#### `dotnet/roslyn`

Heavy C# workload. Real-world Microsoft compiler repo.

Build on Windows with Visual Studio 2022 + .NET SDK matching `global.json` `sdk.version`:

```
Restore.cmd   (untimed, during suite setup)
Build.cmd     (timed)
```

Roslyn uses `Roslyn.slnx` as of recent commits. Requires .NET Framework 4.7.2 minimum.

Scenarios:

- `clean-build` (`Build.cmd`)
- `incremental-build` (touch one stable `.cs` file, rebuild)
- `noop-build`

Restore is always untimed and belongs in suite setup.

#### `psf/black` with Nuitka

Python-to-native packaging workload. Real CLI app.

```
python -m nuitka --standalone black_entry.py
python -m nuitka --onefile black_entry.py
```

Where `black_entry.py` is a small wrapper calling Black's CLI entry point. Keep `standalone` and `onefile` as separate scenarios. Smoke-test the produced binary after each build.

Scenarios:

- `nuitka-standalone`
- `nuitka-onefile`

#### `files-community/Files`

Large C# / WinUI 3 desktop app (modern file manager, 43k stars). Exercises WindowsApp SDK 1.8, XAML compilation, CsWin32 source generator for Win32 P/Invoke, WinUI custom controls, packaged-app build pipeline, and C++ native helper projects. 97% C# — the heaviest Windows-native C# workload in the suite.

Build requires Visual Studio Build Tools with WinUI workload, .NET 10 SDK (per `global.json` `10.0.102`), Windows 11 SDK `10.0.26100.0`, Windows App SDK 1.8, and MSVC v145 C++ tools (already needed for LLVM).

```
msbuild Files.slnx /p:Configuration=Release /p:Platform=x64
```

The solution contains ~15 projects (C# app, server, storage, controls, source generators, C++ dialog helpers, background tasks, tests). Produces a packaged WinUI 3 desktop app.

Scenarios:

- `clean-build` (delete `bin`/`obj`/`AppPackages` dirs, then `msbuild /t:Build`)
- `incremental-build` (touch one `.cs` file in `src/Files.App/`, rebuild)
- `noop-build` (rebuild with no changes)

### API microbench workloads

Split by behavior, not collapsed into one mega-loop.

First families:

- `file-create-delete` — create and delete small temp files
- `file-open-close` — open/close an existing file repeatedly
- `dir-enumerate` — enumerate a directory tree
- `copy-rename-move` — copy/rename/move small files
- `process-create-wait` — `CreateProcess` + `WaitForSingleObject`
- `registry-open-query` — open and query registry keys
- `dll-load-unload` — `LoadLibrary` / `FreeLibrary`

Later additions (not in v1):

- named pipe operations
- memory-mapped file operations

## Benchmark Matrix

### AV identification

`avbench run` requires a `--name <label>` parameter that identifies this VM's AV configuration (e.g., `baseline-os`, `defender-default`, `eset-default`). This label is stamped into every `run.json` and used by `avbench-compare` to group results.

In Milestone 4, `avbench` will auto-detect the installed AV product and version (supporting Microsoft Defender, Huorong, ESET, Bitdefender, and TrendMicro). The detected product name and version are recorded in `run.json`. Both can be manually overridden via `--av-name` and `--av-version` CLI flags for unsupported products or testing.

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
- `incremental-build`
- `noop-build`

### Execution block structure

For each scenario + configuration combination:

1. Idle check (refuse to start if CPU > threshold)
2. One warmup run (discarded — primes AV cache and disk cache)
3. N timed repetitions (default N=5)
4. Quick validation (check exit code and expected output artifacts)

Within one repetition for compile workloads:

1. Clean (delete build artifacts)
2. Build
3. Touch one file, rebuild (incremental)
4. Rebuild again (no-op)

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

### Opt-in metrics

These are not collected by default. Enable via CLI flags:

| Opt-in metric | Flag | Notes |
|---|---|---|
| WPR ETL trace | `--trace` | Full system ETW trace via `wpr -start` / `wpr -stop`. Produces `.etl` for analysis in WPA. |
| PerfMon counters CSV | `--counters` | Sampled system counters via `typeperf`: CPU%, disk bytes/sec, available memory. |

### API microbench metrics

Per benchmark family:

| Metric | Notes |
|---|---|
| Operation name | e.g., `file-create-delete` |
| Batch size | Operations per timed batch |
| Total operations | Across all batches |
| Ops/sec | Total operations / wall time |
| Mean latency (us) | Wall time / total operations |
| Total wall time (ms) | End-to-end |

Latency percentiles (p50/p95/p99) are deferred to a later version. Ops/sec and mean latency are sufficient for v1.

## What To Output

### `avbench` output (per VM)

Each VM produces a results directory that can be copied to shared storage.

```
results/
  <campaign-timestamp>/
    suite-manifest.json
    <scenario>/
      rep-01/
        run.json
        stdout.log
        stderr.log
        combined.log       (merged stdout/stderr for easier manual inspection)
        trace.etl          (opt-in)
        counters.csv       (opt-in)
```

The AV configuration name is recorded inside each `run.json`, not as a directory level, since each VM runs only one configuration.

Also produces:

- `runs.csv` — one row per run, flat columns matching `run.json` fields. Useful for quick inspection without leaving the VM.

### `run.json`

```json
{
  "scenario_id": "ripgrep-clean-build",
  "av_name": "defender-default",
  "repetition": 1,
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

`avbench-compare` reads result directories from multiple VMs (copied to shared storage or a local folder) and produces:

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
| `baseline_name` | e.g., `baseline-os` |
| `repetitions` | Number of measured runs |
| `mean_wall_ms` | Mean wall-clock time |
| `median_wall_ms` | Median wall-clock time |
| `mean_cpu_ms` | Mean total CPU time (user + kernel) |
| `peak_memory_mb` | Max peak job memory across reps |
| `slowdown_pct` | `(mean_wall - baseline_mean_wall) / baseline_mean_wall * 100` |
| `cv_pct` | Coefficient of variation for wall time |
| `status` | `ok`, `noisy`, or `failed` |

### `summary.md`

Answers:

- Which AV configuration slowed which workload the most?
- Was the slowdown CPU-bound or I/O-bound?
- Which runs were too noisy to trust (CV > threshold)?

## C# System Design

### Solution layout

One solution, three projects:

- **`AvBench.Cli`** — in-VM program. Entry point for `avbench setup` and `avbench run`.
- **`AvBench.Compare`** — separate program. Entry point for `avbench-compare`. No Windows-specific dependencies beyond .NET runtime. Can run on the host or any machine.
- **`AvBench.Core`** — shared library. Data model (`run.json` schema), Job runner, collectors, scenario engine, API microbench worker, CSV/JSON serialization.

`AvBench.Cli` and `AvBench.Compare` both reference `AvBench.Core`. Split further only if/when the codebase justifies it.

### `avbench` CLI commands (in-VM program)

- `avbench setup` — install tools, clone repos, hydrate dependencies, write manifests
- `avbench run` — execute scenarios, collect metrics, write `run.json` and `runs.csv`. Requires `--name` to label this VM's AV configuration.

### `avbench-compare` CLI (host program)

- `avbench-compare --baseline <dir> --input <dir> [--input <dir> ...] --output <dir>`

### `setup` — automated environment provisioning

`avbench setup` takes a clean Windows VM (Server 2022, Windows 11, etc.) and makes it ready to run benchmarks. It installs all required tools, clones repos, and hydrates dependencies.

The setup is idempotent — re-running it skips already-installed tools and already-cloned repos.

#### Tool installation

Each tool is installed silently using its official unattended installer. The setup checks whether the tool is already present before downloading.

| Tool | Install method | Detection |
|---|---|---|
| Git for Windows | `Git-*-64-bit.exe /VERYSILENT /NORESTART` | `git --version` |
| Visual Studio Build Tools 2022 | `vs_buildtools.exe --quiet --wait --add Microsoft.VisualStudio.Workload.VCTools --includeRecommended` | `vswhere -products *` |
| CMake | MSI silent install or bundled with VS Build Tools | `cmake --version` |
| Ninja | Download release zip, extract to PATH | `ninja --version` |
| .NET SDK | `dotnet-sdk-*-win-x64.exe /quiet /norestart` | `dotnet --list-sdks` |
| Rust (rustup) | `rustup-init.exe -y --default-toolchain stable` | `rustc --version` |
| Python 3.x | `python-*-amd64.exe /quiet InstallAllUsers=1 PrependPath=1` | `python --version` |
| Nuitka + deps | `pip install nuitka ordered-set` | `python -m nuitka --version` |
| Windows App SDK 1.8 | NuGet restore (auto via `msbuild /t:Restore`) | Restored by build |
| Windows ADK (optional) | Only if `--trace` support is wanted | `wpr -help` |

#### Repo cloning and dependency hydration

After tools are installed:

1. Clone each repo to a pinned location (e.g., `C:\bench\<repo>`).
2. Checkout pinned commit SHA.
3. Read repo manifests where possible:
   - `global.json` (Roslyn .NET SDK version)
   - `Cargo.toml` / `rust-toolchain.toml` (ripgrep Rust version)
   - `pyproject.toml` (Black Python requirements)
   - `CMakeLists.txt` (LLVM CMake version requirements)
4. Hydrate dependencies (untimed):
   - Roslyn: `Restore.cmd`
   - ripgrep: `cargo fetch`
   - Black/Nuitka: `python -m venv` + `pip install`
   - LLVM: CMake configure (generates Ninja build files)
5. Write `suite-manifest.json` (repos, SHAs, tool versions).

`setup` must not silently upgrade toolchains once a campaign has started. Re-running `setup` after a campaign starts should detect and warn about version drift.

#### VM noise reduction

Optionally run `setup-test-vm.ps1` to disable Windows Update, Superfetch, search indexing, and other background services that add measurement noise.

### `run`

Responsibilities:

1. Load suite manifest.
2. Validate setup is complete (tools present, repos cloned, deps hydrated).
3. Idle check: refuse if system CPU > threshold.
4. For each scenario block:
   a. Warmup run (discarded).
   b. For each repetition:
      - Create Windows Job object (`CreateJobObject`).
      - Launch workload process, assign to Job (`AssignProcessToJobObject`).
      - Stream stdout/stderr to log files.
      - On completion, query `JOBOBJECT_BASIC_AND_IO_ACCOUNTING_INFORMATION` and `JOBOBJECT_EXTENDED_LIMIT_INFORMATION` for metrics.
      - Validate exit code and expected artifacts.
      - Write `run.json`.
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
- `CreateProcess` with `CREATE_SUSPENDED` — create the child
- `AssignProcessToJobObject` — attach child to job before it runs
- `ResumeThread` — let the child start
- `WaitForSingleObject` — wait for child exit
- `QueryInformationJobObject` with `JobObjectBasicAndIoAccountingInformation` and `JobObjectExtendedLimitInformation` — get CPU, I/O, memory metrics

Child processes inherit the job assignment by default (nested jobs work on Windows 8+/Server 2012+), so the entire build process tree is captured.

### API microbench worker

A dedicated C# console app launched by the runner. The same Job object measurement wraps it.

Behavior:

- One benchmark family per invocation.
- Explicit warmup period (discarded).
- Fixed-iteration measurement window.
- Batch timing to reduce per-op harness noise.
- Writes results to stdout as JSON, captured by the runner.

### Collector layer

Default:

- **`JobAccountingCollector`** — queries Job object accounting on process exit.

Opt-in:

- **`WprCollector`** — starts/stops `wpr.exe` for ETW tracing.
- **`PerfCounterCollector`** — starts/stops `typeperf.exe` for sampled system counters.

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

- One warmup run per scenario block (discarded).
- At least 5 measured repetitions per scenario.
- Report mean, median, stdev, and coefficient of variation (CV).
- Mark a scenario as `noisy` if CV > 10%.
- Randomize scenario order within a run when possible.
- Never compare a single best run against another single best run.

## Workload-Specific Notes

### LLVM

- Treat `cmake` configure and `ninja` build as separate scenarios.
- Keep generator (`Ninja`) and project set (`clang`, X86-only) fixed for the full campaign.
- Store build dir outside the source tree for easy cleanup.
- LLVM clone should use `git clone --config core.autocrlf=false` on Windows.
- LLVM + Clang Release build needs ~15-20 GB disk space.

### ripgrep

- Best candidate for early runner development — fast builds, simple toolchain.
- Use as the first end-to-end scenario in Milestone 1.
- `cargo build --release` is the baseline; `--features pcre2` is optional.
- Minimum Rust: 1.85.0 stable.

### Roslyn

- Separate `Restore.cmd` (untimed) from `Build.cmd` (timed).
- Requires Visual Studio 2022 Preview + .NET SDK matching `global.json`.
- Solution file is `Roslyn.slnx`.
- Watch for compiler server (`VBCSCompiler.exe`) behavior — it stays resident and may affect subsequent runs.

### Black + Nuitka

- Create a small `black_entry.py` wrapper as the Nuitka entry point.
- Keep `standalone` and `onefile` as separate scenarios.
- Smoke-test the produced executable after build (run `black --version` or format a small file).

### Files (WinUI 3)

- Requires VS Build Tools with WinUI workload, .NET 10 SDK, Windows 11 SDK 10.0.26100.0, Windows App SDK 1.8.
- Most prerequisites overlap with LLVM (MSVC, Win SDK) and Roslyn (.NET SDK, VS Build Tools). The incremental cost is the WinUI workload component and .NET 10 SDK.
- Solution is `Files.slnx` — build with `msbuild Files.slnx /p:Configuration=Release /p:Platform=x64`.
- Run `msbuild /t:Restore` as untimed setup before the timed build.
- For incremental-build, touch a `.cs` file in `src/Files.App/` (e.g., `App.xaml.cs` or a ViewModel file).
- XAML compilation and CsWin32 source generation are key differentiators from Roslyn — they exercise MSBuild targets that generate many intermediate files and trigger AV scanning.
- Watch for `Files.App.Server` and C++ native projects (`Files.App.Launcher`, `Files.App.OpenDialog`, `Files.App.SaveDialog`) — they link against MSVC and may add noise. Measure the full solution build to capture the realistic mixed workload.

## What To Build First

### Milestone 1

Deliverables:

- `avbench setup` — automated tool installation (Git, Rust) + ripgrep repo clone + `cargo fetch`
- `avbench run` — ripgrep compile scenarios + one API microbench (`file-create-delete`)
- Job object process-tree runner with default metrics
- JSON output (`run.json`)
- CSV flattening (`runs.csv`)

Target workloads:

- ripgrep clean/incremental/noop build
- `file-create-delete` API microbench

Why: ripgrep needs only Git + Rust, so setup automation is minimal. One API microbench gives immediate AV overhead signal.

### Milestone 2

- Extend `avbench setup` to install VS Build Tools, CMake, Ninja, .NET SDK (10.0.102 for Files)
- Add Roslyn compile scenarios
- Add LLVM compile scenarios
- Add Files (WinUI 3) compile scenarios — clone repo, `msbuild /t:Restore`, then timed build
- Build `avbench-compare` — reads results from multiple directories, produces `compare.csv` and `summary.md`

### Milestone 3

- Extend `avbench setup` to install Python, Nuitka
- Add Black + Nuitka compile scenarios
- Add remaining API microbench families
- Add `--trace` (WPR) and `--counters` (typeperf) opt-in collectors

### Milestone 4

- Auto-detect installed AV product and version (Microsoft Defender, Huorong, ESET, Bitdefender, TrendMicro)
- Record detected `av_product` and `av_version` in `run.json`
- `--av-name` and `--av-version` CLI overrides for unsupported products or manual testing

## References

- Windows Job Objects: https://learn.microsoft.com/en-us/windows/win32/procthread/job-objects
- `JOBOBJECT_BASIC_AND_IO_ACCOUNTING_INFORMATION`: https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_basic_and_io_accounting_information
- `JOBOBJECT_BASIC_ACCOUNTING_INFORMATION`: https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_basic_accounting_information
- Windows Performance Toolkit (WPR/WPA): https://learn.microsoft.com/en-us/windows-hardware/test/wpt/
- Windows Performance Recorder: https://learn.microsoft.com/en-us/windows-hardware/test/wpt/windows-performance-recorder
- Event Tracing for Windows: https://learn.microsoft.com/en-us/windows/win32/etw/about-event-tracing
- `typeperf`: https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/typeperf
- LLVM getting started: https://github.com/llvm/llvm-project/blob/main/llvm/docs/GettingStarted.rst
- LLVM Visual Studio guide: https://github.com/llvm/llvm-project/blob/main/llvm/docs/GettingStartedVS.rst
- ripgrep README: https://github.com/BurntSushi/ripgrep/blob/master/README.md
- Roslyn Windows build guide: https://github.com/dotnet/roslyn/blob/main/docs/contributing/Building%2C%20Debugging%2C%20and%20Testing%20on%20Windows.md
- Nuitka user manual: https://nuitka.net/doc/user-manual.html
- Black README: https://github.com/psf/black/blob/main/README.md
- Files build guide: https://files.community/docs/contributing/building-from-source
- Files repo: https://github.com/files-community/Files
- Windows App SDK downloads: https://learn.microsoft.com/windows/apps/windows-app-sdk/downloads
