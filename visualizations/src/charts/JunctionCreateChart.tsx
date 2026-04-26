import { MicrobenchScenarioChart } from './MicrobenchScenarioChart'
import type { CompilationWorkloadData } from '../data/compilationWorkloads'

type Props = {
  data: CompilationWorkloadData['microbench']['junctionCreate']
  onReady: () => void
}

export function JunctionCreateChart({ data, onReady }: Props) {
  return (
    <MicrobenchScenarioChart
      chartId="junction-create-average"
      title="Junction Create: Average Impact"
      data={data}
      onReady={onReady}
    />
  )
}
