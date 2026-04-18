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

For scenarios that create a local helper dataset inside the scenario directory, such as `file-enum-large-dir`, `file-copy-large`, and `mem-map-file`, the per-operation latency histogram measures the repeated API operation after the helper file or directory exists. The top-level `wall_ms` still covers the whole in-process scenario invocation.

### How to read the microbench suite

The suite is best read as a set of API-path probes rather than as a flat list of 27 tests.

#### File-system and content-path APIs

These scenarios probe file-system API paths that are both common in normal software and frequently inspected by security products.

| Scenario ID | API path involved | Test data and operation count | Per-operation measured action | Why it is in the suite |
|---|---|---|---|---|
| `file-create-delete` | .NET `File.Create` / `File.Delete`; on Windows this reaches `CreateFile`, `WriteFile`, and `DeleteFile` through FileStream / System.IO | 5,000 files; each file receives a 64-byte zero buffer; loop is grouped internally in batches of 100 | Create `bench_XXXXX.tmp`, write 64 bytes, close the stream, delete the file | Baseline file-create/file-delete overhead on the most common file-system path |
| `archive-extract` | .NET `ZipFile.ExtractToDirectory` plus recursive delete; extraction ultimately exercises System.IO create/write paths such as `CreateFile` and `WriteFile` | Generated zip with 2,000 files; extensions cycle across `.cs`, `.js`, `.json`, `.xml`, `.dll`, `.exe`, `.txt`, `.md`; sizes cycle through 64 B, 256 B, 1 KB, 4 KB, 16 KB, 64 KB; 10 extract/delete iterations | Extract the whole archive into a fresh directory, then delete the extracted tree | Stresses bursty multi-file create/write/delete behavior seen in package restore, unpack, and installer workflows |
| `file-enum-large-dir` | .NET `Directory.EnumerateFiles`; current Windows runtime opens a directory handle and enumerates with `NtQueryDirectoryFile` | Local dataset of 10,000 files; each file is 256 bytes; extensions cycle across source, script, config, executable-looking, and text-like names; 50 enumerations | Enumerate the directory and count every file, verifying the count is exactly 10,000 | Measures directory-enumeration overhead hit by IDEs, sync clients, source-control tools, and search/indexing paths |
| `file-copy-large` | .NET `File.Copy` / `File.Delete`; current Windows runtime calls `CopyFile` and `DeleteFile` | Generated 100 MB random source file, created in 1 MB chunks; 10 copy/delete iterations | Copy `large_source.bin` to a unique destination, then delete the destination | Measures bulk file-copy behavior rather than tiny metadata-only file operations |
| `hardlink-create` | Win32 `CreateHardLink` through P/Invoke | One 4 KB source file; 5,000 hard-link create/delete operations | Create `hlink_XXXXX.dat` pointing at the source file, then delete the hard link | Covers NTFS hard-link creation used by package managers, build caches, and workspace tooling |
| `junction-create` | Win32 `CreateFile` on a directory handle plus `DeviceIoControl(FSCTL_SET_REPARSE_POINT)` | One fixed target directory; 2,000 junction create/delete operations | Create an empty directory, write a mount-point reparse buffer targeting the fixed directory, then delete the junction | Covers reparse-point creation, which can be security-sensitive because it changes path resolution |
| `ext-sensitivity-exe` | .NET `File.WriteAllBytes` / `File.Delete`; on Windows this reaches `CreateFile`, `WriteFile`, and `DeleteFile` | One 4 KB random-content buffer reused for 10,000 `.exe` files | Write `bench_XXXXX.exe`, close it, delete it | Tests whether executable-looking filenames change write-path cost |
| `ext-sensitivity-dll` | .NET `File.WriteAllBytes` / `File.Delete`; on Windows this reaches `CreateFile`, `WriteFile`, and `DeleteFile` | Same 4 KB random-content pattern, 10,000 `.dll` files | Write `bench_XXXXX.dll`, close it, delete it | Same filename-sensitivity probe for library-like payloads |
| `ext-sensitivity-js` | .NET `File.WriteAllBytes` / `File.Delete`; on Windows this reaches `CreateFile`, `WriteFile`, and `DeleteFile` | Same 4 KB random-content pattern, 10,000 `.js` files | Write `bench_XXXXX.js`, close it, delete it | Same filename-sensitivity probe for script-like payloads |
| `ext-sensitivity-ps1` | .NET `File.WriteAllBytes` / `File.Delete`; on Windows this reaches `CreateFile`, `WriteFile`, and `DeleteFile` | Same 4 KB random-content pattern, 10,000 `.ps1` files | Write `bench_XXXXX.ps1`, close it, delete it | Same filename-sensitivity probe for PowerShell-style payloads |
| `file-write-content` | .NET `File.WriteAllBytes` / `File.Delete`, using PE-like content; on Windows this reaches `CreateFile`, `WriteFile`, and `DeleteFile` | Unsigned `noop.exe` template; 10,000 files alternating `.exe` and `.dll`; bytes `0x40` through `0x43` are patched with the iteration index before each write | Write a unique PE-like buffer to disk, close it, delete it | Tests executable-content write cost, not just extension-based filtering |

