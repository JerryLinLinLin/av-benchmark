# Workload profile pipeline

This is the reproducible profiling layer for the ripgrep and Roslyn ProcMon traces. It connects the OS-level behavior, inferred build phases, AV-relevant pressure points, reproducibility controls, and benchmark slowdown data from `data/exp1/compare.csv`.

## What This Adds Beyond Counting Events

- Core scope is a dynamic process tree rooted at the workload-specific `avbench.exe` process.
- `Load Image` is counted as file activity because DLL/EXE image loads are AV-relevant.
- File operations are grouped into `open_close`, `metadata_query`, `read`, `write`, `image_load`, `registry`, `network`, and `other`.
- Phases are inferred from process names, operations, and paths. They are workload-profile heuristics, not compiler-internal ground truth.
- Benchmark correlation uses per-product slowdown from `data/exp1/compare.csv`; ProcMon profiles are baseline workload profiles, not per-AV traces.

## Workload shape summary

| trace | core_events | duration_s | events_s | unique_files | metadata_queries | writes | image_loads | failed_probes | fast_io_disallowed |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| ripgrep | 201969 | 57.127 | 3535.436 | 3396 | 31190 | 33912 | 2263 | 19647 | 9003 |
| roslyn | 12624681 | 716.994 | 17607.784 | 153774 | 4049794 | 432862 | 6823 | 1087406 | 1649094 |

## Phase Detection

### ripgrep
| phase | events | pct | metadata_query | read | write | image_load | registry | network | top_process | top_operation |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| rustc compile | 93193 | 46.142 | 18442 | 8060 | 17770 | 1443 | 9734 | 0 | rustc.exe | WriteFile |
| link/resources | 60512 | 29.961 | 4924 | 4127 | 15779 | 489 | 22534 | 45 | link.exe | WriteFile |
| harness/console | 24526 | 12.143 | 2385 | 457 | 8 | 164 | 13376 | 0 | avbench.exe | RegOpenKey |
| cargo graph/setup | 21983 | 10.884 | 5202 | 1317 | 353 | 115 | 4484 | 0 | cargo.exe | CreateFile |
| build scripts | 1755 | 0.869 | 237 | 0 | 2 | 52 | 1004 | 0 | build-script-build.exe | RegOpenKey |

### Roslyn
| phase | events | pct | metadata_query | read | write | image_load | registry | network | top_process | top_operation |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| msbuild evaluation | 7019629 | 55.602 | 2247244 | 322301 | 5283 | 4921 | 1281013 | 0 | dotnet.exe | CreateFile |
| restore/cache touch | 2729023 | 21.617 | 1044109 | 137287 | 2450 | 5 | 17 | 14630 | dotnet.exe | CreateFile |
| compiler server compile | 2436411 | 19.299 | 746174 | 456582 | 117304 | 150 | 729 | 0 | VBCSCompiler.exe | ReadFile |
| output/write phase | 307760 | 2.438 | 0 | 0 | 307760 | 0 | 0 | 0 | dotnet.exe | WriteFile |
| harness/console | 89786 | 0.711 | 10175 | 696 | 65 | 1246 | 28279 | 0 | avbench.exe | Process Profiling |
| other build tasks | 42072 | 0.333 | 2092 | 340 | 0 | 501 | 32823 | 0 | VsdConfigTool.exe | RegQueryKey |

## AV-Relevance Metrics

These metrics describe the surfaces an AV engine has to make decisions on. They are not direct latency measurements, but they explain where slowdown can plausibly enter the build.

| trace | unique_file_paths | unique_executable_like_paths | new_write_paths | executable_like_write_paths | metadata_query_events | image_load_events | write_events | fast_io_disallowed_events | failed_probe_events | network_events | registry_query_events | observed_read_mb | observed_write_mb |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| ripgrep | 3396 | 590 | 674 | 7 | 31190 | 2263 | 33912 | 9003 | 19647 | 45 | 39241 | 766.9 | 303.9 |
| roslyn | 153774 | 67340 | 82836 | 61377 | 4049794 | 6823 | 432862 | 1649094 | 1087406 | 14630 | 1176435 | 36580.2 | 2232.4 |

## Count, Percent, Rate, and Weighted Pressure

Event count alone is a weak lens. Counts show exposure volume, percentages show workload shape, rates show intensity, and the weighted pressure model estimates where AV cost is most likely to concentrate. The score is intentionally transparent rather than absolute: fresh executable writes and local build outputs carry much more weight than likely trusted Microsoft OS/SDK paths.

### Weighted pressure summary

| trace | score | score_per_second | score_per_1000_events | top_group | top_trust_bucket | top_phase |
| --- | --- | --- | --- | --- | --- | --- |
| ripgrep | 877977.0 | 15368.85 | 4347.088 | write | fresh_build_output | link/resources |
| roslyn | 52713706.8 | 73520.398 | 4175.449 | write | fresh_build_output | output/write phase |

### Pressure by operation group

#### ripgrep
| name | score | pct |
| --- | --- | --- |
| write | 697139.1 | 79.403 |
| metadata_query | 65117.5 | 7.417 |
| open_close | 63239.2 | 7.203 |
| read | 32070.6 | 3.653 |
| registry | 12768.9 | 1.454 |
| image_load | 5137.6 | 0.585 |
| other | 1743.0 | 0.199 |
| process_thread | 626.0 | 0.071 |
| network | 135.0 | 0.015 |

#### Roslyn
| name | score | pct |
| --- | --- | --- |
| write | 31150983.0 | 59.095 |
| metadata_query | 11981211.1 | 22.729 |
| open_close | 6258053.2 | 11.872 |
| read | 2805117.6 | 5.321 |
| registry | 381787.8 | 0.724 |
| other | 74666.5 | 0.142 |
| network | 43890.0 | 0.083 |
| image_load | 15094.4 | 0.029 |
| process_thread | 2903.2 | 0.006 |

### Pressure by trust/reputation bucket

#### ripgrep
| name | score | pct |
| --- | --- | --- |
| fresh_build_output | 637293.2 | 72.587 |
| other_user_profile_path | 148101.6 | 16.869 |
| rustup_toolchain_user_cache | 39518.0 | 4.501 |
| cargo_crate_user_cache | 17077.2 | 1.945 |
| registry | 11260.1 | 1.283 |
| source_tree_non_output | 9412.8 | 1.072 |
| microsoft_os_path | 6273.9 | 0.715 |
| other_path | 4787.1 | 0.545 |
| microsoft_sdk_programfiles_path | 3337.1 | 0.38 |
| none | 656.8 | 0.075 |
| other_programfiles_path | 259.2 | 0.03 |

#### Roslyn
| name | score | pct |
| --- | --- | --- |
| fresh_build_output | 42538277.6 | 80.697 |
| source_tree_non_output | 4914141.6 | 9.322 |
| nuget_user_cache | 3724704.5 | 7.066 |
| microsoft_sdk_programfiles_path | 531030.4 | 1.007 |
| other_user_profile_path | 432670.0 | 0.821 |
| registry | 317353.6 | 0.602 |
| other_path | 223008.5 | 0.423 |
| microsoft_os_path | 12066.5 | 0.023 |
| other_programfiles_path | 10700.4 | 0.02 |
| none | 9746.0 | 0.018 |
| cargo_crate_user_cache | 7.6 | 0.0 |

