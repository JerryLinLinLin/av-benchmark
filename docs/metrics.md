# Metrics

`avbench` is trying to answer three practical questions:

1. How much slower did the workload get?
2. Where did that extra cost show up?
3. Is the result clean enough to trust?

The metrics are easiest to read when you keep those questions separate. `wall_ms` and `slowdown_pct` answer the first one. CPU split, memory, system disk I/O, and latency distribution help with the second. `cv_pct` and `status` help with the third.

The tool also has two measurement paths:

- **Compile workloads** (`ripgrep`, `roslyn`) run as child processes inside a Windows Job Object, so `avbench` can read CPU, I/O, memory, and process counts from Windows job accounting.[1][2][3]
- **Microbench workloads** run in-process and focus on per-operation latency and throughput. On Windows, `.NET` `Stopwatch` uses `QueryPerformanceCounter` as its precise time base.[4]

That distinction is visible in the data: microbench runs do not populate the Job Object counters used by compile workloads, so the process-tree CPU, memory, and I/O fields are left at `0`.

## What to read first

For most readers, the right order is:

1. `wall_ms` or `slowdown_pct`
2. `p95_us` and `p99_us` for microbench scenarios
3. `kernel_cpu_pct`, `peak_memory_mb`, and the I/O counters
4. `cv_pct` and `status`

That order matters. A benchmark can show a large kernel CPU shift and still have little user-visible slowdown. It can also show a dramatic p99 regression while the mean barely moves. The document is easier to read when the top-line answer comes first and the diagnostic clues come second.

## What each output file is for

Each scenario directory contains:

- `run.json`: the result for one scenario run
- `stdout.log` / `stderr.log`: captured process output

`run.json` is the comparison input.

## `run.json`

### Common fields

These fields exist for every scenario:

| Field | Meaning |
|---|---|
| `scenario_id` | Scenario identifier |
| `av_name` | Label for the AV configuration being tested |
| `av_product` / `av_version` | Detected product metadata |
| `timestamp_utc` | When the result was written |
| `command` | External command line for compile workloads; scenario ID for microbench runs |
| `working_dir` | Scenario working directory |
| `exit_code` | Process exit code, or `0` for successful in-process microbench runs |
| `wall_ms` | End-to-end elapsed time |
| `machine` | OS, CPU, RAM, and storage metadata |
| `runner_version` | Runner version string |
| `suite_manifest_sha` | Manifest hash used for the run |

### Process-tree metrics

These fields are populated for external-process workloads:

| Field | Meaning |
|---|---|
| `user_cpu_ms` | Total user-mode CPU time across the whole Job Object tree |
| `kernel_cpu_ms` | Total kernel-mode CPU time across the whole Job Object tree |
| `peak_job_memory_mb` | Peak committed memory for the Job Object, converted to MiB |

Windows defines these totals over the full lifetime of the job, including terminated child processes.[2][3] That is why they are useful for parallel builds.

### System-wide disk I/O

These fields are populated for **all** scenarios (compile and microbench):

| Field | Meaning |
|---|---|
| `system_disk_read_bytes` | Total bytes read from disk across the entire system during the scenario |
| `system_disk_write_bytes` | Total bytes written to disk across the entire system during the scenario |

These are captured via .NET `PerformanceCounter` snapshots (before and after the scenario) of the cumulative `PhysicalDisk(_Total)` counters. Unlike the Job Object I/O counters, these include disk activity from **all processes on the machine** — including AV service processes that scan files, read signature databases, write scan caches, or communicate with cloud verdict services. The baseline-vs-AV delta directly reveals AV-attributed disk overhead.

### Microbench metrics

Microbench scenarios populate the nested `microbench` object:

| Field | Meaning |
|---|---|
| `batch_size` | Operations grouped by the implementation; not a concurrency setting |
| `total_operations` | Number of measured operations |
| `ops_per_sec` | Throughput |
| `mean_latency_us` | Mean latency per operation |
| `p50_us` / `p95_us` / `p99_us` / `max_us` | Latency distribution |

The top-level `p50_us`, `p95_us`, `p99_us`, and `max_us` fields are convenience copies of the same percentile values.

