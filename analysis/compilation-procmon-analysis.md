# ProcMon analysis: ripgrep vs Roslyn compilation

Built from the two Process Monitor captures, `tmp/ripgrep.CSV` and `tmp/roslyn.CSV`.

The main view follows the workload itself: a dynamic process tree rooted at each workload's `avbench.exe` process and all of its descendants. The broader trace view is kept at the end only as ambient-system context, because desktop noise can otherwise look more meaningful than it is.

## High-level event volume

| trace | events | build_events | core_events | core_processes | core_unique_file_paths | core_unique_registry_paths |
| --- | --- | --- | --- | --- | --- | --- |
| ripgrep | 463658 | 267501 | 201969 | 79 | 3396 | 6050 |
| roslyn | 15194989 | 13428524 | 12624681 | 124 | 153774 | 5477 |

## Core process tree

### ripgrep
| pid | name | parent_pid | first_seen | source | current_directory |
| --- | --- | --- | --- | --- | --- |
| 4960 | avbench.exe | 2120 | 6:15:35.3828192 PM | benchmark root | C:\Users\User\ |
| 9320 | cargo.exe | 4960 | 6:15:39.4896443 PM | child of avbench.exe (4960) | C:\bench\ripgrep\ |
| 4068 | Conhost.exe | 9320 | 6:15:39.5116311 PM | child of cargo.exe (9320) | C:\Windows |
| 9088 | cargo.exe | 9320 | 6:15:39.5685382 PM | child of cargo.exe (9320) | C:\bench\ripgrep\ |
| 5996 | rustc.exe | 9088 | 6:15:39.6401956 PM | child of cargo.exe (9088) | C:\bench\ripgrep\ |
| 4512 | rustc.exe | 9088 | 6:15:39.6864007 PM | child of cargo.exe (9088) | C:\bench\ripgrep\ |
| 3064 | rustc.exe | 9088 | 6:15:39.7974666 PM | child of cargo.exe (9088) | C:\bench\ripgrep\ |
| 6708 | rustc.exe | 9088 | 6:15:39.9145826 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\log-0.4.28\ |
| 5232 | rustc.exe | 9088 | 6:15:39.9171370 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\serde_core-1.0.228\ |
| 6204 | rustc.exe | 9088 | 6:15:39.9203707 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\regex-syntax-0.8.8\ |
| 2068 | rustc.exe | 9088 | 6:15:39.9226735 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\windows-link-0.2.1\ |
| 4488 | rustc.exe | 9088 | 6:15:39.9250532 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\memchr-2.7.6\ |
| 5784 | rustc.exe | 9088 | 6:15:39.9319328 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\serde-1.0.228\ |
| 4612 | rustc.exe | 9088 | 6:15:39.9342128 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\cfg-if-1.0.4\ |
| 7064 | rustc.exe | 9088 | 6:15:39.9363909 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\crossbeam-utils-0.8.21\ |
| 7856 | rustc.exe | 9088 | 6:15:39.9392596 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\serde_json-1.0.145\ |
| 9728 | rustc.exe | 9088 | 6:15:39.9419785 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\ryu-1.0.20\ |
| 8504 | rustc.exe | 9088 | 6:15:39.9442872 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\memmap2-0.9.9\ |
| 9616 | rustc.exe | 9088 | 6:15:39.9466545 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\itoa-1.0.15\ |
| 2396 | rustc.exe | 9088 | 6:15:39.9498553 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\anyhow-1.0.100\ |
| 1868 | rustc.exe | 9088 | 6:15:39.9520532 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\lexopt-0.3.1\ |
| 3732 | rustc.exe | 9088 | 6:15:39.9557007 PM | child of cargo.exe (9088) | C:\bench\ripgrep\ |
| 2040 | rustc.exe | 9088 | 6:15:39.9610142 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\textwrap-0.16.2\ |
| 4228 | rustc.exe | 9088 | 6:15:40.3666816 PM | child of cargo.exe (9088) |  |
| 4040 | rustc.exe | 9088 | 6:15:40.3713412 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\encoding_rs-0.8.35\ |
| 4140 | link.exe | 7856 | 6:15:40.7543805 PM | child of rustc.exe (7856) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\serde_json-1.0.145\ |
| 9396 | link.exe | 2396 | 6:15:40.7684064 PM | child of rustc.exe (2396) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\anyhow-1.0.100\ |
| 9328 | link.exe | 5784 | 6:15:40.7974733 PM | child of rustc.exe (5784) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\serde-1.0.228\ |
| 1968 | link.exe | 5232 | 6:15:40.7978503 PM | child of rustc.exe (5232) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\serde_core-1.0.228\ |
| 5976 | link.exe | 3732 | 6:15:40.7980824 PM | child of rustc.exe (3732) | C:\bench\ripgrep\ |
| 8024 | link.exe | 7064 | 6:15:40.7982336 PM | child of rustc.exe (7064) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\crossbeam-utils-0.8.21\ |
| 10336 | rustc.exe | 9088 | 6:15:41.2611778 PM | child of cargo.exe (9088) | C:\bench\ripgrep\ |
| 10344 | rustc.exe | 9088 | 6:15:41.2647503 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\aho-corasick-1.1.3\ |
| 10796 | VCTIP.EXE | 9396 | 6:15:41.6934065 PM | child of link.exe (9396) | C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools\VC\Tools\MSVC\14.50.35717\bin\HostX64\x64\ |
| 10848 | rustc.exe | 9088 | 6:15:41.9384767 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\winapi-util-0.1.11\ |
| 10900 | rustc.exe | 9088 | 6:15:42.0777239 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\termcolor-1.4.1\ |
| 10908 | rustc.exe | 9088 | 6:15:42.0810080 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\same-file-1.0.6\ |
| 10976 | rustc.exe | 9088 | 6:15:42.1902156 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\walkdir-2.5.0\ |
| 11020 | rustc.exe | 9088 | 6:15:42.2342439 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\encoding_rs_io-0.1.7\ |
| 11248 | build-script-build.exe | 9088 | 6:15:42.5660600 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\anyhow-1.0.100\ |
| 5568 | build-script-build.exe | 9088 | 6:15:42.5914627 PM | child of cargo.exe (9088) | C:\bench\ripgrep\ |
| 3296 | build-script-build.exe | 9088 | 6:15:42.6144539 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\serde_json-1.0.145\ |
| 9488 | rustc.exe | 11248 | 6:15:42.6200073 PM | child of build-script-build.exe (11248) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\anyhow-1.0.100\ |
| 10116 | build-script-build.exe | 9088 | 6:15:42.6378695 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\serde-1.0.228\ |
| 10120 | build-script-build.exe | 9088 | 6:15:42.6565317 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\serde_core-1.0.228\ |
| 5312 | rustc.exe | 10116 | 6:15:42.6851785 PM | child of build-script-build.exe (10116) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\serde-1.0.228\ |
| 10296 | rustc.exe | 10120 | 6:15:42.6934558 PM | child of build-script-build.exe (10120) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\serde_core-1.0.228\ |
| 6564 | rustc.exe | 9088 | 6:15:42.7297883 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\crossbeam-utils-0.8.21\ |
| 6272 | rustc.exe | 9088 | 6:15:42.7614345 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\serde_core-1.0.228\ |
| 9824 | rustc.exe | 11248 | 6:15:42.8186688 PM | child of build-script-build.exe (11248) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\anyhow-1.0.100\ |
| 4144 | rustc.exe | 9088 | 6:15:42.8930083 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\anyhow-1.0.100\ |
| 9696 | rustc.exe | 9088 | 6:15:43.4945298 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\regex-automata-0.4.13\ |
| 10480 | rustc.exe | 9088 | 6:15:43.7829418 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\crossbeam-epoch-0.9.18\ |
| 10564 | rustc.exe | 9088 | 6:15:44.3523108 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\crossbeam-deque-0.8.6\ |
| 10332 | rustc.exe | 9088 | 6:15:46.5927051 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\serde-1.0.228\ |
| 8536 | rustc.exe | 9088 | 6:15:46.5962532 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\serde_json-1.0.145\ |
| 11080 | rustc.exe | 9088 | 6:15:47.9019963 PM | child of cargo.exe (9088) | C:\Users\User\.cargo\registry\src\index.crates.io-1949cf8c6b5b557f\bstr-1.12.0\ |
| 10752 | rustc.exe | 9088 | 6:15:48.4742871 PM | child of cargo.exe (9088) | C:\bench\ripgrep\ |
| 10740 | rustc.exe | 9088 | 6:15:48.4783129 PM | child of cargo.exe (9088) | C:\bench\ripgrep\ |
| 10728 | rustc.exe | 9088 | 6:15:48.4823000 PM | child of cargo.exe (9088) | C:\bench\ripgrep\ |
| 2444 | rustc.exe | 9088 | 6:15:49.1205618 PM | child of cargo.exe (9088) | C:\bench\ripgrep\ |
| 11148 | rustc.exe | 9088 | 6:15:49.1780178 PM | child of cargo.exe (9088) | C:\bench\ripgrep\ |
| 11152 | rustc.exe | 9088 | 6:15:49.1828176 PM | child of cargo.exe (9088) | C:\bench\ripgrep\ |
| 7656 | rustc.exe | 9088 | 6:15:51.4556023 PM | child of cargo.exe (9088) | C:\bench\ripgrep\ |
| 8860 | rustc.exe | 9088 | 6:15:53.4006561 PM | child of cargo.exe (9088) | C:\bench\ripgrep\ |
| 10648 | link.exe | 8860 | 6:15:58.7120944 PM | child of rustc.exe (8860) | C:\bench\ripgrep\ |
| 10580 | mt.exe | 10648 | 6:15:58.8674648 PM | child of link.exe (10648) | C:\bench\ripgrep\ |
| 10560 | rc.exe | 10648 | 6:15:58.9118544 PM | child of link.exe (10648) | C:\bench\ripgrep\ |
| 10452 | cvtres.exe | 10648 | 6:15:58.9419526 PM | child of link.exe (10648) | C:\bench\ripgrep\ |
| 10448 | cargo.exe | 4960 | 6:16:19.2308227 PM | child of avbench.exe (4960) | C:\bench\ripgrep\ |
| 10644 | Conhost.exe | 10448 | 6:16:19.2372850 PM | child of cargo.exe (10448) | C:\Windows |
| 10368 | cargo.exe | 10448 | 6:16:19.2701435 PM | child of cargo.exe (10448) | C:\bench\ripgrep\ |
| 10328 | rustc.exe | 10368 | 6:16:19.4148626 PM | child of cargo.exe (10368) | C:\bench\ripgrep\ |
| 10948 | rustc.exe | 10368 | 6:16:20.0941917 PM | child of cargo.exe (10368) | C:\bench\ripgrep\ |
| 10828 | rustc.exe | 10368 | 6:16:20.4088618 PM | child of cargo.exe (10368) | C:\bench\ripgrep\ |
| 7124 | mt.exe | 9328 | 6:16:25.5211657 PM | child of link.exe (9328) | C:\bench\ripgrep\ |
| 11240 | rc.exe | 9328 | 6:16:25.5416355 PM | child of link.exe (9328) | C:\bench\ripgrep\ |
| 7104 | cvtres.exe | 9328 | 6:16:25.5562312 PM | child of link.exe (9328) | C:\bench\ripgrep\ |
| 11216 | VCTIP.EXE | 9328 | 6:16:25.7283188 PM | child of link.exe (9328) | C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools\VC\Tools\MSVC\14.50.35717\bin\HostX64\x64\ |