Two of these scenarios deserve special attention:

- `archive-extract` is intentionally a short API sequence rather than one isolated syscall. The point is to capture the file-create and file-write path under realistic burst conditions.
- `file-write-content` is intentionally different from `ext-sensitivity-*`: it does not just change the extension, it writes PE-like content derived from a real executable image so the write path is closer to something security software may classify as executable content.

#### Process, image-load, and execution APIs

These scenarios focus on API paths around process creation, image loading, execution, and related metadata.

| Scenario ID | API path involved | Test data and operation count | Per-operation measured action | Why it is in the suite |
|---|---|---|---|---|
| `process-create-wait` | .NET `Process.Start` with `UseShellExecute=false`, redirected stdout/stderr, and `WaitForExit`; current Windows runtime reaches `CreateProcessW` rather than ShellExecute | Unsigned local `noop.exe`; 500 launches | Start the executable, drain stdout and stderr, wait for exit, verify exit code 0 | Measures the small-process-launch path directly |
| `dll-load-unique` | Win32 `LoadLibrary` / `FreeLibrary` plus file copy/delete | Copies a system DLL from `%SystemRoot%\System32`; preference order is `urlmon.dll`, then `kernel32.dll`, then `ntdll.dll`; 2,000 unique paths | Copy the DLL to `bench_XXXXX.dll`, load it, unload it, delete the copy | Measures repeated image-load behavior from never-before-seen file paths |
| `new-exe-run` | File copy, direct PE patch, .NET `Process.Start`, recursive delete; process launch uses the same `UseShellExecute=false` / `CreateProcessW` path | Unsigned `noop.exe` support files copied to a fresh temp directory; 50 executions; bytes `0x40` through `0x43` are patched per iteration | Copy all support files, patch the executable to change its hash, run it, delete the directory | Baseline for executing a never-before-seen binary without internet-origin marking |
| `new-exe-run-motw` | Same as `new-exe-run`, plus `Zone.Identifier` alternate data stream write | Same unique executable flow; additionally writes `[ZoneTransfer]` with `ZoneId=3` to `noop.exe:Zone.Identifier`; 50 executions | Copy and patch the executable, add MOTW ADS, run it, delete the directory | Measures whether internet-origin metadata changes first-run executable handling |
| `thread-create` | .NET `Thread.Start` / `Thread.Join` | 5,000 managed background threads with a no-op body | Allocate a thread object, start it, join it | Measures a simple thread-creation path that some products may observe |

The `new-exe-run` / `new-exe-run-motw` pair is especially useful because it creates a controlled A/B comparison on the same execution path. Each iteration patches 4 bytes of the PE header (the same DOS stub padding region that `file-write-content` uses) so every copy has a unique file hash, defeating AV scan-result caching. The executable payload is functionally the same; the only differences are the hash and the presence of the `Zone.Identifier` stream. Any delta can reflect Windows security features, reputation checks, product policy, or other handling tied to internet-origin metadata.[1]

#### Memory and mapping APIs

These scenarios target memory-management API paths that show up in JITs, loaders, and code-generation engines, and that security products may watch more closely than ordinary heap activity.

| Scenario ID | API path involved | Test data and operation count | Per-operation measured action | Why it is in the suite |
|---|---|---|---|---|
| `mem-alloc-protect` | Win32 `VirtualAlloc`, `VirtualProtect`, `VirtualFree`; allocation flags are both `MEM_RESERVE` and `MEM_COMMIT` with `PAGE_READWRITE`, then `PAGE_EXECUTE_READ`, then `MEM_RELEASE` | 50,000 operations; one 4 KB page per operation | Reserve+commit a page as read/write, write one byte, change protection to execute/read, release the page | Measures allocate/protect/free overhead; the writeable-to-executable-style transition is the security-interesting part; it does not use `PAGE_EXECUTE_READWRITE` |
| `mem-map-file` | .NET `MemoryMappedFile.CreateFromFile` and `CreateViewAccessor`; current Windows runtime uses `CreateFileMapping` and `MapViewOfFile` | One 4 KB backing file; 10,000 map/view operations | Open a 4 KB file-backed mapping, create a 4 KB read/write view, write one byte, read one byte, dispose view and mapping | Measures repeated file-backed section mapping rather than ordinary buffered I/O |