### Pressure by phase

#### ripgrep
| name | score | pct |
| --- | --- | --- |
| link/resources | 421906.9 | 48.054 |
| rustc compile | 408668.3 | 46.547 |
| cargo graph/setup | 39384.2 | 4.486 |
| harness/console | 6939.7 | 0.79 |
| build scripts | 1077.8 | 0.123 |

#### Roslyn
| name | score | pct |
| --- | --- | --- |
| output/write phase | 19504734.4 | 37.001 |
| msbuild evaluation | 15592239.4 | 29.579 |
| compiler server compile | 14612443.1 | 27.72 |
| restore/cache touch | 2913726.8 | 5.527 |
| harness/console | 78428.6 | 0.149 |
| other build tasks | 12134.4 | 0.023 |

### Pressure model definition

| metric | value |
| --- | --- |
| description | Heuristic AV pressure score. It is not measured latency. It estimates likely AV cost by weighting operation type and reducing likely trusted Microsoft/SDK paths while increasing fresh outputs and user/package/toolchain cache paths. |
| operation_group_weights | {'open_close': 0.5, 'metadata_query': 1.0, 'read': 1.5, 'write': 6.0, 'image_load': 8.0, 'registry': 0.3, 'network': 2.0, 'process_thread': 0.2, 'other': 0.2} |
| trust_bucket_multipliers | {'microsoft_os_path': 0.2, 'microsoft_sdk_programfiles_path': 0.35, 'other_programfiles_path': 0.8, 'nuget_user_cache': 1.2, 'rustup_toolchain_user_cache': 1.5, 'cargo_crate_user_cache': 1.7, 'source_tree_non_output': 2.0, 'other_user_profile_path': 2.0, 'other_path': 1.5, 'fresh_build_output': 4.0, 'registry': 0.5, 'network': 1.0, 'none': 1.0} |
| executable_write_base_weight | 30.0 |
| fast_io_disallowed_bonus | 1.0 |
| failed_probe_bonus | 0.5 |

## Trust and Reputation Heuristics

This section answers a key AV question: is the workload mostly touching likely-known signed platform binaries, or is it creating/touching less-known generated binaries? The buckets are path-based heuristics only. ProcMon does not include Authenticode signer, catalog signing, cloud reputation, or product whitelist decisions.

### Core file events by trust bucket

#### ripgrep
| name | count | pct |
| --- | --- | --- |
| fresh_build_output | 48320 | 35.271 |
| rustup_toolchain_user_cache | 25943 | 18.937 |
| microsoft_os_path | 22231 | 16.227 |
| other_user_profile_path | 14465 | 10.559 |
| cargo_crate_user_cache | 10202 | 7.447 |
| microsoft_sdk_programfiles_path | 8475 | 6.186 |
| source_tree_non_output | 5089 | 3.715 |
| other_path | 1966 | 1.435 |
| other_programfiles_path | 306 | 0.223 |

#### Roslyn
| name | count | pct |
| --- | --- | --- |
| nuget_user_cache | 2987591 | 30.393 |
| fresh_build_output | 2966226 | 30.176 |
| source_tree_non_output | 2170384 | 22.079 |
| microsoft_sdk_programfiles_path | 1335173 | 13.583 |
| other_user_profile_path | 262851 | 2.674 |
| other_path | 49155 | 0.5 |
| microsoft_os_path | 44548 | 0.453 |
| other_programfiles_path | 13948 | 0.142 |
| cargo_crate_user_cache | 4 | 0.0 |

### Executable-like file events by trust bucket

#### ripgrep
| name | count | pct |
| --- | --- | --- |
| microsoft_os_path | 18042 | 52.5 |
| rustup_toolchain_user_cache | 9421 | 27.414 |
| microsoft_sdk_programfiles_path | 3766 | 10.959 |
| fresh_build_output | 1404 | 4.085 |
| registry | 766 | 2.229 |
| other_path | 426 | 1.24 |
| cargo_crate_user_cache | 374 | 1.088 |
| other_programfiles_path | 137 | 0.399 |
| other_user_profile_path | 26 | 0.076 |
| source_tree_non_output | 4 | 0.012 |

#### Roslyn
| name | count | pct |
| --- | --- | --- |
| nuget_user_cache | 2587458 | 43.986 |
| fresh_build_output | 2195529 | 37.323 |
| microsoft_sdk_programfiles_path | 1057575 | 17.978 |
| microsoft_os_path | 33444 | 0.569 |
| other_user_profile_path | 5263 | 0.089 |
| source_tree_non_output | 1490 | 0.025 |
| registry | 1242 | 0.021 |
| other_path | 442 | 0.008 |
| other_programfiles_path | 45 | 0.001 |

### Image-load events by trust bucket

#### ripgrep
| name | count | pct |
| --- | --- | --- |
| microsoft_os_path | 2014 | 88.997 |
| microsoft_sdk_programfiles_path | 130 | 5.745 |
| rustup_toolchain_user_cache | 108 | 4.772 |
| fresh_build_output | 6 | 0.265 |
| other_path | 3 | 0.133 |
| cargo_crate_user_cache | 2 | 0.088 |

#### Roslyn
| name | count | pct |
| --- | --- | --- |
| microsoft_sdk_programfiles_path | 3422 | 50.154 |
| microsoft_os_path | 3393 | 49.729 |
| nuget_user_cache | 5 | 0.073 |
| other_path | 3 | 0.044 |

### Executable-like writes by trust bucket

#### ripgrep
| name | count | pct |
| --- | --- | --- |
| fresh_build_output | 58 | 100.0 |

#### Roslyn
| name | count | pct |
| --- | --- | --- |
| fresh_build_output | 216285 | 99.445 |
| nuget_user_cache | 712 | 0.327 |
| other_user_profile_path | 496 | 0.228 |

### Trust/reputation interpretation

Roslyn touches far more likely-known platform content in absolute terms, especially `C:\Program Files\dotnet`, reference assemblies, SDK files, Windows DLLs, and Microsoft build infrastructure. That means a large part of its DLL/reference footprint may benefit from signer, catalog, path, or cloud reputation shortcuts in many AV products.

But Roslyn also writes and reopens a much larger number of fresh build-output DLLs under `C:\bench\roslyn\artifacts\obj`. Those fresh outputs are the opposite reputation shape: new, local, unsigned-or-not-yet-known build artifacts. So Roslyn combines a trusted-platform read surface with a very large generated-DLL write surface.

Ripgrep touches fewer Microsoft/SDK binaries overall. Its trusted-platform image loads are mostly normal Windows/MSVC runtime DLLs and linker inputs, while much of the Rust workload comes from user-profile Rustup/Cargo caches plus freshly emitted `target\release` artifacts. That makes ripgrep smaller, but proportionally more exposed to non-Microsoft toolchain/package reputation and freshly generated native artifacts.

## Core Appendix

### File Operation Groups

