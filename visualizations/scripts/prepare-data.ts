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
  status: string
}

type WorkloadMetric = {
  value: number
  wallMs: number
  baselineWallMs: number
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

for (const record of records) {
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
    value: Number(record.first_run_slowdown_pct),
    wallMs: Number(record.first_run_wall_ms),
    baselineWallMs: Number(record.baseline_first_run_wall_ms),
    status: record.status,
  }

  rowsByAv.set(record.av_name, row)
}

const payload = {
  experiment,
  source: path.relative(repoRoot, inputPath).replaceAll('\\', '/'),
  metric: 'first_run_slowdown_pct',
  generatedAt: new Date().toISOString(),
  rows: [...rowsByAv.values()],
}

await mkdir(outputDir, { recursive: true })
await writeFile(outputPath, `${JSON.stringify(payload, null, 2)}\n`)

console.log(`Wrote ${path.relative(process.cwd(), outputPath)}`)

function getArgumentValue(name: string) {
  const index = process.argv.indexOf(name)
  return index >= 0 ? process.argv[index + 1] : undefined
}
