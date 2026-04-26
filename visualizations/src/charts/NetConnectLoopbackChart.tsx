import { MicrobenchScenarioChart } from './MicrobenchScenarioChart'
import { netConnectLoopbackAxis } from './microbenchAxes'
import type { CompilationWorkloadData } from '../data/compilationWorkloads'

type Props = {
  data: CompilationWorkloadData['microbench']['netConnectLoopback']
  onReady: () => void
}

export function NetConnectLoopbackChart({ data, onReady }: Props) {
  return (
    <MicrobenchScenarioChart
      chartId="net-connect-loopback-average"
      title="Loopback Connect: Average Impact"
      data={data}
      onReady={onReady}
      axis={netConnectLoopbackAxis}
      scaleNote="Linear y-axis is used because the range fits within 200%."
    />
  )
}