#### ripgrep
| name | count | pct |
| --- | --- | --- |
| open_close | 62730 | 31.059 |
| registry | 51132 | 25.317 |
| write | 33912 | 16.791 |
| metadata_query | 31190 | 15.443 |
| read | 13961 | 6.912 |
| other | 3641 | 1.803 |
| process_thread | 3095 | 1.532 |
| image_load | 2263 | 1.12 |
| network | 45 | 0.022 |

#### Roslyn
| name | count | pct |
| --- | --- | --- |
| open_close | 5672521 | 44.932 |
| metadata_query | 4049794 | 32.078 |
| registry | 1342861 | 10.637 |
| read | 917206 | 7.265 |
| write | 432862 | 3.429 |
| other | 173381 | 1.373 |
| network | 14630 | 0.116 |
| process_thread | 14603 | 0.116 |
| image_load | 6823 | 0.054 |

### Per-Second Rates

#### ripgrep
| metric | value |
| --- | --- |
| duration_seconds | 57.127 |
| events_per_second | 3535.436 |
| file_events_per_second | 2398.111 |
| registry_events_per_second | 895.058 |
| network_events_per_second | 0.788 |
| open_close_per_second | 1098.079 |
| metadata_query_per_second | 545.976 |
| read_per_second | 244.385 |
| write_per_second | 593.624 |
| image_load_per_second | 39.613 |

#### Roslyn
| metric | value |
| --- | --- |
| duration_seconds | 716.994 |
| events_per_second | 17607.784 |
| file_events_per_second | 13709.844 |
| registry_events_per_second | 1872.903 |
| network_events_per_second | 20.405 |
| open_close_per_second | 7911.529 |
| metadata_query_per_second | 5648.293 |
| read_per_second | 1279.237 |
| write_per_second | 603.717 |
| image_load_per_second | 9.516 |

### Process x Operation Matrix

#### ripgrep
| process | events | CreateFile | QueryOpen | QueryNetworkOpenInformationFile | ReadFile | WriteFile | Load Image | RegOpenKey | RegQueryValue | Process Create | Thread Create |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| rustc.exe | 93193 | 9979 | 569 | 80 | 8060 | 17758 | 1443 | 3859 | 3168 | 8 | 903 |
| link.exe | 32788 | 1960 | 817 | 339 | 3476 | 15356 | 235 | 954 | 594 | 8 | 285 |
| VCTIP.EXE | 25858 | 1155 | 619 | 151 | 465 | 6 | 147 | 6958 | 5729 | 0 | 63 |
| avbench.exe | 23617 | 1791 | 917 | 67 | 451 | 5 | 126 | 4989 | 3351 | 2 | 37 |
| cargo.exe | 21983 | 3835 | 274 | 10 | 1317 | 271 | 115 | 1631 | 1462 | 59 | 86 |
| build-script-build.exe | 1755 | 118 | 31 | 0 | 0 | 2 | 52 | 417 | 302 | 4 | 15 |
| Conhost.exe | 909 | 31 | 14 | 0 | 6 | 0 | 38 | 180 | 212 | 0 | 12 |
| cvtres.exe | 686 | 50 | 12 | 0 | 10 | 14 | 29 | 130 | 85 | 0 | 8 |
| mt.exe | 635 | 23 | 6 | 0 | 95 | 2 | 48 | 146 | 94 | 0 | 8 |
| rc.exe | 545 | 41 | 12 | 0 | 81 | 20 | 30 | 120 | 60 | 0 | 8 |

#### Roslyn
| process | events | CreateFile | QueryOpen | QueryNetworkOpenInformationFile | ReadFile | WriteFile | Load Image | RegOpenKey | RegQueryValue | Process Create | Thread Create |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| dotnet.exe | 10053215 | 1832014 | 1383606 | 821411 | 459567 | 174048 | 4921 | 344028 | 521688 | 111 | 4624 |
| VBCSCompiler.exe | 2436411 | 363524 | 229187 | 228679 | 456582 | 116554 | 150 | 296 | 231 | 2 | 1933 |
| avbench.exe | 51301 | 7713 | 900 | 70 | 686 | 61 | 125 | 4949 | 3317 | 2 | 72 |
| Conhost.exe | 40145 | 838 | 411 | 0 | 10 | 0 | 1121 | 5369 | 6218 | 0 | 400 |
| VsdConfigTool.exe | 37482 | 1693 | 840 | 106 | 314 | 5 | 377 | 5508 | 4480 | 0 | 55 |
| getmac.exe | 3168 | 83 | 42 | 0 | 8 | 0 | 68 | 734 | 474 | 0 | 14 |
| cmd.exe | 2959 | 362 | 36 | 0 | 39 | 0 | 61 | 678 | 316 | 10 | 15 |

### Top Image Loads

#### ripgrep
| name | count | pct |
| --- | --- | --- |
| C:\Windows\System32\ntdll.dll | 82 | 3.624 |
| C:\Windows\System32\kernel32.dll | 82 | 3.624 |
| C:\Windows\System32\KernelBase.dll | 82 | 3.624 |
| C:\Windows\System32\msvcrt.dll | 82 | 3.624 |
| C:\Windows\System32\bcrypt.dll | 79 | 3.491 |
| C:\Windows\System32\sechost.dll | 77 | 3.403 |
| C:\Windows\System32\ucrtbase.dll | 76 | 3.358 |
| C:\Windows\System32\user32.dll | 74 | 3.27 |
| C:\Windows\System32\win32u.dll | 74 | 3.27 |
| C:\Windows\System32\gdi32.dll | 74 | 3.27 |
| C:\Windows\System32\gdi32full.dll | 74 | 3.27 |
| C:\Windows\System32\msvcp_win.dll | 74 | 3.27 |
| C:\Windows\System32\advapi32.dll | 74 | 3.27 |
| C:\Windows\System32\rpcrt4.dll | 74 | 3.27 |
| C:\Windows\System32\imm32.dll | 74 | 3.27 |
| C:\Windows\System32\bcryptprimitives.dll | 74 | 3.27 |
| C:\Windows\System32\combase.dll | 72 | 3.182 |
| C:\Windows\System32\ole32.dll | 70 | 3.093 |
| C:\Windows\System32\shell32.dll | 64 | 2.828 |
| C:\Windows\System32\oleaut32.dll | 60 | 2.651 |
| C:\Windows\System32\ws2_32.dll | 60 | 2.651 |
| C:\Windows\System32\cryptbase.dll | 57 | 2.519 |
| C:\Windows\System32\userenv.dll | 55 | 2.43 |
| C:\Users\User\.rustup\toolchains\1.85.0-x86_64-pc-windows-msvc\bin\rustc.exe | 53 | 2.342 |
| C:\Users\User\.rustup\toolchains\1.85.0-x86_64-pc-windows-msvc\bin\rustc_driver-ebda0089c9512452.dll | 53 | 2.342 |
| C:\Windows\System32\propsys.dll | 53 | 2.342 |
| C:\Windows\System32\kernel.appcore.dll | 31 | 1.37 |
| C:\Windows\System32\SHCore.dll | 13 | 0.574 |
| C:\Windows\System32\apphelp.dll | 13 | 0.574 |
| C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools\VC\Tools\MSVC\14.50.35717\bin\Hostx64\x64\vcruntime140.dll | 12 | 0.53 |
| C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools\VC\Tools\MSVC\14.50.35717\bin\Hostx64\x64\vcruntime140_1.dll | 12 | 0.53 |
| C:\Windows\System32\windows.storage.dll | 11 | 0.486 |
| C:\Windows\System32\WinTypes.dll | 11 | 0.486 |
| C:\Windows\System32\shlwapi.dll | 11 | 0.486 |
| C:\Windows\System32\profapi.dll | 11 | 0.486 |
| C:\Windows\System32\clbcatq.dll | 11 | 0.486 |
| C:\ProgramData\Microsoft\VisualStudio\Setup\x64\Microsoft.VisualStudio.Setup.Configuration.Native.dll | 10 | 0.442 |
| C:\Windows\System32\psapi.dll | 10 | 0.442 |
| C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools\VC\Tools\MSVC\14.50.35717\bin\Hostx64\x64\msvcp140.dll | 10 | 0.442 |
| C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools\VC\Tools\MSVC\14.50.35717\bin\Hostx64\x64\link.exe | 8 | 0.354 |

