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

### How to read the microbench suite

The suite is best read as a set of API-path probes rather than as a flat list of 27 tests.

#### File-system and content-path APIs

These scenarios probe file-system API paths that are both common in normal software and frequently inspected by security products.

| Scenario ID | What it does | Why it is in the suite |
|---|---|---|
| `file-create-delete` | Creates and deletes 5,000 small temp files in batches of 100 | Baseline file-create/file-delete overhead on a very common API path |
| `archive-extract` | Extracts the generated mixed-file zip 10 times, deleting the extracted tree each time | Stresses the bursty multi-file create/write path seen in restore, unpack, and installer workflows |
| `file-enum-large-dir` | Enumerates a generated directory containing 10,000 files, 50 times | Measures directory-enumeration cost on a path hit by IDEs, sync clients, and source-control tools |
| `file-copy-large` | Copies a generated 100 MB file 10 times, deleting the copy after each run | Measures bulk file-copy behavior rather than tiny metadata-only operations |
| `hardlink-create` | Creates and deletes 5,000 hard links | Covers link-creation APIs used by package managers and workspace tooling |
| `junction-create` | Creates and deletes 2,000 directory junctions | Covers reparse-point creation on a path that some developer workflows use heavily |
| `ext-sensitivity-exe` | Writes and deletes 10,000 randomly generated `.exe` files | Tests whether executable-looking filenames change the cost of the write path |
| `ext-sensitivity-dll` | Writes and deletes 10,000 randomly generated `.dll` files | Same idea for library-like payloads |
| `ext-sensitivity-js` | Writes and deletes 10,000 randomly generated `.js` files | Same idea for script-like payloads |
| `ext-sensitivity-ps1` | Writes and deletes 10,000 randomly generated `.ps1` files | Same idea for PowerShell-style payloads |
| `file-write-content` | Rewrites a local unsigned PE template with small byte changes and writes it as alternating `.exe` and `.dll` files | Tests the file-write path with executable-like content, not just executable-like extensions |

Two of these scenarios deserve special attention:

- `archive-extract` is intentionally a short API sequence rather than one isolated syscall. The point is to capture the file-create and file-write path under realistic burst conditions.
- `file-write-content` is intentionally different from `ext-sensitivity-*`: it does not just change the extension, it writes PE-like content derived from a real executable image so the write path is closer to something security software may classify as executable content.

#### Process, image-load, and execution APIs

These scenarios focus on API paths around process creation, image loading, execution, and related metadata.

| Scenario ID | What it does | Why it is in the suite |
|---|---|---|
| `process-create-wait` | Launches the local unsigned `noop.exe` 500 times and waits for exit | Measures the small-process-launch path directly |
| `dll-load-unique` | Copies a system DLL to a unique path, loads it, unloads it, and deletes it 2,000 times | Measures repeated load-from-new-path behavior on the image-load path |
| `new-exe-run` | Copies the unsigned `noop.exe` to a temp directory, patches 4 bytes to produce a unique hash, runs it, and deletes the directory, 50 times | Baseline for executing a never-before-seen binary without internet-origin marking |
| `new-exe-run-motw` | Same as above, but also adds a `Zone.Identifier` alternate data stream with `ZoneId=3` before execution, 50 times | Measures whether internet-origin marking changes the cost of executing a never-before-seen binary |
| `thread-create` | Creates, starts, and joins 5,000 managed threads | Measures a simple thread-creation path that some products also watch closely |

The `new-exe-run` / `new-exe-run-motw` pair is especially useful because it creates a controlled A/B comparison on the same execution path. Each iteration patches 4 bytes of the PE header (the same DOS stub padding region that `file-write-content` uses) so every copy has a unique file hash, defeating AV scan-result caching. The executable payload is functionally the same; the only differences are the hash and the presence of the `Zone.Identifier` stream. Any delta can reflect Windows security features, reputation checks, product policy, or other handling tied to internet-origin metadata.[1]

#### Memory and mapping APIs

These scenarios target memory-management API paths that show up in JITs, loaders, and code-generation engines, and that security products may watch more closely than ordinary heap activity.

| Scenario ID | What it does | Why it is in the suite |
|---|---|---|
| `mem-alloc-protect` | Repeats `VirtualAlloc` -> write -> `VirtualProtect` -> `VirtualFree` 50,000 times | Measures a compact allocate/change-protection/free path that is often security-sensitive |
| `mem-map-file` | Creates a memory-mapped view over a 4 KB backing file, writes one byte, reads one byte, then disposes it 10,000 times | Measures repeated file-backed section mapping rather than ordinary buffered I/O |

These are not whole-application models. They are API-path probes for behaviors that often receive more security scrutiny than plain file reads and writes.

#### Network and registry APIs

These scenarios cover two API surfaces that matter to ordinary software and to security tooling.

| Scenario ID | What it does | Why it is in the suite |
|---|---|---|
| `net-connect-loopback` | Connects to a local echo server 2,000 times, sends 1 KB, reads 1 KB back, and closes | Measures connect/send/receive/close overhead without internet noise dominating the result |
| `net-dns-resolve` | Resolves `localhost` 5,000 times | Measures a lightweight lookup-oriented networking path |
| `registry-crud` | Creates a key, writes five value types, reads them back, enumerates names, and deletes the key 5,000 times | Measures a registry API sequence that is common in installers, apps, and management tooling |

The networking scenarios should be read carefully. They are API-path probes, not network benchmarks. They are useful for exposing relative differences in local networking or inspection overhead, not for predicting end-to-end internet latency.

#### IPC, identity, crypto, and management APIs

These scenarios cover a set of Windows-facing API paths that show up in tools, services, and management software.

| Scenario ID | What it does | Why it is in the suite |
|---|---|---|
| `pipe-roundtrip` | Creates a named-pipe server/client pair, then exchanges 4 KB round-trips 2,000 times over the established connection | Measures steady-state local IPC latency |
| `token-query` | Opens the current process token and reads privileges 50,000 times | Measures a repeated security-context query path |
| `crypto-hash-verify` | Hashes a 64 KB buffer with SHA-256 and verifies an RSA-2048 signature 5,000 times | Acts as a security-related local compute path rather than a file or process path |
| `com-create-instance` | Creates and releases `Scripting.FileSystemObject` 5,000 times | Measures COM activation and teardown |
| `wmi-query` | Runs a `Win32_Process` query 500 times | Measures a heavier management-oriented query path than raw COM alone |
| `fs-watcher` | Enables a `FileSystemWatcher`, then repeatedly creates, appends to, and deletes files | Measures file activity while the change-notification path is active |

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
