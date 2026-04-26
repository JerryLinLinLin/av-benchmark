import { MicrobenchScenarioChart } from './MicrobenchScenarioChart'
import { cryptoHashVerifyAxis } from './microbenchAxes'
import type { CompilationWorkloadData } from '../data/compilationWorkloads'

type Props = {
  data: CompilationWorkloadData['microbench']['cryptoHashVerify']
  onReady: () => void
}

export function CryptoHashVerifyChart({ data, onReady }: Props) {
  return (
    <MicrobenchScenarioChart
      chartId="crypto-hash-verify-average"
      title="Crypto Hash Verify: Average Impact"
      data={data}
      onReady={onReady}
      axis={cryptoHashVerifyAxis}
      scaleNote="Linear y-axis is used because all impacts are below 10%."
    />
  )
}
