import { MicrobenchScenarioChart } from './MicrobenchScenarioChart'
import type { ManualAxisBreak } from './MicrobenchScenarioChart'
import type { CompilationWorkloadData } from '../data/compilationWorkloads'

type Props = {
  data: CompilationWorkloadData['microbench']['fileWriteContent']
  onReady: () => void
}

const fileWriteContentAxis: ManualAxisBreak = {
  max: 160,
  ticks: [0, 18.4, 36.8, 55.2, 73.6, 92, 108, 124, 136, 148, 160],
  tickLabels: new Map([
    [0, 0],
    [18.4, 20],
    [36.8, 40],
    [55.2, 60],
    [73.6, 80],
    [92, 100],
    [108, 250],
    [124, 1000],
    [136, 5000],
    [148, 10000],
    [160, 35000],
  ]),
  transform: transformFileWriteContentValue,
}

export function FileWriteContentChart({ data, onReady }: Props) {
  return (
    <MicrobenchScenarioChart
      chartId="file-write-content-average"
      title="File Write Content: Average Impact"
      data={data}
      onReady={onReady}
      axis={fileWriteContentAxis}
    />
  )
}

function transformFileWriteContentValue(value: number) {
  return interpolateAxis(value, [0, 20, 40, 60, 80, 100, 250, 1000, 5000, 10000, 35000])
}

function interpolateAxis(value: number, actualStops: number[]) {
  const plotStops = [0, 18.4, 36.8, 55.2, 73.6, 92, 108, 124, 136, 148, 160]

  if (value <= actualStops[0]) {
    return plotStops[0]
  }

  for (let index = 1; index < actualStops.length; index += 1) {
    const leftActual = actualStops[index - 1]
    const rightActual = actualStops[index]
    if (value <= rightActual) {
      const leftPlot = plotStops[index - 1]
      const rightPlot = plotStops[index]
      const ratio = (value - leftActual) / (rightActual - leftActual)
      return leftPlot + ratio * (rightPlot - leftPlot)
    }
  }

  return plotStops[plotStops.length - 1]
}
