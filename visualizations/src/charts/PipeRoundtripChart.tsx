import { MicrobenchScenarioChart } from './MicrobenchScenarioChart'
import type { CompilationWorkloadData } from '../data/compilationWorkloads'

type Props = {
  data: CompilationWorkloadData['microbench']['pipeRoundtrip']
  onReady: () => void
}

export function PipeRoundtripChart({ data, onReady }: Props) {
  return (
    <MicrobenchScenarioChart
      chartId="pipe-roundtrip-average"
      title="Pipe Roundtrip: Average Impact"
      data={data}
      onReady={onReady}
    />
  )
}
