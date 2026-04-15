# Performance Metrics

## Overview

`avbench` captures two families of metrics depending on the workload type:

- **Compile workloads** (ripgrep, Roslyn): Run as external processes inside a Win32 Job Object. Metrics come from the OS kernel.
- **API microbenchmarks**: Run in-process using `Stopwatch` (QPC). Metrics come from per-operation timing.

Both types also get **typeperf system counter samples** written alongside every result.

---

## Per-scenario metrics (run.json)

Every scenario produces a `run.json` with these fields:

| Field | Type | Unit | Source | Applies to |
|---|---|---|---|---|
| `wall_ms` | long | milliseconds | QPC Stopwatch | All |
| `user_cpu_ms` | long | milliseconds | Job Object accounting | Compile workloads |
| `kernel_cpu_ms` | long | milliseconds | Job Object accounting | Compile workloads |
| `peak_job_memory_mb` | long | megabytes | Job Object accounting | Compile workloads |
| `io_read_bytes` | ulong | bytes | Job Object I/O counters | Compile workloads |
| `io_write_bytes` | ulong | bytes | Job Object I/O counters | Compile workloads |
| `io_read_ops` | ulong | count | Job Object I/O counters | Compile workloads |
| `io_write_ops` | ulong | count | Job Object I/O counters | Compile workloads |
| `total_processes` | uint | count | Job Object accounting | Compile workloads |
| `p50_us` | double? | microseconds | LatencyHistogram | Microbench only |
| `p95_us` | double? | microseconds | LatencyHistogram | Microbench only |
| `p99_us` | double? | microseconds | LatencyHistogram | Microbench only |
| `max_us` | double? | microseconds | LatencyHistogram | Microbench only |

Plus metadata: `scenario_id`, `av_name`, `av_product`, `av_version`, `timestamp_utc`, `command`, `working_dir`, `exit_code`, `machine` (OS/CPU/RAM), `runner_version`, `suite_manifest_sha`.

Microbench scenarios include an embedded `microbench` object with:

| Field | Type | Unit |
|---|---|---|
| `batch_size` | int | operations per batch |
| `total_operations` | int | total ops executed |
| `ops_per_sec` | double | throughput |
| `mean_latency_us` | double | microseconds |
| `p50_us` | double | microseconds |
| `p95_us` | double | microseconds |
| `p99_us` | double | microseconds |
| `max_us` | double | microseconds |

---

## What each metric means and why it matters

### Wall time (`wall_ms`)

**What:** Elapsed real time from scenario start to finish, measured via `Stopwatch` (QPC-based, sub-microsecond).

**Why:** This is the metric the user feels. If an AV product adds 30 seconds to a 2-minute build, that's directly visible. Wall time captures everything — CPU delays, I/O serialization from minifilter scanning, kernel callback overhead, and contention with AV background threads.

### User CPU time (`user_cpu_ms`)

**What:** Total time the CPU spent executing user-mode code across **all processes** in the Job Object process tree.

**Why:** A clean build of Roslyn spawns dozens of MSBuild worker processes. The Job Object sums all their user-mode time. An increase in user CPU without a corresponding increase in kernel CPU suggests the workload itself is doing more computation (e.g., AV injecting user-mode hooks that add instruction overhead).

### Kernel CPU time (`kernel_cpu_ms`)

**What:** Total time the CPU spent executing kernel-mode code across all processes in the Job Object tree.

**Why:** This is the AV signal. Minifilter callbacks (`FltRegisterFilter`), process/thread/image-load notify routines (`PsSetCreate*NotifyRoutine`), registry callbacks (`CmRegisterCallbackEx`), and WFP callout drivers all execute in kernel mode. On a baseline system, kernel time is dominated by filesystem I/O and process management. When AV is active, kernel time increases because every file open, process create, and registry access runs additional kernel-mode code in the AV driver stack.

**Kernel CPU as a percentage of total CPU** (`kernel_cpu_pct`) is computed during comparison:  
$\text{kernel\\_cpu\\_pct} = \frac{\text{kernel\\_cpu\\_ms}}{\text{user\\_cpu\\_ms} + \text{kernel\\_cpu\\_ms}} \times 100$

A shift from, say, 15% → 35% kernel CPU tells you the AV product is adding significant kernel-mode overhead.

### Peak memory (`peak_job_memory_mb`)

**What:** High-water mark of committed memory across the entire Job Object process tree.

**Why:** AV can increase memory through injected DLLs (user-mode hooks), additional thread stacks, and cached scan verdicts. Large increases indicate the AV product allocates significant per-process memory.

