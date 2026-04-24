import { MicrobenchScenarioChart } from './MicrobenchScenarioChart'
import type { CompilationWorkloadData } from '../data/compilationWorkloads'

type Props = {
  data: CompilationWorkloadData['microbench']['threadCreate']
  onReady: () => void
}

export function ThreadCreateChart({ data, onReady }: Props) {
  return (
    <MicrobenchScenarioChart
      chartId="thread-create-average"
      title="Thread Create: Average Impact"
      data={data}
      onReady={onReady}
    />
  )
}
