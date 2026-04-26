from __future__ import annotations

import csv
import json
import re
from collections import Counter, defaultdict
from dataclasses import dataclass, field
from pathlib import Path
from typing import Iterable


ROOT = Path(__file__).resolve().parents[1]
TMP = ROOT / "tmp"
OUT = ROOT / "analysis"
DATA_EXP1 = ROOT / "data" / "exp1"
COMPARE_CSV = DATA_EXP1 / "compare.csv"

TRACE_FILES = {
    "ripgrep": TMP / "ripgrep.CSV",
    "roslyn": TMP / "roslyn.CSV",
}

NOISE_PROCESSES = {"Procmon64.exe", "System", "System Idle Process"}
TOP_N = 40


FILE_READ_OPS = {
    "ReadFile",
    "QueryBasicInformationFile",
    "QueryStandardInformationFile",
    "QueryNameInformationFile",
    "QueryInformationVolume",
    "QueryAttributeTagFile",
    "QuerySecurityFile",
    "QueryDirectory",
    "QueryEAFile",
    "QueryRemoteProtocolInformation",
}
FILE_WRITE_OPS = {
    "WriteFile",
    "SetBasicInformationFile",
    "SetDispositionInformationFile",
    "SetEndOfFileInformationFile",
    "SetAllocationInformationFile",
    "SetRenameInformationFile",
    "SetSecurityFile",
    "SetEAFile",
    "FlushBuffersFile",
}
FILE_OPEN_OPS = {"CreateFile", "QueryOpen", "CloseFile", "CreateFileMapping", "Load Image"}
FILE_METADATA_OPS = {
    "QueryOpen",
    "QueryBasicInformationFile",
    "QueryStandardInformationFile",
    "QueryNameInformationFile",
    "QueryInformationVolume",
    "QueryAttributeTagFile",
    "QuerySecurityFile",
    "QueryDirectory",
    "QueryEAFile",
    "QueryRemoteProtocolInformation",
    "QueryNetworkOpenInformationFile",
    "QueryAllInformationFile",
    "QueryAttributeInformationVolume",
}
EXECUTABLE_EXTENSIONS = {".exe", ".dll", ".sys", ".ocx", ".cpl", ".scr", ".msi", ".ps1", ".js", ".vbs"}
FAILED_PROBE_RESULTS = {"NAME NOT FOUND", "PATH NOT FOUND", "NAME INVALID", "NO SUCH FILE"}
BUILD_SCENARIOS = {
    "ripgrep": ["ripgrep-clean-build", "ripgrep-incremental-build"],
    "roslyn": ["roslyn-clean-build", "roslyn-incremental-build"],
}
PRESSURE_GROUP_WEIGHTS = {
    "open_close": 0.5,
    "metadata_query": 1.0,
    "read": 1.5,
    "write": 6.0,
    "image_load": 8.0,
    "registry": 0.3,
    "network": 2.0,
    "process_thread": 0.2,
    "other": 0.2,
}
TRUST_BUCKET_MULTIPLIERS = {
    "microsoft_os_path": 0.2,
    "microsoft_sdk_programfiles_path": 0.35,
    "other_programfiles_path": 0.8,
    "nuget_user_cache": 1.2,
    "rustup_toolchain_user_cache": 1.5,
    "cargo_crate_user_cache": 1.7,
    "source_tree_non_output": 2.0,
    "other_user_profile_path": 2.0,
    "other_path": 1.5,
    "fresh_build_output": 4.0,
    "registry": 0.5,
    "network": 1.0,
    "none": 1.0,
}
PRESSURE_EXECUTABLE_WRITE_BASE = 30.0
PRESSURE_FAST_IO_DISALLOWED_BONUS = 1.0
PRESSURE_FAILED_PROBE_BONUS = 0.5


@dataclass
class ProcessStats:
    total: int = 0
    operations: Counter[str] = field(default_factory=Counter)
    families: Counter[str] = field(default_factory=Counter)


@dataclass
class ProcessInfo:
    pid: int
    name: str
    parent_pid: int | None = None
    path: str = ""
    command_line: str = ""
    current_directory: str = ""
    first_seen: str = ""
    source: str = ""


@dataclass
class TraceStats:
    name: str
    path: str
    total_events: int = 0
    build_events: int = 0
    first_time: str | None = None
    last_time: str | None = None
    first_second: float | None = None
    last_second: float | None = None
    operations: Counter[str] = field(default_factory=Counter)
    build_operations: Counter[str] = field(default_factory=Counter)
    families: Counter[str] = field(default_factory=Counter)
    build_families: Counter[str] = field(default_factory=Counter)
    results: Counter[str] = field(default_factory=Counter)
    build_results: Counter[str] = field(default_factory=Counter)
    processes: dict[str, ProcessStats] = field(default_factory=lambda: defaultdict(ProcessStats))
    build_processes: dict[str, ProcessStats] = field(default_factory=lambda: defaultdict(ProcessStats))
    unique_paths_by_family: dict[str, set[str]] = field(default_factory=lambda: defaultdict(set))
    unique_build_paths_by_family: dict[str, set[str]] = field(default_factory=lambda: defaultdict(set))
    path_roots: Counter[str] = field(default_factory=Counter)
    build_path_roots: Counter[str] = field(default_factory=Counter)
    directories: Counter[str] = field(default_factory=Counter)
    write_directories: Counter[str] = field(default_factory=Counter)
    read_directories: Counter[str] = field(default_factory=Counter)
    extensions: Counter[str] = field(default_factory=Counter)
    write_extensions: Counter[str] = field(default_factory=Counter)
    read_extensions: Counter[str] = field(default_factory=Counter)
    hot_paths: Counter[str] = field(default_factory=Counter)
    hot_write_paths: Counter[str] = field(default_factory=Counter)
    hot_read_paths: Counter[str] = field(default_factory=Counter)
    registry_roots: Counter[str] = field(default_factory=Counter)
    registry_paths: Counter[str] = field(default_factory=Counter)
    network_paths: Counter[str] = field(default_factory=Counter)
    detail_keys: Counter[str] = field(default_factory=Counter)
    bytes_read: int = 0
    bytes_written: int = 0
    core_events: int = 0
    core_operations: Counter[str] = field(default_factory=Counter)
    core_families: Counter[str] = field(default_factory=Counter)
    core_results: Counter[str] = field(default_factory=Counter)
    core_processes: dict[str, ProcessStats] = field(default_factory=lambda: defaultdict(ProcessStats))
    unique_core_paths_by_family: dict[str, set[str]] = field(default_factory=lambda: defaultdict(set))
    core_path_roots: Counter[str] = field(default_factory=Counter)
    core_directories: Counter[str] = field(default_factory=Counter)
    core_write_directories: Counter[str] = field(default_factory=Counter)
    core_read_directories: Counter[str] = field(default_factory=Counter)
    core_extensions: Counter[str] = field(default_factory=Counter)
    core_write_extensions: Counter[str] = field(default_factory=Counter)
    core_read_extensions: Counter[str] = field(default_factory=Counter)
    core_hot_paths: Counter[str] = field(default_factory=Counter)
    core_hot_write_paths: Counter[str] = field(default_factory=Counter)
    core_hot_read_paths: Counter[str] = field(default_factory=Counter)
    core_registry_roots: Counter[str] = field(default_factory=Counter)
    core_registry_paths: Counter[str] = field(default_factory=Counter)
    core_network_paths: Counter[str] = field(default_factory=Counter)
    core_bytes_read: int = 0
    core_bytes_written: int = 0
    core_pids: set[int] = field(default_factory=set)
    core_process_info: dict[int, ProcessInfo] = field(default_factory=dict)
    core_file_groups: Counter[str] = field(default_factory=Counter)
    core_image_load_paths: Counter[str] = field(default_factory=Counter)
    core_executable_write_paths: Counter[str] = field(default_factory=Counter)
    core_executable_paths: set[str] = field(default_factory=set)
    core_new_write_paths: set[str] = field(default_factory=set)
    core_failed_probe_paths: Counter[str] = field(default_factory=Counter)
    core_failed_probe_details: Counter[str] = field(default_factory=Counter)
    core_trust_buckets: Counter[str] = field(default_factory=Counter)
    core_executable_trust_buckets: Counter[str] = field(default_factory=Counter)
    core_image_load_trust_buckets: Counter[str] = field(default_factory=Counter)
    core_executable_write_trust_buckets: Counter[str] = field(default_factory=Counter)
    core_phase_events: Counter[str] = field(default_factory=Counter)
    core_phase_file_groups: dict[str, Counter[str]] = field(default_factory=lambda: defaultdict(Counter))
    core_phase_operations: dict[str, Counter[str]] = field(default_factory=lambda: defaultdict(Counter))
    core_phase_processes: dict[str, Counter[str]] = field(default_factory=lambda: defaultdict(Counter))
    core_second_buckets: dict[int, Counter[str]] = field(default_factory=lambda: defaultdict(Counter))
    core_toolchain_hints: set[str] = field(default_factory=set)
    core_repro_hints: Counter[str] = field(default_factory=Counter)
    core_weighted_pressure_total: float = 0.0
    core_weighted_pressure_by_group: Counter[str] = field(default_factory=Counter)
    core_weighted_pressure_by_trust: Counter[str] = field(default_factory=Counter)
    core_weighted_pressure_by_phase: Counter[str] = field(default_factory=Counter)
    core_weighted_pressure_by_process: Counter[str] = field(default_factory=Counter)
    core_weighted_pressure_by_operation: Counter[str] = field(default_factory=Counter)


