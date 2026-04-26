# Compilation Trace Analysis: ripgrep vs Roslyn

This report reads two Process Monitor traces, `tmp/ripgrep.CSV` and `tmp/roslyn.CSV`, as AV-facing workload profiles. The point is not just "which build is slower?" or "which trace is larger?" For AV performance, the sharper question is what each build looks like to a product sitting in the file-system path and making trust decisions in real time.

For the main conclusions I use a core build-process view rather than the whole desktop trace. The analyzer reconstructs the process tree rooted at the workload-specific `avbench.exe` process and follows its children. That removes ProcMon itself, most desktop noise, and unrelated service activity better than a static process-name filter. The supporting tables are in `analysis/compilation-procmon-analysis.md`, the reproducible profiling layer is in `analysis/workload-profile-pipeline.md`, and the machine-readable summary is in `analysis/procmon-summary.json`.

## Executive Summary

Roslyn is not simply a larger ripgrep build. It is a different workload class at the OS boundary.

The ripgrep trace is compact and native-toolchain-heavy. It has about 202k core build events and roughly 3.4k unique file paths. Most of the interesting activity is clustered around `cargo.exe`, `rustc.exe`, `link.exe`, build scripts, Rust metadata, PDBs, static/import libraries, and final native outputs. In AV terms, ripgrep is a dense artifact-production test: fewer paths, but a high concentration of fresh compiler/linker outputs.

The Roslyn trace is a managed build graph at scale. It has about 12.6M core build events and roughly 153.8k unique file paths. The trace fans out through project files, `.props`/`.targets`, source files, reference assemblies, NuGet cache content, SDK files, analyzer/compiler DLLs, generated files, and `VBCSCompiler.exe`. In AV terms, Roslyn is a path-fan-out and metadata-pressure test, with a large secondary surface from fresh generated DLLs.

That is why the pair is useful. They are both "compilation," but they interrogate different parts of an AV product: one asks about native artifact creation, the other asks about massive managed build graph traversal and generated assembly output.

## Source Context

Ripgrep is a Rust command-line search tool. Its README describes a recursive regex searcher designed for large code trees and normal developer workflows. Building it with Cargo means compiling the local package, dependencies, and any crate build scripts.

Roslyn is the open-source C# and Visual Basic compiler platform. Building it through `dotnet build` brings in MSBuild, solution/project evaluation, SDK imports, NuGet/package state, incremental input/output checks, and compiler-server behavior. That is why the Roslyn trace is full of existence checks, timestamp probes, open/query operations, reference reads, and generated outputs.

Sources:

- [ripgrep README](https://github.com/BurntSushi/ripgrep)
- [Cargo build command](https://doc.rust-lang.org/cargo/commands/cargo-build.html)
- [Cargo build scripts](https://doc.rust-lang.org/cargo/reference/build-scripts.html)
- [Roslyn repository README](https://github.com/dotnet/roslyn)
- [dotnet build command](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-build)
- [dotnet restore command](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-restore)
- [MSBuild incremental builds](https://learn.microsoft.com/en-us/visualstudio/msbuild/incremental-builds?view=visualstudio)
- [NuGet MSBuild props and targets](https://learn.microsoft.com/en-us/nuget/concepts/msbuild-props-and-targets)

## Main Argument

The main conclusion is not simply "Roslyn is bigger." That is true, but too shallow. The more useful conclusion is that ripgrep and Roslyn pressure different AV decision paths.

Ripgrep asks a compact native-build question: what happens when trusted developer tools create fresh native artifacts, PDBs, import/static libraries, Rust metadata files, and final executable outputs in a tight burst?

Roslyn asks a large managed-build question: what happens when the build system opens, probes, reads, and writes across a huge graph of project files, imported MSBuild files, SDK files, reference assemblies, NuGet assets, analyzer/compiler DLLs, generated files, and fresh DLL outputs?

Those are different problems. An AV product can handle one well and the other poorly without that being random. A product with strong Microsoft SDK allowlisting may make part of Roslyn look cheap, then still stumble on the fresh local DLL output phase. A product with excellent metadata-query filtering may survive Roslyn's project graph, then slow down ripgrep because it treats fresh native executable generation as more suspicious.

## Why Choose These Two Workloads

"Compilation" is not a single workload. A real developer build can involve dependency discovery, source reads, metadata checks, compiler process trees, image loads, linker outputs, generated DLL/EXE writes, package caches, cloud reputation, and policy decisions around developer tools. Ripgrep and Roslyn land in different parts of that space.

Ripgrep is the compact native-build representative:

- It is a real Rust application, not a synthetic "compile hello world" case.
- Cargo builds a dependency graph and invokes `rustc` repeatedly, so the workload includes package graph orchestration and compiler activity.
- On Windows/MSVC, the final path includes native linker/resource tooling, PDBs, import libraries, object-like artifacts, and executable creation.
- The build is small enough that AV overhead is not buried inside a massive build-system baseline.
- The key AV question is dense: does the product penalize fresh native outputs and linker/compiler artifacts?

Roslyn is the large managed-build representative:

- It is a real, large .NET compiler-platform repository with many projects, source files, resources, generated files, analyzers, tests, and reference dependencies.
- `dotnet build` uses MSBuild and can perform implicit restore unless disabled. That naturally pulls in project evaluation, NuGet/cache behavior, SDK resolution, and target/import processing.
- MSBuild incremental behavior is built around input/output relationships and timestamps; even when work can be skipped, it must inspect many files to decide what is current.
- The trace observes `VBCSCompiler.exe`, so it includes compiler-server input scanning rather than only short-lived compiler invocations.
- The key AV question is broad: can the product handle enormous file graph fan-out and generated managed assembly output without per-open/per-query overhead exploding?

The pair protects the benchmark from a false single-workload story. If we only use ripgrep, we mostly test native artifact creation and Rust/MSVC toolchain behavior. If we only use Roslyn, we mostly test MSBuild/NuGet/reference-assembly traversal and managed DLL output. Together they cover two developer realities that look similar to users but very different to AV engines.

## Why They Are Mechanistically Different

The divergence starts in the build systems.

Cargo's job is comparatively direct: compile the package and its dependencies. In this trace that turns into `cargo.exe` orchestration, many `rustc.exe` processes, crate build scripts, and MSVC linker/resource tools. Cargo and rustc still read source files and metadata, but the build stays centered on compiling crates and writing artifacts under `target\release`. Once Rustup and Cargo caches are present, the path universe is fairly contained.

MSBuild has a broader mandate. A `dotnet build` of a solution has to evaluate project files, imported `.props` and `.targets`, SDK files, package assets, target frameworks, analyzer configuration, reference assemblies, generated outputs, and input/output freshness. Before it can compile, it keeps asking the file system questions: does this file exist, which target applies, which reference wins, is this output stale, which package asset matches this framework? That is how a .NET build creates a storm of `CreateFile`, `QueryOpen`, `QueryDirectory`, and `QueryNetworkOpenInformationFile` even when the final emitted binaries are only a fraction of the paths touched.

Roslyn amplifies MSBuild's natural shape because Roslyn is itself a compiler-platform repository. It contains C# and VB compilers, workspaces, analyzers, tests, source generators, resources, `.editorconfig` and `.globalconfig` inputs, reference-assembly use, and multi-targeted outputs. This is not merely "compile a lot of `.cs` files." It is "evaluate a large .NET build graph, resolve many references, run compiler infrastructure, and emit many managed assemblies."

Ripgrep amplifies the native compiler/linker shape instead. Rust compilation creates crate metadata and libraries (`.rmeta`, `.rlib`), pulls from Rustup toolchain libraries, writes PDBs and native libraries on Windows/MSVC, and invokes linker/resource tools for final outputs. The trace is less about path fan-out and more about concentrated artifact production.

## What Differs In The Trace, And How

The trace splits cleanly along one axis: path fan-out versus artifact concentration.

Roslyn touches about 153.8k unique core file paths. Ripgrep touches about 3.4k. That 45x gap is not just a bigger number; it changes the security problem. Every distinct path is another chance for an AV engine to decide whether a hash, signer, package, location, or generated output is known. A simple hot-cache assumption gets weaker as the path set explodes.

The second difference is metadata pressure. Roslyn has millions of open/query operations because MSBuild and .NET SDK resolution are path-decision systems. The build has to interrogate the file system before it knows what to compile, copy, skip, or generate. Ripgrep performs metadata work too, but it is not dominated by graph evaluation in the same way.

The third difference is output type. Ripgrep creates fewer executable-like outputs, but they are native build artifacts surrounded by linker/PDB/library side effects. Roslyn creates a much larger population of fresh DLLs under `artifacts\obj`. AV engines often treat newly created executable content differently from source text or known platform assemblies, so the output class matters as much as the count.

The fourth difference is trust shape. Roslyn has a large body of likely-known Microsoft/SDK/reference assembly activity, but it also has a large body of fresh local outputs. Ripgrep has fewer likely Microsoft inputs, more Rustup/Cargo user-cache activity proportionally, and fewer but denser fresh native outputs. This is why raw DLL counts are a trap: a Microsoft reference DLL, a NuGet package DLL, and a freshly emitted local DLL do not necessarily travel through the same AV logic.

The fifth difference is phase shape:

| phase question | ripgrep answer | Roslyn answer |
| --- | --- | --- |
| main orchestration | Cargo crate graph | dotnet/MSBuild solution graph |
| compiler core | many `rustc.exe` crate compiles | `dotnet.exe` plus `VBCSCompiler.exe` compiler-server reads |
| metadata pressure | moderate | very high |
| output pressure | concentrated native/PDB/lib/rmeta outputs | huge generated managed DLL/XML/cache/resources outputs |
| trust profile | Rustup/Cargo cache + fresh native outputs | Microsoft SDK/NuGet/reference reads + fresh DLL outputs |
| network/profile noise | near-local | restore/cache/telemetry-like network observed |

## Why AV Products Respond Differently

AV products are not just byte scanners bolted onto file reads. A modern engine usually mixes minifilter callbacks, path policy, hash caches, signer reputation, cloud prevalence, script rules, behavior models, executable-content heuristics, process reputation, and sometimes explicit developer-tool handling. Ripgrep and Roslyn stress different combinations of those systems.

Here are the main AV decision points this pair separates:

1. **Per-open and metadata-query overhead**

   Roslyn is the sharper probe here. If a product adds even a tiny delay to `CreateFile`, `QueryOpen`, directory queries, or metadata checks, Roslyn can multiply that delay millions of times. The same product may look fine on ripgrep because ripgrep does not fan out across the file system the same way.

2. **Fresh executable/DLL output scanning**

   Both workloads hit this, but with different artifacts. Ripgrep emits native executable/build-script/linker artifacts. Roslyn emits many managed DLLs. Products that deeply inspect newly created executable content, wait on cloud reputation, or flag "compiler produced a binary" behavior can slow either workload. Roslyn has the larger total output surface; ripgrep has the denser native-linker surface.

3. **Signer and path reputation**

   Roslyn reads far more likely Microsoft/SDK/reference content. A product with strong Microsoft publisher/path reputation can fast-path a large slice of Roslyn's input side. That does not automatically make Roslyn cheap, because the fresh outputs are local and newly generated. Ripgrep reads fewer Microsoft-path artifacts and more Rustup/Cargo/user-cache files proportionally, so it may depend more on hash/cloud prevalence than Microsoft Authenticode shortcuts.

4. **Package-cache trust**

   NuGet and Cargo/Rustup caches are different reputation ecosystems. NuGet packages may be common and package-signed, but contained DLLs are not guaranteed to be Authenticode-signed. Rustup/Cargo artifacts may be stable among developers, but they are less likely to sit on Microsoft publisher/path allowlists. Different AV vendors can make very different calls here.

5. **Compiler-server and long-lived process behavior**

   Roslyn's `VBCSCompiler.exe` concentrates a large amount of compiler input reading inside a compiler-server process. Some products build reputation and cache decisions around process identity and lifetime. A long-lived compiler server is not the same signal as many short-lived `rustc.exe` processes.

6. **FAST IO fallback and filter-driver behavior**

   Roslyn has far more `FAST IO DISALLOWED` events. That can indicate filters pushing the file system onto slower paths. If a product often prevents fast I/O on metadata-heavy workloads, Roslyn will expose that behavior more clearly than ripgrep.

7. **Cloud cache versus local cache**

   First-cloud-seen and average runs answer different security questions. Roslyn's huge unique path set can trigger many more cache and reputation decisions. Ripgrep may be dominated by fewer but more suspicious fresh artifacts. A product with strong local hash caching but slower cloud reputation can show a large first-run penalty and much better average behavior.

8. **Behavioral "developer tool" rules**

   Compilers write executable code by design, which is exactly why they are awkward for AV engines. Some products special-case trusted toolchains; others treat code generation, linker output, unsigned binaries, or script-generation patterns as behaviorally interesting. Ripgrep tests native compiler/linker behavior. Roslyn tests managed assembly generation at scale.

This is why rankings can diverge between the two charts. A product optimized for Microsoft/.NET SDK allowlisting and cheap metadata hooks can look good on Roslyn until the generated DLL phase bites. A product efficient on file opens but aggressive on native executable creation can survive Roslyn metadata pressure and still stumble on ripgrep's native output phase.

## Trace Scale

| metric | ripgrep | Roslyn | Roslyn / ripgrep |
| --- | ---: | ---: | ---: |
| ProcMon CSV size | 87 MB | 3.7 GB | 43x |
| total captured events | 463,658 | 15,194,989 | 33x |
| build-ish events, excluding ProcMon/System | 267,501 | 13,428,524 | 50x |
| core build-process events | 201,969 | 12,624,681 | 63x |
| core process tree size | 79 | 124 | 1.6x |
| unique core file paths | 3,395 | 153,774 | 45x |
| unique core registry paths | 6,050 | 5,477 | 0.9x |
| trace clock window | 57s | 11m 57s | 12.6x |

The most security-relevant row is unique core file paths. Roslyn touches about 45x as many distinct file paths as ripgrep. That is where the trace starts to look like an AV stress test rather than just a large build: more paths mean more cache misses, metadata decisions, reputation lookups, hash opportunities, and filter callbacks.

## Operation Family Structure

| family | ripgrep count | ripgrep % | Roslyn count | Roslyn % |
| --- | ---: | ---: | ---: | ---: |
| file | 136,997 | 67.8% | 9,829,880 | 77.9% |
| registry | 51,132 | 25.3% | 1,342,861 | 10.6% |
| other | 10,430 | 5.2% | 1,388,455 | 11.0% |
| process/thread | 3,095 | 1.5% | 14,603 | 0.1% |
| profiling | 270 | 0.1% | 34,252 | 0.3% |
| network | 45 | 0.0% | 14,630 | 0.1% |

Both builds are file dominated. Roslyn is more extreme because the file graph is the workload. Ripgrep's registry share stays visible because the native MSVC path probes system, SDK, Visual Studio, and runtime configuration. Roslyn also does a lot of registry work in absolute terms, but the file graph is so large that registry becomes a smaller slice.

## Top Core Processes

| process | ripgrep events | role |
| --- | ---: | --- |
| `rustc.exe` | 93,193 | Rust compiler work; reads Rust sources/metadata/libraries and writes crate metadata/artifacts. |
| `link.exe` | 32,788 | MSVC linker; writes PDB/final native artifacts and reads Windows/MSVC import libraries. |
| `VCTIP.EXE` | 25,858 | Visual Studio/MSVC helper activity, mostly registry/toolchain probing. |
| `avbench.exe` | 23,617 | Benchmark harness and process orchestration. |
| `cargo.exe` | 21,983 | Cargo package/build graph orchestration. |

| process | Roslyn events | role |
| --- | ---: | --- |
| `dotnet.exe` | 10,053,215 | .NET CLI/MSBuild host; dominates project graph, restore/build metadata, SDK, NuGet, and output activity. |
| `VBCSCompiler.exe` | 2,436,411 | Roslyn compiler server process; heavy reads of sources, references, analyzers, and compiler inputs. |
| `avbench.exe` | 51,301 | Benchmark harness and process orchestration. |
| `Conhost.exe` | 40,145 | Console host activity around the command-line build. |
| `VsdConfigTool.exe` | 37,482 | Visual Studio/dotnet configuration helper activity, mostly registry. |

The process split is one of the cleanest fingerprints. Ripgrep is a small process ecosystem around Cargo, rustc, the linker, and toolchain helpers. Roslyn is overwhelmingly `dotnet.exe` plus the compiler server.

## File-Operation Profile

| operation | ripgrep | Roslyn | interpretation |
| --- | ---: | ---: | --- |
| `CreateFile` | 18,983 | 2,206,227 | Roslyn opens vastly more files and directories. |
| `QueryOpen` | 3,257 | 1,615,022 | Roslyn performs huge existence/metadata checks. |
| `QueryNetworkOpenInformationFile` | 647 | 1,050,266 | Roslyn heavily queries file metadata by path. |
| `ReadFile` | 13,961 | 917,206 | Roslyn reads far more sources, assemblies, configs, and generated inputs. |
| `WriteFile` | 33,434 | 290,668 | Roslyn writes more in absolute terms, but ripgrep is much more write-heavy proportionally. |
| `QueryDirectory` | 1,041 | 195,120 | Roslyn performs much broader directory/project/package enumeration. |
| `Load Image` | 2,225 | 6,823 | Both load executables/DLLs; these are now counted as file activity. |
| `RegOpenKey` | 19,384 | 361,562 | Both query environment/toolchain/runtime configuration. |
| `RegQueryValue` | 15,057 | 536,724 | Roslyn has more registry value lookups in absolute terms. |

Roslyn has about 116x more `CreateFile`, 496x more `QueryOpen`, and 1,623x more `QueryNetworkOpenInformationFile` events than ripgrep. That is the signature of MSBuild graph evaluation: project files, imported `.props`/`.targets`, target-framework checks, reference assemblies, package files, generated outputs, and up-to-date decisions.

Ripgrep has only about 8.7x fewer `WriteFile` events than Roslyn despite having 63x fewer total core events. That is the tell: ripgrep is proportionally much more write-heavy. It fits the Rust/native build shape: compiler/linker outputs, PDBs, `.rlib`, `.rmeta`, `.lib`, `.o`, temp import libraries, and final binaries.

## Ripgrep Build Anatomy

Ripgrep's core activity is centered on:

| path root | core events | meaning |
| --- | ---: | --- |
| `C:\bench\ripgrep` | 58,442 | repository source and `target\release` outputs |
| `C:\Users\User` | 55,368 | Rustup toolchain, Cargo cache/config, temp linker/compiler files |
| `HKLM\System\CurrentControlSet` | 28,032 | system/device/toolchain registry lookups |
| `C:\Windows\System32` | 18,402 | system DLLs and Windows runtime inputs |
| `C:\Program Files (x86)\Microsoft Visual Studio` | 4,316 | MSVC linker/toolchain files |
| `C:\Program Files (x86)\Windows Kits` | 2,686 | Windows SDK import libraries/resources |

Top file extensions:

| extension | events | interpretation |
| --- | ---: | --- |
| `.dll` | 25,201 | Rust compiler driver DLLs, system DLLs, runtime/toolchain DLLs |
| `.rlib` | 18,623 | Rust library artifacts |
| `.pdb` | 16,946 | debug symbols, especially linker/compiler output |
| `.lib` | 14,438 | native/MSVC/Windows import libraries |
| `.rmeta` | 13,048 | Rust crate metadata |
| `.rs` | 10,235 | Rust source files |
| `.o` | 7,259 | object files |
| `.exe` | 4,704 | compiler/linker/tool/build executables |

Top write extensions:

| extension | write events | share of core writes |
| --- | ---: | ---: |
| `.pdb` | 15,575 | 45.9% |
| `.lib` | 11,388 | 33.6% |
| `.rmeta` | 4,805 | 14.2% |
| `.o` | 748 | 2.2% |
| `.a` | 747 | 2.2% |

This is the ripgrep story in one paragraph: the build does not explode across hundreds of thousands of paths; it concentrates activity in `target\release`, Rustup/Cargo caches, PDBs, import/static libraries, and native linker output. Products that get expensive on artifact creation, PDB writes, linker temp files, or newly created executable/library content will show up here.

Network activity is negligible: 45 core network events. That makes ripgrep a clean mostly-local compilation workload.

## Roslyn Build Anatomy

Roslyn's core activity is centered on:

| path root | core events | meaning |
| --- | ---: | --- |
| `C:\bench\roslyn` | 5,786,039 | source tree, generated files, `artifacts\obj`, project graph |
| `C:\Users\User` | 3,793,353 | NuGet cache, dotnet telemetry/cache, user package content |
| `HKLM\System\CurrentControlSet` | 1,103,457 | system/device/runtime registry lookups |
| `C:\Program Files\dotnet` | 924,617 | .NET SDK, MSBuild, analyzers, targets, runtime files |
| `C:\Program Files (x86)\Reference Assemblies` | 528,178 | .NET Framework/reference assemblies |

Top file extensions:

| extension | events | interpretation |
| --- | ---: | --- |
| `.dll` | 4,859,576 | reference assemblies, compiler/analyzer/runtime DLLs, build outputs |
| `.cs` | 747,779 | C# source files |
| `.sha512` | 474,300 | NuGet package verification/cache metadata |
| `.targets` | 241,580 | MSBuild target imports |
| `.xml` | 204,553 | docs/config/generated XML |
| `.props` | 176,379 | MSBuild property imports |
| `.resx` | 169,506 | resources |
| `.csproj` | 168,300 | project files |
| `.editorconfig` | 168,204 | analyzer/compiler configuration |
| `.globalconfig` | 161,833 | analyzer/compiler global configuration |
| `.vb` | 161,727 | Visual Basic source files in Roslyn |

Top write extensions:

| extension | write events | share of core writes |
| --- | ---: | ---: |
| `.dll` | 214,373 | 49.5% |
| `.tmp` | 76,748 | 17.7% |
| `.cache` | 39,936 | 9.2% |
| `.xml` | 26,756 | 6.2% |
| `.resources` | 21,138 | 4.9% |

Roslyn's trace reads like a large .NET ecosystem walkthrough:

- MSBuild project evaluation and target execution: `.csproj`, `.props`, `.targets`, `.editorconfig`, `.globalconfig`.
- Dependency and package infrastructure: `.nuget`, `.sha512`, package cache files, generated NuGet props/targets.
- Compiler inputs: `.cs`, `.vb`, `.resx`, reference assemblies, analyzer DLLs.
- Compiler/build outputs: `artifacts\obj`, generated `.cs`, output `.dll`, `.xml`, `.resources`, `.cache`, temp files.
- Compiler-server behavior: a very large `VBCSCompiler.exe` read footprint, which concentrates compiler input scanning in a long-lived process.

This is the kind of workload that punishes weak file-metadata caching or expensive open/query hooks. Even when the bytes are not large, the number of distinct paths and probes is enormous.

Network activity is still small proportionally, but it is not absent: 14,630 core network events. The top destinations include HTTPS traffic to Microsoft/Akamai-associated endpoints. Given the command and the NuGet/cache paths, the likely causes are restore, SDK/workload metadata, package/cache validation, or telemetry-adjacent traffic. If the goal is a strict local-only AV test, future Roslyn runs should use pre-restored packages, `--no-restore`, disabled telemetry, and ideally a blocked-network control.

## Registry and Configuration Behavior

Ripgrep registry activity is proportionally higher: 25.2% of core events. The hot roots are `HKLM\System\CurrentControlSet`, `HKLM\SOFTWARE\Microsoft`, and Visual Studio/MSVC-related locations. That matches a Windows native build path: compiler discovery, linker setup, Windows SDK lookup, runtime configuration, and Visual Studio toolchain plumbing.

Roslyn registry activity is much larger in absolute count but smaller proportionally: 1.34M events, or 10.6% of core events. It is dominated by `HKLM\System\CurrentControlSet` plus .NET/Visual Studio/SDK lookup. The smaller percentage does not make registry irrelevant; it means the file graph is so large that registry becomes background radiation.

For AV performance, registry operations usually matter less than file opens and writes. But registry-heavy setup can still intersect with self-defense, behavioral monitoring, policy checks, and process/toolchain reputation.

## Error And Result Patterns

Both traces have many non-success results that are normal for build systems:

| result | ripgrep | Roslyn | interpretation |
| --- | ---: | ---: | --- |
| `SUCCESS` | 149,553 | 9,374,926 | completed operations |
| `NAME NOT FOUND` | 18,759 | 1,034,436 | probing optional paths/files/registry keys |
| `FAST IO DISALLOWED` | 9,003 | 1,649,094 | Windows falls back to the slower IRP path; common under filters |
| `REPARSE` | 4,230 | 128,917 | path reparse/symlink/junction behavior |
| `NO MORE FILES` | 347 | 70,608 | directory enumeration completion |

`NAME NOT FOUND` is not a failure signal by itself. Build systems probe optional files constantly. Roslyn makes that especially visible because MSBuild and NuGet evaluate many possible imports, target frameworks, package assets, SDK files, generated outputs, and reference locations.

`FAST IO DISALLOWED` is worth watching because file-system filters can influence whether fast I/O paths are allowed. Roslyn has far more of these events, so a product that frequently forces slower paths can hurt Roslyn more than ripgrep.

## How The Differences Affect AV Performance

### Signed/trusted binary reputation

ProcMon does not record Authenticode signer, catalog signature, cloud prevalence, or an AV product's internal allowlist decision. So this analysis cannot prove that a file was trusted. The profiling pipeline uses path-based trust/reputation buckets as a proxy.

The trust story is not one-sided:

| question | likely answer from the trace |
| --- | --- |
| Which workload touches more likely Microsoft/SDK/reference binaries in absolute terms? | Roslyn. It has far more activity under `C:\Program Files\dotnet`, reference assemblies, SDK locations, and Windows DLL paths. |
| Which workload image-loads more platform-trusted DLLs? | Roslyn in absolute count; both workloads' image loads are mostly Windows/SDK paths. |
| Which workload creates more fresh executable-like outputs? | Roslyn by a very large margin. Its generated `artifacts\obj` DLL writes dominate executable-like writes. |
| Which workload is proportionally more non-Microsoft/user-cache/toolchain shaped? | Ripgrep. It is smaller, but much of its activity is Rustup/Cargo user-cache content plus fresh `target\release` outputs. |

Path-bucket summary from the enhanced profile:

| metric | ripgrep | Roslyn |
| --- | ---: | ---: |
| Microsoft OS path file events | 22,231 | 44,548 |
| Microsoft SDK / Program Files path file events | 8,475 | 1,335,173 |
| user package/toolchain cache file events | 36,145 Rustup/Cargo | 2,987,591 NuGet |
| fresh build-output file events | 48,320 | 2,966,226 |
| executable-like events in Microsoft OS/SDK buckets | 21,808 | 1,091,019 |
| executable-like events in package/toolchain cache buckets | 9,795 Rustup/Cargo | 2,587,458 NuGet |
| executable-like events in fresh build outputs | 1,404 | 2,195,529 |
| executable-like writes in fresh build outputs | 58 | 216,285 |

This matters because Roslyn's huge DLL/reference footprint is not uniformly expensive. Many inputs are likely stable, signed, common, or package-cache files that a product may trust quickly. But Roslyn also creates a very large number of new DLLs, and those local outputs may not get the same signer/cloud/cache shortcuts. Ripgrep has fewer files and fewer generated executable-like writes, but the Rustup/Cargo cache and fresh native artifacts may be less covered by Microsoft Authenticode allowlists.

### Count, percentage, rate, and weighted pressure

Event count alone is not enough. It answers "how many chances did AV have to intervene?" It does not answer "how expensive were those chances likely to be?" Percentage is useful for shape, but percentage hides scale. The pipeline therefore reports four layers:

| layer | what it answers | why it matters |
| --- | --- | --- |
| absolute count | how much AV exposure exists | Roslyn has far more file opens, metadata queries, failed probes, and writes |
| percentage | what kind of workload this is | ripgrep is proportionally write-heavy; Roslyn is metadata/open heavy |
| per-second rate | how intense the stream is | Roslyn emits more operations per second despite running longer |
| weighted pressure | which events are likely expensive | fresh executable writes and fresh build outputs count more than known Microsoft OS/SDK reads |

Weighted-pressure summary:

| metric | ripgrep | Roslyn |
| --- | ---: | ---: |
| weighted pressure score | 877,977 | 52,713,707 |
| pressure per second | 15,369 | 73,520 |
| pressure per 1,000 events | 4,347 | 4,175 |
| top pressure group | write | write |
| top pressure trust bucket | fresh build output | fresh build output |
| top pressure phase | link/resources | output/write phase |

The score is heuristic, not measured latency. It uses transparent weights: metadata query is low, read is low/moderate, write is higher, image load is higher, executable-like write is very high. Likely Microsoft OS/SDK paths get a low multiplier; fresh build outputs and user/package/toolchain paths get higher multipliers.

This changes the conclusion. Roslyn has overwhelmingly more total AV pressure because it is huge and creates many fresh DLL outputs. Ripgrep has similar, slightly higher pressure density per 1,000 events because its smaller event stream is concentrated in writes and fresh native artifacts. Roslyn is the larger total stress test; ripgrep is the denser native-artifact stress test.

Similar reputation modifiers to track in future runs:

- Authenticode signer and catalog signing: Microsoft-signed OS/SDK/reference binaries may take a faster AV path than unsigned local outputs.
- Cloud prevalence and file age: common SDK/NuGet/Rustup files may be known; newly built outputs are not.
- Path and publisher trust: `C:\Windows`, `Program Files\dotnet`, Visual Studio, and Windows Kits often behave differently from `C:\bench` or user-profile caches.
- Generated executable-like outputs: new `.exe`/`.dll` files can trigger deeper static, behavioral, or reputation checks.
- Package cache trust: NuGet packages may be package-signed or common, but contained DLLs are not always Authenticode-signed.
- Toolchain cache trust: Rustup/Cargo artifacts may be stable and common, but not Microsoft-signed.
- Image-load vs file-read vs file-write: AV products may treat loading a DLL differently from reading it as data or writing it as a fresh output.
- Script and MOTW semantics: `.ps1`, `.js`, downloaded files, and Zone.Identifier/MOTW can change reputation paths dramatically.
- Cache scope: per-product local cache, cloud cache, file hash cache, and VM-reset behavior can make first-cloud-seen and average runs diverge.

### Ripgrep / Rust Native Build

Primary AV pressure:

- Scanning newly written compiler/linker artifacts.
- Scanning PDB, `.lib`, `.rmeta`, `.rlib`, `.o`, `.a`, and final executable outputs.
- Intercepting linker temp files under `%TEMP%`.
- Repeated reads of Rust toolchain libraries and Rust standard library artifacts.
- Native Windows SDK/MSVC toolchain discovery and registry probing.

Expected AV-sensitive behaviors:

- Products that scan every newly created object/library/debug artifact may show high impact.
- Products with good path/hash caching for Rust toolchain libraries should improve after the cloud/cache-warm run.
- Products that treat compiler/linker output as suspicious executable-generation behavior may add latency even with fewer unique paths.

### Roslyn / .NET Managed Build

Primary AV pressure:

- Massive file open/query/read volume across source, SDK, NuGet, reference assembly, generated, and output files.
- Many `.dll` reads and writes, including analyzers, reference assemblies, compiler/runtime components, and outputs.
- MSBuild/NuGet target and property evaluation through `.props` and `.targets`.
- Incremental-build and dependency graph checks through path existence, timestamps, metadata, and directory enumeration.
- Compiler-server process reading large numbers of compiler inputs.

Expected AV-sensitive behaviors:

- Products with expensive per-open or per-query filtering can suffer even if byte scanning is modest.
- Products with weaker cache reuse across VM-reset runs will pay repeatedly for SDK, NuGet, reference assembly, and analyzer paths.
- Products that apply heavier rules to DLLs, source generators, analyzers, or newly emitted assemblies can show strong impact.
- Products that perform cloud reputation checks for many distinct DLL/source/generated paths may have high first-cloud-seen impact.

## Evidence Matrix

This is the compact version of the argument above:

| dimension | ripgrep | Roslyn |
| --- | --- | --- |
| ecosystem | Rust/Cargo/native MSVC link | .NET/MSBuild/Roslyn/NuGet |
| scale | compact | very large |
| unique files | low thousands | hundred-thousand scale |
| dominant stress | artifact writes and linker/compiler outputs | file graph traversal, metadata checks, references, packages |
| primary process shape | `cargo` + `rustc` + `link` | `dotnet` + `VBCSCompiler` |
| AV cache question | can the product handle generated native artifacts efficiently? | can the product handle huge managed graph traversal and generated DLLs efficiently? |
| network sensitivity | near-local | some restore/metadata/telemetry-like HTTPS activity observed |

The value is in the contrast. Treating these as duplicate samples of "compile time" throws away the signal.

## Recommendations For Future Benchmark Interpretation

1. Keep ripgrep and Roslyn as separate charts, not only one combined "compilation" result. Their OS-operation structures are different enough that a single combined number hides useful product behavior.

2. When building an overall score, treat them as two compilation sub-workloads rather than duplicate samples. A product can be excellent on ripgrep and poor on Roslyn, or the reverse, for defensible technical reasons.

3. For Roslyn, add a strictly offline/pre-restored variant if the goal is pure local AV overhead. The current trace includes some network activity, and `dotnet build` can implicitly restore or touch workload/package metadata.

4. For both workloads, report first-cloud-seen and average/cache-warm behavior separately. The first-cloud-seen run captures cloud reputation and cold product cache behavior; the average run captures steady-state developer experience after reputation/caches have had a chance to settle.

5. If a product is an outlier on Roslyn, inspect file-open/query latency, not only write scanning. Roslyn is dominated by path and metadata behavior more than a simple "bytes written" model.

6. If a product is an outlier on ripgrep, inspect newly created compiler/linker artifacts, PDB writes, and temp import-library behavior.

## Caveats

ProcMon traces are observational. They show what operations occurred, not exactly how much latency each AV product added to each operation. Counts still matter because they show the surface area exposed to file-system filters, but timing attribution needs per-product timing data or ETW/WPA-style latency analysis.

The core build-process filter is based on ProcMon process-create relationships. This is stronger than a static process-name filter, but not perfect: it includes console/helper children such as `Conhost.exe`, and it can miss build-related work performed by long-running system services that were not descendants of the benchmark process.

The byte counters in the generated JSON should be treated as directional. ProcMon `Detail` fields vary by operation, and this analysis is about operation structure, path fan-out, process mix, file type mix, and reputation surface rather than exact byte accounting.

## Bottom Line

Ripgrep is the compact native-artifact stress test. Roslyn is the large managed-graph stress test. They are both compilation workloads, but they ask different AV questions. That is exactly why both should stay in the suite.