These are not whole-application models. They are API-path probes for behaviors that often receive more security scrutiny than plain file reads and writes.

#### Network and registry APIs

These scenarios cover two API surfaces that matter to ordinary software and to security tooling.

| Scenario ID | API path involved | Test data and operation count | Per-operation measured action | Why it is in the suite |
|---|---|---|---|---|
| `net-connect-loopback` | .NET `TcpListener`, `TcpClient`, and `NetworkStream` over loopback; current Windows runtime uses Winsock paths such as `WSASocketW`, `bind` / `listen` / `accept`, `WSAConnect`, `send` / `WSASend`, and `WSARecv` | One local echo server; 2,000 client connections; 1 KB random payload per connection; `NoDelay=true` | Create a fresh TCP client, connect to `127.0.0.1`, write 1 KB, read 1 KB echoed back, dispose the connection | Measures local connect/send/receive/close overhead without internet latency dominating |
| `net-dns-resolve` | .NET `Dns.GetHostAddresses`; current Windows runtime uses Winsock `GetAddrInfoW` for synchronous lookup | 5,000 lookups of `localhost` | Resolve `localhost` and verify at least one address is returned | Measures a lightweight resolver/cache path; useful as a local networking probe, not as an internet DNS benchmark |
| `registry-crud` | .NET `RegistryKey.CreateSubKey`, `SetValue`, `GetValue`, `GetValueNames`, `DeleteSubKeyTree` under HKCU; current Windows runtime uses Advapi32 calls such as `RegCreateKeyEx`, `RegSetValueEx`, `RegQueryValueEx`, and `RegDeleteTree` | 5,000 subkeys under `HKCU\Software\AvBench\Temp`; writes String, DWord, 4-byte Binary, 3-item MultiString, and ExpandString values | Create subkey, write all five value types, read them back, enumerate names, close key, delete tree | Measures registry create/write/read/enumerate/delete overhead common in installers and management tooling |

The networking scenarios should be read carefully. They are API-path probes, not network benchmarks. They are useful for exposing relative differences in local networking or inspection overhead, not for predicting end-to-end internet latency.

#### IPC, identity, crypto, and management APIs

These scenarios cover a set of Windows-facing API paths that show up in tools, services, and management software.

| Scenario ID | API path involved | Test data and operation count | Per-operation measured action | Why it is in the suite |
|---|---|---|---|---|
| `pipe-roundtrip` | .NET `NamedPipeServerStream` / `NamedPipeClientStream`; current Windows runtime uses `CreateNamedPipe`, `ConnectNamedPipe`, client-side `CreateFile`, and stream reads/writes on the pipe handle | One established duplex named pipe; 100 warmup round-trips not recorded; 2,000 measured round-trips; 4 KB random payload | Write 4 KB to the pipe, flush, read the 4 KB echo back | Measures steady-state local IPC latency without per-iteration pipe creation or thread-pool startup noise |
| `token-query` | Win32 `OpenProcessToken(TOKEN_QUERY)`, `GetTokenInformation(TokenPrivileges)`, `CloseHandle` | Current process pseudo-handle; 1 KB token-information buffer; 50,000 token queries | Open current process token, read privilege information, close token handle | Measures repeated security-context query overhead |
| `crypto-hash-verify` | .NET `SHA256.HashData`, `RSA.SignData`, `RSA.VerifyHash` | 64 KB random payload; RSA-2048 key; one precomputed PKCS#1 signature; 5,000 verify operations | Hash 64 KB and verify the hash against the signature | Acts as a security-adjacent local compute path rather than a file/process API probe |
| `com-create-instance` | COM activation via .NET `Activator.CreateInstance` and `Marshal.FinalReleaseComObject` | ProgID `Scripting.FileSystemObject`; 5,000 activations | Create the COM object, verify non-null, release it if it is a COM object | Measures COM activation and teardown |
| `wmi-query` | .NET `ManagementObjectSearcher` over WMI `Win32_Process` | 500 WMI queries; query selects only the current process by PID and asks for `ProcessId` and `Name` | Create searcher, execute query, read `Name`, dispose result objects | Measures a heavier management query path than raw COM activation |
| `fs-watcher` | .NET `FileSystemWatcher` plus ordinary file writes/deletes; current Windows runtime opens a watched directory handle with `CreateFile` and listens with `ReadDirectoryChangesW` | One watched directory; NotifyFilters are `FileName` and `LastWrite`; 5,000 file operations; 64-byte random payload | With watcher enabled, write a file, append one character, delete it | Measures file activity while the OS change-notification path is active |

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