## How to interpret the important metrics

### `wall_ms`

Start here. `wall_ms` is the direct answer to "how long did the workload take?" If AV makes a build slower in a way a developer feels, that effect will show up here.

This is the best primary metric for compile workloads because it includes the full effect of the system under test: CPU work, I/O delays, waiting, and contention.

### `slowdown_pct`

`slowdown_pct` is the comparison form of wall time, always computed from **median** values:

`(median_wall_ms - baseline_median_wall_ms) / baseline_median_wall_ms * 100`

Median is used instead of mean because it is robust to outliers at any sample size. When data is clean (low CV), median ≈ mean and nothing is lost. When data is noisy, median avoids the distortion that a single outlier session can cause. Using the same statistic on both sides also avoids mixed-comparison artifacts.

This is usually the number you want in a summary table or executive comparison. It translates the raw timing difference into a form that is easy to compare across scenarios with very different runtimes.

### `first_run_wall_ms` and `first_run_slowdown_pct`

`first_run_wall_ms` preserves the earliest successful run for that AV/scenario pair, ordered by `timestamp_utc`. `baseline_first_run_wall_ms` is the same value for the baseline.

`first_run_slowdown_pct` compares the AV first run with the baseline steady-state median:

`(first_run_wall_ms - baseline_median_wall_ms) / baseline_median_wall_ms * 100`

This is intentionally separate from the median slowdown. Median wall time is the steadier repeated-run signal; first-run wall time is the cold/cloud-sensitive signal. AV products may use reputation, cloud verdicts, or scan caches that survive local VM snapshot resets, so the first observed successful run can tell a different story than the median of repeated runs.

### `p95_us` and `p99_us`

For microbench scenarios, tail latency is usually more informative than the mean.

Security products often make the worst-case path more expensive than the steady-state path. That means a scenario can look acceptable on average while still producing noticeable stalls in the tail. A reader who only looks at `mean_latency_us` can miss the main story.

As a rule:

- `p50_us` tells you what happens most of the time
- `p95_us` tells you whether slow operations are becoming common
- `p99_us` tells you whether the tail is getting ugly
- `max_us` is a useful clue, not a stable comparison metric

`LatencyHistogram` computes these percentiles from the recorded operation samples using a nearest-rank index after sorting the data.

### `user_cpu_ms` and `kernel_cpu_ms`

These numbers show where the measured CPU time accumulated inside the workload process tree.

- Higher `user_cpu_ms` means more work happened in user mode.
- Higher `kernel_cpu_ms` means more work happened in kernel mode while servicing that workload.

Because compile workloads can run many processes in parallel, total CPU time can exceed wall time. That is expected.

The kernel split is useful because Windows security products commonly add work along kernel-mediated paths such as file-system minifilters, process and image-load notifications, registry callbacks, and Windows Filtering Platform callouts.[6][7][8][9] A rising kernel share is therefore a strong clue that the extra cost is happening in those paths. It is still a clue, not a proof of a single root cause.

### `kernel_cpu_pct`

`kernel_cpu_pct` is derived during comparison:

`kernel_cpu_ms / (user_cpu_ms + kernel_cpu_ms) * 100`

This is better than raw `kernel_cpu_ms` when you are comparing scenarios with different total CPU demand. It tells you how much of the CPU budget moved into kernel work.

### `peak_job_memory_mb`

This is a peak, not an average. It is useful for spotting whether a configuration materially increases the memory footprint of the measured process tree.

Treat it as a supporting signal. A memory increase can matter, but it usually matters less than wall time unless it is large enough to push the system into pressure or paging.

### `system_disk_read_bytes` and `system_disk_write_bytes`

These fields capture total disk I/O across the entire system during a scenario. They are the primary disk metric for AV analysis because AV overhead I/O — signature database reads, scan cache writes, cloud verdict traffic — happens in AV service processes that run **outside** the benchmarked process tree.

Comparing baseline vs. AV system disk writes directly answers "how much extra disk activity did AV cause?" For example, if a Roslyn clean build produces 3 GB of system-wide disk writes on baseline and 4.5 GB with AV enabled, the 1.5 GB difference is AV-attributed.