### Roslyn
| pid | name | parent_pid | first_seen | source | current_directory |
| --- | --- | --- | --- | --- | --- |
| 2836 | avbench.exe | 2120 | 5:53:51.7768333 PM | benchmark root | C:\Users\User\ |
| 6496 | dotnet.exe | 2836 | 5:53:56.7258957 PM | child of avbench.exe (2836) | C:\bench\roslyn\ |
| 9536 | Conhost.exe | 6496 | 5:53:56.7779976 PM | child of dotnet.exe (6496) | C:\Windows |
| 2256 | getmac.exe | 6496 | 5:53:56.9835971 PM | child of dotnet.exe (6496) | C:\bench\roslyn\ |
| 9556 | getmac.exe | 6496 | 5:53:57.0528889 PM | child of dotnet.exe (6496) | C:\bench\roslyn\ |
| 932 | dotnet.exe | 6496 | 5:53:57.3098423 PM | child of dotnet.exe (6496) | C:\bench\roslyn\ |
| 4952 | Conhost.exe | 932 | 5:53:57.3574641 PM | child of dotnet.exe (932) | C:\Windows |
| 11332 | dotnet.exe | 6496 | 5:54:16.2967268 PM | child of dotnet.exe (6496) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 11340 | dotnet.exe | 6496 | 5:54:16.2978437 PM | child of dotnet.exe (6496) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 11348 | dotnet.exe | 6496 | 5:54:16.3013415 PM | child of dotnet.exe (6496) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 11388 | dotnet.exe | 6496 | 5:54:16.3028097 PM | child of dotnet.exe (6496) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 11364 | dotnet.exe | 6496 | 5:54:16.3047345 PM | child of dotnet.exe (6496) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 11372 | dotnet.exe | 6496 | 5:54:16.3076816 PM | child of dotnet.exe (6496) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 11380 | dotnet.exe | 6496 | 5:54:16.3103415 PM | child of dotnet.exe (6496) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 11356 | dotnet.exe | 6496 | 5:54:16.3134002 PM | child of dotnet.exe (6496) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 11396 | dotnet.exe | 6496 | 5:54:16.3165988 PM | child of dotnet.exe (6496) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 11404 | dotnet.exe | 6496 | 5:54:16.3201873 PM | child of dotnet.exe (6496) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 11420 | dotnet.exe | 6496 | 5:54:16.3235654 PM | child of dotnet.exe (6496) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 11412 | dotnet.exe | 6496 | 5:54:16.3271449 PM | child of dotnet.exe (6496) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 11428 | dotnet.exe | 6496 | 5:54:16.3312108 PM | child of dotnet.exe (6496) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 11436 | dotnet.exe | 6496 | 5:54:16.3353154 PM | child of dotnet.exe (6496) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 11444 | dotnet.exe | 6496 | 5:54:16.3392533 PM | child of dotnet.exe (6496) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 11452 | dotnet.exe | 6496 | 5:54:16.3438524 PM | child of dotnet.exe (6496) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 11460 | dotnet.exe | 6496 | 5:54:16.3501498 PM | child of dotnet.exe (6496) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 11468 | dotnet.exe | 6496 | 5:54:16.3566828 PM | child of dotnet.exe (6496) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 11476 | dotnet.exe | 6496 | 5:54:16.3627097 PM | child of dotnet.exe (6496) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 11484 | dotnet.exe | 6496 | 5:54:16.3681748 PM | child of dotnet.exe (6496) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 11492 | dotnet.exe | 6496 | 5:54:16.3737027 PM | child of dotnet.exe (6496) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 11500 | dotnet.exe | 6496 | 5:54:16.3794971 PM | child of dotnet.exe (6496) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 11508 | dotnet.exe | 6496 | 5:54:16.3851262 PM | child of dotnet.exe (6496) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 11516 | Conhost.exe | 11332 | 5:54:16.3911635 PM | child of dotnet.exe (11332) | C:\Windows |
| 11524 | Conhost.exe | 11340 | 5:54:16.3972168 PM | child of dotnet.exe (11340) | C:\Windows |
| 11536 | Conhost.exe | 11348 | 5:54:16.4029034 PM | child of dotnet.exe (11348) | C:\Windows |
| 11544 | Conhost.exe | 11388 | 5:54:16.4089462 PM | child of dotnet.exe (11388) | C:\Windows |
| 11552 | Conhost.exe | 11364 | 5:54:16.4147143 PM | child of dotnet.exe (11364) | C:\Windows |
| 11560 | Conhost.exe | 11372 | 5:54:16.4201311 PM | child of dotnet.exe (11372) | C:\Windows |
| 11568 | Conhost.exe | 11380 | 5:54:16.4255840 PM | child of dotnet.exe (11380) | C:\Windows |
| 11576 | Conhost.exe | 11356 | 5:54:16.4311171 PM | child of dotnet.exe (11356) | C:\Windows |
| 11584 | Conhost.exe | 11396 | 5:54:16.4364407 PM | child of dotnet.exe (11396) | C:\Windows |
| 11592 | Conhost.exe | 11404 | 5:54:16.4415776 PM | child of dotnet.exe (11404) | C:\Windows |
| 11600 | Conhost.exe | 11420 | 5:54:16.4469957 PM | child of dotnet.exe (11420) | C:\Windows |
| 11608 | Conhost.exe | 11412 | 5:54:16.4508134 PM | child of dotnet.exe (11412) | C:\Windows |
| 11616 | Conhost.exe | 11428 | 5:54:16.4508608 PM | child of dotnet.exe (11428) | C:\Windows |
| 11624 | Conhost.exe | 11436 | 5:54:16.4508734 PM | child of dotnet.exe (11436) | C:\Windows |
| 11632 | Conhost.exe | 11444 | 5:54:16.4509322 PM | child of dotnet.exe (11444) | C:\Windows |
| 11640 | Conhost.exe | 11452 | 5:54:16.4509468 PM | child of dotnet.exe (11452) | C:\Windows |
| 11648 | Conhost.exe | 11460 | 5:54:16.4509592 PM | child of dotnet.exe (11460) | C:\Windows |
| 11664 | Conhost.exe | 11468 | 5:54:16.4509957 PM | child of dotnet.exe (11468) | C:\Windows |
| 11680 | Conhost.exe | 11476 | 5:54:16.4510101 PM | child of dotnet.exe (11476) | C:\Windows |
| 11696 | Conhost.exe | 11484 | 5:54:16.4510230 PM | child of dotnet.exe (11484) | C:\Windows |
| 11704 | Conhost.exe | 11492 | 5:54:16.4851507 PM | child of dotnet.exe (11492) | C:\Windows |
| 11728 | Conhost.exe | 11500 | 5:54:16.4882217 PM | child of dotnet.exe (11500) | C:\Windows |
| 11736 | Conhost.exe | 11508 | 5:54:16.4917695 PM | child of dotnet.exe (11508) | C:\Windows |
| 3856 | VBCSCompiler.exe | 11444 | 5:54:47.9607001 PM | child of dotnet.exe (11444) | C:\Program Files\dotnet\sdk\10.0.105\Roslyn\bincore\ |
| 13372 | Conhost.exe | 3856 | 5:54:48.2389132 PM | child of VBCSCompiler.exe (3856) | C:\Windows |
| 4156 | cmd.exe | 11428 | 5:55:58.2101574 PM | child of dotnet.exe (11428) | C:\bench\roslyn\src\ExpressionEvaluator\Core\Source\FunctionResolver\ |
| 4024 | Conhost.exe | 4156 | 5:55:58.5018700 PM | child of cmd.exe (4156) | C:\Windows |
| 6660 | VsdConfigTool.exe | 4156 | 5:55:59.8284618 PM | child of cmd.exe (4156) | C:\bench\roslyn\src\ExpressionEvaluator\Core\Source\FunctionResolver\ |
| 5324 | cmd.exe | 11444 | 5:57:45.7079638 PM | child of dotnet.exe (11444) | C:\bench\roslyn\src\ExpressionEvaluator\VisualBasic\Source\ExpressionCompiler\ |
| 6596 | Conhost.exe | 5324 | 5:57:45.7699619 PM | child of cmd.exe (5324) | C:\Windows |
| 13024 | VsdConfigTool.exe | 5324 | 5:57:46.3365014 PM | child of cmd.exe (5324) | C:\bench\roslyn\src\ExpressionEvaluator\VisualBasic\Source\ExpressionCompiler\ |
| 13036 | cmd.exe | 11484 | 5:57:51.9290180 PM | child of dotnet.exe (11484) | C:\bench\roslyn\src\ExpressionEvaluator\CSharp\Source\ExpressionCompiler\ |
| 8852 | Conhost.exe | 13036 | 5:57:51.9635669 PM | child of cmd.exe (13036) | C:\Windows |
| 10784 | VsdConfigTool.exe | 13036 | 5:57:52.5095394 PM | child of cmd.exe (13036) | C:\bench\roslyn\src\ExpressionEvaluator\CSharp\Source\ExpressionCompiler\ |
| 4308 | cmd.exe | 11372 | 5:58:04.5963045 PM | child of dotnet.exe (11372) | C:\bench\roslyn\src\ExpressionEvaluator\VisualBasic\Source\ResultProvider\Portable\ |
| 14144 | Conhost.exe | 4308 | 5:58:04.6936535 PM | child of cmd.exe (4308) | C:\Windows |
| 13144 | VsdConfigTool.exe | 4308 | 5:58:04.8283829 PM | child of cmd.exe (4308) | C:\bench\roslyn\src\ExpressionEvaluator\VisualBasic\Source\ResultProvider\Portable\ |
| 13968 | cmd.exe | 11468 | 5:58:08.3839941 PM | child of dotnet.exe (11468) | C:\bench\roslyn\src\ExpressionEvaluator\CSharp\Source\ResultProvider\Portable\ |
| 14020 | Conhost.exe | 13968 | 5:58:08.3908333 PM | child of cmd.exe (13968) | C:\Windows |
| 4328 | VsdConfigTool.exe | 13968 | 5:58:08.4677440 PM | child of cmd.exe (13968) | C:\bench\roslyn\src\ExpressionEvaluator\CSharp\Source\ResultProvider\Portable\ |
| 2772 | dotnet.exe | 11356 | 5:58:57.2431231 PM | child of dotnet.exe (11356) | C:\bench\roslyn\src\Tools\SemanticSearch\ReferenceAssemblies\ |
| 1488 | Conhost.exe | 2772 | 5:58:57.5827204 PM | child of dotnet.exe (2772) | C:\Windows |
| 12220 | dotnet.exe | 2836 | 6:02:02.8168564 PM | child of avbench.exe (2836) | C:\bench\roslyn\ |
| 2232 | Conhost.exe | 12220 | 6:02:02.8206433 PM | child of dotnet.exe (12220) | C:\Windows |
| 5136 | dotnet.exe | 12220 | 6:02:03.3535848 PM | child of dotnet.exe (12220) | C:\bench\roslyn\ |
| 2428 | Conhost.exe | 5136 | 6:02:03.3651519 PM | child of dotnet.exe (5136) | C:\Windows |
| 2916 | dotnet.exe | 12220 | 6:02:11.0401915 PM | child of dotnet.exe (12220) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 7464 | dotnet.exe | 12220 | 6:02:11.0404195 PM | child of dotnet.exe (12220) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 14096 | dotnet.exe | 12220 | 6:02:11.0405431 PM | child of dotnet.exe (12220) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 12848 | dotnet.exe | 12220 | 6:02:11.0418924 PM | child of dotnet.exe (12220) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 10760 | dotnet.exe | 12220 | 6:02:11.0460905 PM | child of dotnet.exe (12220) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 6184 | dotnet.exe | 12220 | 6:02:11.0486886 PM | child of dotnet.exe (12220) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 12004 | dotnet.exe | 12220 | 6:02:11.0518958 PM | child of dotnet.exe (12220) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 7288 | dotnet.exe | 12220 | 6:02:11.0546335 PM | child of dotnet.exe (12220) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 3636 | dotnet.exe | 12220 | 6:02:11.0577529 PM | child of dotnet.exe (12220) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 6624 | dotnet.exe | 12220 | 6:02:11.0608036 PM | child of dotnet.exe (12220) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 13336 | dotnet.exe | 12220 | 6:02:11.0644029 PM | child of dotnet.exe (12220) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 13352 | dotnet.exe | 12220 | 6:02:11.0682171 PM | child of dotnet.exe (12220) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 12492 | dotnet.exe | 12220 | 6:02:11.0725844 PM | child of dotnet.exe (12220) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 5976 | dotnet.exe | 12220 | 6:02:11.0772159 PM | child of dotnet.exe (12220) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 5332 | dotnet.exe | 12220 | 6:02:11.0820024 PM | child of dotnet.exe (12220) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 12276 | dotnet.exe | 12220 | 6:02:11.0870882 PM | child of dotnet.exe (12220) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 12236 | dotnet.exe | 12220 | 6:02:11.0921290 PM | child of dotnet.exe (12220) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 10608 | dotnet.exe | 12220 | 6:02:11.0972613 PM | child of dotnet.exe (12220) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 13780 | dotnet.exe | 12220 | 6:02:11.1021207 PM | child of dotnet.exe (12220) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 5880 | dotnet.exe | 12220 | 6:02:11.1067474 PM | child of dotnet.exe (12220) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 13400 | dotnet.exe | 12220 | 6:02:11.1116868 PM | child of dotnet.exe (12220) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 14248 | dotnet.exe | 12220 | 6:02:11.1168940 PM | child of dotnet.exe (12220) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 11784 | dotnet.exe | 12220 | 6:02:11.1217079 PM | child of dotnet.exe (12220) | C:\bench\roslyn\src\CodeStyle\CSharp\CodeFixes\ |
| 8352 | Conhost.exe | 2916 | 6:02:11.1270441 PM | child of dotnet.exe (2916) | C:\Windows |
| 12776 | Conhost.exe | 7464 | 6:02:11.1324353 PM | child of dotnet.exe (7464) | C:\Windows |
| 9852 | Conhost.exe | 14096 | 6:02:11.1375787 PM | child of dotnet.exe (14096) | C:\Windows |
| 11848 | Conhost.exe | 12848 | 6:02:11.1422830 PM | child of dotnet.exe (12848) | C:\Windows |
| 13012 | Conhost.exe | 10760 | 6:02:11.1471850 PM | child of dotnet.exe (10760) | C:\Windows |
| 13108 | Conhost.exe | 6184 | 6:02:11.1518654 PM | child of dotnet.exe (6184) | C:\Windows |
| 4152 | Conhost.exe | 12004 | 6:02:11.1565351 PM | child of dotnet.exe (12004) | C:\Windows |
| 11104 | Conhost.exe | 7288 | 6:02:11.1611522 PM | child of dotnet.exe (7288) | C:\Windows |
| 9192 | Conhost.exe | 3636 | 6:02:11.1657157 PM | child of dotnet.exe (3636) | C:\Windows |
| 10520 | Conhost.exe | 6624 | 6:02:11.1703323 PM | child of dotnet.exe (6624) | C:\Windows |
| 10412 | Conhost.exe | 13336 | 6:02:11.1749048 PM | child of dotnet.exe (13336) | C:\Windows |
| 12792 | Conhost.exe | 13352 | 6:02:11.1795609 PM | child of dotnet.exe (13352) | C:\Windows |
| 2812 | Conhost.exe | 12492 | 6:02:11.1837758 PM | child of dotnet.exe (12492) | C:\Windows |
| 5104 | Conhost.exe | 5976 | 6:02:11.1877911 PM | child of dotnet.exe (5976) | C:\Windows |
| 6064 | Conhost.exe | 5332 | 6:02:11.1921506 PM | child of dotnet.exe (5332) | C:\Windows |
| 12868 | Conhost.exe | 12276 | 6:02:11.1963074 PM | child of dotnet.exe (12276) | C:\Windows |
| 10268 | Conhost.exe | 12236 | 6:02:11.1998945 PM | child of dotnet.exe (12236) | C:\Windows |
| 13032 | Conhost.exe | 10608 | 6:02:11.2031120 PM | child of dotnet.exe (10608) | C:\Windows |
| 10832 | Conhost.exe | 13780 | 6:02:11.2067478 PM | child of dotnet.exe (13780) | C:\Windows |
| 11224 | Conhost.exe | 5880 | 6:02:11.2102641 PM | child of dotnet.exe (5880) | C:\Windows |
| 5456 | Conhost.exe | 13400 | 6:02:11.2136598 PM | child of dotnet.exe (13400) | C:\Windows |
| 8312 | Conhost.exe | 14248 | 6:02:11.2166400 PM | child of dotnet.exe (14248) | C:\Windows |
| 12232 | Conhost.exe | 11784 | 6:02:11.2200479 PM | child of dotnet.exe (11784) | C:\Windows |
| 13396 | Conhost.exe | 11648 | 6:03:09.4172708 PM | child of VBCSCompiler.exe (11648) | C:\Windows |
| 9172 | Conhost.exe | 11508 | 6:04:18.9260326 PM | child of dotnet.exe (11508) | C:\Windows |