#### Roslyn
| name | count | pct |
| --- | --- | --- |
| C:\Windows\System32\ntdll.dll | 126 | 1.847 |
| C:\Windows\System32\kernel32.dll | 121 | 1.773 |
| C:\Windows\System32\KernelBase.dll | 121 | 1.773 |
| C:\Windows\System32\ucrtbase.dll | 121 | 1.773 |
| C:\Windows\System32\msvcrt.dll | 121 | 1.773 |
| C:\Windows\System32\sechost.dll | 121 | 1.773 |
| C:\Windows\System32\bcrypt.dll | 121 | 1.773 |
| C:\Windows\System32\rpcrt4.dll | 121 | 1.773 |
| C:\Windows\System32\advapi32.dll | 119 | 1.744 |
| C:\Windows\System32\user32.dll | 116 | 1.7 |
| C:\Windows\System32\win32u.dll | 116 | 1.7 |
| C:\Windows\System32\gdi32.dll | 116 | 1.7 |
| C:\Windows\System32\gdi32full.dll | 116 | 1.7 |
| C:\Windows\System32\msvcp_win.dll | 116 | 1.7 |
| C:\Windows\System32\imm32.dll | 116 | 1.7 |
| C:\Windows\System32\combase.dll | 116 | 1.7 |
| C:\Windows\System32\shell32.dll | 112 | 1.642 |
| C:\Windows\System32\kernel.appcore.dll | 62 | 0.909 |
| C:\Windows\System32\uxtheme.dll | 62 | 0.909 |
| C:\Windows\System32\conhost.exe | 59 | 0.865 |
| C:\Windows\System32\ole32.dll | 57 | 0.835 |
| C:\Windows\System32\oleaut32.dll | 57 | 0.835 |
| C:\Windows\System32\bcryptprimitives.dll | 57 | 0.835 |
| C:\Program Files\dotnet\host\fxr\10.0.6\hostfxr.dll | 55 | 0.806 |
| C:\Windows\System32\icu.dll | 55 | 0.806 |
| C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\hostpolicy.dll | 54 | 0.791 |
| C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\coreclr.dll | 54 | 0.791 |
| C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Private.CoreLib.dll | 54 | 0.791 |
| C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Runtime.InteropServices.dll | 54 | 0.791 |
| C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Console.dll | 54 | 0.791 |
| C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Threading.dll | 54 | 0.791 |
| C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\clrjit.dll | 54 | 0.791 |
| C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Diagnostics.Process.dll | 54 | 0.791 |
| C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.ComponentModel.Primitives.dll | 54 | 0.791 |
| C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Collections.dll | 54 | 0.791 |
| C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Linq.dll | 54 | 0.791 |
| C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Memory.dll | 54 | 0.791 |
| C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Collections.Concurrent.dll | 54 | 0.791 |
| C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Collections.Immutable.dll | 54 | 0.791 |
| C:\Program Files\dotnet\shared\Microsoft.NETCore.App\10.0.6\System.Reflection.Metadata.dll | 54 | 0.791 |

### Top Executable-Like Writes

#### ripgrep
| name | count | pct |
| --- | --- | --- |
| C:\bench\ripgrep\target\release\deps\rg.exe | 22 | 37.931 |
| C:\bench\ripgrep\target\release\build\serde-383f949fe8b5850a\build_script_build-383f949fe8b5850a.exe | 6 | 10.345 |
| C:\bench\ripgrep\target\release\build\serde_json-a7496737e29b6e52\build_script_build-a7496737e29b6e52.exe | 6 | 10.345 |
| C:\bench\ripgrep\target\release\build\ripgrep-ee6c0b68c2bfff47\build_script_build-ee6c0b68c2bfff47.exe | 6 | 10.345 |
| C:\bench\ripgrep\target\release\build\crossbeam-utils-cb9da71b25dafbce\build_script_build-cb9da71b25dafbce.exe | 6 | 10.345 |
| C:\bench\ripgrep\target\release\build\anyhow-0769136d61e90248\build_script_build-0769136d61e90248.exe | 6 | 10.345 |
| C:\bench\ripgrep\target\release\build\serde_core-9325faa8aac88ce5\build_script_build-9325faa8aac88ce5.exe | 6 | 10.345 |