def main() -> None:
    summaries = {}
    for name, path in TRACE_FILES.items():
        if not path.exists():
            raise FileNotFoundError(path)
        print(f"Analyzing {name}: {path}")
        stats = analyze_trace(name, path)
        summaries[name] = compact_stats(stats)

    benchmark_results = load_benchmark_results()
    for name, summary in summaries.items():
        summary["reproducibility"] = build_reproducibility(summary)
        summary["benchmark_results"] = benchmark_results.get(name, {})
    benchmark_correlation = build_benchmark_correlation(benchmark_results)

    (OUT / "procmon-summary.json").write_text(
        json.dumps(
            {
                "traces": summaries,
                "benchmark_correlation": benchmark_correlation,
                "pressure_model": pressure_model_definition(),
            },
            indent=2,
            ensure_ascii=False,
        ),
        encoding="utf-8",
    )
    write_markdown(summaries)
    write_profile_markdown(summaries, benchmark_correlation)
    print("Wrote analysis/procmon-summary.json")
    print("Wrote analysis/compilation-procmon-analysis.md")
    print("Wrote analysis/workload-profile-pipeline.md")


def analyze_trace(name: str, path: Path) -> TraceStats:
    stats = TraceStats(name=name, path=str(path.relative_to(ROOT)))
    with path.open("r", encoding="utf-8-sig", newline="") as handle:
        reader = csv.DictReader(handle)
        for row in reader:
            process = row["Process Name"]
            pid = parse_int(row["PID"])
            operation = row["Operation"]
            result = row["Result"]
            event_path = row["Path"]
            detail = row["Detail"]
            family = classify_family(operation, event_path)
            is_build = process not in NOISE_PROCESSES
            update_core_process_tree(stats, row)
            is_core = pid in stats.core_pids
            event_second = parse_time_of_day(row["Time of Day"])

            stats.total_events += 1
            stats.operations[operation] += 1
            stats.families[family] += 1
            stats.results[result] += 1
            stats.first_time = stats.first_time or row["Time of Day"]
            stats.last_time = row["Time of Day"]
            stats.first_second = event_second if stats.first_second is None else min(stats.first_second, event_second)
            stats.last_second = event_second if stats.last_second is None else max(stats.last_second, event_second)

            update_process(stats.processes[process], operation, family)
            update_path_stats(stats, operation, family, event_path, detail, is_build=False)

            if is_build:
                stats.build_events += 1
                stats.build_operations[operation] += 1
                stats.build_families[family] += 1
                stats.build_results[result] += 1
                update_process(stats.build_processes[process], operation, family)
                update_path_stats(stats, operation, family, event_path, detail, is_build=True)

            if is_core:
                stats.core_events += 1
                stats.core_operations[operation] += 1
                stats.core_families[family] += 1
                stats.core_results[result] += 1
                update_process(stats.core_processes[process], operation, family)
                update_core_path_stats(stats, operation, family, event_path, detail)
                update_core_profile_stats(stats, row, family, event_second)

    return stats


def update_process(process_stats: ProcessStats, operation: str, family: str) -> None:
    process_stats.total += 1
    process_stats.operations[operation] += 1
    process_stats.families[family] += 1


def update_core_process_tree(stats: TraceStats, row: dict[str, str]) -> None:
    operation = row["Operation"]
    pid = parse_int(row["PID"])
    process = row["Process Name"]
    detail = row["Detail"]
    path = row["Path"]
    time_of_day = row["Time of Day"]

    if operation == "Process Create":
        child_pid = parse_detail_int(detail, "PID")
        if child_pid is None:
            return
        command_line = detail_value(detail, "Command line")
        if is_benchmark_root_create(stats.name, path, command_line):
            add_core_process(
                stats,
                child_pid,
                process_name_from_path(path),
                parent_pid=pid,
                path=path,
                command_line=command_line,
                first_seen=time_of_day,
                source="benchmark root",
            )
        elif pid in stats.core_pids:
            add_core_process(
                stats,
                child_pid,
                process_name_from_path(path),
                parent_pid=pid,
                path=path,
                command_line=command_line,
                first_seen=time_of_day,
                source=f"child of {process} ({pid})",
            )
        return

    if operation == "Process Start":
        parent_pid = parse_detail_int(detail, "Parent PID")
        if parent_pid in stats.core_pids or pid in stats.core_pids:
            add_core_process(
                stats,
                pid,
                process,
                parent_pid=parent_pid,
                path=path,
                command_line=detail_value(detail, "Command line"),
                current_directory=detail_value(detail, "Current directory"),
                first_seen=time_of_day,
                source=f"started by core parent {parent_pid}",
            )


def add_core_process(
    stats: TraceStats,
    pid: int,
    name: str,
    *,
    parent_pid: int | None,
    path: str,
    command_line: str,
    first_seen: str,
    source: str,
    current_directory: str = "",
) -> None:
    stats.core_pids.add(pid)
    existing = stats.core_process_info.get(pid)
    if existing is None:
        stats.core_process_info[pid] = ProcessInfo(
            pid=pid,
            name=name,
            parent_pid=parent_pid,
            path=path,
            command_line=command_line,
            current_directory=current_directory,
            first_seen=first_seen,
            source=source,
        )
        return

    if not existing.command_line and command_line:
        existing.command_line = command_line
    if not existing.current_directory and current_directory:
        existing.current_directory = current_directory
    if not existing.path and path:
        existing.path = path


def is_benchmark_root_create(trace_name: str, path: str, command_line: str) -> bool:
    path_lower = path.lower()
    command_lower = command_line.lower()
    return path_lower.endswith("\\avbench.exe") and f"-w {trace_name.lower()}" in command_lower


def update_path_stats(
    stats: TraceStats,
    operation: str,
    family: str,
    event_path: str,
    detail: str,
    *,
    is_build: bool,
) -> None:
    if not event_path:
        return

    path_root = classify_path_root(event_path)
    target_unique = stats.unique_build_paths_by_family if is_build else stats.unique_paths_by_family
    target_path_roots = stats.build_path_roots if is_build else stats.path_roots
    target_unique[family].add(event_path)
    target_path_roots[path_root] += 1

    if not is_build:
        return

    stats.hot_paths[event_path] += 1
    if family == "registry":
        stats.registry_roots[registry_root(event_path)] += 1
        stats.registry_paths[event_path] += 1
        return
    if family == "network":
        stats.network_paths[event_path] += 1
        return
    if family != "file":
        return

    directory = parent_dir(event_path)
    extension = file_extension(event_path)
    stats.directories[directory] += 1
    stats.extensions[extension] += 1

    length = detail_length(detail)
    if operation in FILE_READ_OPS:
        stats.read_directories[directory] += 1
        stats.read_extensions[extension] += 1
        stats.hot_read_paths[event_path] += 1
        stats.bytes_read += length
    elif operation in FILE_WRITE_OPS:
        stats.write_directories[directory] += 1
        stats.write_extensions[extension] += 1
        stats.hot_write_paths[event_path] += 1
        stats.bytes_written += length

    for key in detail_keys(detail):
        stats.detail_keys[key] += 1


def update_core_path_stats(stats: TraceStats, operation: str, family: str, event_path: str, detail: str) -> None:
    if not event_path:
        return

    stats.unique_core_paths_by_family[family].add(event_path)
    stats.core_path_roots[classify_path_root(event_path)] += 1
    stats.core_hot_paths[event_path] += 1

    if family == "registry":
        stats.core_registry_roots[registry_root(event_path)] += 1
        stats.core_registry_paths[event_path] += 1
        return
    if family == "network":
        stats.core_network_paths[event_path] += 1
        return
    if family != "file":
        return

    directory = parent_dir(event_path)
    extension = file_extension(event_path)
    stats.core_directories[directory] += 1
    stats.core_extensions[extension] += 1

    length = detail_length(detail)
    if operation in FILE_READ_OPS:
        stats.core_read_directories[directory] += 1
        stats.core_read_extensions[extension] += 1
        stats.core_hot_read_paths[event_path] += 1
        stats.core_bytes_read += length
    elif operation in FILE_WRITE_OPS:
        stats.core_write_directories[directory] += 1
        stats.core_write_extensions[extension] += 1
        stats.core_hot_write_paths[event_path] += 1
        stats.core_bytes_written += length


def update_core_profile_stats(
    stats: TraceStats,
    row: dict[str, str],
    family: str,
    event_second: float,
) -> None:
    operation = row["Operation"]
    process = row["Process Name"]
    event_path = row["Path"]
    result = row["Result"]
    extension = file_extension(event_path) if event_path else "(none)"
    group = file_operation_group(operation, family)
    phase = infer_phase(stats.name, process, operation, event_path)
    trust_bucket = trust_reputation_bucket(event_path)
    pressure_score = weighted_pressure_score(group, trust_bucket, extension, operation, result)

    stats.core_file_groups[group] += 1
    stats.core_phase_events[phase] += 1
    stats.core_phase_file_groups[phase][group] += 1
    stats.core_phase_operations[phase][operation] += 1
    stats.core_phase_processes[phase][process] += 1
    stats.core_weighted_pressure_total += pressure_score
    stats.core_weighted_pressure_by_group[group] += pressure_score
    stats.core_weighted_pressure_by_trust[trust_bucket] += pressure_score
    stats.core_weighted_pressure_by_phase[phase] += pressure_score
    stats.core_weighted_pressure_by_process[process] += pressure_score
    stats.core_weighted_pressure_by_operation[operation] += pressure_score

    bucket = int(event_second - (stats.first_second or event_second))
    stats.core_second_buckets[bucket]["events"] += 1
    stats.core_second_buckets[bucket][f"group:{group}"] += 1
    stats.core_second_buckets[bucket][f"family:{family}"] += 1

    if event_path and family == "file":
        stats.core_trust_buckets[trust_bucket] += 1

    if operation == "Load Image" and event_path:
        stats.core_image_load_paths[event_path] += 1
        stats.core_executable_paths.add(event_path)
        stats.core_image_load_trust_buckets[trust_bucket] += 1

    if extension in EXECUTABLE_EXTENSIONS and event_path:
        stats.core_executable_paths.add(event_path)
        stats.core_executable_trust_buckets[trust_bucket] += 1

    if operation in FILE_WRITE_OPS and event_path:
        stats.core_new_write_paths.add(event_path)
        if extension in EXECUTABLE_EXTENSIONS:
            stats.core_executable_write_paths[event_path] += 1
            stats.core_executable_write_trust_buckets[trust_bucket] += 1

    if result in FAILED_PROBE_RESULTS and event_path:
        stats.core_failed_probe_paths[event_path] += 1
        stats.core_failed_probe_details[f"{result} | {operation} | {event_path}"] += 1

    collect_repro_hints(stats, event_path, operation)