## Core operation families

### ripgrep
| name | count | pct |
| --- | --- | --- |
| file | 136997 | 67.831 |
| registry | 51132 | 25.317 |
| other | 10430 | 5.164 |
| process/thread | 3095 | 1.532 |
| profiling | 270 | 0.134 |
| network | 45 | 0.022 |

### Roslyn
| name | count | pct |
| --- | --- | --- |
| file | 9829880 | 77.862 |
| other | 1388455 | 10.998 |
| registry | 1342861 | 10.637 |
| profiling | 34252 | 0.271 |
| network | 14630 | 0.116 |
| process/thread | 14603 | 0.116 |

## Top core processes

### ripgrep
| name | count | pct | top_family | top_operation |
| --- | --- | --- | --- | --- |
| rustc.exe | 93193 | 46.142 | file | WriteFile |
| link.exe | 32788 | 16.234 | file | WriteFile |
| VCTIP.EXE | 25858 | 12.803 | registry | RegOpenKey |
| avbench.exe | 23617 | 11.693 | registry | RegOpenKey |
| cargo.exe | 21983 | 10.884 | file | CreateFile |
| build-script-build.exe | 1755 | 0.869 | registry | RegOpenKey |
| Conhost.exe | 909 | 0.45 | registry | RegQueryValue |
| cvtres.exe | 686 | 0.34 | file | RegOpenKey |
| mt.exe | 635 | 0.314 | registry | RegOpenKey |
| rc.exe | 545 | 0.27 | file | RegOpenKey |

### Roslyn
| name | count | pct | top_family | top_operation |
| --- | --- | --- | --- | --- |
| dotnet.exe | 10053215 | 79.631 | file | CreateFile |
| VBCSCompiler.exe | 2436411 | 19.299 | file | ReadFile |
| avbench.exe | 51301 | 0.406 | file | CreateFile |
| Conhost.exe | 40145 | 0.318 | profiling | Process Profiling |
| VsdConfigTool.exe | 37482 | 0.297 | registry | RegQueryKey |
| getmac.exe | 3168 | 0.025 | registry | RegOpenKey |
| cmd.exe | 2959 | 0.023 | file | RegOpenKey |

## Top core operations

### ripgrep
| name | count | pct |
| --- | --- | --- |
| WriteFile | 33434 | 16.554 |
| RegOpenKey | 19384 | 9.598 |
| CreateFile | 18983 | 9.399 |
| CloseFile | 15595 | 7.721 |
| RegQueryValue | 15057 | 7.455 |
| IRP_MJ_CLOSE | 14897 | 7.376 |
| ReadFile | 13961 | 6.912 |
| RegCloseKey | 8169 | 4.045 |
| QueryNameInformationFile | 7469 | 3.698 |
| CreateFileMapping | 6530 | 3.233 |
| FASTIO_RELEASE_FOR_SECTION_SYNCHRONIZATION | 6530 | 3.233 |
| QueryInformationVolume | 6409 | 3.173 |
| QueryAllInformationFile | 6408 | 3.173 |
| RegQueryKey | 4800 | 2.377 |
| QueryStandardInformationFile | 3288 | 1.628 |
| QueryOpen | 3271 | 1.62 |
| RegEnumKey | 2804 | 1.388 |
| QueryNormalizedNameInformationFile | 2496 | 1.236 |
| Load Image | 2263 | 1.12 |
| QueryBasicInformationFile | 1983 | 0.982 |
| Thread Create | 1425 | 0.706 |
| Thread Exit | 1425 | 0.706 |
| QueryDirectory | 1041 | 0.515 |
| QueryNetworkOpenInformationFile | 647 | 0.32 |
| SetDispositionInformationEx | 491 | 0.243 |
| RegEnumValue | 367 | 0.182 |
| QueryAttributeTagFile | 361 | 0.179 |
| RegSetInfoKey | 322 | 0.159 |
| QuerySecurityFile | 307 | 0.152 |
| Process Profiling | 270 | 0.134 |
| RegCreateKey | 206 | 0.102 |
| SetAllocationInformationFile | 195 | 0.097 |
| SetEndOfFileInformationFile | 189 | 0.094 |
| FileSystemControl | 112 | 0.055 |
| Process Start | 82 | 0.041 |
| FASTIO_ACQUIRE_FOR_CC_FLUSH | 82 | 0.041 |
| FASTIO_RELEASE_FOR_CC_FLUSH | 82 | 0.041 |
| Process Exit | 82 | 0.041 |
| Process Create | 81 | 0.04 |
| SetRenameInformationEx | 77 | 0.038 |

