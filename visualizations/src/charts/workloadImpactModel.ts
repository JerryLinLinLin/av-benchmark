import type {
  BuildMetric,
  CompilationWorkloadData,
  ExtensionSensitivityRow,
  MicrobenchScenarioData,
  MicrobenchScenarioRow,
} from '../data/compilationWorkloads'

export type WorkloadCategory =
  | 'Build'
  | 'File system'
  | 'Process / EXE'
  | 'DLL / COM'
  | 'Memory'
  | 'Network / IPC'
  | 'Registry / Crypto'
  | 'Extension sensitivity'

export type WorkloadColumn = {
  key: string
  label: string
  category: WorkloadCategory
  value: (avName: string) => number | null
}

export const workloadCategories: WorkloadCategory[] = [
  'Build',
  'File system',
  'Process / EXE',
  'DLL / COM',
  'Memory',
  'Network / IPC',
  'Registry / Crypto',
  'Extension sensitivity',
]

export function getAvNames(data: CompilationWorkloadData) {
  return [...data.rows.map((row) => row.avName)].sort((left, right) =>
    left.localeCompare(right),
  )
}

export function buildWorkloadColumns(data: CompilationWorkloadData): WorkloadColumn[] {
  const microbench = data.microbench

  return [
    {
      key: 'ripgrep-build',
      label: 'Ripgrep build',
      category: 'Build',
      value: (avName) => {
        const row = data.rows.find((item) => item.avName === avName)
        return weightedBuildImpact(row?.ripgrep.clean, row?.ripgrep.incremental)
      },
    },
    {
      key: 'roslyn-build',
      label: 'Roslyn build',
      category: 'Build',
      value: (avName) => {
        const row = data.rows.find((item) => item.avName === avName)
        return weightedBuildImpact(row?.roslyn.clean, row?.roslyn.incremental)
      },
    },
    scenarioColumn('file-create-delete', 'File create/delete', 'File system', microbench.fileCreateDelete),
    scenarioColumn('file-enum-large-dir', 'Large dir enum', 'File system', microbench.fileEnumLargeDir),
    scenarioColumn('file-write-content', 'File write content', 'File system', microbench.fileWriteContent),
    scenarioColumn('archive-extract', 'Archive extract', 'File system', microbench.archiveExtract),
    scenarioColumn('hardlink-create', 'Hardlink create', 'File system', microbench.hardlinkCreate),
    scenarioColumn('junction-create', 'Junction create', 'File system', microbench.junctionCreate),
    scenarioColumn('fs-watcher', 'FS watcher', 'File system', microbench.fsWatcher),
    scenarioColumn('process-create-wait', 'Process create/wait', 'Process / EXE', microbench.processCreateWait),
    {
      key: 'new-exe-sequence',
      label: 'New EXE sequence',
      category: 'Process / EXE',
      value: (avName) =>
        maxImpact([
          rowForScenario(microbench.newExeRun, avName),
          rowForScenario(microbench.newExeRunMotw, avName),
        ]),
    },
    scenarioColumn('dll-load-unique', 'DLL load unique', 'DLL / COM', microbench.dllLoadUnique),
    scenarioColumn('com-create-instance', 'COM create', 'DLL / COM', microbench.comCreateInstance),
    scenarioColumn('thread-create', 'Thread create', 'Memory', microbench.threadCreate),
    scenarioColumn('mem-alloc-protect', 'Mem alloc/protect', 'Memory', microbench.memAllocProtect),
    scenarioColumn('mem-map-file', 'Mem map file', 'Memory', microbench.memMapFile),
    scenarioColumn('net-connect-loopback', 'Loopback connect', 'Network / IPC', microbench.netConnectLoopback),
    scenarioColumn('net-dns-resolve', 'DNS resolve', 'Network / IPC', microbench.netDnsResolve),
    scenarioColumn('pipe-roundtrip', 'Pipe roundtrip', 'Network / IPC', microbench.pipeRoundtrip),
    scenarioColumn('registry-crud', 'Registry CRUD', 'Registry / Crypto', microbench.registryCrud),
    scenarioColumn('crypto-hash-verify', 'Crypto hash', 'Registry / Crypto', microbench.cryptoHashVerify),
    {
      key: 'extension-sensitivity',
      label: 'Extension sensitivity',
      category: 'Extension sensitivity',
      value: (avName) => extensionSensitivityAverage(microbench.extensionSensitivity.rows, avName),
    },
  ]
}

