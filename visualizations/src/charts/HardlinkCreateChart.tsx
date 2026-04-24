import { MicrobenchScenarioChart } from './MicrobenchScenarioChart'
import type { CompilationWorkloadData } from '../data/compilationWorkloads'

type Props = {
  data: CompilationWorkloadData['microbench']['hardlinkCreate']
  onReady: () => void
}

export function HardlinkCreateChart({ data, onReady }: Props) {
  return (
    <MicrobenchScenarioChart
      chartId="hardlink-create-average"
      title="Hardlink Create: Average Impact"
      data={data}
      onReady={onReady}
    />
  )
}
