import { MicrobenchScenarioChart } from './MicrobenchScenarioChart'
import { registryCrudAxis } from './microbenchAxes'
import type { CompilationWorkloadData } from '../data/compilationWorkloads'

type Props = {
  data: CompilationWorkloadData['microbench']['registryCrud']
  onReady: () => void
}

export function RegistryCrudChart({ data, onReady }: Props) {
  return (
    <MicrobenchScenarioChart
      chartId="registry-crud-average"
      title="Registry CRUD: Average Impact"
      data={data}
      onReady={onReady}
      axis={registryCrudAxis}
    />
  )
}