Because these are system-wide, they include background OS activity (indexer, updater). The idle checker mitigates this, and across multiple sessions the background noise averages out. Absolute values matter less than the delta between configurations.

## `compare.csv` and `summary.md`

`avbench-compare` groups results by `av_name` and `scenario_id` and computes one comparison row per group.

Important implementation detail: first-run values are computed from the earliest successful run, ordered by `timestamp_utc`. Steady-state aggregates such as `mean_wall_ms`, `median_wall_ms`, `cv_pct`, disk averages, CPU averages, and latency medians are computed from the remaining successful runs after that first successful run is removed. `sessions` still counts all runs seen for that scenario group, while `steady_state_samples` tells you how many runs contributed to the steady-state aggregate.

The main derived columns in `compare.csv` are:

| Column | Meaning |
|---|---|
| `sessions` | Number of discovered runs for that AV/scenario pair |
| `baseline_sessions` | Number of discovered baseline runs for the same scenario |
| `steady_state_samples` | Number of successful AV runs remaining after excluding the first successful run |
| `baseline_steady_state_samples` | Number of successful baseline runs remaining after excluding the first successful baseline run |
| `mean_wall_ms` | Mean wall time across steady-state samples |
| `median_wall_ms` | Median wall time across steady-state samples |
| `first_run_wall_ms` | Earliest successful wall time for that AV/scenario pair, ordered by `timestamp_utc` |
| `baseline_first_run_wall_ms` | Earliest successful baseline wall time for the same scenario |
| `mean_cpu_ms` | Mean total CPU time (`user + kernel`) across steady-state samples |
| `kernel_cpu_pct` | Kernel share of total CPU for that AV/scenario pair |
| `baseline_kernel_cpu_pct` | Same metric for the baseline |
| `kernel_cpu_slowdown_pct` | Percentage-point difference from baseline |
| `peak_memory_mb` | Maximum peak job memory across steady-state samples |
| `slowdown_pct` | Wall-time slowdown versus baseline, computed from median values |
| `first_run_slowdown_pct` | Wall-time slowdown versus baseline, computed from AV first-run wall time versus baseline steady-state median wall time |
| `cv_pct` | Coefficient of variation of AV wall time |
| `baseline_cv_pct` | Coefficient of variation of baseline wall time |
| `status` | `ok`, `failed`, `insufficient`, `noisy`, or `anomaly` |

`summary.md` shows a narrower table focused on the columns that are meaningful for every scenario:

```
| Scenario | Median Wall (ms) | First-Run Wall (ms) | Slowdown | First-Run Slowdown | p95 Slowdown | Disk Read Δ (MB) | Disk Write Δ (MB) | CV % | Baseline CV % | Status |
```

Kernel CPU shift and peak memory are omitted from the summary table because they are zero for all 27 microbenchmarks (which run in-process without Job Object accounting). When a compilation scenario has a significant kernel CPU shift, it appears as a footnote callout below the table. The full data remains in `compare.csv` for detailed analysis.

Rows in each `summary.md` table use the suite's fixed scenario order instead of sorting by slowdown. This keeps reports stable between AV products and between repeated comparisons. The ranked callouts below each table, such as highest slowdown or largest disk delta, still sort by the metric they summarize.

When two or more AV inputs are compared, `summary.md` emits two cross-AV tables:

- `Cross-AV steady-state comparison` uses median wall-time slowdown after excluding each side's earliest successful run.
- `Cross-AV first-run comparison` uses each AV's earliest successful wall time against the baseline steady-state median, which is useful for cloud reputation and scan-cache effects.

Only the steady-state cross-AV table marks `noisy` and `insufficient` cells with `*`. The first-run table does not inherit those markers because CV and steady-state sample count are not meaningful for a single first-run sample. In the first-run table, `failed*` means no successful first run was available, and a negative first-run slowdown is marked with `*` as an anomaly.

Status is assigned like this:

