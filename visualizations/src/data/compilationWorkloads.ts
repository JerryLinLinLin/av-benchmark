export type BuildMetric = {
  value: number
  wallMs: number
  baselineWallMs: number
  status: string
}

export type CompilationWorkloadRow = {
  avName: string
  avProduct: string
  ripgrep: {
    clean: BuildMetric | null
    incremental: BuildMetric | null
  }
  roslyn: {
    clean: BuildMetric | null
    incremental: BuildMetric | null
  }
}

export type CompilationWorkloadData = {
  experiment: string
  source: string
  metric: 'first_run_slowdown_pct'
  generatedAt: string
  rows: CompilationWorkloadRow[]
}

export async function loadCompilationWorkloadData(experiment: string) {
  const response = await fetch(`/generated/${experiment}/compilation-workloads.json`)
  if (!response.ok) {
    throw new Error(`Unable to load compilation workload data for ${experiment}`)
  }

  return (await response.json()) as CompilationWorkloadData
}
