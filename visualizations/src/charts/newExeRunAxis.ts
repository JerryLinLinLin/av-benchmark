import type { ManualAxisBreak } from './MicrobenchScenarioChart'

export const newExeRunAxis: ManualAxisBreak = {
  max: 168,
  ticks: [0, 18, 36, 54, 72, 90, 106, 122, 136, 148, 158, 168],
  tickLabels: new Map([
    [0, 0],
    [18, 20],
    [36, 40],
    [54, 60],
    [72, 80],
    [90, 100],
    [106, 200],
    [122, 500],
    [136, 1000],
    [148, 2000],
    [158, 4000],
    [168, 8000],
  ]),
  transform: transformNewExeRunValue,
}

function transformNewExeRunValue(value: number) {
  return interpolateAxis(value, [0, 20, 40, 60, 80, 100, 200, 500, 1000, 2000, 4000, 8000])
}

function interpolateAxis(value: number, actualStops: number[]) {
  const plotStops = [0, 18, 36, 54, 72, 90, 106, 122, 136, 148, 158, 168]

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
