# Performance Metrics

## Overview

`avbench` captures two families of metrics:

- **Compile workloads** (ripgrep, Roslyn): external processes inside a [Win32 Job Object](https://learn.microsoft.com/en-us/windows/win32/procthread/job-objects). Metrics come from kernel accounting structures ([`JOBOBJECT_BASIC_AND_IO_ACCOUNTING_INFORMATION`](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_basic_and_io_accounting_information), [`JOBOBJECT_EXTENDED_LIMIT_INFORMATION`](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_extended_limit_information)).
- **API microbenchmarks**: in-process, timed with [`Stopwatch`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.stopwatch) (backed by [`QueryPerformanceCounter`](https://learn.microsoft.com/en-us/windows/win32/api/profileapi/nf-profileapi-queryperformancecounter)).

Both types include **`typeperf` system counter samples** written alongside every result.

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

Plus metadata: `scenario_id`, `av_name`, `av_product`, `av_version`, `timestamp_utc`, `command`, `working_dir`, `exit_code`, `machine` (OS, CPU, RAM, storage type), `runner_version`, `suite_manifest_sha`.

Microbench scenarios include an embedded `microbench` object:

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

**What:** Elapsed real time from scenario start to finish, measured via `Stopwatch` (QPC-based, sub-microsecond resolution on modern hardware).

**Why:** The metric the end user feels. If an AV product adds 30 seconds to a 2-minute build, that is directly visible. Wall time captures everything: CPU delays, I/O serialization from minifilter scanning, kernel callback overhead, and contention with AV background threads.

### User CPU time (`user_cpu_ms`)

**What:** Total time the CPU spent executing user-mode code across **all processes** in the Job Object tree. Derived from `JOBOBJECT_BASIC_ACCOUNTING_INFORMATION.TotalUserTime` (100 ns ticks, converted to milliseconds).

**Why:** A Roslyn build spawns dozens of MSBuild worker processes. The Job Object sums all their user-mode time. An increase in user CPU without a corresponding increase in kernel CPU suggests extra user-mode computation — for example, AV-injected DLLs adding instruction overhead in each process.

### Kernel CPU time (`kernel_cpu_ms`)

**What:** Total time the CPU spent executing kernel-mode code across all processes in the Job Object tree. Derived from `JOBOBJECT_BASIC_ACCOUNTING_INFORMATION.TotalKernelTime`.

**Why:** The primary AV-overhead signal. AV drivers intercept system activity through kernel-mode mechanisms:

- **Minifilter callbacks** — registered via [`FltRegisterFilter`](https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/fltkernel/nf-fltkernel-fltregisterfilter); intercept file-system I/O at the [Filter Manager](https://learn.microsoft.com/en-us/windows-hardware/drivers/ifs/filter-manager-concepts) layer.
- **Process/thread/image-load notify routines** — [`PsSetCreateProcessNotifyRoutineEx`](https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntddk/nf-ntddk-pssetcreateprocessnotifyroutineex), [`PsSetCreateThreadNotifyRoutine`](https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntddk/nf-ntddk-pssetcreatethreadnotifyroutine), [`PsSetLoadImageNotifyRoutine`](https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntddk/nf-ntddk-pssetloadimagenotifyroutine).
- **Registry callbacks** — [`CmRegisterCallbackEx`](https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/wdm/nf-wdm-cmregistercallbackex).
- **WFP callout drivers** — network-layer filtering via the [Windows Filtering Platform](https://learn.microsoft.com/en-us/windows/win32/fwp/windows-filtering-platform-start-page).

On a baseline system kernel time is dominated by filesystem I/O and process management. When AV is active, every file create, process launch, and registry access runs additional kernel-mode code in the AV driver stack.

**Kernel CPU as a percentage of total CPU** (`kernel_cpu_pct`) is computed during comparison:

$$\text{kernel\_cpu\_pct} = \frac{\text{kernel\_cpu\_ms}}{\text{user\_cpu\_ms} + \text{kernel\_cpu\_ms}} \times 100$$

A shift from 15 % to 35 % kernel CPU indicates substantial kernel-mode overhead from the AV driver.

### Peak memory (`peak_job_memory_mb`)

**What:** High-water mark of committed memory across the Job Object tree, from `JOBOBJECT_EXTENDED_LIMIT_INFORMATION.PeakJobMemoryUsed` (bytes, converted to MB).

**Why:** AV can increase memory through injected DLLs, additional thread stacks, and cached scan verdicts. Large increases point to significant per-process allocations by the AV product.

### I/O counters (`io_read_bytes`, `io_write_bytes`, `io_read_ops`, `io_write_ops`)

**What:** Total bytes transferred and I/O operation count from the kernel's [`IO_COUNTERS`](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-io_counters) for all processes in the Job Object.

**Why:** AV minifilters sit in the I/O path and intercept every [`IRP_MJ_CREATE`](https://learn.microsoft.com/en-us/windows-hardware/drivers/ifs/irp-mj-create), [`IRP_MJ_READ`](https://learn.microsoft.com/en-us/windows-hardware/drivers/ifs/irp-mj-read), and [`IRP_MJ_WRITE`](https://learn.microsoft.com/en-us/windows-hardware/drivers/ifs/irp-mj-write). Some products issue additional I/O: reading signature databases, writing scan logs, or querying a cloud verdict service. Comparing baseline I/O ops against AV-enabled I/O ops reveals this extra activity.

### Latency percentiles (`p50_us`, `p95_us`, `p99_us`, `max_us`)

**What:** Per-operation latency distribution from the `LatencyHistogram`. Each micro-operation records `Stopwatch.GetTimestamp()` before and after, stores the tick delta, and computes percentiles (nearest-rank method) after all operations complete.

**Why:** Mean latency hides tail effects. AV scanning penalizes cold paths disproportionately — the first time a file hash is seen, the first DLL load, the first network connection. These penalties appear in **p95/p99/max** even when the mean looks reasonable. A p99 of 10 ms with a p50 of 0.1 ms means 1-in-100 operations pays a 100× penalty.

### Operations per second (`ops_per_sec`)

**What:** `total_operations / elapsed_seconds` for microbench scenarios.

**Why:** Direct throughput comparison: "baseline does 50,000 file creates/sec; with AV X it drops to 12,000/sec."

---

## Comparison metrics (compare.csv / summary.md)

`avbench-compare` computes these derived metrics across sessions:

| Column | Meaning |
|---|---|
| `sessions` | Number of run.json files aggregated |
| `mean_wall_ms` | Arithmetic mean of wall time across sessions |
| `median_wall_ms` | Median wall time (robust to outliers) |
| `mean_cpu_ms` | Mean total CPU time (user + kernel) |
| `kernel_cpu_pct` | Kernel CPU as % of total CPU for this AV config |
| `baseline_kernel_cpu_pct` | Same metric for the baseline |
| `kernel_cpu_slowdown_pct` | Percentage-point shift in kernel CPU (AV − baseline) |
| `peak_memory_mb` | Max peak memory across sessions |
| `slowdown_pct` | Wall-time slowdown vs. baseline: $(\bar{x}_{AV} - \bar{x}_{baseline}) / \bar{x}_{baseline} \times 100$ |
| `cv_pct` | Coefficient of variation (population σ / μ × 100) of wall time |
| `status` | `ok`, `noisy` (CV > 10 %), or `failed` |

### Interpreting slowdown %

- **+5 % to +15 %**: Typical for well-configured consumer AV on compile workloads.
- **+15 % to +50 %**: Notable overhead; consider exclusion rules or product tuning.
- **+50 % to +200 %**: Severe; file-heavy workloads (archive extraction, PE writing) often land here.
- **> +200 %**: Usually indicates a specific interception pathology (e.g., every file create triggers a synchronous cloud lookup).

### Interpreting kernel CPU shift

A large positive `kernel_cpu_slowdown_pct` (e.g., +20 pp) pinpoints overhead in the AV kernel driver rather than in user-mode processing. This distinction matters:

- **Kernel overhead** — minifilter, registry callbacks, process notify routines — can often be reduced with exclusion policies.
- **User overhead** — hook trampolines, injected DLLs — stems from product architecture and is harder to exclude.

### Interpreting CV %

Coefficient of variation above 10 % flags a **noisy** scenario. Common causes:

- AV signature update ran during the session.
- Background OS tasks (Windows Update, Search Indexer).
- Thermal throttling on the VM host.

Noisy results should not be used for product comparison without additional sessions.

---

## System counter samples (counters.csv)

Each scenario folder contains a `counters.csv` from [`typeperf`](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/typeperf) with 1-second samples:

| Counter | Purpose |
|---|---|
| `\Processor(_Total)\% Processor Time` | Whether the CPU saturated during the scenario |
| `\PhysicalDisk(_Total)\Disk Bytes/sec` | Total disk throughput |
| `\PhysicalDisk(_Total)\Disk Read Bytes/sec` | Read-side throughput |
| `\PhysicalDisk(_Total)\Disk Write Bytes/sec` | Write-side throughput |
| `\Memory\Available MBytes` | Whether memory pressure caused paging |
| `\Memory\Pages/sec` | Pages read from or written to disk to resolve page faults — indicates memory contention |

These counters are for **diagnostics**, not primary comparison. They help explain *why* a run was slow, not *how much* slower it was.
