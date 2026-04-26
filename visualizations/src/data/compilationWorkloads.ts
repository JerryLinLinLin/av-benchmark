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

export type MicrobenchScenarioRow = {
  avName: string
  avProduct: string
  average: ImpactMetric
  status: string
}

export type MicrobenchScenarioData = {
  id: string
  title: string
  rows: MicrobenchScenarioRow[]
}

export type ExtensionSensitivityRow = {
  avName: string
  avProduct: string
  exe: MicrobenchScenarioRow | null
  dll: MicrobenchScenarioRow | null
  js: MicrobenchScenarioRow | null
  ps1: MicrobenchScenarioRow | null
}

export type ExtensionSensitivityData = {
  id: string
  title: string
  rows: ExtensionSensitivityRow[]
}

export type CompilationWorkloadData = {
  experiment: string
  source: string
  metrics: Array<'cloudCold' | 'average'>
  generatedAt: string
  rows: CompilationWorkloadRow[]
  microbench: {
    fileCreateDelete: MicrobenchScenarioData
    archiveExtract: MicrobenchScenarioData
    fileEnumLargeDir: MicrobenchScenarioData
    hardlinkCreate: MicrobenchScenarioData
    junctionCreate: MicrobenchScenarioData
    processCreateWait: MicrobenchScenarioData
    dllLoadUnique: MicrobenchScenarioData
    fileWriteContent: MicrobenchScenarioData
    newExeRun: MicrobenchScenarioData
    newExeRunMotw: MicrobenchScenarioData
    threadCreate: MicrobenchScenarioData
    memAllocProtect: MicrobenchScenarioData
    memMapFile: MicrobenchScenarioData
    netConnectLoopback: MicrobenchScenarioData
    netDnsResolve: MicrobenchScenarioData
    registryCrud: MicrobenchScenarioData
    pipeRoundtrip: MicrobenchScenarioData
    cryptoHashVerify: MicrobenchScenarioData
    comCreateInstance: MicrobenchScenarioData
    fsWatcher: MicrobenchScenarioData
    extensionSensitivity: ExtensionSensitivityData
  }
}

export async function loadCompilationWorkloadData(experiment: string) {
  const response = await fetch(`/generated/${experiment}/compilation-workloads.json`)
  if (!response.ok) {
    throw new Error(`Unable to load compilation workload data for ${experiment}`)
  }

  return (await response.json()) as CompilationWorkloadData
}
