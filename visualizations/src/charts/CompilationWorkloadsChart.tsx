import { useCallback, useState } from 'react'
import ReactECharts from 'echarts-for-react'
import type { EChartsOption } from 'echarts'
import type {
  BuildMetric,
  CompilationWorkloadData,
  CompilationWorkloadRow,
} from '../data/compilationWorkloads'

type Props = {
  data: CompilationWorkloadData
  onReady?: () => void
}

type WorkloadKey = 'ripgrep' | 'roslyn'
type BarPoint = {
  value: number | null
  status?: string
}

type SortedWorkloadRow = {
  avName: string
  clean: number
  incremental: number
  total: number
}

type ManualAxisBreak = {
  max: number
  ticks: number[]
  tickLabels: Map<number, number>
  transform: (value: number) => number
}

const cleanColor = '#0f9f8f'
const incrementalColor = '#d18a00'

const ripgrepBrokenAxis: ManualAxisBreak = {
  max: 138,
  ticks: [
    0,
    15.3,
    30.7,
    46,
    61.3,
    76.7,
    92,
    104,
    116,
    124,
    136,
  ],
  tickLabels: new Map([
    [0, 0],
    [15.3, 10],
    [30.7, 20],
    [46, 30],
    [61.3, 40],
    [76.7, 50],
    [92, 60],
    [104, 200],
    [116, 300],
    [124, 700],
    [136, 800],
  ]),
  transform: transformRipgrepValue,
}

const roslynBrokenAxis: ManualAxisBreak = {
  max: 130,
  ticks: [0, 11.5, 23, 34.5, 46, 57.5, 69, 80.5, 92, 104, 114, 124],
  tickLabels: new Map([
    [0, 0],
    [11.5, 10],
    [23, 20],
    [34.5, 30],
    [46, 40],
    [57.5, 50],
    [69, 60],
    [80.5, 70],
    [92, 80],
    [104, 200],
    [114, 250],
    [124, 300],
  ]),
  transform: transformRoslynValue,
}

const chartConfig = {
  ripgrep: {
    title: 'Ripgrep Build: Cloud-Cold Impact',
    subtitle: 'Clean + incremental build impact, sorted from lowest to highest',
    footnote: 'Cloud-cold means first cloud/reputation exposure; VM reset removes local cache between runs. Broken y-axis emphasizes 0-60%. Negative values are shown as 0%.',
    axisMax: 60,
  },
  roslyn: {
    title: 'Roslyn Build: Cloud-Cold Impact',
    subtitle: 'Clean + incremental build impact, sorted from lowest to highest',
    footnote: 'Cloud-cold means first cloud/reputation exposure; VM reset removes local cache between runs. Broken y-axis emphasizes 0-80%. Negative values are shown as 0%.',
    axisMax: 80,
  },
} satisfies Record<
  WorkloadKey,
  { title: string; subtitle: string; footnote: string; axisMax: number }
>

export function CompilationWorkloadsChart({ data, onReady }: Props) {
  const [readyCount, setReadyCount] = useState(0)
  const handleChartReady = useCallback(() => {
    setReadyCount((current) => {
      const next = current + 1
      if (next === 2) {
        onReady?.()
      }
      return next
    })
  }, [onReady])

  return (
    <div className="figure">
      <header className="figure-header">
        <div>
          <h1>Compilation Workload Cloud-Cold Impact</h1>
          <p>
            Impact vs baseline OS before cloud reputation/cache has warmed for
            the workload. Negative values are shown as 0%.
          </p>
        </div>
        <div className="legend" aria-hidden="true">
          <span><i className="swatch clean" />Clean build</span>
          <span><i className="swatch incremental" />Incremental build</span>
        </div>
      </header>

      <WorkloadChart data={data.rows} workload="ripgrep" onReady={handleChartReady} />
      <WorkloadChart data={data.rows} workload="roslyn" onReady={handleChartReady} />
      <span hidden>{readyCount}</span>
    </div>
  )
}

function WorkloadChart({
  data,
  workload,
  onReady,
}: {
  data: CompilationWorkloadRow[]
  workload: WorkloadKey
  onReady: () => void
}) {
  const option = buildWorkloadOption(data, workload)

  return (
    <section className="chart-card" data-workload={workload}>
      <ReactECharts
        option={option}
        notMerge
        lazyUpdate
        onChartReady={onReady}
        style={{ height: '430px', width: '100%' }}
        opts={{ renderer: 'canvas' }}
      />
    </section>
  )
}

