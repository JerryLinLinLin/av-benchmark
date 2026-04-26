import { MicrobenchScenarioChart } from './MicrobenchScenarioChart'
import type { ManualAxisBreak } from './MicrobenchScenarioChart'
import type { CompilationWorkloadData } from '../data/compilationWorkloads'

type Props = {
  data: CompilationWorkloadData['microbench']['dllLoadUnique']
  onReady: () => void
}

const dllLoadUniqueAxis: ManualAxisBreak = {
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
    [124, 750],
    [136, 1500],
    [148, 3000],
    [160, 5000],
  ]),
  transform: transformDllLoadUniqueValue,
}

export function DllLoadUniqueChart({ data, onReady }: Props) {
  return (
    <MicrobenchScenarioChart
      chartId="dll-load-unique-average"
      title="DLL Load Unique: Average Impact"
      data={data}
      onReady={onReady}
      axis={dllLoadUniqueAxis}
    />
  )
}

function transformDllLoadUniqueValue(value: number) {
  return interpolateAxis(value, [0, 20, 40, 60, 80, 100, 250, 750, 1500, 3000, 5000])
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