#### Roslyn
| name | count | pct |
| --- | --- | --- |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.CSharp.Features.UnitTests\Release\net10.0-windows\Microsoft.CodeAnalysis.CSharp.Features.UnitTests.dll | 1849 | 0.85 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.CSharp.Features.UnitTests\Release\net472\Microsoft.CodeAnalysis.CSharp.Features.UnitTests.dll | 1670 | 0.768 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.CSharp.Emit3.UnitTests\Release\net472\Microsoft.CodeAnalysis.CSharp.Emit3.UnitTests.dll | 1332 | 0.612 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.CSharp.Emit3.UnitTests\Release\net10.0\Microsoft.CodeAnalysis.CSharp.Emit3.UnitTests.dll | 1311 | 0.603 |
| C:\bench\roslyn\artifacts\obj\Microsoft.VisualStudio.LanguageServices\Release\net472\Microsoft.VisualStudio.LanguageServices.dll | 1293 | 0.595 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis\Release\net10.0\Microsoft.CodeAnalysis.dll | 1238 | 0.569 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis\Release\netstandard2.0\Microsoft.CodeAnalysis.dll | 1211 | 0.557 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.CSharp\Release\netstandard2.0\Microsoft.CodeAnalysis.CSharp.dll | 1170 | 0.538 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis\Release\net8.0\Microsoft.CodeAnalysis.dll | 1168 | 0.537 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.CSharp\Release\net10.0\Microsoft.CodeAnalysis.CSharp.dll | 1164 | 0.535 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.CSharp\Release\net8.0\Microsoft.CodeAnalysis.CSharp.dll | 1164 | 0.535 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.CSharp.CodeStyle.UnitTests\Release\net472\Microsoft.CodeAnalysis.CSharp.CodeStyle.UnitTests.dll | 1146 | 0.527 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.EditorFeatures\Release\net472\Microsoft.CodeAnalysis.EditorFeatures.dll | 1136 | 0.522 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests\Release\net472\Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.dll | 1059 | 0.487 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests\Release\net10.0\Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests.dll | 1054 | 0.485 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.CSharp.Symbol.UnitTests\Release\net10.0\Microsoft.CodeAnalysis.CSharp.Symbol.UnitTests.dll | 1008 | 0.463 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.EditorFeatures2.UnitTests\Release\net472\Microsoft.CodeAnalysis.EditorFeatures2.UnitTests.dll | 1002 | 0.461 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.CSharp.Symbol.UnitTests\Release\net472\Microsoft.CodeAnalysis.CSharp.Symbol.UnitTests.dll | 952 | 0.438 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.Features\Release\netstandard2.0\Microsoft.CodeAnalysis.Features.dll | 899 | 0.413 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.Features\Release\net8.0\Microsoft.CodeAnalysis.Features.dll | 893 | 0.411 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.Features\Release\net10.0\Microsoft.CodeAnalysis.Features.dll | 893 | 0.411 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.VisualBasic.EditorFeatures.UnitTests\Release\net472\Microsoft.CodeAnalysis.VisualBasic.EditorFeatures.UnitTests.dll | 825 | 0.379 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.Workspaces\Release\net8.0\Microsoft.CodeAnalysis.Workspaces.dll | 813 | 0.374 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.CSharp.Emit.UnitTests\Release\net472\Microsoft.CodeAnalysis.CSharp.Emit.UnitTests.dll | 799 | 0.367 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.CSharp.EditorFeatures.UnitTests\Release\net472\Microsoft.CodeAnalysis.CSharp.EditorFeatures.UnitTests.dll | 795 | 0.366 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.CSharp.Emit.UnitTests\Release\net10.0\Microsoft.CodeAnalysis.CSharp.Emit.UnitTests.dll | 772 | 0.355 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.Workspaces\Release\net10.0\Microsoft.CodeAnalysis.Workspaces.dll | 753 | 0.346 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis\Release\netstandard2.0\refint\Microsoft.CodeAnalysis.dll | 745 | 0.343 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.Workspaces\Release\netstandard2.0\Microsoft.CodeAnalysis.Workspaces.dll | 745 | 0.343 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.VisualBasic\Release\net8.0\Microsoft.CodeAnalysis.VisualBasic.dll | 739 | 0.34 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.VisualBasic\Release\netstandard2.0\Microsoft.CodeAnalysis.VisualBasic.dll | 738 | 0.339 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.VisualBasic\Release\net10.0\Microsoft.CodeAnalysis.VisualBasic.dll | 738 | 0.339 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis\Release\net8.0\refint\Microsoft.CodeAnalysis.dll | 715 | 0.329 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis\Release\net10.0\refint\Microsoft.CodeAnalysis.dll | 712 | 0.327 |
| C:\Users\User\.nuget\packages\microsoft.netcore.app.crossgen2.win-x64\10.0.5\tools\crossgen2.exe | 691 | 0.318 |
| C:\bench\roslyn\artifacts\obj\Microsoft.VisualStudio.LanguageServices\Release\net472\refint\Microsoft.VisualStudio.LanguageServices.dll | 669 | 0.308 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.CSharp\Release\netstandard2.0\refint\Microsoft.CodeAnalysis.CSharp.dll | 634 | 0.292 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.CSharp\Release\net10.0\refint\Microsoft.CodeAnalysis.CSharp.dll | 634 | 0.292 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.CSharp\Release\net8.0\refint\Microsoft.CodeAnalysis.CSharp.dll | 634 | 0.292 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.VisualBasic.Features.UnitTests\Release\net472\Microsoft.CodeAnalysis.VisualBasic.Features.UnitTests.dll | 633 | 0.291 |

### Top Failed Probes

#### ripgrep
| name | count | pct |
| --- | --- | --- |
| NAME NOT FOUND \| RegOpenKey \| HKLM\Software\Microsoft\LanguageOverlay\OverlayPackages\en-US | 334 | 1.7 |
| NAME NOT FOUND \| RegOpenKey \| HKLM\Software\Policies\Microsoft\Windows\Display | 296 | 1.507 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\SdbUpdates\DisableDoubleQuerySdbs | 285 | 1.451 |
| NAME NOT FOUND \| RegOpenKey \| HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options\rustc.exe | 265 | 1.349 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Control\Session Manager\ResourcePolicies | 251 | 1.278 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Control\WMI\Security\703fcc13-b66f-5868-ddd9-e2db7f381ffb | 207 | 1.054 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Control\WMI\Security\f3a71a4b-6118-4257-8ccb-39a33ba059d4 | 153 | 0.779 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\GRE_Initialize\DisableMetaFiles | 146 | 0.743 |
| NAME NOT FOUND \| RegOpenKey \| HKLM\Software\Microsoft\Ole\FeatureDevelopmentProperties | 144 | 0.733 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Control\WMI\Security\1aff6089-e863-4d36-bdfd-3581f07440be | 143 | 0.728 |
| NAME NOT FOUND \| RegOpenKey \| HKLM\Software\Microsoft\OLE\Tracing | 142 | 0.723 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Control\WMI\Security\f0558438-f56a-5987-47da-040ca75aef05 | 142 | 0.723 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Control\WMI\Security\32980f26-c8f5-5767-6b26-635b3fa83c61 | 128 | 0.651 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\SdbUpdates\sysmain.sdb | 127 | 0.646 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\SideBySide\PreferExternalManifest | 114 | 0.58 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Services\bam\State\UserSettings\S-1-5-21-751846449-727868010-3078789655-1000\\Device\HarddiskVolume4\Users\User\.rustup\toolchains\1.85.0-x86_64-pc-windows-msvc\bin\rustc.exe | 106 | 0.54 |
| NAME NOT FOUND \| RegOpenKey \| HKLM\Software\Policies\Microsoft\MUI\Settings | 102 | 0.519 |
| NAME NOT FOUND \| RegOpenKey \| HKLM\System\CurrentControlSet\Control\SafeBoot\Option | 101 | 0.514 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\SOFTWARE\Policies\Microsoft\Windows\safer\codeidentifiers\TransparentEnabled | 101 | 0.514 |
| NAME NOT FOUND \| RegOpenKey \| HKCU\Software\Policies\Microsoft\Windows\Safer\CodeIdentifiers | 101 | 0.514 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Control\Session Manager\RaiseExceptionOnPossibleDeadlock | 82 | 0.417 |
| NAME NOT FOUND \| RegOpenKey \| HKLM\System\CurrentControlSet\Control\Session Manager\Segment Heap | 82 | 0.417 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Control\WMI\Security\57a6ed1a-79f7-5011-b242-4784e5620cf7 | 82 | 0.417 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Control\WMI\Security\3c74afb9-8d82-44e3-b52c-365dbf48382a | 82 | 0.417 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Control\WMI\Security\b749553b-d950-5e03-6282-3145a61b1002 | 82 | 0.417 |
| NAME NOT FOUND \| RegOpenKey \| HKLM\System\CurrentControlSet\Control\StateSeparation\RedirectionMap\Keys | 82 | 0.417 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Control\WMI\Security\05f95efe-7f75-49c7-a994-60a55cc09571 | 82 | 0.417 |
| NAME NOT FOUND \| RegOpenKey \| HKLM\System\CurrentControlSet\Control\Srp\GP\DLL | 82 | 0.417 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Control\Session Manager\SmtDelaySleepLoopWindowSize | 82 | 0.417 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Control\Session Manager\SmtDelaySpinCountThreshold | 82 | 0.417 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Control\Session Manager\SmtDelayBaseYield | 82 | 0.417 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Control\Session Manager\SmtFactorYield | 82 | 0.417 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Control\Session Manager\SmtDelayMaxYield | 82 | 0.417 |
| NAME NOT FOUND \| RegOpenKey \| HKLM\System\CurrentControlSet\Control\Session Manager\BAM | 81 | 0.412 |
| NAME NOT FOUND \| RegOpenKey \| HKLM\Software\Microsoft\Wow64\x86\xtajit | 79 | 0.402 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Control\WMI\Security\ca967c75-04bf-40b5-9a16-98b5f9332a92 | 77 | 0.392 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Control\WMI\Security\b6fd710b-f783-4b1c-ab9c-c68099dcc0c7 | 77 | 0.392 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Control\Session Manager\SafeDllSearchMode | 76 | 0.387 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Control\WMI\Security\30336ed4-e327-447c-9de0-51b652c86108 | 75 | 0.382 |
| NAME NOT FOUND \| RegOpenKey \| HKLM\System\CurrentControlSet\Control\Error Message Instrument | 74 | 0.377 |

