# Workloads

`avbench` uses three workload families and 31 scenarios in total:

- 2 `ripgrep` build scenarios
- 2 `roslyn` build scenarios
- 27 microbench scenarios

The design is deliberate. Real builds answer the question users actually care about: "does this AV configuration slow down normal work?" Microbench scenarios answer the follow-up question at a lower level: "which Windows API call paths got slower, and are the biggest regressions showing up on ordinary APIs or on security-sensitive ones?" You need both views. Build workloads are realistic but messy. Microbench workloads are narrower, but much easier to attribute.

## Workload families at a glance

| Family | Scenarios | What it is good at |
|---|---:|---|
| `ripgrep` | 2 | Native-code build behavior with `cargo` |
| `roslyn` | 2 | Large managed-code build behavior with `dotnet` and MSBuild |
| `microbench` | 27 | Measuring API-call overhead across common Windows paths and security-sensitive call paths |

## Selector behavior

`avbench setup` accepts workload families only:

- `ripgrep`
- `roslyn`
- `microbench`
- `all`

`avbench run` accepts those same family IDs and also accepts specific microbench scenario IDs when you want to run a narrower slice of the suite. For example:

- `--workload microbench` runs all 27 microbench scenarios
- `--workload file-create-delete` runs only the `file-create-delete` scenario
- `--workload ripgrep,file-create-delete` runs both ripgrep build scenarios plus the single named microbench scenario

Legacy shortcuts such as `--workload file` are not supported.

## Compile workloads

Builds are useful benchmark targets because they combine several things security products often care about at the same time:

- lots of file creation, reads, writes, and deletes
- repeated process launches
- many DLL and toolchain loads
- enough total runtime for small overheads to become visible

That makes build workloads a good first check for whether a product is meaningfully affecting developer or CI work.

Each build family has two scenarios:

- **clean build**: delete the main build output directories and build from scratch
- **incremental build**: ensure outputs already exist, touch one source file, then measure the rebuild

That staging matters. The incremental scenario is not "whatever happened after the last run." The runner first performs an untimed prerequisite build when needed, then advances the touched source file's last-write timestamp by at least two seconds so the build system sees a real change.

### `ripgrep`

