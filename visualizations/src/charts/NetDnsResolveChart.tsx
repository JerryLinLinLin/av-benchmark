import { MicrobenchScenarioChart } from './MicrobenchScenarioChart'
import type { CompilationWorkloadData } from '../data/compilationWorkloads'

type Props = {
  data: CompilationWorkloadData['microbench']['netDnsResolve']
  onReady: () => void
}

export function NetDnsResolveChart({ data, onReady }: Props) {
  return (
    <MicrobenchScenarioChart
      chartId="net-dns-resolve-average"
      title="DNS Resolve: Average Impact"
      data={data}
      onReady={onReady}
    />
  )
}
