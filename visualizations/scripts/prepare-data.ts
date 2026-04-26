import { mkdir, readFile, writeFile } from 'node:fs/promises'
import path from 'node:path'
import { parse } from 'csv-parse/sync'

type CompareRow = {
  scenario_id: string
  av_name: string
  av_product: string
  first_run_wall_ms: string
  baseline_first_run_wall_ms: string
  first_run_slowdown_pct: string
  all_runs_mean_wall_ms: string
  baseline_all_runs_mean_wall_ms: string
  status: string
}

type ImpactMetric = {
  value: number
  wallMs: number
  baselineWallMs: number
}

type WorkloadMetric = {
  cloudCold: ImpactMetric
  average: ImpactMetric
  status: string
}

type ChartRow = {
  avName: string
  avProduct: string
  ripgrep: {
    clean: WorkloadMetric | null
    incremental: WorkloadMetric | null
  }
  roslyn: {
    clean: WorkloadMetric | null
    incremental: WorkloadMetric | null
  }
}

type MicrobenchScenarioRow = {
  avName: string
  avProduct: string
  average: ImpactMetric
  status: string
}

type ExtensionSensitivityRow = {
  avName: string
  avProduct: string
  exe: MicrobenchScenarioRow | null
  dll: MicrobenchScenarioRow | null
  js: MicrobenchScenarioRow | null
  ps1: MicrobenchScenarioRow | null
}

const scenarioMap = {
  'ripgrep-clean-build': ['ripgrep', 'clean'],
  'ripgrep-incremental-build': ['ripgrep', 'incremental'],
  'roslyn-clean-build': ['roslyn', 'clean'],
  'roslyn-incremental-build': ['roslyn', 'incremental'],
} as const

const experiment = getArgumentValue('--exp') ?? 'exp1'
const repoRoot = path.resolve(import.meta.dirname, '..', '..')
const inputPath = path.join(repoRoot, 'data', experiment, 'compare.csv')
const outputDir = path.resolve(import.meta.dirname, '..', 'public', 'generated', experiment)
const outputPath = path.join(outputDir, 'compilation-workloads.json')

const csv = await readFile(inputPath, 'utf8')
const records = parse(csv, {
  columns: true,
  skip_empty_lines: true,
  trim: true,
}) as CompareRow[]

const rowsByAv = new Map<string, ChartRow>()
const fileCreateDeleteRows: MicrobenchScenarioRow[] = []
const archiveExtractRows: MicrobenchScenarioRow[] = []
const fileEnumLargeDirRows: MicrobenchScenarioRow[] = []
const hardlinkCreateRows: MicrobenchScenarioRow[] = []
const junctionCreateRows: MicrobenchScenarioRow[] = []
const processCreateWaitRows: MicrobenchScenarioRow[] = []
const dllLoadUniqueRows: MicrobenchScenarioRow[] = []
const fileWriteContentRows: MicrobenchScenarioRow[] = []
const newExeRunRows: MicrobenchScenarioRow[] = []
const newExeRunMotwRows: MicrobenchScenarioRow[] = []
const threadCreateRows: MicrobenchScenarioRow[] = []
const memAllocProtectRows: MicrobenchScenarioRow[] = []
const memMapFileRows: MicrobenchScenarioRow[] = []
const netConnectLoopbackRows: MicrobenchScenarioRow[] = []
const netDnsResolveRows: MicrobenchScenarioRow[] = []
const registryCrudRows: MicrobenchScenarioRow[] = []
const pipeRoundtripRows: MicrobenchScenarioRow[] = []
const cryptoHashVerifyRows: MicrobenchScenarioRow[] = []
const comCreateInstanceRows: MicrobenchScenarioRow[] = []
const fsWatcherRows: MicrobenchScenarioRow[] = []
const extensionSensitivityRows = new Map<string, ExtensionSensitivityRow>()

