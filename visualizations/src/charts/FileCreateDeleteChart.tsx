import { MicrobenchScenarioChart } from './MicrobenchScenarioChart'
import type { CompilationWorkloadData } from '../data/compilationWorkloads'

type Props = {
  data: CompilationWorkloadData['microbench']['fileCreateDelete']
  onReady: () => void
}

export function FileCreateDeleteChart({ data, onReady }: Props) {
  return (
    <MicrobenchScenarioChart
      chartId="file-create-delete-average"
      title="File Create/Delete: Average Impact"
      data={data}
      onReady={onReady}
    />
  )
}