#### Roslyn
| name | count | pct |
| --- | --- | --- |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{ff024ff6-8658-4ea7-b235-889cad409853}\ProfileNameServer | 12368 | 1.137 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{53c87bda-4a58-11ef-ae96-806e6f6e6963}\ProfileNameServer | 12368 | 1.137 |
| NAME NOT FOUND \| RegOpenKey \| HKLM\System\CurrentControlSet\Services\Tcpip6\Parameters\Interfaces\{53C87BDA-4A58-11EF-AE96-806E6F6E6963} | 12368 | 1.137 |
| NAME NOT FOUND \| RegOpenKey \| HKLM\System\CurrentControlSet\Services\Dnscache\InterfaceSpecificParameters\{ff024ff6-8658-4ea7-b235-889cad409853} | 9276 | 0.853 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{ff024ff6-8658-4ea7-b235-889cad409853}\RegistrationEnabled | 9276 | 0.853 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{ff024ff6-8658-4ea7-b235-889cad409853}\RegisterAdapterName | 9276 | 0.853 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{ff024ff6-8658-4ea7-b235-889cad409853}\EnableMulticast | 9276 | 0.853 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{ff024ff6-8658-4ea7-b235-889cad409853}\QueryAdapterName | 9276 | 0.853 |
| NAME NOT FOUND \| RegOpenKey \| HKLM\System\CurrentControlSet\Services\Tcpip6\Parameters\Interfaces\{53c87bda-4a58-11ef-ae96-806e6f6e6963} | 9276 | 0.853 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{ff024ff6-8658-4ea7-b235-889cad409853}\SearchList | 6184 | 0.569 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Services\Tcpip6\Parameters\Interfaces\{ff024ff6-8658-4ea7-b235-889cad409853}\ProfileNameServer | 6184 | 0.569 |
| NAME NOT FOUND \| RegOpenKey \| HKLM\System\CurrentControlSet\Services\Dnscache\InterfaceSpecificParameters\{53c87bda-4a58-11ef-ae96-806e6f6e6963} | 6184 | 0.569 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{53c87bda-4a58-11ef-ae96-806e6f6e6963}\RegistrationEnabled | 6184 | 0.569 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{53c87bda-4a58-11ef-ae96-806e6f6e6963}\RegisterAdapterName | 6184 | 0.569 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{53c87bda-4a58-11ef-ae96-806e6f6e6963}\EnableMulticast | 6184 | 0.569 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{53c87bda-4a58-11ef-ae96-806e6f6e6963}\QueryAdapterName | 6184 | 0.569 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{53c87bda-4a58-11ef-ae96-806e6f6e6963}\Domain | 6184 | 0.569 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{53c87bda-4a58-11ef-ae96-806e6f6e6963}\SearchList | 6184 | 0.569 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{53c87bda-4a58-11ef-ae96-806e6f6e6963}\NameServer | 6184 | 0.569 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{53c87bda-4a58-11ef-ae96-806e6f6e6963}\DhcpNameServer | 6184 | 0.569 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\{53c87bda-4a58-11ef-ae96-806e6f6e6963}\EnableDhcp | 4644 | 0.427 |
| PATH NOT FOUND \| CreateFile \| C:\Program Files\dotnet\metadata\workloads\10.0.100\userlocal | 3310 | 0.304 |
| NAME NOT FOUND \| RegOpenKey \| HKLM\Software\Policies\Microsoft\System\DNSClient | 3135 | 0.288 |
| NAME NOT FOUND \| RegOpenKey \| HKLM\Software\Policies\Microsoft\Windows NT\DnsClient | 3134 | 0.288 |
| NAME NOT FOUND \| RegOpenKey \| HKLM\System\CurrentControlSet\Services\DNS | 3095 | 0.285 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Services\Dnscache\Parameters\QueryAdapterName | 3095 | 0.285 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Services\Tcpip\Parameters\DisableAdapterDomainName | 3095 | 0.285 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Services\Dnscache\Parameters\UseDomainNameDevolution | 3095 | 0.285 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Services\Tcpip\Parameters\UseDomainNameDevolution | 3095 | 0.285 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Services\Dnscache\Parameters\DomainNameDevolutionLevel | 3095 | 0.285 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Services\Dnscache\Parameters\PrioritizeRecordData | 3095 | 0.285 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Services\Tcpip\Parameters\PrioritizeRecordData | 3095 | 0.285 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Services\Dnscache\Parameters\AllowUnqualifiedQuery | 3095 | 0.285 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Services\Tcpip\Parameters\AllowUnqualifiedQuery | 3095 | 0.285 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Services\Dnscache\Parameters\AppendToMultiLabelName | 3095 | 0.285 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Services\Dnscache\Parameters\ScreenBadTlds | 3095 | 0.285 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Services\Dnscache\Parameters\ValidateResponseNames | 3095 | 0.285 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Services\Dnscache\Parameters\ScreenUnreachableServers | 3095 | 0.285 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Services\Dnscache\Parameters\ScreenDefaultServers | 3095 | 0.285 |
| NAME NOT FOUND \| RegQueryValue \| HKLM\System\CurrentControlSet\Services\Dnscache\Parameters\DynamicServerQueryOrder | 3095 | 0.285 |

### Busiest Seconds