### Roslyn
| name | count | pct |
| --- | --- | --- |
| CreateFile | 2206227 | 17.476 |
| CloseFile | 1667984 | 13.212 |
| QueryOpen | 1615022 | 12.793 |
| IRP_MJ_CLOSE | 1464414 | 11.6 |
| QueryNetworkOpenInformationFile | 1050266 | 8.319 |
| ReadFile | 917206 | 7.265 |
| RegQueryValue | 536724 | 4.251 |
| QueryStandardInformationFile | 479794 | 3.8 |
| RegOpenKey | 361562 | 2.864 |
| WriteFile | 290668 | 2.302 |
| RegQueryKey | 278149 | 2.203 |
| QueryBasicInformationFile | 230656 | 1.827 |
| QueryAttributeInformationVolume | 199035 | 1.577 |
| QueryDirectory | 195120 | 1.546 |
| CreateFileMapping | 157674 | 1.249 |
| FASTIO_RELEASE_FOR_SECTION_SYNCHRONIZATION | 157674 | 1.249 |
| RegCloseKey | 148437 | 1.176 |
| QueryRemoteProtocolInformation | 132690 | 1.051 |
| QueryAttributeTagFile | 76501 | 0.606 |
| SetBasicInformationFile | 70678 | 0.56 |
| SetEndOfFileInformationFile | 67015 | 0.531 |
| QuerySecurityFile | 66648 | 0.528 |
| QueryStreamInformationFile | 66345 | 0.526 |
| QueryEaInformationFile | 66345 | 0.526 |
| Process Profiling | 34252 | 0.271 |
| TCP TCPCopy | 8185 | 0.065 |
| FASTIO_ACQUIRE_FOR_CC_FLUSH | 8157 | 0.065 |
| FASTIO_RELEASE_FOR_CC_FLUSH | 8157 | 0.065 |
| RegCreateKey | 7676 | 0.061 |
| Thread Create | 7113 | 0.056 |
| Thread Exit | 7113 | 0.056 |
| Load Image | 6823 | 0.054 |
| SetDispositionInformationEx | 6032 | 0.048 |
| RegEnumKey | 5275 | 0.042 |
| SetRenameInformationFile | 4124 | 0.033 |
| QueryNameInformationFile | 4007 | 0.032 |
| TCP Receive | 3647 | 0.029 |
| RegSetInfoKey | 2752 | 0.022 |
| FASTIO_MDL_READ_COMPLETE | 2234 | 0.018 |
| RegEnumValue | 2224 | 0.018 |

## Core file operation groups

### ripgrep
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

### Roslyn
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

## Core per-second rates

### ripgrep
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

### Roslyn
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

## Phase detection

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

## AV-relevance metrics

### ripgrep
| metric | value |
| --- | --- |
| unique_file_paths | 3396 |
| unique_executable_like_paths | 590 |
| new_write_paths | 674 |
| executable_like_write_paths | 7 |
| metadata_query_events | 31190 |
| image_load_events | 2263 |
| write_events | 33912 |
| fast_io_disallowed_events | 9003 |
| failed_probe_events | 19647 |
| network_events | 45 |
| registry_query_events | 39241 |
| observed_read_mb | 766.9 |
| observed_write_mb | 303.9 |

### Roslyn
| metric | value |
| --- | --- |
| unique_file_paths | 153774 |
| unique_executable_like_paths | 67340 |
| new_write_paths | 82836 |
| executable_like_write_paths | 61377 |
| metadata_query_events | 4049794 |
| image_load_events | 6823 |
| write_events | 432862 |
| fast_io_disallowed_events | 1649094 |
| failed_probe_events | 1087406 |
| network_events | 14630 |
| registry_query_events | 1176435 |
| observed_read_mb | 36580.2 |
| observed_write_mb | 2232.4 |

## Weighted AV pressure model

This model is heuristic. It treats counts as exposure, percentages as shape, and weighted score as likely AV cost. Fresh executable writes and fresh build outputs are weighted much higher than Microsoft OS/SDK reads and metadata checks.

### Model weights
| metric | value |
| --- | --- |
| description | Heuristic AV pressure score. It is not measured latency. It estimates likely AV cost by weighting operation type and reducing likely trusted Microsoft/SDK paths while increasing fresh outputs and user/package/toolchain cache paths. |
| operation_group_weights | {'open_close': 0.5, 'metadata_query': 1.0, 'read': 1.5, 'write': 6.0, 'image_load': 8.0, 'registry': 0.3, 'network': 2.0, 'process_thread': 0.2, 'other': 0.2} |
| trust_bucket_multipliers | {'microsoft_os_path': 0.2, 'microsoft_sdk_programfiles_path': 0.35, 'other_programfiles_path': 0.8, 'nuget_user_cache': 1.2, 'rustup_toolchain_user_cache': 1.5, 'cargo_crate_user_cache': 1.7, 'source_tree_non_output': 2.0, 'other_user_profile_path': 2.0, 'other_path': 1.5, 'fresh_build_output': 4.0, 'registry': 0.5, 'network': 1.0, 'none': 1.0} |
| executable_write_base_weight | 30.0 |
| fast_io_disallowed_bonus | 1.0 |
| failed_probe_bonus | 0.5 |

### Summary
| trace | score | score_per_second | score_per_1000_events | top_group | top_trust_bucket | top_phase |
| --- | --- | --- | --- | --- | --- | --- |
| ripgrep | 877977.0 | 15368.85 | 4347.088 | write | fresh_build_output | link/resources |
| roslyn | 52713706.8 | 73520.398 | 4175.449 | write | fresh_build_output | output/write phase |

### Pressure by operation group: ripgrep
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

### Pressure by operation group: Roslyn
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

### Pressure by trust bucket: ripgrep
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

### Pressure by trust bucket: Roslyn
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

### Pressure by phase: ripgrep
| name | score | pct |
| --- | --- | --- |
| link/resources | 421906.9 | 48.054 |
| rustc compile | 408668.3 | 46.547 |
| cargo graph/setup | 39384.2 | 4.486 |
| harness/console | 6939.7 | 0.79 |
| build scripts | 1077.8 | 0.123 |

### Pressure by phase: Roslyn
| name | score | pct |
| --- | --- | --- |
| output/write phase | 19504734.4 | 37.001 |
| msbuild evaluation | 15592239.4 | 29.579 |
| compiler server compile | 14612443.1 | 27.72 |
| restore/cache touch | 2913726.8 | 5.527 |
| harness/console | 78428.6 | 0.149 |
| other build tasks | 12134.4 | 0.023 |

## Trust and reputation heuristics

These are path-based heuristics. ProcMon does not record Authenticode signer, catalog signing, cloud reputation, or whether an AV product actually trusted a file.

### Core file events by trust bucket: ripgrep
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

### Core file events by trust bucket: Roslyn
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

### Executable-like file events by trust bucket: ripgrep
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

### Executable-like file events by trust bucket: Roslyn
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

### Image-load events by trust bucket: ripgrep
| name | count | pct |
| --- | --- | --- |
| microsoft_os_path | 2014 | 88.997 |
| microsoft_sdk_programfiles_path | 130 | 5.745 |
| rustup_toolchain_user_cache | 108 | 4.772 |
| fresh_build_output | 6 | 0.265 |
| other_path | 3 | 0.133 |
| cargo_crate_user_cache | 2 | 0.088 |

### Image-load events by trust bucket: Roslyn
| name | count | pct |
| --- | --- | --- |
| microsoft_sdk_programfiles_path | 3422 | 50.154 |
| microsoft_os_path | 3393 | 49.729 |
| nuget_user_cache | 5 | 0.073 |
| other_path | 3 | 0.044 |

### Executable-like writes by trust bucket: ripgrep
| name | count | pct |
| --- | --- | --- |
| fresh_build_output | 58 | 100.0 |

### Executable-like writes by trust bucket: Roslyn
| name | count | pct |
| --- | --- | --- |
| fresh_build_output | 216285 | 99.445 |
| nuget_user_cache | 712 | 0.327 |
| other_user_profile_path | 496 | 0.228 |

## Process x operation matrix

### ripgrep
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

### Roslyn
| process | events | CreateFile | QueryOpen | QueryNetworkOpenInformationFile | ReadFile | WriteFile | Load Image | RegOpenKey | RegQueryValue | Process Create | Thread Create |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| dotnet.exe | 10053215 | 1832014 | 1383606 | 821411 | 459567 | 174048 | 4921 | 344028 | 521688 | 111 | 4624 |
| VBCSCompiler.exe | 2436411 | 363524 | 229187 | 228679 | 456582 | 116554 | 150 | 296 | 231 | 2 | 1933 |
| avbench.exe | 51301 | 7713 | 900 | 70 | 686 | 61 | 125 | 4949 | 3317 | 2 | 72 |
| Conhost.exe | 40145 | 838 | 411 | 0 | 10 | 0 | 1121 | 5369 | 6218 | 0 | 400 |
| VsdConfigTool.exe | 37482 | 1693 | 840 | 106 | 314 | 5 | 377 | 5508 | 4480 | 0 | 55 |
| getmac.exe | 3168 | 83 | 42 | 0 | 8 | 0 | 68 | 734 | 474 | 0 | 14 |
| cmd.exe | 2959 | 362 | 36 | 0 | 39 | 0 | 61 | 678 | 316 | 10 | 15 |

## Core path roots

