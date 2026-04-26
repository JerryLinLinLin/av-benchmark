import { MicrobenchScenarioChart } from './MicrobenchScenarioChart'
import { memMapFileAxis } from './microbenchAxes'
import type { CompilationWorkloadData } from '../data/compilationWorkloads'

type Props = {
  data: CompilationWorkloadData['microbench']['memMapFile']
  onReady: () => void
}

export function MemMapFileChart({ data, onReady }: Props) {
  return (
    <MicrobenchScenarioChart
      chartId="mem-map-file-average"
      title="Memory Map File: Average Impact"
      data={data}
      onReady={onReady}
      axis={memMapFileAxis}
      scaleNote="Linear y-axis is used because the range fits within 300%."
    />
  )
}
