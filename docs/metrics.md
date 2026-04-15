# Metrics

`avbench` is trying to answer three practical questions:

1. How much slower did the workload get?
2. Where did that extra cost show up?
3. Is the result clean enough to trust?

The metrics are easiest to read when you keep those questions separate. `wall_ms` and `slowdown_pct` answer the first one. CPU split, memory, I/O, and latency distribution help with the second. `cv_pct`, `status`, and `counters.csv` help with the third.

The tool also has two measurement paths:

- **Compile workloads** (`ripgrep`, `roslyn`) run as child processes inside a Windows Job Object, so `avbench` can read CPU, I/O, memory, and process counts from Windows job accounting.[1][2][3]
- **Microbench workloads** run in-process and focus on per-operation latency and throughput. On Windows, `.NET` `Stopwatch` uses `QueryPerformanceCounter` as its precise time base.[4]

That distinction is visible in the data: microbench runs do not populate the Job Object counters used by compile workloads, so the process-tree CPU, memory, and I/O fields are left at `0`.

## What to read first

For most readers, the right order is:

1. `wall_ms` or `slowdown_pct`
2. `p95_us` and `p99_us` for microbench scenarios
3. `kernel_cpu_pct`, `peak_memory_mb`, and the I/O counters
4. `cv_pct`, `status`, and `counters.csv`

That order matters. A benchmark can show a large kernel CPU shift and still have little user-visible slowdown. It can also show a dramatic p99 regression while the mean barely moves. The document is easier to read when the top-line answer comes first and the diagnostic clues come second.

## What each output file is for

Each scenario directory contains:

- `run.json`: the result for one scenario run
- `counters.csv`: 1-second system samples collected with `typeperf`[5]

`run.json` is the comparison input. `counters.csv` is supporting evidence when a run looks noisy or unexpectedly slow.

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
| `io_read_bytes` / `io_write_bytes` | Total bytes read and written by processes in the job |
| `io_read_ops` / `io_write_ops` | Total read and write operations issued by processes in the job |
| `total_processes` | Total processes associated with the job during its lifetime |

Windows defines these totals over the full lifetime of the job, including terminated child processes.[2][3] That is why they are useful for parallel builds.

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

`slowdown_pct` is the comparison form of `wall_ms`:

`(mean_wall_ms - baseline_mean_wall_ms) / baseline_mean_wall_ms * 100`

This is usually the number you want in a summary table or executive comparison. It translates the raw timing difference into a form that is easy to compare across scenarios with very different runtimes.

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

### `io_read_*` and `io_write_*`

These fields describe I/O issued by the benchmarked processes in the job.[1][3] They are good for answering questions like:

- Did the workload itself perform more reads or writes?
- Did the operation count jump even when bytes moved did not?

They are **not** a full accounting of everything the AV product did on the machine. They do not directly include work performed by a kernel driver or by AV service processes running outside the job.

### `total_processes`

This is mostly a context field. It tells you how much process fan-out the workload created. It helps explain why some scenarios are naturally more exposed to process-creation and image-load overhead than others.

## `compare.csv` and `summary.md`

`avbench-compare` groups results by `av_name` and `scenario_id` and computes one comparison row per group.

Important implementation detail: averages are computed from **successful runs only** (`exit_code == 0`), but `sessions` still counts all runs seen for that scenario group. A row can therefore show `sessions = 5` even if only 4 runs contributed to the mean.

The main derived columns are:

| Column | Meaning |
|---|---|
| `sessions` | Number of discovered runs for that AV/scenario pair |
| `mean_wall_ms` | Mean wall time across successful runs |
| `median_wall_ms` | Median wall time across successful runs |
| `mean_cpu_ms` | Mean total CPU time (`user + kernel`) across successful runs |
| `kernel_cpu_pct` | Kernel share of total CPU for that AV/scenario pair |
| `baseline_kernel_cpu_pct` | Same metric for the baseline |
| `kernel_cpu_slowdown_pct` | Percentage-point difference from baseline |
| `peak_memory_mb` | Maximum peak job memory across successful runs |
| `slowdown_pct` | Wall-time slowdown versus baseline |
| `cv_pct` | Coefficient of variation of wall time |
| `status` | `ok`, `noisy`, or `failed` |