const microbenchRowsByScenario = new Map<string, MicrobenchScenarioRow[]>([
  ['file-create-delete', fileCreateDeleteRows],
  ['archive-extract', archiveExtractRows],
  ['file-enum-large-dir', fileEnumLargeDirRows],
  ['hardlink-create', hardlinkCreateRows],
  ['junction-create', junctionCreateRows],
  ['process-create-wait', processCreateWaitRows],
  ['dll-load-unique', dllLoadUniqueRows],
  ['file-write-content', fileWriteContentRows],
  ['new-exe-run', newExeRunRows],
  ['new-exe-run-motw', newExeRunMotwRows],
  ['thread-create', threadCreateRows],
  ['mem-alloc-protect', memAllocProtectRows],
  ['mem-map-file', memMapFileRows],
  ['net-connect-loopback', netConnectLoopbackRows],
  ['net-dns-resolve', netDnsResolveRows],
  ['registry-crud', registryCrudRows],
  ['pipe-roundtrip', pipeRoundtripRows],
  ['crypto-hash-verify', cryptoHashVerifyRows],
  ['com-create-instance', comCreateInstanceRows],
  ['fs-watcher', fsWatcherRows],
])

const extensionSensitivityMap = {
  'ext-sensitivity-exe': 'exe',
  'ext-sensitivity-dll': 'dll',
  'ext-sensitivity-js': 'js',
  'ext-sensitivity-ps1': 'ps1',
} as const

for (const record of records) {
  const averageWallMs = Number(record.all_runs_mean_wall_ms)
  const averageBaselineWallMs = Number(record.baseline_all_runs_mean_wall_ms)

  const microbenchRow = createMicrobenchRow(record, averageWallMs, averageBaselineWallMs)
  if (microbenchRow) {
    const microbenchRows = microbenchRowsByScenario.get(record.scenario_id)
    if (microbenchRows) {
      microbenchRows.push(microbenchRow)
    }

    const extensionKey =
      extensionSensitivityMap[record.scenario_id as keyof typeof extensionSensitivityMap]
    if (extensionKey) {
      const extensionRow =
        extensionSensitivityRows.get(record.av_name) ??
        ({
          avName: record.av_name,
          avProduct: record.av_product,
          exe: null,
          dll: null,
          js: null,
          ps1: null,
        } satisfies ExtensionSensitivityRow)

      extensionRow[extensionKey] = microbenchRow
      extensionSensitivityRows.set(record.av_name, extensionRow)
    }
  }

  const mapping = scenarioMap[record.scenario_id as keyof typeof scenarioMap]
  if (!mapping) {
    continue
  }

  const [workload, buildType] = mapping
  const row =
    rowsByAv.get(record.av_name) ??
    ({
      avName: record.av_name,
      avProduct: record.av_product,
      ripgrep: { clean: null, incremental: null },
      roslyn: { clean: null, incremental: null },
    } satisfies ChartRow)

  row[workload][buildType] = {
    cloudCold: {
      value: Number(record.first_run_slowdown_pct),
      wallMs: Number(record.first_run_wall_ms),
      baselineWallMs: Number(record.baseline_first_run_wall_ms),
    },
    average: {
      value: ((averageWallMs - averageBaselineWallMs) / averageBaselineWallMs) * 100,
      wallMs: averageWallMs,
      baselineWallMs: averageBaselineWallMs,
    },
    status: record.status,
  }

  rowsByAv.set(record.av_name, row)
}