### ripgrep
| name | count | pct |
| --- | --- | --- |
| C:\bench\ripgrep | 58442 | 29.414 |
| C:\Users\User | 55368 | 27.867 |
| HKLM\System\CurrentControlSet | 28032 | 14.109 |
| C:\Windows\System32 | 18402 | 9.262 |
| HKLM\SOFTWARE\Microsoft | 11267 | 5.671 |
| C:\Program Files (x86)\Microsoft Visual Studio | 4316 | 2.172 |
| HKLM\Software\Microsoft | 2693 | 1.355 |
| C:\Program Files (x86)\Windows Kits | 2686 | 1.352 |
| HKLM | 2509 | 1.263 |
| C:\Windows\Microsoft.NET | 2272 | 1.144 |
| HKCU\Software\Microsoft | 1373 | 0.691 |
| C:\ProgramData\Microsoft | 992 | 0.499 |
| C: | 972 | 0.489 |
| C:\Program Files\dotnet | 904 | 0.455 |
| HKCU | 667 | 0.336 |
| C:\Windows\assembly | 662 | 0.333 |
| HKLM\Software\Policies | 657 | 0.331 |
| C:\Windows\apppatch | 622 | 0.313 |
| HKLM\SYSTEM\CurrentControlSet | 535 | 0.269 |
| HKCU\Control Panel\Desktop | 528 | 0.266 |
| HKLM\SOFTWARE\Policies | 516 | 0.26 |
| HKCR\CLSID\{177F0C4A-1CD3-4DE7-A32C-71DBBB9FA36D} | 510 | 0.257 |
| C:\tools\avbench | 463 | 0.233 |
| HKCU\Software\Policies | 398 | 0.2 |
| C:\$Extend\$Deleted | 393 | 0.198 |
| HKLM\SOFTWARE\WOW6432Node | 382 | 0.192 |
| C:\Program Files\Microsoft Visual Studio | 163 | 0.082 |
| C:\Windows\Globalization | 152 | 0.077 |
| HKCR | 131 | 0.066 |
| C:\$Extend\$UsnJrnl:$J:$DATA | 120 | 0.06 |
| C:\Windows\Microsoft.Net | 110 | 0.055 |
| C:\Program Files\Microsoft SQL Server | 93 | 0.047 |
| HKLM\System\CurrentControlset | 74 | 0.037 |
| HKCU\Software\Classes | 64 | 0.032 |
| HKCR\CLSID\{CF4CC405-E2C5-4DDD-B3CE-5E7582D8C9FA} | 53 | 0.027 |
| HKCR\CLSID\{4590F811-1D3A-11D0-891F-00AA004B2E24} | 53 | 0.027 |
| HKCR\CLSID\{7C857801-7381-11CF-884D-00AA004B2E24} | 53 | 0.027 |
| HKCR\CLSID\{D68AF00A-29CB-43FA-8504-CE99A996D9EA} | 53 | 0.027 |
| HKCR\CLSID\{E7D35CFA-348B-485E-B524-252725D697CA} | 53 | 0.027 |
| HKCR\CLSID\{1B1CAD8C-2DAB-11D2-B604-00104B703EFD} | 53 | 0.027 |

### Roslyn
| name | count | pct |
| --- | --- | --- |
| C:\bench\roslyn | 5786039 | 46.009 |
| C:\Users\User | 3793353 | 30.164 |
| HKLM\System\CurrentControlSet | 1103457 | 8.774 |
| C:\Program Files\dotnet | 924617 | 7.352 |
| C:\Program Files (x86)\Reference Assemblies | 528178 | 4.2 |
| HKLM | 137592 | 1.094 |
| C: | 42284 | 0.336 |
| C:\Windows\System32 | 35573 | 0.283 |
| C:\ProgramData\Microsoft | 34132 | 0.271 |
| HKLM\SOFTWARE\Microsoft | 28838 | 0.229 |
| HKLM\SYSTEM\CurrentControlSet | 22539 | 0.179 |
| C:\Program Files (x86)\Microsoft Visual Studio | 17352 | 0.138 |
| C:\Program Files (x86)\Microsoft SDKs | 13399 | 0.107 |
| C:\$Extend\$UsnJrnl:$J:$DATA | 13341 | 0.106 |
| HKLM\SOFTWARE\WOW6432Node | 13141 | 0.104 |
| HKLM\Software\Policies | 7783 | 0.062 |
| HKCU\Software\Microsoft | 5232 | 0.042 |
| WinDev2407Eval.mshome.net:51344 -> 150.171.73.16:https | 4918 | 0.039 |
| HKCU | 4887 | 0.039 |
| C:\bench\.editorconfig | 4264 | 0.034 |
| C:\bench\.globalconfig | 4264 | 0.034 |
| HKLM\Software\Microsoft | 3890 | 0.031 |
| C:\bench\.git | 3120 | 0.025 |
| HKLM\SOFTWARE\Policies | 3108 | 0.025 |
| C:\Program Files (x86)\coreservicing | 2946 | 0.023 |
| C:\Windows\Microsoft.NET | 2762 | 0.022 |
| C:\Windows\Globalization | 2719 | 0.022 |
| C:\Windows\SysWOW64 | 2694 | 0.021 |
| HKCU\Software\Policies | 2231 | 0.018 |
| C:\bench\HEAD | 1560 | 0.012 |
| HKCU\Control Panel\Desktop | 1527 | 0.012 |
| C:\Program Files (x86)\Windows Kits | 1500 | 0.012 |
| C:\Program Files (x86)\NuGet | 1276 | 0.01 |
| HKLM\Software\WOW6432Node | 1152 | 0.009 |
| C:\bench\NuGet.Config | 1142 | 0.009 |
| C:\Windows\assembly | 789 | 0.006 |
| HKCU\Console | 767 | 0.006 |
| WinDev2407Eval.mshome.net:51359 -> 52.188.247.146:https | 698 | 0.006 |
| WinDev2407Eval.mshome.net:51305 -> 52.188.247.146:https | 688 | 0.005 |
| HKU | 560 | 0.004 |

## Core file extensions

### ripgrep
| name | count | pct |
| --- | --- | --- |
| .dll | 25201 | 18.395 |
| .rlib | 18623 | 13.594 |
| .pdb | 16946 | 12.37 |
| .lib | 14438 | 10.539 |
| .rmeta | 13048 | 9.524 |
| (none) | 11466 | 8.37 |
| .rs | 10235 | 7.471 |
| .o | 7795 | 5.69 |
| .exe | 5106 | 3.727 |
| .mui | 2699 | 1.97 |
| .sys | 2589 | 1.89 |
| .a | 1103 | 0.805 |
| .json | 839 | 0.612 |
| .d | 785 | 0.573 |
| .sdb | 622 | 0.454 |
| .natvis | 600 | 0.438 |
| .toml | 484 | 0.353 |
| .tmp | 396 | 0.289 |
| .temp-archive | 381 | 0.278 |
| .timestamp | 372 | 0.272 |
| .cargo-ok | 270 | 0.197 |
| .crate | 216 | 0.158 |
| .config | 215 | 0.157 |
| .aux | 144 | 0.105 |
| .nls | 130 | 0.095 |
| .txt | 109 | 0.08 |
| .cmd | 104 | 0.076 |
| .228 | 96 | 0.07 |
| .global-cache-journal | 89 | 0.065 |
| .1 | 88 | 0.064 |
| .dfa | 80 | 0.058 |
| .0 | 68 | 0.05 |
| .ini | 68 | 0.05 |
| .lock | 66 | 0.048 |
| .6 | 63 | 0.046 |
| .mun | 58 | 0.042 |
| .global-cache | 58 | 0.042 |
| .xml | 57 | 0.042 |
| .100 | 51 | 0.037 |
| .cache | 51 | 0.037 |

### Roslyn
| name | count | pct |
| --- | --- | --- |
| .dll | 4859576 | 49.437 |
| (none) | 852221 | 8.67 |
| .cs | 747779 | 7.607 |
| .sha512 | 474300 | 4.825 |
| .targets | 241580 | 2.458 |
| .xml | 204569 | 2.081 |
| .trn | 187821 | 1.911 |
| .props | 176379 | 1.794 |
| .resx | 169506 | 1.724 |
| .csproj | 168303 | 1.712 |
| .editorconfig | 168204 | 1.711 |
| .globalconfig | 161833 | 1.646 |
| .vb | 161727 | 1.645 |
| .cache | 124148 | 1.263 |
| .xlf | 112463 | 1.144 |
| .exe | 104213 | 1.06 |
| .0 | 96576 | 0.982 |
| .winmd | 93788 | 0.954 |
| .json | 93022 | 0.946 |
| .tmp | 90548 | 0.921 |
| .config | 68506 | 0.697 |
| .resources | 62328 | 0.634 |
| .vbproj | 27555 | 0.28 |
| .txt | 26578 | 0.27 |
| .pdb | 22114 | 0.225 |
| .projitems | 21779 | 0.222 |
| .git | 21712 | 0.221 |
| .5 | 17605 | 0.179 |
| .metadata | 15642 | 0.159 |
| .buildwithskipanalyzers | 15206 | 0.155 |
| .pri | 14808 | 0.151 |
| .unittests | 12797 | 0.13 |
| .1m2 | 10256 | 0.104 |
| .105 | 9961 | 0.101 |
| .snk | 9126 | 0.093 |
| .mui | 7019 | 0.071 |
| .so | 6921 | 0.07 |
| .up2date | 5996 | 0.061 |
| .nuspec | 5199 | 0.053 |
| .lastrun | 4355 | 0.044 |

## Core read extensions

### ripgrep
| name | count | pct |
| --- | --- | --- |
| .dll | 9884 | 28.385 |
| .rlib | 6600 | 18.954 |
| (none) | 3698 | 10.62 |
| .rs | 3639 | 10.451 |
| .rmeta | 2105 | 6.045 |
| .exe | 2082 | 5.979 |
| .o | 2041 | 5.861 |
| .pdb | 929 | 2.668 |
| .lib | 925 | 2.656 |
| .mui | 336 | 0.965 |
| .sys | 333 | 0.956 |
| .toml | 252 | 0.724 |
| .d | 199 | 0.571 |
| .sdb | 188 | 0.54 |
| .json | 174 | 0.5 |
| .natvis | 164 | 0.471 |
| .cargo-ok | 162 | 0.465 |
| .temp-archive | 114 | 0.327 |
| .config | 84 | 0.241 |
| .a | 83 | 0.238 |
| .tmp | 83 | 0.238 |
| .aux | 65 | 0.187 |
| .txt | 58 | 0.167 |
| .crate | 54 | 0.155 |
| .timestamp | 52 | 0.149 |
| .lock | 47 | 0.135 |
| .dfa | 40 | 0.115 |
| .1 | 27 | 0.078 |
| .global-cache | 26 | 0.075 |
| .help | 24 | 0.069 |
| .xml | 21 | 0.06 |
| .trn | 21 | 0.06 |
| .228 | 18 | 0.052 |
| .nls | 17 | 0.049 |
| .6 | 15 | 0.043 |
| .md | 14 | 0.04 |
| .sh | 12 | 0.034 |
| .fish | 12 | 0.034 |
| .zsh | 12 | 0.034 |
| .dat | 10 | 0.029 |

