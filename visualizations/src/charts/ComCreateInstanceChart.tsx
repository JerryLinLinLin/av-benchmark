import { MicrobenchScenarioChart } from './MicrobenchScenarioChart'
import type { CompilationWorkloadData } from '../data/compilationWorkloads'

type Props = {
  data: CompilationWorkloadData['microbench']['comCreateInstance']
  onReady: () => void
}

export function ComCreateInstanceChart({ data, onReady }: Props) {
  return (
    <MicrobenchScenarioChart
      chartId="com-create-instance-average"
      title="COM Create Instance: Average Impact"
      data={data}
      onReady={onReady}
    />
  )
}
