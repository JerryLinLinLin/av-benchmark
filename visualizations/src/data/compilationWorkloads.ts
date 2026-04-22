export type ImpactMetric = {
  value: number
  wallMs: number
  baselineWallMs: number
}

export type BuildMetric = {
  cloudCold: ImpactMetric
  average: ImpactMetric
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
  metrics: Array<'cloudCold' | 'average'>
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