### Roslyn
| name | count | pct |
| --- | --- | --- |
| .dll | 950865 | 45.222 |
| .cs | 433850 | 20.633 |
| (none) | 185396 | 8.817 |
| .vb | 107151 | 5.096 |
| .xlf | 60381 | 2.872 |
| .resx | 49808 | 2.369 |
| .trn | 47631 | 2.265 |
| .targets | 47569 | 2.262 |
| .cache | 39336 | 1.871 |
| .xml | 31848 | 1.515 |
| .props | 18117 | 0.862 |
| .json | 14919 | 0.71 |
| .0 | 13474 | 0.641 |
| .1m2 | 10142 | 0.482 |
| .exe | 9888 | 0.47 |
| .editorconfig | 9531 | 0.453 |
| .config | 6819 | 0.324 |
| .resources | 6636 | 0.316 |
| .txt | 6141 | 0.292 |
| .csproj | 5113 | 0.243 |
| .globalconfig | 4630 | 0.22 |
| .dic | 4287 | 0.204 |
| .snk | 3736 | 0.178 |
| .projitems | 2750 | 0.131 |
| .tmp | 2750 | 0.131 |
| .unittests | 2579 | 0.123 |
| .so | 1965 | 0.093 |
| .nuspec | 1782 | 0.085 |
| .xaml | 1338 | 0.064 |
| .105 | 1300 | 0.062 |
| .metadata | 1020 | 0.049 |
| .vbproj | 885 | 0.042 |
| .mui | 857 | 0.041 |
| .package | 674 | 0.032 |
| .dylib | 608 | 0.029 |
| .1 | 504 | 0.024 |
| .baml | 480 | 0.023 |
| .6 | 476 | 0.023 |
| .utilities | 443 | 0.021 |
| .manifest | 396 | 0.019 |

## Core write extensions

### ripgrep
| name | count | pct |
| --- | --- | --- |
| .pdb | 15575 | 45.928 |
| .lib | 11388 | 33.581 |
| .rmeta | 4805 | 14.169 |
| .o | 748 | 2.206 |
| .a | 747 | 2.203 |
| (none) | 244 | 0.72 |
| .tmp | 88 | 0.259 |
| .d | 67 | 0.198 |
| .json | 61 | 0.18 |
| .exe | 58 | 0.171 |
| .timestamp | 56 | 0.165 |
| .global-cache-journal | 35 | 0.103 |
| .cache | 18 | 0.053 |
| .global-cache | 12 | 0.035 |
| .rs | 3 | 0.009 |
| .trn | 3 | 0.009 |
| .log | 2 | 0.006 |
| .tag | 1 | 0.003 |
| .csv | 1 | 0.003 |

### Roslyn
| name | count | pct |
| --- | --- | --- |
| .dll | 214373 | 49.525 |
| .tmp | 76748 | 17.73 |
| .cache | 39936 | 9.226 |
| .xml | 26756 | 6.181 |
| .resources | 21138 | 4.883 |
| (none) | 7457 | 1.723 |
| .cs | 6097 | 1.409 |
| .exe | 3120 | 0.721 |
| .config | 1774 | 0.41 |
| .dat-new | 1631 | 0.377 |
| .json | 1587 | 0.367 |
| .buildwithskipanalyzers | 1560 | 0.36 |
| .txt | 1414 | 0.327 |
| .up2date | 722 | 0.167 |
| .0 | 719 | 0.166 |
| .vb | 528 | 0.122 |
| .editorconfig | 398 | 0.092 |
| .lastrun | 378 | 0.087 |
| .targets | 363 | 0.084 |
| .props | 295 | 0.068 |
| .unittests | 266 | 0.061 |
| .xlf | 198 | 0.046 |
| .43n | 189 | 0.044 |
| .box | 186 | 0.043 |
| .so | 186 | 0.043 |
| .4we | 185 | 0.043 |
| .adl | 156 | 0.036 |
| .513 | 156 | 0.036 |
| .bxu | 156 | 0.036 |
| .zbp | 154 | 0.036 |
| .izy | 151 | 0.035 |
| .bep | 150 | 0.035 |
| .csproj | 150 | 0.035 |
| .chz | 146 | 0.034 |
| .fcj | 146 | 0.034 |
| .3hr | 146 | 0.034 |
| .bhm | 146 | 0.034 |
| .3to | 146 | 0.034 |
| .3h0 | 146 | 0.034 |
| .3ft | 146 | 0.034 |

## Core write directories

### ripgrep
| name | count | pct |
| --- | --- | --- |
| C:\Users\User\AppData\Local\Temp\rustcbtanAj | 11389 | 33.584 |
| C:\bench\ripgrep\target\release\deps | 6482 | 19.114 |
| C:\bench\ripgrep\target\release\build\serde_core-9325faa8aac88ce5 | 1662 | 4.901 |
| C:\bench\ripgrep\target\release\build\crossbeam-utils-cb9da71b25dafbce | 1661 | 4.898 |
| C:\bench\ripgrep\target\release\build\serde-383f949fe8b5850a | 1660 | 4.895 |
| C:\bench\ripgrep\target\release\build\anyhow-0769136d61e90248 | 1660 | 4.895 |
| C:\bench\ripgrep\target\release\build\serde_json-a7496737e29b6e52 | 1656 | 4.883 |
| C:\bench\ripgrep\target\release\build\ripgrep-ee6c0b68c2bfff47 | 1655 | 4.88 |
| C:\bench\ripgrep\target\release\deps\rmeta4XM6cx | 659 | 1.943 |
| C:\bench\ripgrep\target\release\deps\rmetaEu4I1V | 634 | 1.87 |
| C:\bench\ripgrep\target\release\deps\rmetajW7MfD | 629 | 1.855 |
| C:\bench\ripgrep\target\release\deps\rmetanyWg82 | 564 | 1.663 |
| C:\bench\ripgrep\target\release\deps\rmetas2RKMJ | 271 | 0.799 |
| C:\bench\ripgrep\target\release\deps\rmetafopqFl | 207 | 0.61 |
| C:\bench\ripgrep\target\release\deps\rmetasw3JbY | 190 | 0.56 |
| C:\bench\ripgrep\target\release\deps\rmeta8SzrNg | 151 | 0.445 |
| C:\bench\ripgrep\target\release\deps\rmeta533ZpN | 147 | 0.433 |
| C:\bench\ripgrep\target\release\deps\rmetaTNiVZP | 146 | 0.431 |
| C:\bench\ripgrep\target\release\deps\rmetaVMvmwt | 140 | 0.413 |
| C:\bench\ripgrep\target\release\deps\rmetarE2tGt | 120 | 0.354 |
| C:\bench\ripgrep\target\release\deps\rmeta6hQZqe | 112 | 0.33 |
| C:\bench\ripgrep\target\release\deps\.tmpAsbD11.temp-archive | 105 | 0.31 |
| C:\Users\User\AppData\Local\Temp | 92 | 0.271 |
| C:\bench\ripgrep\target\release\deps\rmetav2SpmG | 89 | 0.262 |
| C:\bench\ripgrep\target\release\deps\rmetauAoeta | 77 | 0.227 |
| C:\bench\ripgrep\target\release\deps\rmeta7z2siZ | 76 | 0.224 |
| C:\bench\ripgrep\target\release\deps\rmetaGyA7Ox | 66 | 0.195 |
| C:\bench\ripgrep\target\release\deps\.tmpIqDh3D.temp-archive | 62 | 0.183 |
| C: | 59 | 0.174 |
| C:\bench\ripgrep\target\release\deps\rmeta2JlfOM | 51 | 0.15 |
| C:\bench\ripgrep\target\release\deps\.tmprnKhA7.temp-archive | 51 | 0.15 |
| C:\Users\User\.cargo | 49 | 0.144 |
| C:\bench\ripgrep\target\release\deps\rmetawmcOUL | 49 | 0.144 |
| C:\bench\ripgrep\target\release\deps\.tmpepz6ro.temp-archive | 45 | 0.133 |
| C:\bench\ripgrep\target\release\deps\.tmpIDUiu6.temp-archive | 44 | 0.13 |
| C:\bench\ripgrep\target\release\deps\rmetamPwktx | 42 | 0.124 |
| C:\bench\ripgrep\target\release\deps\rmetalmrLjZ | 40 | 0.118 |
| C:\bench\ripgrep\target\release\deps\.tmpUfjh9O.temp-archive | 37 | 0.109 |
| C:\bench\ripgrep\target\release\deps\rmetaTKoRlG | 36 | 0.106 |
| C:\bench\ripgrep\target\release\deps\.tmpFSQTJq.temp-archive | 35 | 0.103 |

### Roslyn
| name | count | pct |
| --- | --- | --- |
| C: | 3096 | 0.715 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis\Release\net10.0 | 2837 | 0.655 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis\Release\netstandard2.0 | 2835 | 0.655 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis\Release\net8.0 | 2768 | 0.639 |
| C:\bench\roslyn\src\Compilers\CSharp\Portable\Generated\CSharpSyntaxGenerator\CSharpSyntaxGenerator.SourceGenerator | 2742 | 0.633 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.CSharp\Release\netstandard2.0 | 2593 | 0.599 |
| C:\bench\roslyn\artifacts\obj\Microsoft.VisualStudio.LanguageServices\Release\net472 | 2436 | 0.563 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.CSharp\Release\net10.0 | 2431 | 0.562 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.CSharp\Release\net8.0 | 2431 | 0.562 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.EditorFeatures\Release\net472 | 2243 | 0.518 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.VisualBasic\Release\netstandard2.0 | 2183 | 0.504 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.VisualBasic\Release\net10.0 | 2164 | 0.5 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.VisualBasic\Release\net8.0 | 2154 | 0.498 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.CSharp.Features.UnitTests\Release\net10.0-windows | 2004 | 0.463 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.Workspaces\Release\net8.0 | 1928 | 0.445 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.Workspaces\Release\netstandard2.0 | 1905 | 0.44 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.CSharp\Release\net10.0\Microsoft.CodeAnalysis.CSharp.xlf | 1899 | 0.439 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.CSharp\Release\net8.0\Microsoft.CodeAnalysis.CSharp.xlf | 1886 | 0.436 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.CSharp\Release\netstandard2.0\Microsoft.CodeAnalysis.CSharp.xlf | 1885 | 0.435 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.Workspaces\Release\net10.0 | 1871 | 0.432 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.CSharp.Features.UnitTests\Release\net472 | 1859 | 0.429 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.Features\Release\netstandard2.0 | 1817 | 0.42 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.Features\Release\net10.0 | 1778 | 0.411 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.Features\Release\net8.0 | 1777 | 0.411 |
| C:\Users\User\AppData\Local\NuGet\v3-cache\4e48ed527caa97eafe70a0e2da61ea717b2b3a08$otnet-public_nuget_v3_index.json | 1544 | 0.357 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.CSharp.Emit3.UnitTests\Release\net472 | 1457 | 0.337 |
| C:\Users\User\.dotnet\TelemetryStorageService | 1444 | 0.334 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.CSharp.Emit3.UnitTests\Release\net10.0 | 1414 | 0.327 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.CSharp.CodeStyle.UnitTests\Release\net472 | 1400 | 0.323 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.EditorFeatures2.UnitTests\Release\net472 | 1319 | 0.305 |
| C:\bench\roslyn\artifacts\obj\Microsoft.VisualStudio.LanguageServices.CSharp\Release\net472 | 1309 | 0.302 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.VisualBasic\Release\net8.0\Microsoft.CodeAnalysis.VisualBasic.xlf | 1284 | 0.297 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.VisualBasic\Release\netstandard2.0\Microsoft.CodeAnalysis.VisualBasic.xlf | 1276 | 0.295 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.VisualBasic\Release\net10.0\Microsoft.CodeAnalysis.VisualBasic.xlf | 1274 | 0.294 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.Analyzers\Release\netstandard2.0 | 1270 | 0.293 |
| C:\bench\roslyn\artifacts\obj\Roslyn.Diagnostics.Analyzers\Release\netstandard2.0 | 1245 | 0.288 |
| C:\bench\roslyn\artifacts\obj\Text.Analyzers\Release\netstandard2.0 | 1233 | 0.285 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers\Release\netstandard2.0 | 1218 | 0.281 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.BannedApiAnalyzers\Release\netstandard2.0 | 1207 | 0.279 |
| C:\bench\roslyn\artifacts\obj\Microsoft.CodeAnalysis.CSharp.Semantic.UnitTests\Release\net472 | 1192 | 0.275 |