#### ripgrep
| second | events | metadata_query | read | write | image_load | registry | network |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 8 | 35618 | 3302 | 531 | 12151 | 368 | 11474 | 0 |
| 6 | 26156 | 5001 | 6871 | 495 | 262 | 3696 | 0 |
| 7 | 22047 | 2085 | 1954 | 10632 | 147 | 1917 | 0 |
| 5 | 12256 | 2028 | 881 | 78 | 575 | 4291 | 0 |
| 1 | 11288 | 601 | 271 | 2 | 105 | 8874 | 0 |
| 51 | 11227 | 1614 | 786 | 2909 | 135 | 2326 | 0 |
| 2 | 10361 | 1402 | 40 | 0 | 5 | 3129 | 0 |
| 45 | 10239 | 2855 | 559 | 137 | 127 | 1669 | 0 |
| 9 | 8963 | 1623 | 316 | 933 | 63 | 2708 | 18 |
| 52 | 8506 | 294 | 33 | 14 | 19 | 7006 | 0 |
| 24 | 6016 | 1221 | 794 | 317 | 92 | 1150 | 0 |
| 46 | 5349 | 1577 | 63 | 211 | 52 | 354 | 0 |
| 15 | 4999 | 1415 | 103 | 200 | 78 | 507 | 0 |
| 12 | 4575 | 984 | 85 | 982 | 52 | 366 | 0 |
| 19 | 4382 | 1287 | 144 | 152 | 26 | 189 | 0 |
| 25 | 3821 | 140 | 175 | 2616 | 1 | 164 | 2 |
| 14 | 3568 | 952 | 85 | 204 | 78 | 495 | 0 |
| 17 | 3284 | 873 | 19 | 209 | 26 | 201 | 0 |
| 13 | 3242 | 645 | 94 | 925 | 26 | 189 | 0 |
| 10 | 2690 | 578 | 96 | 175 | 26 | 383 | 19 |
| 16 | 1485 | 318 | 13 | 316 | 0 | 24 | 0 |
| 18 | 729 | 163 | 5 | 83 | 0 | 12 | 0 |
| 11 | 336 | 88 | 17 | 84 | 0 | 0 | 0 |
| 23 | 176 | 41 | 2 | 40 | 0 | 0 | 6 |
| 50 | 126 | 20 | 1 | 42 | 0 | 0 | 0 |
| 48 | 119 | 0 | 0 | 0 | 0 | 0 | 0 |
| 21 | 115 | 0 | 0 | 0 | 0 | 0 | 0 |
| 49 | 93 | 44 | 0 | 3 | 0 | 0 | 0 |
| 22 | 72 | 28 | 0 | 0 | 0 | 0 | 0 |
| 20 | 64 | 5 | 23 | 1 | 0 | 0 | 0 |
| 47 | 32 | 4 | 0 | 1 | 0 | 0 | 0 |
| 3 | 9 | 2 | 0 | 0 | 0 | 4 | 0 |
| 4 | 6 | 0 | 0 | 0 | 0 | 4 | 0 |
| 29 | 3 | 0 | 0 | 0 | 0 | 0 | 0 |
| 27 | 2 | 0 | 0 | 0 | 0 | 0 | 0 |
| 33 | 2 | 0 | 0 | 0 | 0 | 0 | 0 |
| 37 | 2 | 0 | 0 | 0 | 0 | 0 | 0 |
| 42 | 2 | 0 | 0 | 0 | 0 | 0 | 0 |
| 26 | 1 | 0 | 0 | 0 | 0 | 0 | 0 |
| 31 | 1 | 0 | 0 | 0 | 0 | 0 | 0 |

#### Roslyn
| second | events | metadata_query | read | write | image_load | registry | network |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 626 | 68128 | 23294 | 470 | 80 | 4 | 4976 | 28 |
| 631 | 61249 | 20801 | 754 | 192 | 4 | 4608 | 15 |
| 712 | 61107 | 23790 | 811 | 552 | 0 | 3168 | 10 |
| 23 | 59533 | 9957 | 3614 | 25967 | 9 | 2503 | 1296 |
| 425 | 56330 | 21777 | 1961 | 1405 | 1 | 0 | 0 |
| 20 | 56109 | 19012 | 1563 | 4599 | 35 | 825 | 99 |
| 660 | 51572 | 16664 | 1154 | 435 | 3 | 7632 | 25 |
| 645 | 51052 | 17112 | 1250 | 192 | 0 | 6198 | 15 |
| 633 | 50633 | 16367 | 705 | 258 | 1 | 6343 | 20 |
| 24 | 50194 | 8097 | 5752 | 21938 | 0 | 757 | 16 |
| 418 | 47719 | 19962 | 2415 | 1930 | 2 | 0 | 0 |
| 638 | 46414 | 14765 | 732 | 837 | 0 | 6031 | 15 |
| 644 | 44480 | 14865 | 1035 | 324 | 0 | 4752 | 15 |
| 243 | 44446 | 12119 | 4434 | 2192 | 21 | 4510 | 5 |
| 458 | 44368 | 17864 | 2460 | 1986 | 0 | 0 | 0 |
| 333 | 44088 | 15988 | 2874 | 1945 | 0 | 0 | 0 |
| 628 | 43357 | 14526 | 588 | 282 | 48 | 4065 | 15 |
| 677 | 43179 | 15867 | 431 | 1455 | 4 | 5 | 0 |
| 412 | 43064 | 16956 | 2446 | 1389 | 1 | 0 | 0 |
| 652 | 42350 | 14117 | 426 | 183 | 0 | 6336 | 20 |
| 630 | 42232 | 14009 | 470 | 157 | 3 | 4903 | 15 |
| 106 | 41903 | 11483 | 1102 | 630 | 2 | 10209 | 40 |
| 444 | 41682 | 12765 | 3352 | 160 | 1 | 0 | 0 |
| 442 | 41340 | 15663 | 1917 | 1043 | 0 | 0 | 0 |
| 185 | 41019 | 11340 | 1610 | 692 | 0 | 9360 | 26 |
| 408 | 40905 | 14095 | 4337 | 576 | 4 | 0 | 0 |
| 379 | 40589 | 15972 | 2434 | 1012 | 0 | 0 | 0 |
| 426 | 40269 | 14039 | 1277 | 491 | 0 | 3168 | 11 |
| 299 | 40086 | 14942 | 2192 | 2997 | 5 | 3168 | 10 |
| 97 | 40014 | 9293 | 1205 | 967 | 9 | 12861 | 50 |
| 385 | 39940 | 17523 | 2417 | 1691 | 0 | 0 | 0 |
| 411 | 39657 | 14498 | 3558 | 3112 | 0 | 0 | 16 |
| 714 | 39639 | 13669 | 314 | 376 | 0 | 800 | 1 |
| 22 | 39596 | 9615 | 454 | 10226 | 2 | 117 | 3629 |
| 683 | 39278 | 15211 | 760 | 407 | 0 | 0 | 0 |
| 451 | 39136 | 18825 | 2145 | 2453 | 0 | 0 | 0 |
| 452 | 38976 | 18935 | 2023 | 2640 | 0 | 0 | 0 |
| 332 | 38754 | 14745 | 2283 | 2020 | 0 | 0 | 0 |
| 449 | 38716 | 17369 | 3401 | 3026 | 0 | 0 | 0 |
| 661 | 38529 | 11935 | 489 | 253 | 0 | 6336 | 15 |

## Reproducibility