def collect_repro_hints(stats: TraceStats, path: str, operation: str) -> None:
    path_lower = path.lower()
    for match in re.finditer(r"\\.rustup\\toolchains\\([^\\]+)", path, re.IGNORECASE):
        stats.core_toolchain_hints.add(f"Rust toolchain: {match.group(1)}")
    for match in re.finditer(r"\\dotnet\\sdk\\([^\\]+)", path, re.IGNORECASE):
        stats.core_toolchain_hints.add(f".NET SDK: {match.group(1)}")
    for match in re.finditer(r"\\microsoft visual studio\\([^\\]+)\\", path, re.IGNORECASE):
        stats.core_toolchain_hints.add(f"Visual Studio toolchain root: {match.group(1)}")
    if "\\.nuget\\" in path_lower or "\\v3-cache\\" in path_lower:
        stats.core_repro_hints["nuget_cache_touched"] += 1
    if "telemetrystorageservice" in path_lower:
        stats.core_repro_hints["dotnet_telemetry_storage_touched"] += 1
    if operation.startswith(("TCP ", "UDP ")):
        stats.core_repro_hints["network_observed"] += 1


def weighted_pressure_score(group: str, trust_bucket: str, extension: str, operation: str, result: str) -> float:
    base = PRESSURE_GROUP_WEIGHTS.get(group, PRESSURE_GROUP_WEIGHTS["other"])
    if operation in FILE_WRITE_OPS and extension in EXECUTABLE_EXTENSIONS:
        base = PRESSURE_EXECUTABLE_WRITE_BASE

    multiplier = TRUST_BUCKET_MULTIPLIERS.get(trust_bucket, 1.0)
    score = base * multiplier
    if result == "FAST IO DISALLOWED":
        score += PRESSURE_FAST_IO_DISALLOWED_BONUS * multiplier
    if result in FAILED_PROBE_RESULTS:
        score += PRESSURE_FAILED_PROBE_BONUS * multiplier
    return score


def pressure_model_definition() -> dict[str, object]:
    return {
        "description": "Heuristic AV pressure score. It is not measured latency. It estimates likely AV cost by weighting operation type and reducing likely trusted Microsoft/SDK paths while increasing fresh outputs and user/package/toolchain cache paths.",
        "operation_group_weights": PRESSURE_GROUP_WEIGHTS,
        "trust_bucket_multipliers": TRUST_BUCKET_MULTIPLIERS,
        "executable_write_base_weight": PRESSURE_EXECUTABLE_WRITE_BASE,
        "fast_io_disallowed_bonus": PRESSURE_FAST_IO_DISALLOWED_BONUS,
        "failed_probe_bonus": PRESSURE_FAILED_PROBE_BONUS,
    }


def trust_reputation_bucket(path: str) -> str:
    path_lower = path.lower()
    if not path:
        return "none"
    if path_lower.startswith(("hklm\\", "hkcu\\", "hkcr\\", "hku\\")):
        return "registry"
    if path_lower.startswith(("tcp ", "udp ")):
        return "network"
    if "\\target\\release\\" in path_lower or "\\target\\debug\\" in path_lower or "\\artifacts\\obj\\" in path_lower or "\\artifacts\\bin\\" in path_lower:
        return "fresh_build_output"
    if path_lower.startswith("c:\\windows\\"):
        return "microsoft_os_path"
    if (
        path_lower.startswith("c:\\program files\\dotnet\\")
        or path_lower.startswith("c:\\program files (x86)\\reference assemblies\\")
        or path_lower.startswith("c:\\program files (x86)\\windows kits\\")
        or path_lower.startswith("c:\\program files (x86)\\microsoft visual studio\\")
        or path_lower.startswith("c:\\program files (x86)\\microsoft sdks\\")
        or path_lower.startswith("c:\\programdata\\microsoft\\")
    ):
        return "microsoft_sdk_programfiles_path"
    if "\\.rustup\\" in path_lower:
        return "rustup_toolchain_user_cache"
    if "\\.cargo\\" in path_lower:
        return "cargo_crate_user_cache"
    if "\\.nuget\\" in path_lower or "\\appdata\\local\\nuget\\" in path_lower or "\\v3-cache\\" in path_lower:
        return "nuget_user_cache"
    if path_lower.startswith("c:\\bench\\"):
        return "source_tree_non_output"
    if path_lower.startswith("c:\\users\\"):
        return "other_user_profile_path"
    if path_lower.startswith("c:\\program files"):
        return "other_programfiles_path"
    return "other_path"


def file_operation_group(operation: str, family: str) -> str:
    if operation == "Load Image":
        return "image_load"
    if operation in FILE_WRITE_OPS:
        return "write"
    if operation == "ReadFile":
        return "read"
    if operation in FILE_METADATA_OPS:
        return "metadata_query"
    if operation in FILE_OPEN_OPS or operation.startswith("IRP_MJ_") or operation.startswith("FASTIO_"):
        return "open_close"
    if family == "registry":
        return "registry"
    if family == "network":
        return "network"
    if family == "process/thread":
        return "process_thread"
    return "other"


def infer_phase(trace_name: str, process: str, operation: str, path: str) -> str:
    process_lower = process.lower()
    path_lower = path.lower()

    if trace_name == "ripgrep":
        if process_lower in {"link.exe", "mt.exe", "rc.exe", "cvtres.exe", "vctip.exe"}:
            return "link/resources"
        if process_lower == "build-script-build.exe":
            return "build scripts"
        if process_lower == "rustc.exe":
            return "rustc compile"
        if process_lower == "cargo.exe":
            return "cargo graph/setup"
        if process_lower in {"avbench.exe", "conhost.exe"}:
            return "harness/console"
        return "other core child"

    if process_lower == "vbcscompiler.exe":
        return "compiler server compile"
    if operation.startswith(("TCP ", "UDP ")):
        return "restore/cache touch"
    if operation in FILE_WRITE_OPS and (
        "\\artifacts\\obj\\" in path_lower
        or "\\artifacts\\bin\\" in path_lower
        or "\\bin\\" in path_lower
        or path_lower.endswith((".dll", ".exe", ".resources", ".xml", ".cache", ".tmp"))
    ):
        return "output/write phase"
    if (
        "\\.nuget\\" in path_lower
        or "\\nuget\\" in path_lower
        or "\\packages\\" in path_lower
        or "\\v3-cache\\" in path_lower
        or path_lower.endswith(".sha512")
    ):
        return "restore/cache touch"
    if process_lower == "dotnet.exe" and (
        path_lower.endswith((".csproj", ".sln", ".slnx", ".props", ".targets", ".editorconfig", ".globalconfig"))
        or "\\sdk\\" in path_lower
        or "\\reference assemblies\\" in path_lower
        or operation in FILE_METADATA_OPS
    ):
        return "msbuild evaluation"
    if process_lower == "dotnet.exe":
        return "msbuild evaluation"
    if process_lower in {"avbench.exe", "conhost.exe"}:
        return "harness/console"
    return "other build tasks"


def classify_family(operation: str, path: str) -> str:
    if operation.startswith("Reg") or path.startswith(("HKLM\\", "HKCU\\", "HKCR\\", "HKU\\")):
        return "registry"
    if operation.startswith(("TCP ", "UDP ")) or path.startswith(("TCP ", "UDP ")):
        return "network"
    if operation in FILE_READ_OPS or operation in FILE_WRITE_OPS or operation in FILE_OPEN_OPS:
        return "file"
    if operation.startswith("IRP_MJ_") or operation.startswith("FASTIO_"):
        return "file"
    if operation in {
        "Process Create",
        "Process Start",
        "Process Exit",
        "Thread Create",
        "Thread Exit",
    }:
        return "process/thread"
    if operation in {"Profiling", "Process Profiling"}:
        return "profiling"
    return "other"


def classify_path_root(path: str) -> str:
    if path.startswith(("HKLM\\", "HKCU\\", "HKCR\\", "HKU\\")):
        return registry_root(path)
    if path.startswith(("TCP ", "UDP ")):
        return path.split(" ", 1)[0]
    if path.startswith("\\\\"):
        parts = path.split("\\")
        return "\\\\" + "\\".join(parts[2:4])
    if len(path) >= 3 and path[1:3] == ":\\":
        parts = path.split("\\")
        if len(parts) >= 3:
            return "\\".join(parts[:3])
        return parts[0]
    return path.split("\\", 1)[0] if "\\" in path else path


def registry_root(path: str) -> str:
    parts = path.split("\\")
    return "\\".join(parts[:3]) if len(parts) >= 3 else path


def parent_dir(path: str) -> str:
    if "\\" not in path:
        return "(no directory)"
    return path.rsplit("\\", 1)[0]


def file_extension(path: str) -> str:
    name = path.rsplit("\\", 1)[-1]
    if "." not in name or name.endswith("."):
        return "(none)"
    return "." + name.rsplit(".", 1)[-1].lower()


def process_name_from_path(path: str) -> str:
    if not path:
        return ""
    return path.rsplit("\\", 1)[-1]