function buildWorkloadOption(rows: CompilationWorkloadRow[], workload: WorkloadKey): EChartsOption {
  const config = chartConfig[workload]
  const sortedRows = getSortedWorkloadRows(rows, workload)
  const avNames = sortedRows.map((row) => row.avName)
  const brokenAxis = workload === 'ripgrep' ? ripgrepBrokenAxis : roslynBrokenAxis
  const chartMax = brokenAxis?.max ?? config.axisMax
  const { cleanPoints, incrementalPoints } = getStackedPoints(sortedRows, brokenAxis)
  return {
    animation: false,
    backgroundColor: '#ffffff',
    title: {
      text: config.title,
      subtext: config.subtitle,
      left: 72,
      top: 0,
      textStyle: {
        color: '#17202a',
        fontSize: 20,
        fontWeight: 700,
      },
      subtextStyle: {
        color: '#596573',
        fontSize: 13,
      },
    },
    legend: {
      top: 12,
      right: 28,
      itemWidth: 14,
      itemHeight: 10,
      itemGap: 16,
      textStyle: {
        color: '#35404b',
        fontSize: 13,
      },
      data: ['Clean build', 'Incremental build'],
    },
    grid: {
      left: 102,
      right: 34,
      top: 76,
      bottom: 122,
      containLabel: false,
    },
    graphic: [
      {
        type: 'text',
        left: 76,
        bottom: 8,
        style: {
          text: config.footnote,
          fill: '#677380',
          font: '11px system-ui, Segoe UI, Roboto, sans-serif',
        },
      },
    ],
    tooltip: {
      trigger: 'axis',
      axisPointer: { type: 'shadow' },
      extraCssText: 'box-shadow: 0 12px 32px rgba(15, 23, 42, 0.18);',
    },
    xAxis: {
      type: 'category',
      data: avNames,
      axisTick: { alignWithLabel: true },
      axisLine: { lineStyle: { color: '#c8d0d8' } },
      name: 'Antivirus product',
      nameLocation: 'middle',
      nameGap: 72,
      nameTextStyle: {
        color: '#596573',
        fontSize: 12,
        fontWeight: 600,
      },
      axisLabel: {
        color: '#3b4652',
        interval: 0,
        rotate: 36,
        fontSize: 13,
      },
    },
    yAxis: {
      type: 'value',
      min: 0,
      max: chartMax,
      splitNumber: workload === 'ripgrep' ? 7 : 5,
      name: 'Cloud-cold impact (%)',
      nameLocation: 'middle',
      nameGap: 62,
      nameTextStyle: {
        color: '#596573',
        fontSize: 12,
        fontWeight: 600,
      },
      axisLabel: {
        color: '#46515d',
        fontSize: 12,
        customValues: brokenAxis?.ticks,
        formatter: (value: number) => formatAxisLabel(value, brokenAxis, config.axisMax),
      },
      axisTick: {
        customValues: brokenAxis?.ticks,
      },
      splitLine: {
        show: true,
        showMaxLine: false,
        lineStyle: { color: '#e8edf2' },
      },
      axisLine: { show: false },
    },
    series: [
      buildSeries('Clean build', cleanPoints, cleanColor),
      buildSeries('Incremental build', incrementalPoints, incrementalColor),
    ],
  }
}

function buildSeries(name: string, points: BarPoint[], color: string) {
  return {
    name,
    type: 'bar' as const,
    stack: 'total-impact',
    barMaxWidth: 24,
    data: points.map((point) => point.value),
    itemStyle: { color },
    emphasis: {
      focus: 'series' as const,
    },
  }
}

function getSortedWorkloadRows(rows: CompilationWorkloadRow[], workload: WorkloadKey) {
  return rows
    .map((row): SortedWorkloadRow => {
      const clean = metricValue(row[workload].clean)
      const incremental = metricValue(row[workload].incremental)
      return {
        avName: row.avName,
        clean,
        incremental,
        total: clean + incremental,
      }
    })
    .sort((left, right) => left.total - right.total)
}

function getStackedPoints(rows: SortedWorkloadRow[], brokenAxis: ManualAxisBreak | undefined) {
  if (!brokenAxis) {
    return {
      cleanPoints: rows.map((row) => ({ value: row.clean })),
      incrementalPoints: rows.map((row) => ({ value: row.incremental })),
    }
  }

  return {
    cleanPoints: rows.map((row) => ({ value: brokenAxis.transform(row.clean) })),
    incrementalPoints: rows.map((row) => ({
      value: brokenAxis.transform(row.total) - brokenAxis.transform(row.clean),
    })),
  }
}

function metricValue(metric: BuildMetric | null) {
  return metric ? Math.max(0, metric.value) : 0
}

function transformRipgrepValue(value: number) {
  if (value <= 60) {
    return (value / 60) * 92
  }

  if (value <= 300) {
    return 104 + ((Math.max(value, 200) - 200) / 100) * 12
  }

  return 124 + ((Math.min(value, 800) - 700) / 100) * 12
}

function transformRoslynValue(value: number) {
  if (value <= 80) {
    return (value / 80) * 92
  }

  return 104 + ((Math.min(Math.max(value, 200), 320) - 200) / 120) * 24
}

function formatAxisLabel(value: number, brokenAxis: ManualAxisBreak | undefined, cap: number) {
  if (!brokenAxis) {
    return value <= cap ? `${value}%` : ''
  }

  const label = brokenAxis.tickLabels.get(value)
  return label === undefined ? '' : `${label}%`
}