const payload = {
  experiment,
  source: path.relative(repoRoot, inputPath).replaceAll('\\', '/'),
  metrics: ['cloudCold', 'average'],
  generatedAt: new Date().toISOString(),
  rows: [...rowsByAv.values()],
  microbench: {
    fileCreateDelete: {
      id: 'file-create-delete',
      title: 'File Create/Delete',
      rows: fileCreateDeleteRows,
    },
    archiveExtract: {
      id: 'archive-extract',
      title: 'Archive Extract',
      rows: archiveExtractRows,
    },
    fileEnumLargeDir: {
      id: 'file-enum-large-dir',
      title: 'File Enum Large Dir',
      rows: fileEnumLargeDirRows,
    },
    hardlinkCreate: {
      id: 'hardlink-create',
      title: 'Hardlink Create',
      rows: hardlinkCreateRows,
    },
    junctionCreate: {
      id: 'junction-create',
      title: 'Junction Create',
      rows: junctionCreateRows,
    },
    processCreateWait: {
      id: 'process-create-wait',
      title: 'Process Create Wait',
      rows: processCreateWaitRows,
    },
    dllLoadUnique: {
      id: 'dll-load-unique',
      title: 'DLL Load Unique',
      rows: dllLoadUniqueRows,
    },
    fileWriteContent: {
      id: 'file-write-content',
      title: 'File Write Content',
      rows: fileWriteContentRows,
    },
    newExeRun: {
      id: 'new-exe-run',
      title: 'New EXE Run',
      rows: newExeRunRows,
    },
    newExeRunMotw: {
      id: 'new-exe-run-motw',
      title: 'New EXE Run MOTW',
      rows: newExeRunMotwRows,
    },
    threadCreate: {
      id: 'thread-create',
      title: 'Thread Create',
      rows: threadCreateRows,
    },
    memAllocProtect: {
      id: 'mem-alloc-protect',
      title: 'Memory Allocate/Protect',
      rows: memAllocProtectRows,
    },
    memMapFile: {
      id: 'mem-map-file',
      title: 'Memory Map File',
      rows: memMapFileRows,
    },
    netConnectLoopback: {
      id: 'net-connect-loopback',
      title: 'Loopback Connect',
      rows: netConnectLoopbackRows,
    },
    netDnsResolve: {
      id: 'net-dns-resolve',
      title: 'DNS Resolve',
      rows: netDnsResolveRows,
    },
    registryCrud: {
      id: 'registry-crud',
      title: 'Registry CRUD',
      rows: registryCrudRows,
    },
    pipeRoundtrip: {
      id: 'pipe-roundtrip',
      title: 'Pipe Roundtrip',
      rows: pipeRoundtripRows,
    },
    cryptoHashVerify: {
      id: 'crypto-hash-verify',
      title: 'Crypto Hash Verify',
      rows: cryptoHashVerifyRows,
    },
    comCreateInstance: {
      id: 'com-create-instance',
      title: 'COM Create Instance',
      rows: comCreateInstanceRows,
    },
    fsWatcher: {
      id: 'fs-watcher',
      title: 'File System Watcher',
      rows: fsWatcherRows,
    },
    extensionSensitivity: {
      id: 'extension-sensitivity',
      title: 'Extension Sensitivity',
      rows: [...extensionSensitivityRows.values()],
    },
  },
}

await mkdir(outputDir, { recursive: true })
await writeFile(outputPath, `${JSON.stringify(payload, null, 2)}\n`)

console.log(`Wrote ${path.relative(process.cwd(), outputPath)}`)

function getArgumentValue(name: string) {
  const index = process.argv.indexOf(name)
  return index >= 0 ? process.argv[index + 1] : undefined
}

function createMicrobenchRow(
  record: CompareRow,
  averageWallMs: number,
  averageBaselineWallMs: number,
): MicrobenchScenarioRow | null {
  if (!Number.isFinite(averageWallMs) || !Number.isFinite(averageBaselineWallMs) || averageBaselineWallMs <= 0) {
    return null
  }

  return {
    avName: record.av_name,
    avProduct: record.av_product,
    average: {
      value: ((averageWallMs - averageBaselineWallMs) / averageBaselineWallMs) * 100,
      wallMs: averageWallMs,
      baselineWallMs: averageBaselineWallMs,
    },
    status: record.status,
  }
}