def parse_int(value: str) -> int:
    try:
        return int(value)
    except ValueError:
        return -1


def parse_time_of_day(value: str) -> float:
    match = re.match(
        r"(?P<hour>\d+):(?P<minute>\d+):(?P<second>\d+)(?:\.(?P<fraction>\d+))?\s*(?P<ampm>AM|PM)",
        value,
        re.IGNORECASE,
    )
    if not match:
        return 0.0
    hour = int(match.group("hour"))
    minute = int(match.group("minute"))
    second = int(match.group("second"))
    fraction = float(f"0.{match.group('fraction') or '0'}")
    ampm = match.group("ampm").upper()
    if ampm == "PM" and hour != 12:
        hour += 12
    if ampm == "AM" and hour == 12:
        hour = 0
    return hour * 3600 + minute * 60 + second + fraction


def parse_detail_int(detail: str, key: str) -> int | None:
    value = detail_value(detail, key)
    if not value:
        return None
    match = re.match(r"\d+", value.replace(",", ""))
    if not match:
        return None
    return int(match.group(0))


def detail_value(detail: str, key: str) -> str:
    prefix = f"{key}:"
    start = detail.find(prefix)
    if start == -1:
        return ""
    start += len(prefix)
    end = len(detail)
    for separator in (", Current directory:", ", Environment:", ", Command line:", ", Parent PID:", ", PID:"):
        if separator.startswith(f", {key}:"):
            continue
        idx = detail.find(separator, start)
        if idx != -1:
            end = min(end, idx)
    return detail[start:end].strip()


def detail_length(detail: str) -> int:
    match = re.search(r"\bLength:\s*([0-9,]+)", detail)
    if not match:
        return 0
    return int(match.group(1).replace(",", ""))


def float_value(value: object) -> float:
    if value is None or value == "":
        return 0.0
    return float(str(value).replace(",", ""))


def mean(values: list[float]) -> float:
    return sum(values) / len(values) if values else 0.0


def median(values: list[float]) -> float:
    if not values:
        return 0.0
    ordered = sorted(values)
    mid = len(ordered) // 2
    if len(ordered) % 2:
        return ordered[mid]
    return (ordered[mid - 1] + ordered[mid]) / 2


def pearson(xs: list[float], ys: list[float]) -> float:
    if len(xs) != len(ys) or len(xs) < 2:
        return 0.0
    x_mean = mean(xs)
    y_mean = mean(ys)
    numerator = sum((x - x_mean) * (y - y_mean) for x, y in zip(xs, ys))
    x_den = sum((x - x_mean) ** 2 for x in xs) ** 0.5
    y_den = sum((y - y_mean) ** 2 for y in ys) ** 0.5
    if not x_den or not y_den:
        return 0.0
    return numerator / (x_den * y_den)


def detail_keys(detail: str) -> Iterable[str]:
    for part in detail.split(","):
        if ":" in part:
            yield part.split(":", 1)[0].strip()


def compact_stats(stats: TraceStats) -> dict[str, object]:
    return {
        "trace": stats.name,
        "path": stats.path,
        "total_events": stats.total_events,
        "build_events_excluding_procmon_system": stats.build_events,
        "first_time": stats.first_time,
        "last_time": stats.last_time,
        "operation_families": counter_table(stats.families),
        "build_operation_families": counter_table(stats.build_families),
        "top_operations": counter_table(stats.operations),
        "top_build_operations": counter_table(stats.build_operations),
        "top_results": counter_table(stats.results),
        "top_build_results": counter_table(stats.build_results),
        "top_processes": process_table(stats.processes),
        "top_build_processes": process_table(stats.build_processes),
        "unique_paths_by_family": {key: len(value) for key, value in sorted(stats.unique_paths_by_family.items())},
        "unique_build_paths_by_family": {key: len(value) for key, value in sorted(stats.unique_build_paths_by_family.items())},
        "path_roots": counter_table(stats.path_roots),
        "build_path_roots": counter_table(stats.build_path_roots),
        "top_directories": counter_table(stats.directories),
        "top_read_directories": counter_table(stats.read_directories),
        "top_write_directories": counter_table(stats.write_directories),
        "top_extensions": counter_table(stats.extensions),
        "top_read_extensions": counter_table(stats.read_extensions),
        "top_write_extensions": counter_table(stats.write_extensions),
        "top_hot_paths": counter_table(stats.hot_paths),
        "top_read_paths": counter_table(stats.hot_read_paths),
        "top_write_paths": counter_table(stats.hot_write_paths),
        "top_registry_roots": counter_table(stats.registry_roots),
        "top_registry_paths": counter_table(stats.registry_paths),
        "top_network_paths": counter_table(stats.network_paths),
        "detail_keys": counter_table(stats.detail_keys),
        "observed_read_length_sum": stats.bytes_read,
        "observed_write_length_sum": stats.bytes_written,
        "core_filter": "dynamic descendant tree rooted at the avbench process for this workload",
        "core_process_count": len(stats.core_process_info),
        "core_process_tree": process_info_table(stats.core_process_info),
        "core_events": stats.core_events,
        "core_duration_seconds": round(trace_duration_seconds(stats), 3),
        "core_per_second_rates": per_second_rates(stats),
        "core_file_operation_groups": counter_table(stats.core_file_groups),
        "core_phase_summary": phase_summary_table(stats),
        "core_process_operation_matrix": process_operation_matrix(stats.core_processes),
        "core_av_relevance_metrics": av_relevance_metrics(stats),
        "core_weighted_pressure": weighted_pressure_summary(stats),
        "core_weighted_pressure_by_group": score_table(stats.core_weighted_pressure_by_group),
        "core_weighted_pressure_by_trust": score_table(stats.core_weighted_pressure_by_trust),
        "core_weighted_pressure_by_phase": score_table(stats.core_weighted_pressure_by_phase),
        "core_weighted_pressure_by_process": score_table(stats.core_weighted_pressure_by_process),
        "core_weighted_pressure_by_operation": score_table(stats.core_weighted_pressure_by_operation),
        "core_trust_reputation_buckets": counter_table(stats.core_trust_buckets),
        "core_executable_trust_buckets": counter_table(stats.core_executable_trust_buckets),
        "core_image_load_trust_buckets": counter_table(stats.core_image_load_trust_buckets),
        "core_executable_write_trust_buckets": counter_table(stats.core_executable_write_trust_buckets),
        "core_operation_families": counter_table(stats.core_families),
        "top_core_operations": counter_table(stats.core_operations),
        "top_core_results": counter_table(stats.core_results),
        "top_core_processes": process_table(stats.core_processes),
        "unique_core_paths_by_family": {key: len(value) for key, value in sorted(stats.unique_core_paths_by_family.items())},
        "core_path_roots": counter_table(stats.core_path_roots),
        "top_core_directories": counter_table(stats.core_directories),
        "top_core_read_directories": counter_table(stats.core_read_directories),
        "top_core_write_directories": counter_table(stats.core_write_directories),
        "top_core_extensions": counter_table(stats.core_extensions),
        "top_core_read_extensions": counter_table(stats.core_read_extensions),
        "top_core_write_extensions": counter_table(stats.core_write_extensions),
        "top_core_hot_paths": counter_table(stats.core_hot_paths),
        "top_core_read_paths": counter_table(stats.core_hot_read_paths),
        "top_core_write_paths": counter_table(stats.core_hot_write_paths),
        "top_core_image_loads": counter_table(stats.core_image_load_paths),
        "top_core_executable_writes": counter_table(stats.core_executable_write_paths),
        "top_core_failed_probes": counter_table(stats.core_failed_probe_paths),
        "top_core_failed_probe_details": counter_table(stats.core_failed_probe_details),
        "top_core_registry_roots": counter_table(stats.core_registry_roots),
        "top_core_registry_paths": counter_table(stats.core_registry_paths),
        "top_core_network_paths": counter_table(stats.core_network_paths),
        "core_observed_read_length_sum": stats.core_bytes_read,
        "core_observed_write_length_sum": stats.core_bytes_written,
        "core_toolchain_hints": sorted(stats.core_toolchain_hints),
        "core_repro_hints": counter_table(stats.core_repro_hints),
        "core_second_buckets": second_bucket_table(stats),
    }


def counter_table(counter: Counter[str], n: int = TOP_N) -> list[dict[str, object]]:
    total = sum(counter.values())
    return [
        {"name": key, "count": value, "pct": round(value / total * 100, 3) if total else 0}
        for key, value in counter.most_common(n)
    ]


def process_table(processes: dict[str, ProcessStats], n: int = TOP_N) -> list[dict[str, object]]:
    total = sum(process.total for process in processes.values())
    rows = []
    for name, process in sorted(processes.items(), key=lambda item: item[1].total, reverse=True)[:n]:
        rows.append(
            {
                "name": name,
                "count": process.total,
                "pct": round(process.total / total * 100, 3) if total else 0,
                "top_family": process.families.most_common(1)[0][0] if process.families else "",
                "top_operation": process.operations.most_common(1)[0][0] if process.operations else "",
            }
        )
    return rows


def process_info_table(processes: dict[int, ProcessInfo]) -> list[dict[str, object]]:
    rows = []
    for process in sorted(processes.values(), key=lambda item: (item.first_seen, item.pid)):
        rows.append(
            {
                "pid": process.pid,
                "name": process.name,
                "parent_pid": process.parent_pid if process.parent_pid is not None else "",
                "first_seen": process.first_seen,
                "source": process.source,
                "current_directory": process.current_directory,
                "command_line": process.command_line,
            }
        )
    return rows


def trace_duration_seconds(stats: TraceStats) -> float:
    if stats.first_second is None or stats.last_second is None:
        return 0.0
    duration = stats.last_second - stats.first_second
    if duration < 0:
        duration += 24 * 60 * 60
    return max(duration, 0.001)


