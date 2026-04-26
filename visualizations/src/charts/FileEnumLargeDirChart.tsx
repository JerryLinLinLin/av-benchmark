import { MicrobenchScenarioChart } from './MicrobenchScenarioChart'
import type { CompilationWorkloadData } from '../data/compilationWorkloads'

type Props = {
  data: CompilationWorkloadData['microbench']['fileEnumLargeDir']
  onReady: () => void
}

export function FileEnumLargeDirChart({ data, onReady }: Props) {
  return (
    <MicrobenchScenarioChart
      chartId="file-enum-large-dir-average"
      title="File Enum Large Dir: Average Impact"
      data={data}
      onReady={onReady}
    />
  )
}