### ripgrep
Trace root:
| metric | value |
| --- | --- |
| trace_root_command | "C:\tools\avbench\avbench.exe" run --name baseline-os --bench-dir C:\bench --output C:\results -w ripgrep |
| trace_root_working_dir | C:\Users\User\ |

Trace child commands:
| process | command_line | working_dir |
| --- | --- | --- |
| cargo.exe | "cargo" build --release | C:\bench\ripgrep\ |
| cargo.exe | "cargo" build --release | C:\bench\ripgrep\ |

Baseline run metadata:
| scenario | command | working_dir | wall_ms | disk_read_mb | disk_write_mb | runner_version |
| --- | --- | --- | --- | --- | --- | --- |
| ripgrep-clean-build | cargo build --release | C:\bench\ripgrep | 15061 | 195.9 | 74.3 | 0.1.0 |
| ripgrep-incremental-build | cargo build --release | C:\bench\ripgrep | 5947 | 0.0 | 18.0 | 0.1.0 |

Toolchain hints:
| hint |
| --- |
| Rust toolchain: 1.85.0-x86_64-pc-windows-msvc |
| Visual Studio toolchain root: 18 |
| Visual Studio toolchain root: 2022 |

Inferred controls:
| control | value |
| --- | --- |
| network | observed |
| NuGet/cache | not prominent in trace |
| dotnet telemetry storage | not prominent in trace |
| Cargo network | not disabled by command; trace network activity is minimal |

### Roslyn
Trace root:
| metric | value |
| --- | --- |
| trace_root_command | "C:\tools\avbench\avbench.exe" run --name baseline-os --bench-dir C:\bench --output C:\results -w roslyn |
| trace_root_working_dir | C:\Users\User\ |

Trace child commands:
| process | command_line | working_dir |
| --- | --- | --- |
| dotnet.exe | "dotnet" build "C:\bench\roslyn\Roslyn.slnx" -c Release /m /nr:false /p:RepositoryUrl=https://github.com/dotnet/roslyn /p:RepositoryCommit=358fc4c237c3ec174e66c5d6e430ba176bf793cf /p:RepositoryBranch=main | C:\bench\roslyn\ |
| dotnet.exe | "dotnet" build "C:\bench\roslyn\Roslyn.slnx" -c Release /m /nr:false /p:RepositoryUrl=https://github.com/dotnet/roslyn /p:RepositoryCommit=358fc4c237c3ec174e66c5d6e430ba176bf793cf /p:RepositoryBranch=main | C:\bench\roslyn\ |

Baseline run metadata:
| scenario | command | working_dir | wall_ms | disk_read_mb | disk_write_mb | runner_version |
| --- | --- | --- | --- | --- | --- | --- |
| roslyn-clean-build | dotnet build "C:\bench\roslyn\Roslyn.slnx" -c Release /m /nr:false /p:RepositoryUrl=https://github.com/dotnet/roslyn /p:RepositoryCommit=358fc4c237c3ec174e66c5d6e430ba176bf793cf /p:RepositoryBranch=main | C:\bench\roslyn | 214489 | 12317.7 | 30630.5 | 0.1.0 |
| roslyn-incremental-build | dotnet build "C:\bench\roslyn\Roslyn.slnx" -c Release /m /nr:false /p:RepositoryUrl=https://github.com/dotnet/roslyn /p:RepositoryCommit=358fc4c237c3ec174e66c5d6e430ba176bf793cf /p:RepositoryBranch=main | C:\bench\roslyn | 62030 | 130.7 | 1169.1 | 0.1.0 |

Toolchain hints:
| hint |
| --- |
| .NET SDK: 10.0.105 |
| Visual Studio toolchain root: Shared |

Inferred controls:
| control | value |
| --- | --- |
| network | observed |
| NuGet/cache | touched |
| dotnet telemetry storage | touched |
| MSBuild node reuse | disabled by /nr:false |
| implicit restore | possible; command does not include --no-restore |
| Roslyn compiler server | observed via VBCSCompiler.exe |

## Benchmark Correlation

Correlation uses per-product slowdown from data/exp1/compare.csv. ProcMon profiles are baseline workload profiles, so this correlates observed AV slowdown patterns with workload structure rather than per-product ProcMon traces.

### Scenario Summary

#### ripgrep
| scenario | products | avg_slowdown_pct | median_slowdown_pct | avg_first_run_slowdown_pct | median_first_run_slowdown_pct | avg_p95_slowdown_pct |
| --- | --- | --- | --- | --- | --- | --- |
| ripgrep-clean-build | 14 | 19.857 | 19.35 | 21.3 | 21.75 | 0.0 |
| ripgrep-incremental-build | 15 | 5.58 | 4.2 | 5.513 | 3.7 | 0.0 |

#### Roslyn
| scenario | products | avg_slowdown_pct | median_slowdown_pct | avg_first_run_slowdown_pct | median_first_run_slowdown_pct | avg_p95_slowdown_pct |
| --- | --- | --- | --- | --- | --- | --- |
| roslyn-clean-build | 16 | 21.925 | 15.0 | 32.356 | 15.95 | 0.0 |
| roslyn-incremental-build | 16 | 15.312 | 9.75 | 25.581 | 14.95 | 0.0 |

### Per-Product Correlations

| label | left | right | field | products | pearson_r |
| --- | --- | --- | --- | --- | --- |
| clean average slowdown | ripgrep-clean-build | roslyn-clean-build | slowdown_pct | 14 | 0.022 |
| clean average slowdown, first-cloud-seen | ripgrep-clean-build | roslyn-clean-build | first_run_slowdown_pct | 14 | 0.037 |
| incremental average slowdown | ripgrep-incremental-build | roslyn-incremental-build | slowdown_pct | 15 | 0.551 |
| incremental average slowdown, first-cloud-seen | ripgrep-incremental-build | roslyn-incremental-build | first_run_slowdown_pct | 15 | 0.08 |
| ripgrep clean vs incremental | ripgrep-clean-build | ripgrep-incremental-build | slowdown_pct | 13 | 0.2 |
| ripgrep clean vs incremental, first-cloud-seen | ripgrep-clean-build | ripgrep-incremental-build | first_run_slowdown_pct | 13 | 0.187 |
| roslyn clean vs incremental | roslyn-clean-build | roslyn-incremental-build | slowdown_pct | 16 | 0.959 |
| roslyn clean vs incremental, first-cloud-seen | roslyn-clean-build | roslyn-incremental-build | first_run_slowdown_pct | 16 | 0.86 |

### Interpretation

Roslyn's baseline profile has far more metadata queries, unique paths, DLL/image-load activity, and failed probes than ripgrep. If a product is disproportionately slow on Roslyn in `compare.csv`, the likely explanation is per-open/per-query filtering, reference/package/analyzer DLL handling, compiler-server input scanning, or cloud/reputation checks across many unique paths.

Ripgrep's profile has fewer paths but a higher concentration of compiler/linker artifact writes. If a product is disproportionately slow on ripgrep, the likely explanation is scanning of newly emitted native artifacts, PDBs, `.lib`/`.rmeta`/`.rlib` files, linker temp files, or executable-generation behavior rules.