**Repository:** [BurntSushi/ripgrep](https://github.com/BurntSushi/ripgrep)  
**Source policy:** latest release by default, or a user-specified ref via `--ripgrep-ref`  
**Toolchain:** Rust `1.85.0`, `cargo`  
**Measured command:** `cargo build --release`

| Scenario ID | What the runner does |
|---|---|
| `ripgrep-clean-build` | Deletes `target/`, then runs `cargo build --release` |
| `ripgrep-incremental-build` | Ensures a previous build exists, touches a Rust source file, then runs `cargo build --release` again |

Why this workload is in the suite:

- It is a real, nontrivial Rust codebase, not a synthetic compiler stress test.
- The build creates a large number of intermediate artifacts, which makes file-path overhead visible.
- `cargo` fans work out across repeated compiler invocations, so process and image-load cost can also matter.
- Setup runs `cargo fetch` first, which removes dependency download noise from the measured build itself.

One practical detail matters here: `ripgrep` is fairly stable by default because setup resolves it from the latest release unless you override the revision. The exact commit used for a run is recorded in `suite-manifest.json`.

### `roslyn`

**Repository:** [dotnet/roslyn](https://github.com/dotnet/roslyn)  
**Source policy:** default branch head at setup time  
**Toolchain:** `.NET` SDK from the repo's `global.json`, plus Visual Studio Build Tools as required by that repo state  
**Setup hydration:** `Restore.cmd`  
**Measured command:** `dotnet build "Roslyn.slnx" -c Release /m /nr:false`

| Scenario ID | What the runner does |
|---|---|
| `roslyn-clean-build` | Deletes `artifacts/bin` and `artifacts/obj`, then builds the solution |
| `roslyn-incremental-build` | Ensures build outputs already exist, touches a C# source file under `src/Compilers/Core/Portable` when possible, then rebuilds |

Why this workload is in the suite:

- It represents a large .NET/MSBuild build rather than a small app template.
- It stresses a different toolchain shape than `ripgrep`: more MSBuild coordination, different file types, different loader behavior, and different restore/build steps.
- `/m` enables parallel build execution, which makes scheduler and process-tree overhead easier to surface.
- `/nr:false` disables MSBuild node reuse, which keeps each measured build closer to a fresh process-launch workload.

The main logistical difference from `ripgrep` is source stability. By default, `roslyn` is taken from the repository's default branch head at setup time, so it is not a fixed workload across calendar time unless you preserve and reuse the same prepared benchmark image. The exact repo URL and commit SHA are recorded in the suite manifest so runs remain auditable.

### Why both build families matter

Using both families is more valuable than running either one alone.

- `ripgrep` gives a native-code build with `cargo` orchestration.
- `roslyn` gives a large managed-code build with MSBuild orchestration.

If both move in the same direction, confidence increases that the result reflects a broad build-time effect rather than a quirk of one toolchain. If they diverge, that is also useful: it often means the product interacts differently with file churn, process launch patterns, or framework-specific build behavior.

## Microbench workloads

The microbench suite exists to measure how AV affects the performance of calling Windows APIs, or thin runtime wrappers over those APIs.

That goal has two parts:

- cover APIs that ordinary Windows applications call all the time
- cover APIs and call patterns that are often more security-sensitive, and therefore more likely to be inspected, filtered, or hooked by AV and endpoint products

That is why the suite includes both mundane paths such as file create/delete, directory enumeration, named pipes, DNS resolution, and COM activation, and more sensitive paths such as process launch, DLL load, protection changes, executable-content writes, registry churn, and MOTW-marked execution.

The suite complements the build workloads rather than replacing them. If a build gets slower, microbench scenarios help identify which API families are likely responsible. If a build does not get slower, microbench scenarios can still expose narrow API-level regressions that only show up on certain call paths.

The API names below describe the implementation-level path the benchmark intentionally exercises. Some scenarios call Win32 APIs directly through P/Invoke, such as `CreateHardLink`, `LoadLibrary`, `VirtualAlloc`, `OpenProcessToken`, and `FSCTL_SET_REPARSE_POINT`. Others intentionally use .NET wrappers such as `File.WriteAllBytes`, `ZipFile.ExtractToDirectory`, `MemoryMappedFile`, `TcpClient`, `RegistryKey`, `FileSystemWatcher`, and `ManagementObjectSearcher`, because those are the APIs many managed Windows tools actually call.

For those .NET scenarios, the tables also name the relevant native Windows primitive where the current .NET Windows runtime source makes that mapping clear. Treat those native names as a measurement aid, not as a public .NET compatibility contract: the benchmark chooses the managed API surface, while the notes explain the Windows path that is expected on the current runtime.

### Design rules

The microbench suite follows a few simple rules:

- Each scenario targets one Windows API family, or one small call sequence built around a specific API path.
- Inputs are prepared locally so the benchmark does not depend on network downloads during the measured phase.
- Each scenario runs in a fresh working directory.
- Operation counts are high enough to produce stable percentile data without making the whole suite take too long.
- The suite does not perform a benchmark-wide warmup pass that would deliberately hide first-touch cost. Individual scenarios may still use a small targeted warmup when needed to remove harness noise from the API path being measured.

Support assets are generated locally during setup:

- a zip archive containing 2,000 mixed files with extensions such as `.cs`, `.js`, `.json`, `.xml`, `.dll`, `.exe`, `.txt`, and `.md`
- an unsigned `noop.exe` built from a tiny local `net8.0` project
- a 10,000-file enumeration dataset for the large-directory test
- a 100 MB source file for the large-copy test

That keeps the scenarios reproducible and makes it clear what API path is actually being measured.

Each scenario reports both whole-scenario `wall_ms` and microbench latency percentiles. The latency histogram is recorded around the per-operation API sequence called out in the table. Support assets and helper datasets are normally created before that measured loop, so they do not inflate the per-operation percentiles. Small per-iteration preparation can still be visible in `wall_ms` when it happens after the scenario stopwatch starts; the rows below call this out where it matters.

### How to read the microbench suite

The suite is best read as a set of API-path probes rather than as a flat list of 27 tests.

#### File-system and content-path APIs

These scenarios probe file-system API paths that are both common in normal software and frequently inspected by security products.

| Scenario ID | API path involved | Test data and operation count | Per-operation measured action | Why it is in the suite |
|---|---|---|---|---|
| `file-create-delete` | .NET `File.Create` / `File.Delete`; on Windows this reaches `CreateFile`, `WriteFile`, and `DeleteFile` through FileStream / System.IO | 5,000 files; one 64-byte zero buffer is written to each file; loop is grouped internally in batches of 100 | Per operation, create `bench_XXXXX.tmp`, write 64 zero bytes, close the stream, and delete the file; the histogram starts before create and ends after delete | Baseline file-create/file-delete overhead on the most common file-system path |
| `archive-extract` | .NET `ZipFile.ExtractToDirectory` plus recursive delete; extraction ultimately exercises System.IO create/write paths such as `CreateFile` and `WriteFile` | Setup creates one zip with 2,000 deterministic random files; extensions cycle across `.cs`, `.js`, `.json`, `.xml`, `.dll`, `.exe`, `.txt`, `.md`; sizes cycle through 64 B, 256 B, 1 KB, 4 KB, 16 KB, 64 KB; `.dll` and `.exe` entries start with `MZ`; 10 extract/delete iterations | Per operation, extract the entire zip into a fresh `extract_XX` directory, then recursively delete that directory; both extract and delete are in the histogram | Stresses bursty multi-file create/write/delete behavior seen in package restore, unpack, and installer workflows |
| `file-enum-large-dir` | .NET `Directory.EnumerateFiles`; current Windows runtime opens a directory handle and enumerates with `NtQueryDirectoryFile` | Scenario setup creates or refreshes `enum_dataset` before the stopwatch: 10,000 deterministic 256-byte files, with extensions cycling across source, script, config, executable-looking, and text-like names; 50 enumerations | Per operation, enumerate all files in `enum_dataset`, increment a count for every result, and fail if the count is not exactly 10,000; dataset creation is outside the histogram | Measures directory-enumeration overhead hit by IDEs, sync clients, source-control tools, and search/indexing paths |
| `file-copy-large` | .NET `File.Copy` / `File.Delete`; current Windows runtime calls `CopyFile` and `DeleteFile` | Scenario setup creates or refreshes one deterministic 100 MB `large_source.bin` before the stopwatch, written in 1 MB chunks; 10 copy/delete iterations | Per operation, copy the 100 MB source to `large_copy_XX.bin` with overwrite enabled, then delete that destination; source-file generation is outside the histogram | Measures bulk file-copy behavior rather than tiny metadata-only file operations |
| `hardlink-create` | Win32 `CreateHardLink` through P/Invoke | Scenario setup writes one 4 KB zero-filled `hardlink_source.dat` before the stopwatch; 5,000 hard-link create/delete operations | Per operation, create `hlink_XXXXX.dat` pointing at the fixed source file, then delete only the hard link; the source file is cleaned up after the stopwatch | Covers NTFS hard-link creation used by package managers, build caches, and workspace tooling |
| `junction-create` | Win32 `CreateFile` on a directory handle plus `DeviceIoControl(FSCTL_SET_REPARSE_POINT)` | Scenario setup creates a fixed `junction_target` directory before the stopwatch; 2,000 junction create/delete operations | Per operation, create an empty `junction_XXXXX` directory, build the mount-point reparse buffer in memory, open the directory with both `FILE_FLAG_OPEN_REPARSE_POINT` and `FILE_FLAG_BACKUP_SEMANTICS`, call `FSCTL_SET_REPARSE_POINT`, then delete the junction directory | Covers reparse-point creation, which can be security-sensitive because it changes path resolution |
| `ext-sensitivity-exe` | .NET `File.WriteAllBytes` / `File.Delete`; on Windows this reaches `CreateFile`, `WriteFile`, and `DeleteFile` | Scenario setup creates one 4 KB random-content buffer before the stopwatch; the same bytes are reused for 10,000 `.exe` files | Per operation, write the unchanged 4 KB buffer to `bench_XXXXX.exe`, close it, and delete it; there is no per-file content mutation | Tests whether executable-looking filenames change write-path cost |
| `ext-sensitivity-dll` | .NET `File.WriteAllBytes` / `File.Delete`; on Windows this reaches `CreateFile`, `WriteFile`, and `DeleteFile` | Same 4 KB random-content buffer pattern, reused for 10,000 `.dll` files | Per operation, write the unchanged 4 KB buffer to `bench_XXXXX.dll`, close it, and delete it; only the extension changes versus the other extension-sensitivity rows | Same filename-sensitivity probe for library-like payloads |
| `ext-sensitivity-js` | .NET `File.WriteAllBytes` / `File.Delete`; on Windows this reaches `CreateFile`, `WriteFile`, and `DeleteFile` | Same 4 KB random-content buffer pattern, reused for 10,000 `.js` files | Per operation, write the unchanged 4 KB buffer to `bench_XXXXX.js`, close it, and delete it; only the extension changes versus the other extension-sensitivity rows | Same filename-sensitivity probe for script-like payloads |
| `ext-sensitivity-ps1` | .NET `File.WriteAllBytes` / `File.Delete`; on Windows this reaches `CreateFile`, `WriteFile`, and `DeleteFile` | Same 4 KB random-content buffer pattern, reused for 10,000 `.ps1` files | Per operation, write the unchanged 4 KB buffer to `bench_XXXXX.ps1`, close it, and delete it; only the extension changes versus the other extension-sensitivity rows | Same filename-sensitivity probe for PowerShell-style payloads |
| `file-write-content` | .NET `File.WriteAllBytes` / `File.Delete`, using PE-like content; on Windows this reaches `CreateFile`, `WriteFile`, and `DeleteFile` | Setup builds an unsigned `noop.exe`; before the stopwatch, the scenario reads it once into a template byte array and allocates one reusable buffer; 10,000 files alternate `.exe` and `.dll` | Each iteration copies the template into memory, patches bytes `0x40..0x43` in the in-memory buffer with the little-endian iteration index, then starts the per-operation timer, writes the full mutated buffer to a new file, and deletes it; no already-written file is patched on disk | Tests executable-content write cost, not just extension-based filtering |

Two of these scenarios deserve special attention:

- `archive-extract` is intentionally a short API sequence rather than one isolated syscall. The point is to capture the file-create and file-write path under realistic burst conditions.
- `file-write-content` is intentionally different from `ext-sensitivity-*`: it does not just change the extension, it writes PE-like content derived from a real executable image. The uniqueness patch is made in memory before `File.WriteAllBytes`; the disk operation sees a unique full-file write, not an on-disk patch/update.

#### Process, image-load, and execution APIs

These scenarios focus on API paths around process creation, image loading, execution, and related metadata.

| Scenario ID | API path involved | Test data and operation count | Per-operation measured action | Why it is in the suite |
|---|---|---|---|---|
| `process-create-wait` | .NET `Process.Start` with `UseShellExecute=false`, redirected stdout/stderr, and `WaitForExit`; current Windows runtime reaches `CreateProcessW` rather than ShellExecute | Setup builds one unsigned `noop.exe`; 500 launches reuse that same path | Per operation, start `noop.exe` with no shell window, redirect stdout/stderr, synchronously drain both streams, wait for exit, and require exit code 0 | Measures the small-process-launch path directly |
| `dll-load-unique` | Win32 `LoadLibrary` / `FreeLibrary` plus file copy/delete | Before the stopwatch, resolve a source DLL from `%SystemRoot%\System32`; preference order is `urlmon.dll`, then `kernel32.dll`, then `ntdll.dll`; 2,000 unique destination paths | Per operation, copy the source DLL to `bench_XXXXX.dll`, call `LoadLibrary` on that unique path, call `FreeLibrary`, then delete the copy | Measures repeated image-load behavior from never-before-seen file paths |
| `new-exe-run` | File copy, direct PE patch, .NET `Process.Start`, recursive delete; process launch uses the same `UseShellExecute=false` / `CreateProcessW` path | Setup builds `noop.exe` and its adjacent support files; 50 executions; each operation uses a fresh directory and a unique executable hash | Per operation, create `bench_XXXXX`, copy all `noop.*` support files into it, patch bytes `0x40..0x43` on the copied executable file on disk with the little-endian iteration index, run it, require exit code 0, and recursively delete the directory | Baseline for executing a never-before-seen binary without internet-origin marking |
| `new-exe-run-motw` | Same as `new-exe-run`, plus `Zone.Identifier` alternate data stream write | Same unique executable flow; additionally writes `[ZoneTransfer]` with `ZoneId=3` to `noop.exe:Zone.Identifier`; 50 executions | Per operation, copy support files, patch the copied executable on disk, write the MOTW alternate data stream, run the executable, require exit code 0, and recursively delete the directory | Measures whether internet-origin metadata changes first-run executable handling |
| `thread-create` | .NET `Thread.Start` / `Thread.Join` | 5,000 managed background threads with a no-op body | Per operation, allocate a new `Thread`, set `IsBackground=true`, start it, run an empty delegate, and join it | Measures a simple thread-creation path that some products may observe |

The `new-exe-run` / `new-exe-run-motw` pair is especially useful because it creates a controlled A/B comparison on the same execution path. Each iteration patches 4 bytes of the PE header (the same DOS stub padding region that `file-write-content` uses) so every copy has a unique file hash, defeating AV scan-result caching. The executable payload is functionally the same; the only differences are the hash and the presence of the `Zone.Identifier` stream. Any delta can reflect Windows security features, reputation checks, product policy, or other handling tied to internet-origin metadata.[1]

#### Memory and mapping APIs

These scenarios target memory-management API paths that show up in JITs, loaders, and code-generation engines, and that security products may watch more closely than ordinary heap activity.

| Scenario ID | API path involved | Test data and operation count | Per-operation measured action | Why it is in the suite |
|---|---|---|---|---|
| `mem-alloc-protect` | Win32 `VirtualAlloc`, `VirtualProtect`, `VirtualFree`; allocation flags are both `MEM_RESERVE` and `MEM_COMMIT` with `PAGE_READWRITE`, then `PAGE_EXECUTE_READ`, then `MEM_RELEASE` | 50,000 operations; one 4 KB page per operation; no support file is involved | Per operation, reserve+commit one page as read/write, write byte `0x41` at offset 0, change protection to execute/read, and release the allocation; it never uses `PAGE_EXECUTE_READWRITE` | Measures allocate/protect/free overhead; the writeable-to-executable-style transition is the security-interesting part |
| `mem-map-file` | .NET `MemoryMappedFile.CreateFromFile` and `CreateViewAccessor`; current Windows runtime uses `CreateFileMapping` and `MapViewOfFile` | Scenario setup creates one 4 KB zero-filled `mmap-backing.bin` before the stopwatch if missing; 10,000 map/view operations | Per operation, open a file-backed mapping over the same 4 KB file, create a 4 KB read/write view, write one byte derived from the iteration index at offset 0, read offset 0 back, then dispose the view and mapping | Measures repeated file-backed section mapping rather than ordinary buffered I/O |

These are not whole-application models. They are API-path probes for behaviors that often receive more security scrutiny than plain file reads and writes.

#### Network and registry APIs

These scenarios cover two API surfaces that matter to ordinary software and to security tooling.

| Scenario ID | API path involved | Test data and operation count | Per-operation measured action | Why it is in the suite |
|---|---|---|---|---|
| `net-connect-loopback` | .NET `TcpListener`, `TcpClient`, and `NetworkStream` over loopback; current Windows runtime uses Winsock paths such as `WSASocketW`, `bind` / `listen` / `accept`, `WSAConnect`, `send` / `WSASend`, and `WSARecv` | Before the stopwatch, create a 1 KB random payload, start one loopback `TcpListener` on an ephemeral port, and start a server task that accepts exactly 2,000 clients | Per operation, create a fresh IPv4 `TcpClient`, set `NoDelay=true`, connect to `127.0.0.1`, write the same 1 KB payload, synchronously read exactly 1 KB echoed back, and dispose the connection | Measures local connect/send/receive/close overhead without internet latency dominating |
| `net-dns-resolve` | .NET `Dns.GetHostAddresses`; current Windows runtime uses Winsock `GetAddrInfoW` for synchronous lookup | 5,000 lookups of the literal host name `localhost`; no internet host is queried | Per operation, resolve `localhost` synchronously and fail if the returned address array is empty | Measures a lightweight resolver/cache path; useful as a local networking probe, not as an internet DNS benchmark |
| `registry-crud` | .NET `RegistryKey.CreateSubKey`, `SetValue`, `GetValue`, `GetValueNames`, `DeleteSubKeyTree` under HKCU; current Windows runtime uses Advapi32 calls such as `RegCreateKeyEx`, `RegSetValueEx`, `RegQueryValueEx`, and `RegDeleteTree` | 5,000 unique subkeys under `HKCU\Software\AvBench\Temp`; each operation writes String, DWord, 4-byte Binary, 3-item MultiString, and ExpandString values | Per operation, create `bench_XXXXX`, write all five values, read all five values back, enumerate value names, close the key, and delete that subkey tree; the base temp key is cleaned after the stopwatch | Measures registry create/write/read/enumerate/delete overhead common in installers and management tooling |

The networking scenarios should be read carefully. They are API-path probes, not network benchmarks. They are useful for exposing relative differences in local networking or inspection overhead, not for predicting end-to-end internet latency.

#### IPC, identity, crypto, and management APIs

These scenarios cover a set of Windows-facing API paths that show up in tools, services, and management software.

| Scenario ID | API path involved | Test data and operation count | Per-operation measured action | Why it is in the suite |
|---|---|---|---|---|
| `pipe-roundtrip` | .NET `NamedPipeServerStream` / `NamedPipeClientStream`; current Windows runtime uses `CreateNamedPipe`, `ConnectNamedPipe`, client-side `CreateFile`, and stream reads/writes on the pipe handle | Before the stopwatch, create one duplex named pipe, connect one client, start one dedicated echo server thread, generate one 4 KB random payload, and run 100 unmeasured warmup round-trips; 2,000 measured round-trips follow | Per operation, write the same 4 KB payload to the already-connected pipe, flush, synchronously read exactly 4 KB echoed back, and keep the pipe open for the next operation | Measures steady-state local IPC latency without per-iteration pipe creation or thread-pool startup noise |
| `token-query` | Win32 `OpenProcessToken(TOKEN_QUERY)`, `GetTokenInformation(TokenPrivileges)`, `CloseHandle` | Current process pseudo-handle is captured before the stopwatch; 50,000 token queries; each operation uses a fresh 1 KB managed buffer for token information | Per operation, open the current process token with `TOKEN_QUERY`, call `GetTokenInformation(TokenPrivileges)`, close the token handle in a finally block, and fail on any Win32 error | Measures repeated security-context query overhead |
| `crypto-hash-verify` | .NET `SHA256.HashData`, `RSA.SignData`, `RSA.VerifyHash` | Before the stopwatch, generate one 64 KB random payload, create one RSA-2048 key, and precompute one PKCS#1 v1.5 SHA-256 signature; 5,000 verify operations | Per operation, hash the same 64 KB payload with SHA-256 and verify that hash against the precomputed signature; key generation and signing are outside the histogram | Acts as a security-adjacent local compute path rather than a file/process API probe |
| `com-create-instance` | COM activation via .NET `Activator.CreateInstance` and `Marshal.FinalReleaseComObject` | Before the stopwatch, resolve ProgID `Scripting.FileSystemObject` to a COM type and fail early if it is not registered; 5,000 activations | Per operation, instantiate the COM object, verify the returned object is non-null, and call `Marshal.FinalReleaseComObject` when it is a COM object | Measures COM activation and teardown |
| `wmi-query` | .NET `ManagementObjectSearcher` over WMI `Win32_Process` | Before the stopwatch, build one WQL string selecting the current process by PID and requesting only `ProcessId` and `Name`; 500 WMI queries | Per operation, construct a `ManagementObjectSearcher`, execute the query, iterate returned management objects, read the `Name` property, dispose each item, and dispose the result collection | Measures a heavier management query path than raw COM activation |
| `fs-watcher` | .NET `FileSystemWatcher` plus ordinary file writes/deletes; current Windows runtime opens a watched directory handle with `CreateFile` and listens with `ReadDirectoryChangesW` | Before the stopwatch, create one watched directory, generate one 64-byte random payload, enable a non-recursive watcher for `FileName` and `LastWrite`, and attach created/changed/deleted handlers; 5,000 file operations | Per operation, write the 64-byte payload to `watch_XXXXX.tmp`, append one character, and delete the file while the watcher is active; notification counts are kept alive but not awaited per operation | Measures file activity while the OS change-notification path is active |

`crypto-hash-verify` is the outlier in this group. It is less direct than the file/process/registry scenarios as a Windows API probe, but it is still useful as a security-adjacent path: it shows whether a configuration changes the cost of local hash-and-verify work enough to matter.

## Operation counts

Operation counts are tuned by scenario type rather than by one fixed rule:

- 50,000 operations for very cheap paths
- 5,000 to 10,000 for ordinary short operations
- 2,000 for moderately heavier paths
- 500 for obviously slower operations such as process launch or WMI query
- 10 to 50 iterations for multi-file or large-transfer scenarios, and for per-operation-heavy paths such as `new-exe-run` / `new-exe-run-motw` where each iteration triggers a full AV scan of a unique binary

That keeps the suite long enough to produce meaningful percentile data, but short enough to run repeatedly as part of a multi-session comparison workflow.

## What the suite is optimized for

This workload set is optimized for one job: comparing the performance impact of AV or endpoint-security configurations on developer-style Windows activity, with microbench scenarios specifically aimed at Windows API call paths.

It is not trying to be:

- a malware-evasion lab
- a generic OS microbenchmark suite
- a single-number "system performance" score
- an exhaustive catalog of every Windows API a security product might touch

That focus is a feature. The suite is intentionally opinionated: it favors workloads that are easy to rerun, easy to explain, and plausible enough to matter to real users.

## References

1. [Microsoft Defender SmartScreen](https://learn.microsoft.com/en-us/windows/security/operating-system-security/virus-and-threat-protection/microsoft-defender-smartscreen/)
2. [Filter Manager concepts](https://learn.microsoft.com/en-us/windows-hardware/drivers/ifs/filter-manager-concepts)
3. [Filtering Registry Calls](https://learn.microsoft.com/en-us/windows-hardware/drivers/kernel/filtering-registry-calls)
4. [About Windows Filtering Platform](https://learn.microsoft.com/en-us/windows/win32/fwp/about-windows-filtering-platform)
5. [BurntSushi/ripgrep](https://github.com/BurntSushi/ripgrep)
6. [dotnet/roslyn](https://github.com/dotnet/roslyn)
7. [CreateHardLink function](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-createhardlinka)
8. [FSCTL_SET_REPARSE_POINT control code](https://learn.microsoft.com/en-us/windows/win32/api/winioctl/ni-winioctl-fsctl_set_reparse_point)
9. [LoadLibrary function](https://learn.microsoft.com/en-us/windows/win32/api/libloaderapi/nf-libloaderapi-loadlibrarya)
10. [VirtualAlloc function](https://learn.microsoft.com/en-us/windows/win32/api/memoryapi/nf-memoryapi-virtualalloc)
11. [.NET memory-mapped files](https://learn.microsoft.com/en-us/dotnet/standard/io/memory-mapped-files)
12. [.NET pipe operations](https://learn.microsoft.com/en-us/dotnet/standard/io/pipe-operations)
13. [OpenProcessToken function](https://learn.microsoft.com/en-us/windows/win32/api/processthreadsapi/nf-processthreadsapi-openprocesstoken)
14. [GetTokenInformation function](https://learn.microsoft.com/en-us/windows/win32/api/securitybaseapi/nf-securitybaseapi-gettokeninformation)
15. [Win32_Process WMI class](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-process)
16. [FileSystemWatcher class](https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-io-filesystemwatcher)
17. [Memory protection constants](https://learn.microsoft.com/en-us/windows/win32/memory/memory-protection-constants)
18. [VirtualProtect function](https://learn.microsoft.com/en-us/windows/win32/api/memoryapi/nf-memoryapi-virtualprotect)
19. [.NET runtime Windows FileSystem source](https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/IO/FileSystem.Windows.cs)
20. [.NET runtime Windows SafeFileHandle source](https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/Microsoft/Win32/SafeHandles/SafeFileHandle.Windows.cs)
21. [.NET runtime Windows RandomAccess source](https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/IO/RandomAccess.Windows.cs)
22. [.NET runtime Windows file enumeration source](https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/IO/Enumeration/FileSystemEnumerator.Windows.cs)
23. [.NET runtime Windows process-start source](https://github.com/dotnet/runtime/blob/main/src/libraries/System.Diagnostics.Process/src/Microsoft/Win32/SafeHandles/SafeProcessHandle.Windows.cs)
24. [.NET runtime Windows memory-mapped file source](https://github.com/dotnet/runtime/blob/main/src/libraries/System.IO.MemoryMappedFiles/src/System/IO/MemoryMappedFiles/MemoryMappedFile.Windows.cs)
25. [.NET runtime Windows memory-mapped view source](https://github.com/dotnet/runtime/blob/main/src/libraries/System.IO.MemoryMappedFiles/src/System/IO/MemoryMappedFiles/MemoryMappedView.Windows.cs)
26. [.NET runtime Windows socket source](https://github.com/dotnet/runtime/blob/main/src/libraries/System.Net.Sockets/src/System/Net/Sockets/SocketPal.Windows.cs)
27. [.NET runtime Windows DNS source](https://github.com/dotnet/runtime/blob/main/src/libraries/System.Net.NameResolution/src/System/Net/NameResolutionPal.Windows.cs)
28. [.NET runtime RegistryKey source](https://github.com/dotnet/runtime/blob/main/src/libraries/Microsoft.Win32.Registry/src/Microsoft/Win32/RegistryKey.cs)
29. [.NET runtime Windows named-pipe server source](https://github.com/dotnet/runtime/blob/main/src/libraries/System.IO.Pipes/src/System/IO/Pipes/NamedPipeServerStream.Windows.cs)
30. [.NET runtime Windows named-pipe client source](https://github.com/dotnet/runtime/blob/main/src/libraries/System.IO.Pipes/src/System/IO/Pipes/NamedPipeClientStream.Windows.cs)
31. [.NET runtime Windows FileSystemWatcher source](https://github.com/dotnet/runtime/blob/main/src/libraries/System.IO.FileSystem.Watcher/src/System/IO/FileSystemWatcher.Windows.cs)
