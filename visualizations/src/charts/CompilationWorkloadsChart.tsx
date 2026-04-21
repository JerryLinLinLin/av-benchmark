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
  actualValue?: number
  capped?: boolean
  status?: string
}

const cleanColor = '#0f9f8f'
const incrementalColor = '#d18a00'

const chartConfig = {
  ripgrep: {
    title: 'Ripgrep',
    axisMax: 60,
  },
  roslyn: {
    title: 'Roslyn',
    axisMax: 190,
  },
} satisfies Record<WorkloadKey, { title: string; axisMax: number }>

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
          <h1>Compilation Workload First-Run Impact</h1>
          <p>
            Impact vs baseline OS. Noisy/anomaly runs are included; capped bars
            show their actual value as a label.
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
  const avNames = rows.map((row) => row.avName)
  const cleanPoints = rows.map((row) => toBarPoint(row[workload].clean, config.axisMax))
  const incrementalPoints = rows.map((row) => toBarPoint(row[workload].incremental, config.axisMax))

  return {
    animation: false,
    backgroundColor: '#ffffff',
    title: {
      text: config.title,
      left: 64,
      top: 4,
      textStyle: {
        color: '#25313d',
        fontSize: 17,
        fontWeight: 650,
      },
    },
    legend: {
      top: 8,
      right: 26,
      itemWidth: 14,
      itemHeight: 10,
      textStyle: {
        color: '#303841',
      },
    },
    grid: {
      left: 76,
      right: 34,
      top: 64,
      bottom: 112,
      containLabel: false,
    },
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
      max: config.axisMax,
      splitNumber: workload === 'ripgrep' ? 7 : 5,
      name: 'Impact (%)',
      nameLocation: 'middle',
      nameGap: 48,
      axisLabel: {
        color: '#46515d',
        formatter: '{value}%',
      },
      splitLine: {
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
    barMaxWidth: 24,
    barGap: '22%',
    data: points.map((point) => point.value),
    itemStyle: { color },
    label: {
      show: true,
      position: 'top' as const,
      color: '#394450',
      fontSize: 11,
      formatter: (params: unknown) => {
        const dataIndex = (params as { dataIndex?: number }).dataIndex ?? -1
        const point = points[dataIndex]
        return point?.capped && point.actualValue !== undefined
          ? `{capped|${formatNumber(point.actualValue)}%}`
          : ''
      },
      rich: {
        capped: {
          color: '#c73535',
          fontWeight: 700,
        },
      },
    },
    emphasis: {
      focus: 'series' as const,
    },
    markLine: {
      silent: true,
      symbol: 'none',
      lineStyle: { color: '#9aa4af', type: 'dashed' as const, width: 1 },
      label: { show: false },
      data: [{ yAxis: 0 }],
    },
  }
}

function toBarPoint(metric: BuildMetric | null, cap: number): BarPoint {
  if (!metric) {
    return { value: null }
  }

  const capped = metric.value > cap
  const displayValue = Math.max(0, metric.value)
  return {
    value: capped ? cap : displayValue,
    actualValue: metric.value,
    capped,
    status: metric.status,
  }
}

function formatNumber(value: number) {
  return Number.isFinite(value) ? value.toFixed(1) : '-'
}
