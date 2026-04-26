import type { ManualAxisBreak } from './MicrobenchScenarioChart'

export const registryCrudAxis: ManualAxisBreak = createAxis([
  0, 20, 40, 60, 80, 100, 250, 750, 1500, 3000,
])

export const fsWatcherAxis: ManualAxisBreak = createAxis([
  0, 20, 40, 60, 80, 100, 150, 250, 500, 1000,
])

export const cryptoHashVerifyAxis: ManualAxisBreak = createLinearAxis([0, 2, 4, 6, 8, 10])

export const memAllocProtectAxis: ManualAxisBreak = createLinearAxis([
  0, 25, 50, 75, 100, 125, 150,
])

export const netConnectLoopbackAxis: ManualAxisBreak = createLinearAxis([
  0, 25, 50, 75, 100, 125, 150, 175, 200,
])

export const memMapFileAxis: ManualAxisBreak = createLinearAxis([
  0, 50, 100, 150, 200, 250, 300,
])

function createAxis(actualStops: number[]): ManualAxisBreak {
  const plotStops = [0, 18.4, 36.8, 55.2, 73.6, 92, 108, 124, 136, 148]

  return {
    max: plotStops[plotStops.length - 1],
    ticks: plotStops,
    tickLabels: new Map(plotStops.map((tick, index) => [tick, actualStops[index]])),
    transform: (value: number) => interpolateAxis(value, actualStops, plotStops),
  }
}

function createLinearAxis(ticks: number[]): ManualAxisBreak {
  const max = ticks[ticks.length - 1]

  return {
    max,
    ticks,
    tickLabels: new Map(ticks.map((tick) => [tick, tick])),
    transform: (value: number) => Math.min(value, max),
  }
}

function interpolateAxis(value: number, actualStops: number[], plotStops: number[]) {
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