def per_second_rates(stats: TraceStats) -> dict[str, object]:
    duration = trace_duration_seconds(stats)
    rates = {
        "duration_seconds": round(duration, 3),
        "events_per_second": round(stats.core_events / duration, 3),
        "file_events_per_second": round(stats.core_families["file"] / duration, 3),
        "registry_events_per_second": round(stats.core_families["registry"] / duration, 3),
        "network_events_per_second": round(stats.core_families["network"] / duration, 3),
    }
    for group in ["open_close", "metadata_query", "read", "write", "image_load"]:
        rates[f"{group}_per_second"] = round(stats.core_file_groups[group] / duration, 3)
    return rates


def av_relevance_metrics(stats: TraceStats) -> dict[str, object]:
    metadata_queries = stats.core_file_groups["metadata_query"]
    image_loads = stats.core_file_groups["image_load"]
    writes = stats.core_file_groups["write"]
    failed_probes = sum(stats.core_failed_probe_paths.values())
    return {
        "unique_file_paths": len(stats.unique_core_paths_by_family["file"]),
        "unique_executable_like_paths": len(stats.core_executable_paths),
        "new_write_paths": len(stats.core_new_write_paths),
        "executable_like_write_paths": len(stats.core_executable_write_paths),
        "metadata_query_events": metadata_queries,
        "image_load_events": image_loads,
        "write_events": writes,
        "fast_io_disallowed_events": stats.core_results["FAST IO DISALLOWED"],
        "failed_probe_events": failed_probes,
        "network_events": stats.core_families["network"],
        "registry_query_events": sum(
            count for op, count in stats.core_operations.items() if op in {"RegOpenKey", "RegQueryValue", "RegQueryKey"}
        ),
        "observed_read_mb": round(stats.core_bytes_read / 1024 / 1024, 1),
        "observed_write_mb": round(stats.core_bytes_written / 1024 / 1024, 1),
    }


def weighted_pressure_summary(stats: TraceStats) -> dict[str, object]:
    duration = trace_duration_seconds(stats)
    return {
        "score": round(stats.core_weighted_pressure_total, 1),
        "score_per_second": round(stats.core_weighted_pressure_total / duration, 3),
        "score_per_1000_events": round(stats.core_weighted_pressure_total / max(stats.core_events, 1) * 1000, 3),
        "model": "operation weight x trust/reputation multiplier, plus small result bonuses",
    }


def score_table(counter: Counter[str], n: int = TOP_N) -> list[dict[str, object]]:
    total = sum(counter.values())
    rows = []
    for key, value in counter.most_common(n):
        rows.append(
            {
                "name": key,
                "score": round(value, 1),
                "pct": round(value / total * 100, 3) if total else 0,
            }
        )
    return rows


def phase_summary_table(stats: TraceStats) -> list[dict[str, object]]:
    rows = []
    total = sum(stats.core_phase_events.values())
    for phase, count in stats.core_phase_events.most_common():
        file_groups = stats.core_phase_file_groups[phase]
        top_process = stats.core_phase_processes[phase].most_common(1)
        top_operation = stats.core_phase_operations[phase].most_common(1)
        rows.append(
            {
                "phase": phase,
                "events": count,
                "pct": round(count / total * 100, 3) if total else 0,
                "metadata_query": file_groups["metadata_query"],
                "read": file_groups["read"],
                "write": file_groups["write"],
                "image_load": file_groups["image_load"],
                "registry": file_groups["registry"],
                "network": file_groups["network"],
                "top_process": top_process[0][0] if top_process else "",
                "top_operation": top_operation[0][0] if top_operation else "",
            }
        )
    return rows


def process_operation_matrix(processes: dict[str, ProcessStats], n: int = 20) -> list[dict[str, object]]:
    rows = []
    operations = [
        "CreateFile",
        "QueryOpen",
        "QueryNetworkOpenInformationFile",
        "ReadFile",
        "WriteFile",
        "Load Image",
        "RegOpenKey",
        "RegQueryValue",
        "Process Create",
        "Thread Create",
    ]
    for name, process in sorted(processes.items(), key=lambda item: item[1].total, reverse=True)[:n]:
        row: dict[str, object] = {"process": name, "events": process.total}
        for operation in operations:
            row[operation] = process.operations[operation]
        rows.append(row)
    return rows


def second_bucket_table(stats: TraceStats, n: int = TOP_N) -> list[dict[str, object]]:
    rows = []
    for second, bucket in sorted(stats.core_second_buckets.items(), key=lambda item: item[1]["events"], reverse=True)[:n]:
        rows.append(
            {
                "second": second,
                "events": bucket["events"],
                "metadata_query": bucket["group:metadata_query"],
                "read": bucket["group:read"],
                "write": bucket["group:write"],
                "image_load": bucket["group:image_load"],
                "registry": bucket["family:registry"],
                "network": bucket["family:network"],
            }
        )
    return rows


def load_benchmark_results() -> dict[str, dict[str, object]]:
    by_trace: dict[str, dict[str, object]] = {
        name: {"scenarios": {}, "scenario_summary": []} for name in TRACE_FILES
    }
    if not COMPARE_CSV.exists():
        return by_trace

    rows_by_scenario: dict[str, list[dict[str, str]]] = defaultdict(list)
    with COMPARE_CSV.open("r", encoding="utf-8-sig", newline="") as handle:
        reader = csv.DictReader(handle)
        for row in reader:
            if row.get("status") == "ok":
                rows_by_scenario[row["scenario_id"]].append(row)

    for trace_name, scenarios in BUILD_SCENARIOS.items():
        for scenario in scenarios:
            rows = rows_by_scenario.get(scenario, [])
            summary = summarize_benchmark_scenario(scenario, rows)
            by_trace[trace_name]["scenarios"][scenario] = summary
            by_trace[trace_name]["scenario_summary"].append(summary)
    return by_trace


def summarize_benchmark_scenario(scenario: str, rows: list[dict[str, str]]) -> dict[str, object]:
    slowdowns = [float_value(row.get("slowdown_pct")) for row in rows if row.get("av_name") != "baseline-os"]
    first_run = [float_value(row.get("first_run_slowdown_pct")) for row in rows if row.get("av_name") != "baseline-os"]
    p95 = [float_value(row.get("p95_slowdown_pct")) for row in rows if row.get("av_name") != "baseline-os"]
    return {
        "scenario": scenario,
        "products": len(rows),
        "avg_slowdown_pct": round(mean(slowdowns), 3),
        "median_slowdown_pct": round(median(slowdowns), 3),
        "avg_first_run_slowdown_pct": round(mean(first_run), 3),
        "median_first_run_slowdown_pct": round(median(first_run), 3),
        "avg_p95_slowdown_pct": round(mean(p95), 3),
        "top_average_slowdown": top_slowdown_rows(rows, "slowdown_pct"),
        "top_first_run_slowdown": top_slowdown_rows(rows, "first_run_slowdown_pct"),
    }


def top_slowdown_rows(rows: list[dict[str, str]], field: str, n: int = 8) -> list[dict[str, object]]:
    ranked = sorted(
        [row for row in rows if row.get("av_name") != "baseline-os"],
        key=lambda row: float_value(row.get(field)),
        reverse=True,
    )
    return [
        {
            "av_name": row["av_name"],
            "av_product": row["av_product"],
            field: round(float_value(row.get(field)), 3),
        }
        for row in ranked[:n]
    ]


def build_benchmark_correlation(benchmark_results: dict[str, dict[str, object]]) -> dict[str, object]:
    if not COMPARE_CSV.exists():
        return {"note": "compare.csv not found"}

    rows: list[dict[str, str]] = []
    with COMPARE_CSV.open("r", encoding="utf-8-sig", newline="") as handle:
        reader = csv.DictReader(handle)
        rows = [row for row in reader if row.get("status") == "ok" and row.get("av_name") != "baseline-os"]

    by_scenario = {scenario: {row["av_name"]: row for row in rows if row["scenario_id"] == scenario} for scenarios in BUILD_SCENARIOS.values() for scenario in scenarios}

    correlations = []
    pairs = [
        ("ripgrep-clean-build", "roslyn-clean-build", "clean average slowdown"),
        ("ripgrep-incremental-build", "roslyn-incremental-build", "incremental average slowdown"),
        ("ripgrep-clean-build", "ripgrep-incremental-build", "ripgrep clean vs incremental"),
        ("roslyn-clean-build", "roslyn-incremental-build", "roslyn clean vs incremental"),
    ]
    for left, right, label in pairs:
        correlations.append(correlate_scenarios(by_scenario, left, right, "slowdown_pct", label))
        correlations.append(correlate_scenarios(by_scenario, left, right, "first_run_slowdown_pct", f"{label}, first-cloud-seen"))

    return {
        "note": "Correlation uses per-product slowdown from data/exp1/compare.csv. ProcMon profiles are baseline workload profiles, so this correlates observed AV slowdown patterns with workload structure rather than per-product ProcMon traces.",
        "scenario_correlations": correlations,
    }


def correlate_scenarios(
    by_scenario: dict[str, dict[str, dict[str, str]]],
    left: str,
    right: str,
    field: str,
    label: str,
) -> dict[str, object]:
    common = sorted(set(by_scenario.get(left, {})) & set(by_scenario.get(right, {})))
    xs = [float_value(by_scenario[left][av].get(field)) for av in common]
    ys = [float_value(by_scenario[right][av].get(field)) for av in common]
    return {
        "label": label,
        "left": left,
        "right": right,
        "field": field,
        "products": len(common),
        "pearson_r": round(pearson(xs, ys), 3) if len(common) >= 2 else None,
    }


