# avbench

Measure the performance cost of antivirus software on Windows workloads.

**Docs:** [Architecture](docs/architecture.md) · [Metrics](docs/metrics.md) · [Workloads](docs/workloads.md)

---

## Quick start

### Prerequisites

- Windows 10/11 VM (clean snapshot recommended)
- Administrator privileges
- Internet access during setup (downloads toolchains and source repos)
- ~20 GB free disk space on `C:\`

### Build

```powershell
cd C:\projects\av-benchmark\src
dotnet publish AvBench.Cli -c Release -o C:\tools\avbench
dotnet publish AvBench.Compare -c Release -o C:\tools\avbench
```

This produces `C:\tools\avbench\avbench.exe` and `C:\tools\avbench\avbench-compare.exe`.

---

## VM workflow (one-liners)

> **Assumption:** `avbench.exe` and `avbench-compare.exe` are on PATH or in `C:\tools\avbench\`.
> All commands run in an **elevated PowerShell** terminal.

### 1. Setup (run once per VM image, before snapshotting)

```powershell
avbench setup --bench-dir C:\bench
```

This installs Git, Rust 1.85.0, .NET SDK, VS Build Tools, clones repos (ripgrep + Roslyn), hydrates dependencies, builds the unsigned noop.exe, creates the archive zip, and writes `C:\bench\suite-manifest.json`.

To set up only specific workloads:

```powershell
avbench setup --bench-dir C:\bench -w microbench
avbench setup --bench-dir C:\bench -w ripgrep,roslyn
```

**After setup completes, take a VM snapshot.** Restore to this snapshot before each run.

### 2. Run — baseline VM (no AV installed)

```powershell
avbench run --name baseline-os --bench-dir C:\bench --output C:\results
```

### 3. Run — AV VM (e.g., Defender with default settings)

```powershell
avbench run --name defender-default --bench-dir C:\bench --output C:\results
```

AV product and version are auto-detected from Windows Security Center. To override:

```powershell
avbench run --name eset-default --bench-dir C:\bench --output C:\results --av-product "ESET Security" --av-version "19.1.12.0"
```

### 4. Run only specific workloads

```powershell
avbench run --name defender-default --bench-dir C:\bench --output C:\results -w microbench
avbench run --name defender-default --bench-dir C:\bench --output C:\results -w ripgrep,roslyn
```

### 5. Compare — host machine (or any machine with the result folders)

Copy `C:\results` from each VM to the host, then:

```powershell
avbench-compare --baseline C:\compare\baseline-os --input C:\compare\defender-default --output C:\compare\report
```

Compare multiple AV configs at once:

```powershell
avbench-compare --baseline C:\compare\baseline-os --input C:\compare\defender-default C:\compare\eset-default --output C:\compare\report
```

**Outputs:**

| File | Description |
|---|---|
| `compare.csv` | 16-column spreadsheet with per-scenario slowdown %, kernel CPU shift, CV, status |
| `summary.md` | Markdown report with tables sorted by worst slowdown, highlights noisy/failed |

---

## Multiple sessions (recommended)

For statistically reliable results, run multiple sessions per AV configuration. Each session should start from the same VM snapshot:

```
Restore snapshot → avbench run ... → copy results → Restore snapshot → avbench run ... → copy results → ...
```

Collect 3–5 sessions per configuration. `avbench-compare` aggregates all `run.json` files found under each input directory, computing mean/median/CV across sessions.

---

## Output structure

```
C:\results\                              # --output directory
├── suite-manifest.json
├── runs.csv
├── ripgrep-clean-build\
│   ├── run.json
│   ├── stdout.log
│   └── stderr.log
├── ripgrep-incremental-build\
│   └── ...
├── roslyn-clean-build\
│   └── ...
├── file-create-delete\
│   └── ...
├── mem-alloc-protect\
│   └── ...
└── ... (32 scenario folders total)
```

---

## CLI reference

### `avbench setup`

| Option | Default | Description |
|---|---|---|
| `--bench-dir` | `C:\bench` | Root directory for repos and manifests |
| `--ripgrep-ref` | latest release | Optional branch/tag/SHA for ripgrep |
| `-w`, `--workload` | all | `ripgrep`, `roslyn`, `microbench`, or `all` |

Exit codes: `0` = success, `1` = error, `2` = reboot required (re-run setup after reboot).

### `avbench run`

| Option | Default | Description |
|---|---|---|
| `--name` | *required* | Label for this AV config (e.g., `baseline-os`, `defender-default`) |
| `--bench-dir` | `C:\bench` | Where setup stored repos and manifest |
| `--output` | `./results` | Where to write result folders |
| `-w`, `--workload` | all | Which workloads to run |
| `--av-product` | auto-detect | Override AV product name |
| `--av-version` | auto-detect | Override AV version string |

### `avbench-compare`

| Option | Default | Description |
|---|---|---|
| `--baseline` | *required* | Result directory for the no-AV baseline |
| `--input` | *required* | One or more result directories to compare |
| `--output` | *required* | Where to write `compare.csv` and `summary.md` |