### I/O counters (`io_read_bytes`, `io_write_bytes`, `io_read_ops`, `io_write_ops`)

**What:** Total bytes transferred and number of I/O operations, as reported by the kernel for all processes in the Job Object.

**Why:** AV minifilters intercept every `IRP_MJ_READ` and `IRP_MJ_WRITE`. Some products issue additional I/O operations — reading signature databases from disk, writing scan logs, or communicating with a cloud verdict service. Comparing baseline I/O ops vs. AV-enabled I/O ops reveals this extra activity.

### Latency percentiles (`p50_us`, `p95_us`, `p99_us`, `max_us`)

**What:** Per-operation latency distribution from the `LatencyHistogram`. Each micro-operation records `Stopwatch.GetTimestamp()` before and after, stores the tick delta, and computes percentiles after all operations complete.

**Why:** Mean latency hides tail effects. AV scanning is biased toward worst-case behavior — the first time a file hash is seen, the first time a DLL path is loaded, the first time a network connection is made. These cold-path penalties appear in **p95/p99/max** even when the mean looks reasonable. A p99 of 10 ms when p50 is 0.1 ms reveals that 1-in-100 operations pays a 100x penalty.

### Operations per second (`ops_per_sec`)

**What:** `total_operations / elapsed_seconds` for microbench scenarios.

**Why:** Throughput metric, easy to compare: "baseline does 50,000 file creates/sec, with AV X it drops to 12,000 file creates/sec."

---

## Comparison metrics (compare.csv / summary.md)

`avbench-compare` computes these derived metrics across sessions:

| Column | Meaning |
|---|---|
| `sessions` | Number of VM sessions (run.json files) aggregated |
| `mean_wall_ms` | Average wall time across sessions |
| `median_wall_ms` | Median wall time (robust to outliers) |
| `mean_cpu_ms` | Average total CPU time (user + kernel) |
| `kernel_cpu_pct` | Kernel CPU as % of total CPU for this AV config |
| `baseline_kernel_cpu_pct` | Same metric for the baseline |
| `kernel_cpu_slowdown_pct` | Percentage-point shift in kernel CPU usage (AV − baseline) |
| `peak_memory_mb` | Max peak memory across sessions |
| `slowdown_pct` | Wall-time slowdown vs. baseline: $(mean_{AV} - mean_{baseline}) / mean_{baseline} \times 100$ |
| `cv_pct` | Coefficient of variation of wall time across sessions |
| `status` | `ok`, `noisy` (CV > 10%), or `failed` |

### Interpreting slowdown %

- **+5% to +15%**: Typical for well-configured consumer AV on compile workloads.
- **+15% to +50%**: Notable overhead; consider exclusion rules or product tuning.
- **+50% to +200%**: Severe; file-heavy workloads (archive extraction, PE writing) often land here.
- **> +200%**: Usually indicates a specific interception pathology (e.g., every file create triggers a cloud lookup).

### Interpreting kernel CPU shift

A large positive `kernel_cpu_slowdown_pct` (e.g., +20 pp) pinpoints that the overhead is in the AV kernel driver, not in user-mode processing. This distinction matters because:

- **Kernel overhead** → minifilter, registry callbacks, process notify → fixable with exclusion policies.
- **User overhead** → user-mode hook trampolines, injected DLLs → product architecture, harder to exclude.

### Interpreting CV %

Coefficient of variation above 10% flags a **noisy** scenario. Common causes:

- AV signature update ran during the session
- Background OS tasks (Windows Update, indexer)
- Thermal throttling on the VM host

Noisy results should not be used for product comparison without additional sessions.

---

## System counter samples (counters.csv)

Each scenario folder contains a `counters.csv` from `typeperf` with 1-second samples:

| Counter | Purpose |
|---|---|
| `\Processor(_Total)\% Processor Time` | Whether the CPU saturated during the scenario |
| `\PhysicalDisk(_Total)\Disk Bytes/sec` | Total disk throughput |
| `\PhysicalDisk(_Total)\Disk Read Bytes/sec` | Read-side throughput |
| `\PhysicalDisk(_Total)\Disk Write Bytes/sec` | Write-side throughput |
| `\Memory\Available MBytes` | Whether memory pressure caused paging |
| `\Memory\Pages/sec` | Hard page faults (symptom of memory contention) |

These are for **diagnostics**, not primary comparison. They help explain *why* a run was slow, not *how much* slower it was.