def build_reproducibility(summary: dict[str, object]) -> dict[str, object]:
    trace_name = str(summary["trace"])
    tree = summary.get("core_process_tree", [])
    root = tree[0] if tree else {}
    direct_children = [
        process
        for process in tree
        if process.get("parent_pid") == root.get("pid") and process.get("name") not in {"Conhost.exe"}
    ]
    run_metadata = {}
    for scenario in BUILD_SCENARIOS.get(trace_name, []):
        path = DATA_EXP1 / "baseline-os" / "results5" / scenario / "run.json"
        if path.exists():
            run_metadata[scenario] = json.loads(path.read_text(encoding="utf-8"))

    commands = []
    for process in direct_children:
        commands.append(
            {
                "process": process.get("name"),
                "command_line": process.get("command_line"),
                "working_dir": process.get("current_directory"),
            }
        )

    run_commands = [
        {
            "scenario": scenario,
            "command": data.get("command"),
            "working_dir": data.get("working_dir"),
            "wall_ms": data.get("wall_ms"),
            "disk_read_mb": round(float_value(data.get("system_disk_read_bytes")) / 1024 / 1024, 1),
            "disk_write_mb": round(float_value(data.get("system_disk_write_bytes")) / 1024 / 1024, 1),
            "runner_version": data.get("runner_version"),
            "machine": data.get("machine"),
        }
        for scenario, data in run_metadata.items()
    ]
    return {
        "trace_root_command": root.get("command_line"),
        "trace_root_working_dir": root.get("current_directory"),
        "trace_child_commands": commands,
        "baseline_run_metadata": run_commands,
        "toolchain_hints": summary.get("core_toolchain_hints", []),
        "repro_hints": summary.get("core_repro_hints", []),
        "inferred_controls": inferred_repro_controls(trace_name, root, commands, summary),
    }


def inferred_repro_controls(
    trace_name: str,
    root: dict[str, object],
    commands: list[dict[str, object]],
    summary: dict[str, object],
) -> list[dict[str, str]]:
    command_text = " ".join(str(item.get("command_line") or "") for item in commands)
    hints = {item["name"]: item["count"] for item in summary.get("core_repro_hints", []) if "name" in item}
    controls = [
        {
            "control": "network",
            "value": "observed" if summary["core_operation_families"] and any(item["name"] == "network" and item["count"] for item in summary["core_operation_families"]) else "not observed",
        },
        {
            "control": "NuGet/cache",
            "value": "touched" if hints.get("nuget_cache_touched", 0) else "not prominent in trace",
        },
        {
            "control": "dotnet telemetry storage",
            "value": "touched" if hints.get("dotnet_telemetry_storage_touched", 0) else "not prominent in trace",
        },
    ]
    if trace_name == "roslyn":
        controls.extend(
            [
                {
                    "control": "MSBuild node reuse",
                    "value": "disabled by /nr:false" if "/nr:false" in command_text.lower() else "not disabled in command",
                },
                {
                    "control": "implicit restore",
                    "value": "possible; command does not include --no-restore" if "--no-restore" not in command_text.lower() else "disabled by --no-restore",
                },
                {
                    "control": "Roslyn compiler server",
                    "value": "observed via VBCSCompiler.exe" if any(item.get("process") == "VBCSCompiler.exe" for item in commands) or "VBCSCompiler.exe" in json.dumps(summary.get("top_core_processes", [])) else "not observed",
                },
            ]
        )
    if trace_name == "ripgrep":
        controls.append({"control": "Cargo network", "value": "not disabled by command; trace network activity is minimal"})
    return controls


def md_table(rows: list[dict[str, object]], columns: list[str]) -> str:
    if not rows:
        return "_No rows._\n"
    lines = [
        "| " + " | ".join(columns) + " |",
        "| " + " | ".join("---" for _ in columns) + " |",
    ]
    for row in rows:
        lines.append("| " + " | ".join(markdown_cell(row.get(col, "")) for col in columns) + " |")
    return "\n".join(lines) + "\n"


def markdown_cell(value: object) -> str:
    return str(value).replace("|", "\\|").replace("\r", " ").replace("\n", " ")


def key_value_table(values: dict[str, object]) -> str:
    return md_table([{"metric": key, "value": value} for key, value in values.items()], ["metric", "value"])


def write_markdown(summaries: dict[str, dict[str, object]]) -> None:
    ripgrep = summaries["ripgrep"]
    roslyn = summaries["roslyn"]
    lines = [
        "# ProcMon analysis: ripgrep vs Roslyn compilation",
        "",
        "Built from the two Process Monitor captures, `tmp/ripgrep.CSV` and `tmp/roslyn.CSV`.",
        "",
        "The main view follows the workload itself: a dynamic process tree rooted at each workload's `avbench.exe` process and all of its descendants. The broader trace view is kept at the end only as ambient-system context, because desktop noise can otherwise look more meaningful than it is.",
        "",
        "## High-level event volume",
        "",
        md_table(
            [
                {
                    "trace": "ripgrep",
                    "events": ripgrep["total_events"],
                    "build_events": ripgrep["build_events_excluding_procmon_system"],
                    "core_events": ripgrep["core_events"],
                    "core_processes": ripgrep["core_process_count"],
                    "core_unique_file_paths": ripgrep["unique_core_paths_by_family"].get("file", 0),
                    "core_unique_registry_paths": ripgrep["unique_core_paths_by_family"].get("registry", 0),
                },
                {
                    "trace": "roslyn",
                    "events": roslyn["total_events"],
                    "build_events": roslyn["build_events_excluding_procmon_system"],
                    "core_events": roslyn["core_events"],
                    "core_processes": roslyn["core_process_count"],
                    "core_unique_file_paths": roslyn["unique_core_paths_by_family"].get("file", 0),
                    "core_unique_registry_paths": roslyn["unique_core_paths_by_family"].get("registry", 0),
                },
            ],
            [
                "trace",
                "events",
                "build_events",
                "core_events",
                "core_processes",
                "core_unique_file_paths",
                "core_unique_registry_paths",
            ],
        ),
        "## Core process tree",
        "",
        "### ripgrep",
        md_table(ripgrep["core_process_tree"], ["pid", "name", "parent_pid", "first_seen", "source", "current_directory"]),
        "### Roslyn",
        md_table(roslyn["core_process_tree"], ["pid", "name", "parent_pid", "first_seen", "source", "current_directory"]),
        "## Core operation families",
        "",
        "### ripgrep",
        md_table(ripgrep["core_operation_families"], ["name", "count", "pct"]),
        "### Roslyn",
        md_table(roslyn["core_operation_families"], ["name", "count", "pct"]),
        "## Top core processes",
        "",
        "### ripgrep",
        md_table(ripgrep["top_core_processes"], ["name", "count", "pct", "top_family", "top_operation"]),
        "### Roslyn",
        md_table(roslyn["top_core_processes"], ["name", "count", "pct", "top_family", "top_operation"]),
        "## Top core operations",
        "",
        "### ripgrep",
        md_table(ripgrep["top_core_operations"], ["name", "count", "pct"]),
        "### Roslyn",
        md_table(roslyn["top_core_operations"], ["name", "count", "pct"]),
        "## Core file operation groups",
        "",
        "### ripgrep",
        md_table(ripgrep["core_file_operation_groups"], ["name", "count", "pct"]),
        "### Roslyn",
        md_table(roslyn["core_file_operation_groups"], ["name", "count", "pct"]),
        "## Core per-second rates",
        "",
        "### ripgrep",
        key_value_table(ripgrep["core_per_second_rates"]),
        "### Roslyn",
        key_value_table(roslyn["core_per_second_rates"]),
        "## Phase detection",
        "",
        "### ripgrep",
        md_table(
            ripgrep["core_phase_summary"],
            ["phase", "events", "pct", "metadata_query", "read", "write", "image_load", "registry", "network", "top_process", "top_operation"],
        ),
        "### Roslyn",
        md_table(
            roslyn["core_phase_summary"],
            ["phase", "events", "pct", "metadata_query", "read", "write", "image_load", "registry", "network", "top_process", "top_operation"],
        ),
        "## AV-relevance metrics",
        "",
        "### ripgrep",
        key_value_table(ripgrep["core_av_relevance_metrics"]),
        "### Roslyn",
        key_value_table(roslyn["core_av_relevance_metrics"]),
        "## Weighted AV pressure model",
        "",
        "This model is heuristic. It treats counts as exposure, percentages as shape, and weighted score as likely AV cost. Fresh executable writes and fresh build outputs are weighted much higher than Microsoft OS/SDK reads and metadata checks.",
        "",
        "### Model weights",
        key_value_table(pressure_model_definition()),
        "### Summary",
        md_table(
            [
                weighted_pressure_row("ripgrep", ripgrep),
                weighted_pressure_row("roslyn", roslyn),
            ],
            ["trace", "score", "score_per_second", "score_per_1000_events", "top_group", "top_trust_bucket", "top_phase"],
        ),
        "### Pressure by operation group: ripgrep",
        md_table(ripgrep["core_weighted_pressure_by_group"], ["name", "score", "pct"]),
        "### Pressure by operation group: Roslyn",
        md_table(roslyn["core_weighted_pressure_by_group"], ["name", "score", "pct"]),
        "### Pressure by trust bucket: ripgrep",
        md_table(ripgrep["core_weighted_pressure_by_trust"], ["name", "score", "pct"]),
        "### Pressure by trust bucket: Roslyn",
        md_table(roslyn["core_weighted_pressure_by_trust"], ["name", "score", "pct"]),
        "### Pressure by phase: ripgrep",
        md_table(ripgrep["core_weighted_pressure_by_phase"], ["name", "score", "pct"]),
        "### Pressure by phase: Roslyn",
        md_table(roslyn["core_weighted_pressure_by_phase"], ["name", "score", "pct"]),
        "## Trust and reputation heuristics",
        "",
        "These are path-based heuristics. ProcMon does not record Authenticode signer, catalog signing, cloud reputation, or whether an AV product actually trusted a file.",
        "",
        "### Core file events by trust bucket: ripgrep",
        md_table(ripgrep["core_trust_reputation_buckets"], ["name", "count", "pct"]),
        "### Core file events by trust bucket: Roslyn",
        md_table(roslyn["core_trust_reputation_buckets"], ["name", "count", "pct"]),
        "### Executable-like file events by trust bucket: ripgrep",
        md_table(ripgrep["core_executable_trust_buckets"], ["name", "count", "pct"]),
        "### Executable-like file events by trust bucket: Roslyn",
        md_table(roslyn["core_executable_trust_buckets"], ["name", "count", "pct"]),
        "### Image-load events by trust bucket: ripgrep",
        md_table(ripgrep["core_image_load_trust_buckets"], ["name", "count", "pct"]),
        "### Image-load events by trust bucket: Roslyn",
        md_table(roslyn["core_image_load_trust_buckets"], ["name", "count", "pct"]),
        "### Executable-like writes by trust bucket: ripgrep",
        md_table(ripgrep["core_executable_write_trust_buckets"], ["name", "count", "pct"]),
        "### Executable-like writes by trust bucket: Roslyn",
        md_table(roslyn["core_executable_write_trust_buckets"], ["name", "count", "pct"]),
        "## Process x operation matrix",
        "",
        "### ripgrep",
        md_table(
            ripgrep["core_process_operation_matrix"],
            ["process", "events", "CreateFile", "QueryOpen", "QueryNetworkOpenInformationFile", "ReadFile", "WriteFile", "Load Image", "RegOpenKey", "RegQueryValue", "Process Create", "Thread Create"],
        ),
        "### Roslyn",
        md_table(
            roslyn["core_process_operation_matrix"],
            ["process", "events", "CreateFile", "QueryOpen", "QueryNetworkOpenInformationFile", "ReadFile", "WriteFile", "Load Image", "RegOpenKey", "RegQueryValue", "Process Create", "Thread Create"],
        ),
        "## Core path roots",
        "",
        "### ripgrep",
        md_table(ripgrep["core_path_roots"], ["name", "count", "pct"]),
        "### Roslyn",
        md_table(roslyn["core_path_roots"], ["name", "count", "pct"]),
        "## Core file extensions",
        "",
        "### ripgrep",
        md_table(ripgrep["top_core_extensions"], ["name", "count", "pct"]),
        "### Roslyn",
        md_table(roslyn["top_core_extensions"], ["name", "count", "pct"]),
        "## Core read extensions",
        "",
        "### ripgrep",
        md_table(ripgrep["top_core_read_extensions"], ["name", "count", "pct"]),
        "### Roslyn",
        md_table(roslyn["top_core_read_extensions"], ["name", "count", "pct"]),
        "## Core write extensions",
        "",
        "### ripgrep",
        md_table(ripgrep["top_core_write_extensions"], ["name", "count", "pct"]),
        "### Roslyn",
        md_table(roslyn["top_core_write_extensions"], ["name", "count", "pct"]),
        "## Core write directories",
        "",
        "### ripgrep",
        md_table(ripgrep["top_core_write_directories"], ["name", "count", "pct"]),
        "### Roslyn",
        md_table(roslyn["top_core_write_directories"], ["name", "count", "pct"]),
        "## Top image loads",
        "",
        "### ripgrep",
        md_table(ripgrep["top_core_image_loads"], ["name", "count", "pct"]),
        "### Roslyn",
        md_table(roslyn["top_core_image_loads"], ["name", "count", "pct"]),
        "## Top executable-like writes",
        "",
        "### ripgrep",
        md_table(ripgrep["top_core_executable_writes"], ["name", "count", "pct"]),
        "### Roslyn",
        md_table(roslyn["top_core_executable_writes"], ["name", "count", "pct"]),
        "## Top failed probes",
        "",
        "### ripgrep",
        md_table(ripgrep["top_core_failed_probe_details"], ["name", "count", "pct"]),
        "### Roslyn",
        md_table(roslyn["top_core_failed_probe_details"], ["name", "count", "pct"]),
        "## Core registry roots",
        "",
        "### ripgrep",
        md_table(ripgrep["top_core_registry_roots"], ["name", "count", "pct"]),
        "### Roslyn",
        md_table(roslyn["top_core_registry_roots"], ["name", "count", "pct"]),
        "## Core network paths",
        "",
        "### ripgrep",
        md_table(ripgrep["top_core_network_paths"], ["name", "count", "pct"]),
        "### Roslyn",
        md_table(roslyn["top_core_network_paths"], ["name", "count", "pct"]),
        "## Broad trace context",
        "",
        "These tables exclude only ProcMon/System and can include unrelated desktop or service activity. Use them as context, not as the primary workload profile.",
        "",
        "### Operation families: ripgrep",
        md_table(ripgrep["build_operation_families"], ["name", "count", "pct"]),
        "### Operation families: Roslyn",
        md_table(roslyn["build_operation_families"], ["name", "count", "pct"]),
        "### Top broad processes: ripgrep",
        md_table(ripgrep["top_build_processes"], ["name", "count", "pct", "top_family", "top_operation"]),
        "### Top broad processes: Roslyn",
        md_table(roslyn["top_build_processes"], ["name", "count", "pct", "top_family", "top_operation"]),
    ]
    (OUT / "compilation-procmon-analysis.md").write_text("\n".join(lines), encoding="utf-8")


