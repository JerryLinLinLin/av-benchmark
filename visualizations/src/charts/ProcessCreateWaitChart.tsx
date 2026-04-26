import { MicrobenchScenarioChart } from './MicrobenchScenarioChart'
import type { CompilationWorkloadData } from '../data/compilationWorkloads'

type Props = {
  data: CompilationWorkloadData['microbench']['processCreateWait']
  onReady: () => void
}

export function ProcessCreateWaitChart({ data, onReady }: Props) {
  return (
    <MicrobenchScenarioChart
      chartId="process-create-wait-average"
      title="Process Create Wait: Average Impact"
      data={data}
      onReady={onReady}
    />
  )
}