export function valuesForColumn(column: WorkloadColumn, avNames: string[]) {
  return avNames
    .map((avName) => column.value(avName))
    .filter((value): value is number => value !== null && Number.isFinite(value))
}

export function normalizedLogScore(value: number | null, values: number[]) {
  if (value === null || !Number.isFinite(value) || values.length === 0) {
    return null
  }

  const min = Math.min(...values)
  const max = Math.max(...values)
  if (max <= min) {
    return 0
  }

  return (Math.log1p(value - min) / Math.log1p(max - min)) * 100
}

export function normalizedLevel(value: number | null, values: number[]) {
  const score = normalizedLogScore(value, values)
  if (score === null) {
    return null
  }

  return Math.min(6, Math.max(0, Math.floor((score / 100) * 7)))
}

export function levelPenaltyScore(level: number) {
  return 2 ** level
}

export function average(values: Array<number | null>) {
  const finiteValues = values.filter((value): value is number => value !== null && Number.isFinite(value))
  return finiteValues.length
    ? finiteValues.reduce((sum, value) => sum + value, 0) / finiteValues.length
    : null
}

export function formatImpactPercent(value: number | null) {
  if (value === null || !Number.isFinite(value)) {
    return 'n/a'
  }

  if (value >= 10000) {
    return `${(value / 1000).toFixed(0)}k%`
  }

  if (value >= 1000) {
    return `${(value / 1000).toFixed(1)}k%`
  }

  if (value >= 100) {
    return `${value.toFixed(0)}%`
  }

  return `${value.toFixed(1)}%`
}

function scenarioColumn(
  key: string,
  label: string,
  category: WorkloadCategory,
  scenario: MicrobenchScenarioData,
): WorkloadColumn {
  return {
    key,
    label,
    category,
    value: (avName) => rowImpact(rowForScenario(scenario, avName)),
  }
}

function rowForScenario(scenario: MicrobenchScenarioData, avName: string) {
  return scenario.rows.find((row) => row.avName === avName) ?? null
}

function rowImpact(row: MicrobenchScenarioRow | null) {
  return row ? Math.max(0, row.average.value) : null
}

function metricImpact(metric: BuildMetric | null | undefined) {
  return metric ? Math.max(0, metric.average.value) : null
}

function weightedBuildImpact(
  clean: BuildMetric | null | undefined,
  incremental: BuildMetric | null | undefined,
) {
  const cleanImpact = metricImpact(clean)
  const incrementalImpact = metricImpact(incremental)
  if (cleanImpact === null && incrementalImpact === null) {
    return null
  }

  if (cleanImpact === null) {
    return incrementalImpact
  }

  if (incrementalImpact === null) {
    return cleanImpact
  }

  return cleanImpact * 0.25 + incrementalImpact * 0.75
}

function maxImpact(rows: Array<MicrobenchScenarioRow | null>) {
  const values = rows
    .map(rowImpact)
    .filter((value): value is number => value !== null && Number.isFinite(value))
  return values.length ? Math.max(...values) : null
}

function extensionSensitivityAverage(rows: ExtensionSensitivityRow[], avName: string) {
  const row = rows.find((item) => item.avName === avName)
  if (!row) {
    return null
  }

  const values = [row.exe, row.dll, row.js, row.ps1]
    .map(rowImpact)
    .filter((value): value is number => value !== null && Number.isFinite(value))
  return values.length ? values.reduce((sum, value) => sum + value, 0) / values.length : null
}
