import { MicrobenchScenarioChart } from './MicrobenchScenarioChart'
import { fsWatcherAxis } from './microbenchAxes'
import type { CompilationWorkloadData } from '../data/compilationWorkloads'

type Props = {
  data: CompilationWorkloadData['microbench']['fsWatcher']
  onReady: () => void
}

export function FsWatcherChart({ data, onReady }: Props) {
  return (
    <MicrobenchScenarioChart
      chartId="fs-watcher-average"
      title="File System Watcher: Average Impact"
      data={data}
      onReady={onReady}
      axis={fsWatcherAxis}
    />
  )
}