## Top image loads

### ripgrep
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

### Roslyn
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

## Top executable-like writes

### ripgrep
| name | count | pct |
| --- | --- | --- |
| C:\bench\ripgrep\target\release\deps\rg.exe | 22 | 37.931 |
| C:\bench\ripgrep\target\release\build\serde-383f949fe8b5850a\build_script_build-383f949fe8b5850a.exe | 6 | 10.345 |
| C:\bench\ripgrep\target\release\build\serde_json-a7496737e29b6e52\build_script_build-a7496737e29b6e52.exe | 6 | 10.345 |
| C:\bench\ripgrep\target\release\build\ripgrep-ee6c0b68c2bfff47\build_script_build-ee6c0b68c2bfff47.exe | 6 | 10.345 |
| C:\bench\ripgrep\target\release\build\crossbeam-utils-cb9da71b25dafbce\build_script_build-cb9da71b25dafbce.exe | 6 | 10.345 |
| C:\bench\ripgrep\target\release\build\anyhow-0769136d61e90248\build_script_build-0769136d61e90248.exe | 6 | 10.345 |
| C:\bench\ripgrep\target\release\build\serde_core-9325faa8aac88ce5\build_script_build-9325faa8aac88ce5.exe | 6 | 10.345 |

### Roslyn
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

## Top failed probes

### ripgrep
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

### Roslyn
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

## Core registry roots

### ripgrep
| name | count | pct |
| --- | --- | --- |
| HKLM\System\CurrentControlSet | 28032 | 54.823 |
| HKLM\SOFTWARE\Microsoft | 11267 | 22.035 |
| HKLM\Software\Microsoft | 2693 | 5.267 |
| HKLM | 2509 | 4.907 |
| HKCU\Software\Microsoft | 1373 | 2.685 |
| HKCU | 667 | 1.304 |
| HKLM\Software\Policies | 657 | 1.285 |
| HKLM\SYSTEM\CurrentControlSet | 535 | 1.046 |
| HKCU\Control Panel\Desktop | 528 | 1.033 |
| HKLM\SOFTWARE\Policies | 516 | 1.009 |
| HKCR\CLSID\{177F0C4A-1CD3-4DE7-A32C-71DBBB9FA36D} | 510 | 0.997 |
| HKCU\Software\Policies | 398 | 0.778 |
| HKLM\SOFTWARE\WOW6432Node | 382 | 0.747 |
| HKCR | 131 | 0.256 |
| HKLM\System\CurrentControlset | 74 | 0.145 |
| HKCU\Software\Classes | 64 | 0.125 |
| HKCR\CLSID\{CF4CC405-E2C5-4DDD-B3CE-5E7582D8C9FA} | 53 | 0.104 |
| HKCR\CLSID\{4590F811-1D3A-11D0-891F-00AA004B2E24} | 53 | 0.104 |
| HKCR\CLSID\{7C857801-7381-11CF-884D-00AA004B2E24} | 53 | 0.104 |
| HKCR\CLSID\{D68AF00A-29CB-43FA-8504-CE99A996D9EA} | 53 | 0.104 |
| HKCR\CLSID\{E7D35CFA-348B-485E-B524-252725D697CA} | 53 | 0.104 |
| HKCR\CLSID\{1B1CAD8C-2DAB-11D2-B604-00104B703EFD} | 53 | 0.104 |
| HKCR\CLSID\{057EEE47-2572-4AA1-88D7-60CE2149E33C} | 53 | 0.104 |
| HKU | 46 | 0.09 |
| HKCU\SOFTWARE\Microsoft | 30 | 0.059 |
| HKLM\HARDWARE\DESCRIPTION | 29 | 0.057 |
| HKCU\Console | 26 | 0.051 |
| HKLM\SYSTEM\Setup | 16 | 0.031 |
| HKCR\CLSID\{8BC3F05E-D86B-11D0-A075-00C04FB68820} | 14 | 0.027 |
| HKCU\Control Panel\International | 14 | 0.027 |
| HKCR\CLSID\{4991d34b-80a1-4291-83b6-3328366b9097} | 13 | 0.025 |
| HKLM\OSDATA\Software | 13 | 0.025 |
| HKU\.DEFAULT | 12 | 0.023 |
| HKU\.DEFAULT\Software | 12 | 0.023 |
| HKLM\System\Setup | 8 | 0.016 |
| HKCU\Console\%%Startup | 8 | 0.016 |
| HKLM\Software\WOW6432Node | 8 | 0.016 |
| HKCR\Interface\{F309AD18-D86A-11D0-A075-00C04FB68820} | 6 | 0.012 |
| HKCR\Interface\{D4781CD6-E5D3-44DF-AD94-930EFE48A887} | 6 | 0.012 |
| HKCR\Interface\{9F6C78EF-FCE5-42FA-ABEA-3E7DF91921DC} | 6 | 0.012 |

### Roslyn
| name | count | pct |
| --- | --- | --- |
| HKLM\System\CurrentControlSet | 1103457 | 82.172 |
| HKLM | 137592 | 10.246 |
| HKLM\SOFTWARE\Microsoft | 28838 | 2.148 |
| HKLM\SYSTEM\CurrentControlSet | 22539 | 1.678 |
| HKLM\SOFTWARE\WOW6432Node | 13141 | 0.979 |
| HKLM\Software\Policies | 7783 | 0.58 |
| HKCU\Software\Microsoft | 5232 | 0.39 |
| HKCU | 4887 | 0.364 |
| HKLM\Software\Microsoft | 3890 | 0.29 |
| HKLM\SOFTWARE\Policies | 3108 | 0.231 |
| HKCU\Software\Policies | 2231 | 0.166 |
| HKCU\Control Panel\Desktop | 1527 | 0.114 |
| HKLM\Software\WOW6432Node | 1152 | 0.086 |
| HKCU\Console | 767 | 0.057 |
| HKU | 560 | 0.042 |
| HKLM\SYSTEM\Setup | 315 | 0.023 |
| HKCU\Software\Classes | 292 | 0.022 |
| HKCR\Installer\Assemblies | 250 | 0.019 |
| HKCU\Console\%%Startup | 236 | 0.018 |
| HKCR\CLSID\{4590F811-1D3A-11D0-891F-00AA004B2E24} | 159 | 0.012 |
| HKCR\CLSID\{7C857801-7381-11CF-884D-00AA004B2E24} | 159 | 0.012 |
| HKCR\CLSID\{D68AF00A-29CB-43FA-8504-CE99A996D9EA} | 159 | 0.012 |
| HKCR\CLSID\{E7D35CFA-348B-485E-B524-252725D697CA} | 159 | 0.012 |
| HKCR\CLSID\{1B1CAD8C-2DAB-11D2-B604-00104B703EFD} | 159 | 0.012 |
| HKCR | 148 | 0.011 |
| HKLM\System\Setup | 145 | 0.011 |
| HKCU\Console\WindowPosition | 118 | 0.009 |
| HKCR\CLSID\{4590F812-1D3A-11D0-891F-00AA004B2E24} | 106 | 0.008 |
| HKCU\SOFTWARE\Microsoft | 95 | 0.007 |
| HKLM\System\CurrentControlset | 62 | 0.005 |
| HKCU\Console\ForceV2 | 59 | 0.004 |
| HKCU\Console\VirtualTerminalLevel | 59 | 0.004 |
| HKCU\Console\PopupColors | 59 | 0.004 |
| HKCU\Console\InsertMode | 59 | 0.004 |
| HKCU\Console\LineSelection | 59 | 0.004 |
| HKCU\Console\FilterOnPaste | 59 | 0.004 |
| HKCU\Console\LineWrap | 59 | 0.004 |
| HKCU\Console\CtrlKeyShortcutsDisabled | 59 | 0.004 |
| HKCU\Console\QuickEdit | 59 | 0.004 |
| HKCU\Console\WindowAlpha | 59 | 0.004 |

## Core network paths

### ripgrep
| name | count | pct |
| --- | --- | --- |
| WinDev2407Eval.mshome.net:51025 -> a23-215-55-136.deploy.static.akamaitechnologies.com:https | 25 | 55.556 |
| WinDev2407Eval.mshome.net:51026 -> 20.189.173.28:https | 20 | 44.444 |

