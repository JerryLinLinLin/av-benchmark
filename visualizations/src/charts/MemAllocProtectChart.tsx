import { MicrobenchScenarioChart } from './MicrobenchScenarioChart'
import { memAllocProtectAxis } from './microbenchAxes'
import type { CompilationWorkloadData } from '../data/compilationWorkloads'

type Props = {
  data: CompilationWorkloadData['microbench']['memAllocProtect']
  onReady: () => void
}

export function MemAllocProtectChart({ data, onReady }: Props) {
  return (
    <MicrobenchScenarioChart
      chartId="mem-alloc-protect-average"
      title="Memory Allocate/Protect: Average Impact"
      data={data}
      onReady={onReady}
      axis={memAllocProtectAxis}
      scaleNote="Linear y-axis is used because the range fits within 150%."
    />
  )
}