def write_profile_markdown(summaries: dict[str, dict[str, object]], benchmark_correlation: dict[str, object]) -> None:
    ripgrep = summaries["ripgrep"]
    roslyn = summaries["roslyn"]
    lines = [
        "# Workload profile pipeline",
        "",
        "This is the reproducible profiling layer for the ripgrep and Roslyn ProcMon traces. It connects the OS-level behavior, inferred build phases, AV-relevant pressure points, reproducibility controls, and benchmark slowdown data from `data/exp1/compare.csv`.",
        "",
        "## What This Adds Beyond Counting Events",
        "",
        "- Core scope is a dynamic process tree rooted at the workload-specific `avbench.exe` process.",
        "- `Load Image` is counted as file activity because DLL/EXE image loads are AV-relevant.",
        "- File operations are grouped into `open_close`, `metadata_query`, `read`, `write`, `image_load`, `registry`, `network`, and `other`.",
        "- Phases are inferred from process names, operations, and paths. They are workload-profile heuristics, not compiler-internal ground truth.",
        "- Benchmark correlation uses per-product slowdown from `data/exp1/compare.csv`; ProcMon profiles are baseline workload profiles, not per-AV traces.",
        "",
        "## Workload shape summary",
        "",
        md_table(
            [
                profile_overview_row("ripgrep", ripgrep),
                profile_overview_row("roslyn", roslyn),
            ],
            [
                "trace",
                "core_events",
                "duration_s",
                "events_s",
                "unique_files",
                "metadata_queries",
                "writes",
                "image_loads",
                "failed_probes",
                "fast_io_disallowed",
            ],
        ),
        "## Phase Detection",
        "",
        "### ripgrep",
        md_table(
            ripgrep["core_phase_summary"],
            ["phase", "events", "pct", "metadata_query", "read", "write", "image_load", "registry", "network", "top_process", "top_operation"],
        ),
        "### Roslyn",
        md_table(
            roslyn["core_phase_summary"],
            ["phase", "events", "pct", "metadata_query", "read", "write", "image_load", "registry", "network", "top_process", "top_operation"],
        ),
        "## AV-Relevance Metrics",
        "",
        "These metrics describe the surfaces an AV engine has to make decisions on. They are not direct latency measurements, but they explain where slowdown can plausibly enter the build.",
        "",
        md_table(
            [
                av_metric_row("ripgrep", ripgrep),
                av_metric_row("roslyn", roslyn),
            ],
            [
                "trace",
                "unique_file_paths",
                "unique_executable_like_paths",
                "new_write_paths",
                "executable_like_write_paths",
                "metadata_query_events",
                "image_load_events",
                "write_events",
                "fast_io_disallowed_events",
                "failed_probe_events",
                "network_events",
                "registry_query_events",
                "observed_read_mb",
                "observed_write_mb",
            ],
        ),
        "## Count, Percent, Rate, and Weighted Pressure",
        "",
        "Event count alone is a weak lens. Counts show exposure volume, percentages show workload shape, rates show intensity, and the weighted pressure model estimates where AV cost is most likely to concentrate. The score is intentionally transparent rather than absolute: fresh executable writes and local build outputs carry much more weight than likely trusted Microsoft OS/SDK paths.",
        "",
        "### Weighted pressure summary",
        "",
        md_table(
            [
                weighted_pressure_row("ripgrep", ripgrep),
                weighted_pressure_row("roslyn", roslyn),
            ],
            ["trace", "score", "score_per_second", "score_per_1000_events", "top_group", "top_trust_bucket", "top_phase"],
        ),
        "### Pressure by operation group",
        "",
        "#### ripgrep",
        md_table(ripgrep["core_weighted_pressure_by_group"], ["name", "score", "pct"]),
        "#### Roslyn",
        md_table(roslyn["core_weighted_pressure_by_group"], ["name", "score", "pct"]),
        "### Pressure by trust/reputation bucket",
        "",
        "#### ripgrep",
        md_table(ripgrep["core_weighted_pressure_by_trust"], ["name", "score", "pct"]),
        "#### Roslyn",
        md_table(roslyn["core_weighted_pressure_by_trust"], ["name", "score", "pct"]),
        "### Pressure by phase",
        "",
        "#### ripgrep",
        md_table(ripgrep["core_weighted_pressure_by_phase"], ["name", "score", "pct"]),
        "#### Roslyn",
        md_table(roslyn["core_weighted_pressure_by_phase"], ["name", "score", "pct"]),
        "### Pressure model definition",
        "",
        key_value_table(pressure_model_definition()),
        "## Trust and Reputation Heuristics",
        "",
        "This section answers a key AV question: is the workload mostly touching likely-known signed platform binaries, or is it creating/touching less-known generated binaries? The buckets are path-based heuristics only. ProcMon does not include Authenticode signer, catalog signing, cloud reputation, or product whitelist decisions.",
        "",
        "### Core file events by trust bucket",
        "",
        "#### ripgrep",
        md_table(ripgrep["core_trust_reputation_buckets"], ["name", "count", "pct"]),
        "#### Roslyn",
        md_table(roslyn["core_trust_reputation_buckets"], ["name", "count", "pct"]),
        "### Executable-like file events by trust bucket",
        "",
        "#### ripgrep",
        md_table(ripgrep["core_executable_trust_buckets"], ["name", "count", "pct"]),
        "#### Roslyn",
        md_table(roslyn["core_executable_trust_buckets"], ["name", "count", "pct"]),
        "### Image-load events by trust bucket",
        "",
        "#### ripgrep",
        md_table(ripgrep["core_image_load_trust_buckets"], ["name", "count", "pct"]),
        "#### Roslyn",
        md_table(roslyn["core_image_load_trust_buckets"], ["name", "count", "pct"]),
        "### Executable-like writes by trust bucket",
        "",
        "#### ripgrep",
        md_table(ripgrep["core_executable_write_trust_buckets"], ["name", "count", "pct"]),
        "#### Roslyn",
        md_table(roslyn["core_executable_write_trust_buckets"], ["name", "count", "pct"]),
        "### Trust/reputation interpretation",
        "",
        "Roslyn touches far more likely-known platform content in absolute terms, especially `C:\\Program Files\\dotnet`, reference assemblies, SDK files, Windows DLLs, and Microsoft build infrastructure. That means a large part of its DLL/reference footprint may benefit from signer, catalog, path, or cloud reputation shortcuts in many AV products.",
        "",
        "But Roslyn also writes and reopens a much larger number of fresh build-output DLLs under `C:\\bench\\roslyn\\artifacts\\obj`. Those fresh outputs are the opposite reputation shape: new, local, unsigned-or-not-yet-known build artifacts. So Roslyn combines a trusted-platform read surface with a very large generated-DLL write surface.",
        "",
        "Ripgrep touches fewer Microsoft/SDK binaries overall. Its trusted-platform image loads are mostly normal Windows/MSVC runtime DLLs and linker inputs, while much of the Rust workload comes from user-profile Rustup/Cargo caches plus freshly emitted `target\\release` artifacts. That makes ripgrep smaller, but proportionally more exposed to non-Microsoft toolchain/package reputation and freshly generated native artifacts.",
        "",
        "## Core Appendix",
        "",
        "### File Operation Groups",
        "",
        "#### ripgrep",
        md_table(ripgrep["core_file_operation_groups"], ["name", "count", "pct"]),
        "#### Roslyn",
        md_table(roslyn["core_file_operation_groups"], ["name", "count", "pct"]),
        "### Per-Second Rates",
        "",
        "#### ripgrep",
        key_value_table(ripgrep["core_per_second_rates"]),
        "#### Roslyn",
        key_value_table(roslyn["core_per_second_rates"]),
        "### Process x Operation Matrix",
        "",
        "#### ripgrep",
        md_table(
            ripgrep["core_process_operation_matrix"],
            ["process", "events", "CreateFile", "QueryOpen", "QueryNetworkOpenInformationFile", "ReadFile", "WriteFile", "Load Image", "RegOpenKey", "RegQueryValue", "Process Create", "Thread Create"],
        ),
        "#### Roslyn",
        md_table(
            roslyn["core_process_operation_matrix"],
            ["process", "events", "CreateFile", "QueryOpen", "QueryNetworkOpenInformationFile", "ReadFile", "WriteFile", "Load Image", "RegOpenKey", "RegQueryValue", "Process Create", "Thread Create"],
        ),
        "### Top Image Loads",
        "",
        "#### ripgrep",
        md_table(ripgrep["top_core_image_loads"], ["name", "count", "pct"]),
        "#### Roslyn",
        md_table(roslyn["top_core_image_loads"], ["name", "count", "pct"]),
        "### Top Executable-Like Writes",
        "",
        "#### ripgrep",
        md_table(ripgrep["top_core_executable_writes"], ["name", "count", "pct"]),
        "#### Roslyn",
        md_table(roslyn["top_core_executable_writes"], ["name", "count", "pct"]),
        "### Top Failed Probes",
        "",
        "#### ripgrep",
        md_table(ripgrep["top_core_failed_probe_details"], ["name", "count", "pct"]),
        "#### Roslyn",
        md_table(roslyn["top_core_failed_probe_details"], ["name", "count", "pct"]),
        "### Busiest Seconds",
        "",
        "#### ripgrep",
        md_table(ripgrep["core_second_buckets"], ["second", "events", "metadata_query", "read", "write", "image_load", "registry", "network"]),
        "#### Roslyn",
        md_table(roslyn["core_second_buckets"], ["second", "events", "metadata_query", "read", "write", "image_load", "registry", "network"]),
        "## Reproducibility",
        "",
        "### ripgrep",
        reproducibility_section(ripgrep),
        "### Roslyn",
        reproducibility_section(roslyn),
        "## Benchmark Correlation",
        "",
        str(benchmark_correlation.get("note", "")),
        "",
        "### Scenario Summary",
        "",
        "#### ripgrep",
        md_table(ripgrep["benchmark_results"]["scenario_summary"], ["scenario", "products", "avg_slowdown_pct", "median_slowdown_pct", "avg_first_run_slowdown_pct", "median_first_run_slowdown_pct", "avg_p95_slowdown_pct"]),
        "#### Roslyn",
        md_table(roslyn["benchmark_results"]["scenario_summary"], ["scenario", "products", "avg_slowdown_pct", "median_slowdown_pct", "avg_first_run_slowdown_pct", "median_first_run_slowdown_pct", "avg_p95_slowdown_pct"]),
        "### Per-Product Correlations",
        "",
        md_table(benchmark_correlation.get("scenario_correlations", []), ["label", "left", "right", "field", "products", "pearson_r"]),
        "### Interpretation",
        "",
        "Roslyn's baseline profile has far more metadata queries, unique paths, DLL/image-load activity, and failed probes than ripgrep. If a product is disproportionately slow on Roslyn in `compare.csv`, the likely explanation is per-open/per-query filtering, reference/package/analyzer DLL handling, compiler-server input scanning, or cloud/reputation checks across many unique paths.",
        "",
        "Ripgrep's profile has fewer paths but a higher concentration of compiler/linker artifact writes. If a product is disproportionately slow on ripgrep, the likely explanation is scanning of newly emitted native artifacts, PDBs, `.lib`/`.rmeta`/`.rlib` files, linker temp files, or executable-generation behavior rules.",
        "",
    ]
    (OUT / "workload-profile-pipeline.md").write_text("\n".join(lines), encoding="utf-8")


