# Workloads

`avbench` has three workload families, each targeting a different aspect of how AV impacts Windows use.

## Workload families at a glance

| Workload ID | Type | Scenarios | What it measures |
|---|---|---|---|
| `ripgrep` | Compile (Rust/cargo) | 2 | Full native-code build pipeline via LLVM |
| `roslyn` | Compile (C#/dotnet) | 2 | Managed-code build pipeline via MSBuild |
| `microbench` | API micro-operations | 27 | Individual Windows API call overhead |

---

## Compile workloads

### Why compile builds?

Compilation is one of the most AV-sensitive activities on a developer or CI machine. A single build:

- Creates, reads, writes, and deletes **thousands of files** (source, objects, intermediates, outputs)
- Spawns **dozens of processes** (compiler, linker, MSBuild workers)
- Loads **hundreds of DLLs** (SDK, toolchain, analyzers)
- Performs **heavy I/O**: reading source from disk, writing object files, linking final binaries

Every one of these operations passes through the AV kernel [minifilter](https://learn.microsoft.com/en-us/windows-hardware/drivers/ifs/filter-manager-concepts), process-notify callbacks, and image-load-notify callbacks. The aggregate overhead on a large build is measurable.

Two languages with different toolchains cover distinct AV interaction patterns:

### `ripgrep` — Rust / cargo / LLVM

**Repository:** [BurntSushi/ripgrep](https://github.com/BurntSushi/ripgrep)  
**Toolchain:** Rust 1.85.0, cargo, LLVM backend  
**Build command:** `cargo build --release`

| Scenario | ID | What happens |
|---|---|---|
| Clean build | `ripgrep-clean-build` | Delete `target/`, then `cargo build --release`. Compiles ~300 crates from scratch. |
| Incremental build | `ripgrep-incremental-build` | Touch a core `.rs` source file, then `cargo build --release`. Recompiles only the changed crate and downstream dependents. |

**Why ripgrep?**
- Medium-sized Rust project (~300 crates) — large enough to stress AV, small enough to finish in minutes.
- Pure Rust with no C/C++ dependencies — isolates the cargo/LLVM toolchain.
- Deterministic: pinned toolchain version (1.85.0), `cargo fetch` during setup pre-downloads all crate sources.

**AV interception exercised:**
- **File minifilter:** thousands of `.rlib`, `.rmeta`, `.o`, `.d` files created/read/written/deleted.
- **Process notify:** cargo spawns per-crate `rustc` processes.
- **Image load notify:** `rustc` loads LLVM codegen DLLs.
- **Registry callbacks:** `rustc` and LLVM read toolchain configuration paths.

### `roslyn` — C# / dotnet / MSBuild

**Repository:** [dotnet/roslyn](https://github.com/dotnet/roslyn)  
**Toolchain:** .NET SDK (per repo's `global.json`), MSBuild, Visual Studio Build Tools  
**Build command:** `dotnet build "Roslyn.slnx" -c Release /m /nr:false`

| Scenario | ID | What happens |
|---|---|---|
| Clean build | `roslyn-clean-build` | Delete `artifacts/bin` and `artifacts/obj`, then build the full solution. |
| Incremental build | `roslyn-incremental-build` | Touch a core `.cs` file in `Compilers/Core/Portable/`, triggering a cascade rebuild through downstream assemblies. |

**Why Roslyn?**
- Large managed-code solution (~180 projects) — representative of upper-end .NET builds.
- Heavy MSBuild parallelism (`/m`) spawns many worker processes.
- Exercises both the compiler (`csc.dll` in-process) and analyzer infrastructure.
- `/nr:false` (no node reuse) forces fresh MSBuild worker processes each build — no cached AV verdicts from prior sessions.

**AV interception exercised:**
- **File minifilter:** massive `.dll`, `.pdb`, `.cs`, `.xml` I/O across hundreds of project output directories.
- **Process notify:** MSBuild spawns worker nodes, dotnet SDK host processes.
- **Image load notify:** hundreds of SDK/analyzer DLLs loaded per build.
- **Registry callbacks:** MSBuild reads the registry for toolset resolution.

### Incremental builds — why they matter

The incremental scenario simulates the most common developer action: **edit one file, rebuild**. It measures per-file AV marginal cost by touching a "core" source file that cascades through dependent projects/crates. The `SourceFileToucher` advances the file's last-write time by at least 2 seconds to reliably trigger the build system's change detection.

If AV adds 5 seconds to a 90-second clean build, that is 5.5 %. If AV adds 3 seconds to a 4-second incremental build, that is 75 % — far more disruptive to the edit-build-test loop.

---

## API microbenchmarks

### Design philosophy

The microbench suite tests **individual Windows API call costs** in isolation. Each bench:

1. Calls one API (or a small API sequence) in a tight loop for a fixed number of operations.
2. Records per-operation latency via [`Stopwatch.GetTimestamp()`](https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.stopwatch.gettimestamp) (QPC-backed, sub-microsecond resolution on modern hardware).
3. Computes throughput (ops/sec) and latency percentiles (p50/p95/p99/max).

API selection criteria:

- **Primary:** Operations commonly performed by Windows applications — file managers, browsers, installers, Office, IDEs, cloud sync tools, databases.
- **Secondary:** APIs known to be intercepted by security software (kernel callbacks, user-mode hooks), where AV overhead is expected to be measurable.
- **Not a goal:** Exhaustive coverage of every hookable API. The focus is on operations that real applications perform frequently.

> **Reference:** For public data on which ntdll/kernel32 APIs are commonly intercepted by endpoint security products, see [Mr-Un1k0d3r/EDRs](https://github.com/Mr-Un1k0d3r/EDRs).

### No warmup

Every iteration is measured cold. A discarded warmup would prime the AV cache and hide the real overhead users experience on first launch, first file access, or first network connection.

### Complete microbench catalog

#### Tier 1 — File I/O (minifilter layer)

These exercise the kernel file system [minifilter](https://learn.microsoft.com/en-us/windows-hardware/drivers/ifs/filter-manager-concepts) — the single largest source of AV overhead for file-heavy workloads.

| Scenario ID | Ops | Description | Why this test |
|---|---:|---|---|
| `file-create-delete` | 5,000 | Create + delete small temp files in batches of 100. | The most basic file operation. Every application creates temp files. Directly measures per-file minifilter overhead for [`IRP_MJ_CREATE`](https://learn.microsoft.com/en-us/windows-hardware/drivers/ifs/irp-mj-create) + `IRP_MJ_CLEANUP`. |
| `archive-extract` | 10 iters | Extract a pre-built zip with ~2,000 mixed files (`.cs`, `.js`, `.dll`, `.exe`, `.json`, `.xml`, sizes 64 B–64 KB), then delete the tree. | Simulates package restore (NuGet, npm, pip) and installer extraction. The burst of heterogeneous file creates with mixed extensions stresses AV extension-based dispatch and content scanning. Varying file content prevents verdict caching. |
| `ext-sensitivity-exe` | 10,000 | Create + write + delete files with `.exe` extension (random content). | `.exe` files trigger PE header parsing and signature checks in the minifilter. |
| `ext-sensitivity-dll` | 10,000 | Same with `.dll` extension. | `.dll` files also trigger PE content scanning. |
| `ext-sensitivity-js` | 10,000 | Same with `.js` extension. | `.js` files trigger script heuristic scanning in some AV products. |
| `ext-sensitivity-ps1` | 10,000 | Same with `.ps1` extension. | `.ps1` files trigger PowerShell script heuristic scanning. |
| `file-write-content` | 10,000 | Clone the unsigned `noop.exe` template, patch 4 bytes to create a unique hash, write as `.exe`/`.dll`, delete. | Forces **full PE content inspection** on every write. The unique hash means AV cannot cache the verdict — it must parse the PE header, compute the hash, and check signature status for every operation. Isolates worst-case AV cost for writing executable content. |
| `file-enum-large-dir` | 50 iters | Enumerate a pre-created directory with ~10,000 files. | IDEs, `git status`, file sync tools, and search indexers enumerate large directories constantly. Exercises [`NtQueryDirectoryFile`](https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntifs/nf-ntifs-ntquerydirectoryfile) through the minifilter. |
| `file-copy-large` | 10 iters | Copy a ~100 MB file, then delete the copy. | Measures sustained minifilter overhead on bulk data transfer. Build outputs, installer packages, and large artifacts follow this pattern. |
| `hardlink-create` | 5,000 | Create a hard link then delete it. | npm/pnpm use hard links for package deduplication. Each hard link traverses the minifilter via `NtSetInformationFile(FileLinkInformation)`. |
| `junction-create` | 2,000 | Create a directory junction then delete it. | Used for monorepo workspace linking and `node_modules` hoisting. Exercises [`FSCTL_SET_REPARSE_POINT`](https://learn.microsoft.com/en-us/windows/win32/api/winioctl/ni-winioctl-fsctl_set_reparse_point) through the minifilter. |

#### Tier 2 — Process, thread, DLL/image (kernel notify callbacks)

These exercise the kernel [`PsSetCreateProcessNotifyRoutineEx`](https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntddk/nf-ntddk-pssetcreateprocessnotifyroutineex), [`PsSetCreateThreadNotifyRoutine`](https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntddk/nf-ntddk-pssetcreatethreadnotifyroutine), and [`PsSetLoadImageNotifyRoutine`](https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/ntddk/nf-ntddk-pssetloadimagenotifyroutine) callbacks.

| Scenario ID | Ops | Description | Why this test |
|---|---:|---|---|
| `process-create-wait` | 500 | Spawn an unsigned `noop.exe`, wait for exit. | An unsigned exe forces a full on-execute AV scan — no trust-cache shortcut. Measures the per-process-create cost that impacts build tools, CI pipelines, and application launchers. |
| `dll-load-unique` | 2,000 | Copy a system DLL to a unique temp path, `LoadLibrary`, `FreeLibrary`, delete. | Bypasses the Windows section cache and forces AV to scan each copy as a "new" DLL. Applications that generate or extract DLLs at runtime (plugin systems, build tools) hit this path. |
| `motw-exe-no-motw` | 500 | Copy unsigned exe to temp path, execute, delete. No `Zone.Identifier`. | Baseline for comparing MOTW overhead. Tests the process-create path for locally-created binaries. |
| `motw-exe-motw-zone3` | 500 | Same as above, but stamp `Zone.Identifier` with `ZoneId=3` (Internet zone) before executing. | Triggers [SmartScreen](https://learn.microsoft.com/en-us/windows/security/operating-system-security/virus-and-threat-protection/microsoft-defender-smartscreen/) reputation lookup. The delta vs. no-motw isolates the cost of executing "downloaded from the internet" binaries — exactly what happens with CI artifacts, GitHub releases, and package-extracted tools. |
| `thread-create` | 5,000 | `new Thread()` → `Start()` → `Join()`. | Exercises `NtCreateThreadEx` and the kernel thread-notify callback. The .NET runtime, browser engines, and database servers all create threads dynamically. |

#### Tier 3 — Memory (user-mode hooks)

Memory operations are core primitives that security software monitors for process injection patterns.

| Scenario ID | Ops | Description | Why this test |
|---|---:|---|---|
| `mem-alloc-protect` | 50,000 | `VirtualAlloc(RW)` → `VirtualProtect(RX)` → `VirtualFree`. | The RW→RX transition is a pattern security software watches for. The .NET JIT, V8 JIT, and any code-generation engine perform this on every method compilation. `NtAllocateVirtualMemory` and `NtProtectVirtualMemory` are among the most widely intercepted ntdll APIs. |
| `mem-map-file` | 10,000 | `CreateFileMapping` → `MapViewOfFile` → read → `UnmapViewOfFile`. | Exercises `NtCreateSection` / `NtMapViewOfSection` — the same path used for DLL loading, PE image mapping, and memory-mapped databases (SQLite, LMDB). |

#### Tier 4 — Network ([WFP](https://learn.microsoft.com/en-us/windows/win32/fwp/windows-filtering-platform-start-page) callout drivers)

Network filtering happens in kernel mode via Windows Filtering Platform callout drivers.

| Scenario ID | Ops | Description | Why this test |
|---|---:|---|---|
| `net-connect-loopback` | 2,000 | TCP connect → send 1 KB → recv → close against a local echo server. | Each connection triggers the WFP `FWPM_LAYER_ALE_AUTH_CONNECT` callout. This is the hot path for every HTTP request, package download, API call, and database connection. Loopback isolates AV WFP overhead from actual network latency. |
| `net-dns-resolve` | 5,000 | `Dns.GetHostEntry("localhost")` in a loop. | DNS queries transit the WFP layer. Applications that connect to remote services resolve hostnames constantly. |

#### Tier 5 — Registry ([kernel callbacks](https://learn.microsoft.com/en-us/windows-hardware/drivers/kernel/filtering-registry-calls))

| Scenario ID | Ops | Description | Why this test |
|---|---:|---|---|
| `registry-crud` | 5,000 | Create key → set 5 values (REG_SZ, DWORD, BINARY, MULTI_SZ, EXPAND_SZ) → query each → enumerate → delete. Under `HKCU\Software\AvBench\Temp`. | Security software monitors registry operations via [`CmRegisterCallbackEx`](https://learn.microsoft.com/en-us/windows-hardware/drivers/ddi/wdm/nf-wdm-cmregistercallbackex). Installers, application settings, COM registration lookups, and system tools all perform heavy registry I/O. Five value types cover the major registry data paths. |

#### Tier 6 — IPC (named pipes through minifilter)

| Scenario ID | Ops | Description | Why this test |
|---|---:|---|---|
| `pipe-roundtrip` | 2,000 | Create named pipe server → client connect → write 4 KB → read → disconnect. | Named pipes are file system objects (`\Device\NamedPipe\`) and transit the minifilter stack. Build tools, database servers, Docker, and many Windows services use pipes for inter-process communication. |

#### Tier 7 — Security & crypto

| Scenario ID | Ops | Description | Why this test |
|---|---:|---|---|
| `token-query` | 50,000 | `OpenProcessToken` → `GetTokenInformation(TokenPrivileges)` → `CloseHandle`. | Every elevated application, installer, service, and UAC-aware tool queries token privileges. Security software may intercept the underlying `NtOpenProcessToken` syscall via SSDT hooks or user-mode detours. |
| `crypto-hash-verify` | 5,000 | SHA-256 hash a 64 KB buffer + RSA-2048 signature verification. | Simulates package signature verification, Authenticode checks, and HTTPS handshake crypto. Not directly hooked, but AV's own concurrent signature verification competes for CPU and cache resources. |

#### Tier 8 — COM & WMI

| Scenario ID | Ops | Description | Why this test |
|---|---:|---|---|
| `com-create-instance` | 5,000 | `Activator.CreateInstance(Type.GetTypeFromProgID("Scripting.FileSystemObject"))` + `Marshal.ReleaseComObject`. | COM activation exercises the class factory, DLL loading (image-load notify), and registry CLSID lookup (registry callbacks). Office applications, shell extensions, management tools, and many Windows applications use COM extensively. |
| `wmi-query` | 500 | `ManagementObjectSearcher("SELECT ProcessId, Name FROM Win32_Process WHERE ProcessId = {pid}")`. | WMI exercises COM + named pipes (DCOM) + registry + process enumeration. System monitoring, hardware inventory, and management tools use WMI. |

#### Tier 9 — File system notifications

| Scenario ID | Ops | Description | Why this test |
|---|---:|---|---|
| `fs-watcher` | 5,000 | Set up `FileSystemWatcher`, then create + delete files rapidly. Measures per-operation latency under an active watcher. | IDEs, file sync tools, cloud storage clients, and build systems use [`ReadDirectoryChangesW`](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-readdirectorychangesw). The minifilter sits in the notification delivery path. Measures whether AV adds latency to the combined file-operation + notification pipeline. |

---

## Microbench operation counts

Operation counts balance **statistical stability** (enough ops for reliable percentiles) against **total runtime** (all 27 scenarios complete in roughly 5–10 minutes):

| Range | Rationale |
|---|---|
| 50,000 ops | Very fast operations (VirtualAlloc, token query) — microsecond-scale, need many ops for stable distributions |
| 5,000–10,000 ops | Standard-speed operations (file create, registry, thread create, crypto) — tens-of-microseconds-scale |
| 2,000 ops | Moderate operations (DLL load, pipe, TCP connect) — sub-millisecond |
| 500 ops | Slow operations (process spawn, MOTW execute, WMI query) — millisecond-scale |
| 10–50 iterations | Batch operations (archive extract, large file copy, directory enum) — multi-millisecond each |

---

## Summary — scenario ID reference

| # | Scenario ID | Family | Tier |
|---|---|---|---|
| 1 | `file-create-delete` | File I/O | 1 |
| 2 | `archive-extract` | File I/O | 1 |
| 3 | `ext-sensitivity-exe` | File I/O | 1 |
| 4 | `ext-sensitivity-dll` | File I/O | 1 |
| 5 | `ext-sensitivity-js` | File I/O | 1 |
| 6 | `ext-sensitivity-ps1` | File I/O | 1 |
| 7 | `file-write-content` | File I/O | 1 |
| 8 | `file-enum-large-dir` | File I/O | 1 |
| 9 | `file-copy-large` | File I/O | 1 |
| 10 | `hardlink-create` | File I/O | 1 |
| 11 | `junction-create` | File I/O | 1 |
| 12 | `process-create-wait` | Process | 2 |
| 13 | `dll-load-unique` | DLL/Image | 2 |
| 14 | `motw-exe-no-motw` | MOTW | 2 |
| 15 | `motw-exe-motw-zone3` | MOTW | 2 |
| 16 | `thread-create` | Thread | 2 |
| 17 | `mem-alloc-protect` | Memory | 3 |
| 18 | `mem-map-file` | Memory | 3 |
| 19 | `net-connect-loopback` | Network | 4 |
| 20 | `net-dns-resolve` | Network | 4 |
| 21 | `registry-crud` | Registry | 5 |
| 22 | `pipe-roundtrip` | IPC | 6 |
| 23 | `token-query` | Security | 7 |
| 24 | `crypto-hash-verify` | Crypto | 7 |
| 25 | `com-create-instance` | COM | 8 |
| 26 | `wmi-query` | WMI | 8 |
| 27 | `fs-watcher` | FS notify | 9 |