- `failed`: at least one run in the group failed, or no successful runs exist
- `insufficient`: fewer than 3 steady-state samples remain on either side after first-run exclusion
- `anomaly`: all runs succeeded, but the AV side appears faster than baseline, which usually points to cache or ordering artifacts rather than a real security-product speedup
- `noisy`: all runs succeeded and steady-state slowdown is non-negative, but `cv_pct > 10` or `baseline_cv_pct > 10` on the steady-state samples
- `ok`: all runs succeeded, both `cv_pct` and `baseline_cv_pct` are at or below 10, and steady-state slowdown is non-negative

That makes `status` intentionally conservative. If even one run failed, the row is marked `failed` even when some successful samples were available.

### First-run exclusion

The comparison engine always separates the earliest successful run from the repeated-run aggregate. The first run is reported through `first_run_wall_ms` and `first_run_slowdown_pct`. All steady-state aggregates, including median wall time, mean CPU, system disk averages, latency medians, and CV, use only the successful runs after that first successful run.

No additional outlier run is removed after the first-run split. This is deliberate: first-run cost is where cloud reputation and scan-cache effects tend to appear, while CV is meant to describe the remaining steady-state repeatability. The exclusion only affects the comparison aggregation layer; raw `run.json` and `runs.csv` files are never modified.

For microbench scenarios, the comparison columns derived from `user_cpu_ms`, `kernel_cpu_ms`, and `peak_job_memory_mb` stay at `0` because those source fields are not populated by the in-process execution path.

## How to judge result quality

### `cv_pct` and `baseline_cv_pct`

`cv_pct` is the coefficient of variation of the AV side's wall time:

`standard deviation / mean * 100`

`baseline_cv_pct` is the same metric for the baseline side.

Both are needed because noisy steady-state results on either side make the `slowdown_pct` unreliable. A 2% slowdown in a scenario with a 9% AV CV but a 15% baseline CV is weak evidence - the baseline instability alone could explain the difference.

Note: the CV values reported in `compare.csv` and `summary.md` always exclude the earliest successful run and do not exclude any other run. Check `steady_state_samples` / `baseline_steady_state_samples` to see how many samples contributed to CV and median wall time.

### `status`

`status` is not a severity score. It is a reliability flag.

- `ok` means the repeated measurements were stable enough for ordinary comparison.
- `noisy` means the scenario likely needs more investigation or more runs.
- `failed` means you should not treat the comparison row as clean evidence.
- `insufficient` means there are too few steady-state samples to make a reliable comparison.
- `anomaly` means the direction of the result is suspicious enough that it should be investigated before drawing conclusions.

## The shortest useful reading of a result

If you only have a minute, read a result like this:

1. Did `wall_ms` or `slowdown_pct` move enough to matter?
2. If this is a microbench, did `p95_us` or `p99_us` get much worse?
3. Did system disk I/O move in the same direction and support the story? (For compilation scenarios, also check `kernel_cpu_pct` in `compare.csv`.)
4. Is `cv_pct` and `baseline_cv_pct` low enough, and is `status` clean enough, to trust the conclusion?

That sequence usually gets you to the right interpretation faster than reading every field one by one.

## References

1. [JOBOBJECT_BASIC_AND_IO_ACCOUNTING_INFORMATION](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_basic_and_io_accounting_information)
2. [JOBOBJECT_BASIC_ACCOUNTING_INFORMATION](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_basic_accounting_information)
3. [JOBOBJECT_EXTENDED_LIMIT_INFORMATION](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_extended_limit_information)
4. [Acquiring high-resolution time stamps](https://learn.microsoft.com/en-us/windows/win32/sysinfo/acquiring-high-resolution-time-stamps)
5. [PerformanceCounter class](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.performancecounter)
6. [Filter Manager and minifilter concepts](https://learn.microsoft.com/en-us/windows-hardware/drivers/ifs/filter-manager-concepts)
7. [PsSetCreateProcessNotifyRoutineEx](https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntddk/nf-ntddk-pssetcreateprocessnotifyroutineex)
8. [CmRegisterCallbackEx](https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/wdm/nf-wdm-cmregistercallbackex)
9. [About Windows Filtering Platform](https://learn.microsoft.com/en-us/windows/win32/fwp/about-windows-filtering-platform)