### Roslyn
| name | count | pct |
| --- | --- | --- |
| WinDev2407Eval.mshome.net:51344 -> 150.171.73.16:https | 4918 | 33.616 |
| WinDev2407Eval.mshome.net:51359 -> 52.188.247.146:https | 698 | 4.771 |
| WinDev2407Eval.mshome.net:51305 -> 52.188.247.146:https | 688 | 4.703 |
| WinDev2407Eval.mshome.net:51358 -> 52.188.247.146:https | 493 | 3.37 |
| WinDev2407Eval.mshome.net:51347 -> 52.188.247.146:https | 338 | 2.31 |
| WinDev2407Eval.mshome.net:51350 -> 52.188.247.146:https | 338 | 2.31 |
| WinDev2407Eval.mshome.net:51356 -> 52.188.247.146:https | 332 | 2.269 |
| WinDev2407Eval.mshome.net:51352 -> 52.188.247.146:https | 228 | 1.558 |
| WinDev2407Eval.mshome.net:51348 -> 52.188.247.146:https | 213 | 1.456 |
| WinDev2407Eval.mshome.net:51304 -> 52.188.247.146:https | 153 | 1.046 |
| WinDev2407Eval.mshome.net:51306 -> 52.188.247.146:https | 133 | 0.909 |
| WinDev2407Eval.mshome.net:51355 -> 52.188.247.146:https | 128 | 0.875 |
| WinDev2407Eval.mshome.net:51351 -> 52.188.247.146:https | 123 | 0.841 |
| WinDev2407Eval.mshome.net:51349 -> 52.188.247.146:https | 88 | 0.602 |
| WinDev2407Eval.mshome.net:51084 -> 150.171.73.16:https | 72 | 0.492 |
| WinDev2407Eval.mshome.net:51346 -> 52.188.247.146:https | 63 | 0.431 |
| WinDev2407Eval.mshome.net:51029 -> 150.171.73.16:https | 58 | 0.396 |
| WinDev2407Eval.mshome.net:51040 -> 150.171.73.16:https | 54 | 0.369 |
| WinDev2407Eval.mshome.net:51035 -> 150.171.73.16:https | 50 | 0.342 |
| WinDev2407Eval.mshome.net:51041 -> 150.171.73.16:https | 44 | 0.301 |
| WinDev2407Eval.mshome.net:51032 -> 150.171.73.16:https | 44 | 0.301 |
| WinDev2407Eval.mshome.net:51037 -> 150.171.73.16:https | 43 | 0.294 |
| WinDev2407Eval.mshome.net:51028 -> 150.171.73.16:https | 40 | 0.273 |
| WinDev2407Eval.mshome.net:51030 -> 150.171.73.16:https | 38 | 0.26 |
| WinDev2407Eval.mshome.net:51039 -> 150.171.73.16:https | 37 | 0.253 |
| WinDev2407Eval.mshome.net:51031 -> 150.171.73.16:https | 37 | 0.253 |
| WinDev2407Eval.mshome.net:51038 -> 150.171.73.16:https | 35 | 0.239 |
| WinDev2407Eval.mshome.net:51042 -> 150.171.73.16:https | 35 | 0.239 |
| WinDev2407Eval.mshome.net:51100 -> 150.171.73.16:https | 35 | 0.239 |
| WinDev2407Eval.mshome.net:51033 -> 150.171.73.16:https | 34 | 0.232 |
| WinDev2407Eval.mshome.net:51044 -> 150.171.73.16:https | 34 | 0.232 |
| WinDev2407Eval.mshome.net:51034 -> 150.171.73.16:https | 34 | 0.232 |
| WinDev2407Eval.mshome.net:51036 -> 150.171.73.16:https | 32 | 0.219 |
| WinDev2407Eval.mshome.net:51027 -> 150.171.73.16:https | 32 | 0.219 |
| WinDev2407Eval.mshome.net:51043 -> 150.171.73.16:https | 32 | 0.219 |
| WinDev2407Eval.mshome.net:51099 -> 150.171.73.16:https | 32 | 0.219 |
| WinDev2407Eval.mshome.net:51121 -> 150.171.73.16:https | 31 | 0.212 |
| WinDev2407Eval.mshome.net:51354 -> 52.188.247.146:https | 28 | 0.191 |
| WinDev2407Eval.mshome.net:51087 -> 150.171.73.16:https | 27 | 0.185 |
| WinDev2407Eval.mshome.net:51102 -> 150.171.73.16:https | 27 | 0.185 |

## Broad trace context

These tables exclude only ProcMon/System and can include unrelated desktop or service activity. Use them as context, not as the primary workload profile.

### Operation families: ripgrep
| name | count | pct |
| --- | --- | --- |
| file | 148859 | 55.648 |
| registry | 96564 | 36.099 |
| other | 10636 | 3.976 |
| profiling | 7785 | 2.91 |
| process/thread | 3440 | 1.286 |
| network | 217 | 0.081 |

### Operation families: Roslyn
| name | count | pct |
| --- | --- | --- |
| file | 9860534 | 73.43 |
| registry | 2023407 | 15.068 |
| other | 1388706 | 10.341 |
| profiling | 124049 | 0.924 |
| process/thread | 16428 | 0.122 |
| network | 15400 | 0.115 |

### Top broad processes: ripgrep
| name | count | pct | top_family | top_operation |
| --- | --- | --- | --- | --- |
| rustc.exe | 93193 | 34.838 | file | WriteFile |
| link.exe | 32788 | 12.257 | file | WriteFile |
| svchost.exe | 31919 | 11.932 | registry | RegOpenKey |
| VCTIP.EXE | 25858 | 9.667 | registry | RegOpenKey |
| avbench.exe | 23617 | 8.829 | registry | RegOpenKey |
| cargo.exe | 21983 | 8.218 | file | CreateFile |
| WindowsTerminal.exe | 10843 | 4.053 | registry | ReadFile |
| Explorer.EXE | 7780 | 2.908 | registry | RegQueryKey |
| csrss.exe | 2762 | 1.033 | registry | RegOpenKey |
| WmiApSrv.exe | 2406 | 0.899 | registry | RegQueryKey |
| build-script-build.exe | 1755 | 0.656 | registry | RegOpenKey |
| ctfmon.exe | 1576 | 0.589 | registry | RegQueryKey |
| sihost.exe | 1361 | 0.509 | registry | RegQueryKey |
| wmiprvse.exe | 1063 | 0.397 | registry | RegOpenKey |
| Conhost.exe | 909 | 0.34 | registry | RegQueryValue |
| Registry | 704 | 0.263 | file | WriteFile |
| cvtres.exe | 686 | 0.256 | file | RegOpenKey |
| mt.exe | 635 | 0.237 | registry | RegOpenKey |
| services.exe | 613 | 0.229 | registry | RegQueryValue |
| DllHost.exe | 549 | 0.205 | file | Process Profiling |
| rc.exe | 545 | 0.204 | file | RegOpenKey |
| StartMenuExperienceHost.exe | 520 | 0.194 | registry | RegOpenKey |
| LogonUI.exe | 468 | 0.175 | file | CreateFileMapping |
| lsass.exe | 334 | 0.125 | registry | RegOpenKey |
| powershell.exe | 287 | 0.107 | registry | Process Profiling |
| RuntimeBroker.exe | 274 | 0.102 | profiling | Process Profiling |
| fontdrvhost.exe | 174 | 0.065 | profiling | Process Profiling |
| SecurityHealthService.exe | 127 | 0.047 | registry | Process Profiling |
| dwm.exe | 124 | 0.046 | profiling | Process Profiling |
| winlogon.exe | 119 | 0.044 | profiling | Process Profiling |
| TabTip.exe | 115 | 0.043 | profiling | Process Profiling |
| AggregatorHost.exe | 75 | 0.028 | profiling | Process Profiling |
| spoolsv.exe | 74 | 0.028 | profiling | Process Profiling |
| TextInputHost.exe | 74 | 0.028 | profiling | Process Profiling |
| Procmon.exe | 69 | 0.026 | profiling | Process Profiling |
| OpenConsole.exe | 67 | 0.025 | profiling | Process Profiling |
| smartscreen.exe | 60 | 0.022 | profiling | Process Profiling |
| SystemSettingsBroker.exe | 60 | 0.022 | profiling | Process Profiling |
| AUDIODG.EXE | 60 | 0.022 | profiling | Process Profiling |
| smss.exe | 59 | 0.022 | profiling | Process Profiling |

### Top broad processes: Roslyn
| name | count | pct | top_family | top_operation |
| --- | --- | --- | --- | --- |
| dotnet.exe | 10053215 | 74.865 | file | CreateFile |
| VBCSCompiler.exe | 2436411 | 18.144 | file | ReadFile |
| svchost.exe | 669344 | 4.984 | registry | RegOpenKey |
| avbench.exe | 51301 | 0.382 | file | CreateFile |
| Conhost.exe | 40145 | 0.299 | profiling | Process Profiling |
| VsdConfigTool.exe | 37482 | 0.279 | registry | RegQueryKey |
| lsass.exe | 26196 | 0.195 | registry | RegOpenKey |
| wmiprvse.exe | 21126 | 0.157 | registry | RegQueryValue |
| Explorer.EXE | 18453 | 0.137 | registry | RegQueryKey |
| WindowsTerminal.exe | 16719 | 0.125 | registry | ReadFile |
| LogonUI.exe | 5538 | 0.041 | file | CreateFileMapping |
| ctfmon.exe | 5091 | 0.038 | registry | RegQueryKey |
| csrss.exe | 4165 | 0.031 | profiling | Process Profiling |
| getmac.exe | 3168 | 0.024 | registry | RegOpenKey |
| cmd.exe | 2959 | 0.022 | file | RegOpenKey |
| RuntimeBroker.exe | 2907 | 0.022 | profiling | Process Profiling |
| WmiApSrv.exe | 2404 | 0.018 | registry | RegQueryKey |
| DllHost.exe | 2170 | 0.016 | profiling | Process Profiling |
| fontdrvhost.exe | 2155 | 0.016 | profiling | Process Profiling |
| services.exe | 1774 | 0.013 | registry | Process Profiling |
| SecurityHealthService.exe | 1585 | 0.012 | registry | Process Profiling |
| dwm.exe | 1529 | 0.011 | profiling | Process Profiling |
| Registry | 1511 | 0.011 | file | Process Profiling |
| winlogon.exe | 1440 | 0.011 | profiling | Process Profiling |
| sihost.exe | 1083 | 0.008 | profiling | Process Profiling |
| powershell.exe | 958 | 0.007 | profiling | Process Profiling |
| AggregatorHost.exe | 877 | 0.007 | profiling | Process Profiling |
| TabTip.exe | 820 | 0.006 | profiling | Process Profiling |
| TextInputHost.exe | 811 | 0.006 | profiling | Process Profiling |
| StartMenuExperienceHost.exe | 755 | 0.006 | profiling | Process Profiling |
| Procmon.exe | 753 | 0.006 | profiling | Process Profiling |
| spoolsv.exe | 737 | 0.005 | profiling | Process Profiling |
| MemCompression | 736 | 0.005 | profiling | Process Profiling |
| rdpclip.exe | 733 | 0.005 | profiling | Process Profiling |
| OpenConsole.exe | 729 | 0.005 | profiling | Process Profiling |
| WUDFHost.exe | 726 | 0.005 | profiling | Process Profiling |
| LsaIso.exe | 720 | 0.005 | profiling | Process Profiling |
| rdpinput.EXE | 720 | 0.005 | profiling | Process Profiling |
| SystemSettingsBroker.exe | 720 | 0.005 | profiling | Process Profiling |
| smss.exe | 719 | 0.005 | profiling | Process Profiling |
