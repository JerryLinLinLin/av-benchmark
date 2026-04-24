import { MicrobenchScenarioChart } from './MicrobenchScenarioChart'
import type { CompilationWorkloadData } from '../data/compilationWorkloads'

type Props = {
  data: CompilationWorkloadData['microbench']['archiveExtract']
  onReady: () => void
}

export function ArchiveExtractChart({ data, onReady }: Props) {
  return (
    <MicrobenchScenarioChart
      chartId="archive-extract-average"
      title="Archive Extract: Average Impact"
      data={data}
      onReady={onReady}
    />
  )
}