def profile_overview_row(trace: str, summary: dict[str, object]) -> dict[str, object]:
    av = summary["core_av_relevance_metrics"]
    rates = summary["core_per_second_rates"]
    return {
        "trace": trace,
        "core_events": summary["core_events"],
        "duration_s": rates["duration_seconds"],
        "events_s": rates["events_per_second"],
        "unique_files": av["unique_file_paths"],
        "metadata_queries": av["metadata_query_events"],
        "writes": av["write_events"],
        "image_loads": av["image_load_events"],
        "failed_probes": av["failed_probe_events"],
        "fast_io_disallowed": av["fast_io_disallowed_events"],
    }


def av_metric_row(trace: str, summary: dict[str, object]) -> dict[str, object]:
    row = {"trace": trace}
    row.update(summary["core_av_relevance_metrics"])
    return row


def weighted_pressure_row(trace: str, summary: dict[str, object]) -> dict[str, object]:
    pressure = summary["core_weighted_pressure"]
    top_group = summary["core_weighted_pressure_by_group"][0]["name"] if summary["core_weighted_pressure_by_group"] else ""
    top_trust = summary["core_weighted_pressure_by_trust"][0]["name"] if summary["core_weighted_pressure_by_trust"] else ""
    top_phase = summary["core_weighted_pressure_by_phase"][0]["name"] if summary["core_weighted_pressure_by_phase"] else ""
    return {
        "trace": trace,
        "score": pressure["score"],
        "score_per_second": pressure["score_per_second"],
        "score_per_1000_events": pressure["score_per_1000_events"],
        "top_group": top_group,
        "top_trust_bucket": top_trust,
        "top_phase": top_phase,
    }


def reproducibility_section(summary: dict[str, object]) -> str:
    repro = summary["reproducibility"]
    parts = [
        "Trace root:",
        key_value_table(
            {
                "trace_root_command": repro.get("trace_root_command", ""),
                "trace_root_working_dir": repro.get("trace_root_working_dir", ""),
            }
        ),
        "Trace child commands:",
        md_table(repro.get("trace_child_commands", []), ["process", "command_line", "working_dir"]),
        "Baseline run metadata:",
        md_table(
            repro.get("baseline_run_metadata", []),
            ["scenario", "command", "working_dir", "wall_ms", "disk_read_mb", "disk_write_mb", "runner_version"],
        ),
        "Toolchain hints:",
        md_table([{"hint": hint} for hint in repro.get("toolchain_hints", [])], ["hint"]),
        "Inferred controls:",
        md_table(repro.get("inferred_controls", []), ["control", "value"]),
    ]
    return "\n".join(parts)


if __name__ == "__main__":
    main()