Status is assigned like this:

- `failed`: at least one run in the group failed, or no successful runs exist
- `noisy`: all runs succeeded, but `cv_pct > 10`
- `ok`: all runs succeeded and `cv_pct <= 10`

That makes `status` intentionally conservative. If even one run failed, the row is marked `failed` even when some successful samples were available.

For microbench scenarios, the comparison columns derived from `user_cpu_ms`, `kernel_cpu_ms`, and `peak_job_memory_mb` stay at `0` because those source fields are not populated by the in-process execution path.

## How to judge result quality

### `cv_pct`

`cv_pct` is the coefficient of variation of wall time:

`standard deviation / mean * 100`

This is the simplest way to ask whether repeated runs are stable. A 2% slowdown in a scenario with a 9% CV is weak evidence. The same 2% slowdown in a scenario with a 1% CV is much more believable.

### `status`

`status` is not a severity score. It is a reliability flag.

- `ok` means the repeated measurements were stable enough for ordinary comparison.
- `noisy` means the scenario likely needs more investigation or more runs.
- `failed` means you should not treat the comparison row as clean evidence.

### `counters.csv`

`counters.csv` is there to explain suspicious runs, not to replace the primary metrics. The current collector records:

| Counter | What it helps diagnose |
|---|---|
| `\Processor(_Total)\% Processor Time` | CPU saturation |
| `\PhysicalDisk(_Total)\Disk Bytes/sec` | Total disk throughput |
| `\PhysicalDisk(_Total)\Disk Read Bytes/sec` | Read throughput |
| `\PhysicalDisk(_Total)\Disk Write Bytes/sec` | Write throughput |
| `\Memory\Available MBytes` | Whether available RAM fell low enough to suggest memory pressure[10] |
| `\Memory\Pages/sec` | Paging traffic to resolve hard page faults; useful when correlated with low available memory[11] |

Two cautions matter here:

- `\Memory\Pages/sec` is not the same thing as "hard faults per second."
- A counter spike is evidence of pressure, not automatic proof that AV caused it.

## The shortest useful reading of a result

If you only have a minute, read a result like this:

1. Did `wall_ms` or `slowdown_pct` move enough to matter?
2. If this is a microbench, did `p95_us` or `p99_us` get much worse?
3. Did `kernel_cpu_pct`, memory, or I/O move in the same direction and support the story?
4. Is `cv_pct` low enough, and is `status` clean enough, to trust the conclusion?

That sequence usually gets you to the right interpretation faster than reading every field one by one.

## References

1. [JOBOBJECT_BASIC_AND_IO_ACCOUNTING_INFORMATION](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_basic_and_io_accounting_information)
2. [JOBOBJECT_BASIC_ACCOUNTING_INFORMATION](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_basic_accounting_information)
3. [JOBOBJECT_EXTENDED_LIMIT_INFORMATION](https://learn.microsoft.com/en-us/windows/win32/api/winnt/ns-winnt-jobobject_extended_limit_information)
4. [Acquiring high-resolution time stamps](https://learn.microsoft.com/en-us/windows/win32/sysinfo/acquiring-high-resolution-time-stamps)
5. [typeperf](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/typeperf)
6. [Filter Manager and minifilter concepts](https://learn.microsoft.com/en-us/windows-hardware/drivers/ifs/filter-manager-concepts)
7. [PsSetCreateProcessNotifyRoutineEx](https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntddk/nf-ntddk-pssetcreateprocessnotifyroutineex)
8. [CmRegisterCallbackEx](https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/wdm/nf-wdm-cmregistercallbackex)
9. [About Windows Filtering Platform](https://learn.microsoft.com/en-us/windows/win32/fwp/about-windows-filtering-platform)
10. [Troubleshoot performance problems in Windows](https://learn.microsoft.com/en-us/troubleshoot/windows-server/performance/troubleshoot-performance-problems-in-windows)
11. [Useful Performance Counters](https://learn.microsoft.com/en-us/host-integration-server/core/useful-performance-counters2)
