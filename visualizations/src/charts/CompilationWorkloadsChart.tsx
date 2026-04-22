import { useCallback, useEffect, useMemo, useState } from 'react'
import {
  Bar,
  CartesianGrid,
  ComposedChart,
  Label,
  LabelList,
  Scatter,
  XAxis,
  YAxis,
} from 'recharts'
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import {
  ChartContainer,
  ChartLegend,
  ChartLegendContent,
  ChartTooltip,
  type ChartConfig,
} from '@/components/ui/chart'
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

type SortedWorkloadRow = {
  avName: string
  clean: number
  incremental: number
  total: number
}

type ChartDatum = {
  avName: string
  cleanActual: number
  incrementalActual: number
  totalActual: number
  normalAxisMax: number
  cleanPlot: number
  incrementalPlot: number
  totalPlot: number
}

type ManualAxisBreak = {
  max: number
  ticks: number[]
  tickLabels: Map<number, number>
  transform: (value: number) => number
}

type LabelContentProps = {
  x?: number | string
  y?: number | string
  width?: number | string
  value?: number | string
  payload?: ChartDatum
}

type ImpactTooltipProps = {
  active?: boolean
  label?: string
  payload?: Array<{ payload?: ChartDatum }>
}

const seriesConfig = {
  cleanPlot: {
    label: 'Clean build',
    color: 'var(--chart-1)',
  },
  incrementalPlot: {
    label: 'Incremental build',
    color: 'var(--chart-2)',
  },
} satisfies ChartConfig

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

const workloadConfig = {
  ripgrep: {
    title: 'Ripgrep Build: Cloud-Cold Impact',
    subtitle: 'Clean + incremental build impact, sorted from lowest to highest',
    footnote:
      'Cloud-cold means first cloud/reputation exposure; VM reset removes local cache between runs. Broken y-axis emphasizes 0-60%. Negative values are shown as 0%.',
    axisMax: 60,
    brokenAxis: ripgrepBrokenAxis,
  },
  roslyn: {
    title: 'Roslyn Build: Cloud-Cold Impact',
    subtitle: 'Clean + incremental build impact, sorted from lowest to highest',
    footnote:
      'Cloud-cold means first cloud/reputation exposure; VM reset removes local cache between runs. Broken y-axis emphasizes 0-80%. Negative values are shown as 0%.',
    axisMax: 80,
    brokenAxis: roslynBrokenAxis,
  },
} satisfies Record<
  WorkloadKey,
  {
    title: string
    subtitle: string
    footnote: string
    axisMax: number
    brokenAxis: ManualAxisBreak
  }
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
  const config = workloadConfig[workload]
  const chartData = useMemo(() => buildChartData(data, workload), [data, workload])

  useEffect(() => {
    const frame = window.requestAnimationFrame(onReady)
    return () => window.cancelAnimationFrame(frame)
  }, [onReady])

  return (
    <section className="chart-export-shell" data-workload={workload}>
      <Card className="chart-card">
        <CardHeader className="chart-card-header">
          <CardTitle className="chart-card-title">{config.title}</CardTitle>
          <CardDescription>{config.subtitle}</CardDescription>
        </CardHeader>
        <CardContent className="chart-card-content">
          <ChartContainer className="impact-chart" config={seriesConfig}>
            <ComposedChart
              accessibilityLayer
              data={chartData}
              margin={{ top: 12, right: 18, bottom: 66, left: 28 }}
            >
              <CartesianGrid vertical={false} strokeDasharray="3 3" />
              <XAxis
                dataKey="avName"
                interval={0}
                angle={-36}
                textAnchor="end"
                height={82}
                tickLine={false}
                axisLine={false}
                tickMargin={12}
              >
                <Label
                  value="Antivirus product"
                  position="insideBottom"
                  offset={-54}
                  className="axis-label"
                />
              </XAxis>
              <YAxis
                domain={[0, config.brokenAxis.max]}
                ticks={config.brokenAxis.ticks}
                tickFormatter={(value) =>
                  formatAxisLabel(Number(value), config.brokenAxis)
                }
                tickLine={false}
                axisLine={false}
                width={74}
              >
                <Label
                  value="Cloud-cold impact (%)"
                  angle={-90}
                  position="insideLeft"
                  offset={-14}
                  className="axis-label"
                />
              </YAxis>
              <ChartTooltip
                cursor={false}
                content={<ImpactTooltip />}
              />
              <ChartLegend
                verticalAlign="top"
                align="left"
                content={<ChartLegendContent className="chart-legend" />}
              />
              <Bar
                dataKey="cleanPlot"
                name="cleanPlot"
                stackId="impact"
                fill="var(--color-cleanPlot)"
                radius={[0, 0, 4, 4]}
                maxBarSize={40}
              />
              <Bar
                dataKey="incrementalPlot"
                name="incrementalPlot"
                stackId="impact"
                fill="var(--color-incrementalPlot)"
                radius={[4, 4, 0, 0]}
                maxBarSize={40}
              >
              </Bar>
              <Scatter
                dataKey="totalPlot"
                legendType="none"
                fill="transparent"
              >
                <LabelList dataKey="totalActual" content={renderImpactLabel} />
              </Scatter>
            </ComposedChart>
          </ChartContainer>
        </CardContent>
        <CardFooter className="chart-card-footer">{config.footnote}</CardFooter>
      </Card>
    </section>
  )
}

function buildChartData(rows: CompilationWorkloadRow[], workload: WorkloadKey) {
  const { brokenAxis } = workloadConfig[workload]

  return getSortedWorkloadRows(rows, workload).map((row): ChartDatum => {
    const cleanPlot = brokenAxis.transform(row.clean)
    const totalPlot = brokenAxis.transform(row.total)

    return {
      avName: row.avName,
      cleanActual: row.clean,
      incrementalActual: row.incremental,
      totalActual: row.total,
      normalAxisMax: workloadConfig[workload].axisMax,
      cleanPlot,
      incrementalPlot: totalPlot - cleanPlot,
      totalPlot,
    }
  })
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

function ImpactTooltip({ active, label, payload }: ImpactTooltipProps) {
  if (!active || !payload?.length) {
    return null
  }

  const row = payload[0]?.payload
  if (!row) {
    return null
  }

  return (
    <div className="chart-tooltip">
      <div className="chart-tooltip-title">{label}</div>
      <TooltipRow
        className="clean"
        label="Clean build"
        value={row.cleanActual}
      />
      <TooltipRow
        className="incremental"
        label="Incremental build"
        value={row.incrementalActual}
      />
      <div className="chart-tooltip-total">
        <span>Total impact</span>
        <strong>{formatPercent(row.totalActual)}</strong>
      </div>
    </div>
  )
}

function TooltipRow({
  className,
  label,
  value,
}: {
  className: string
  label: string
  value: number
}) {
  return (
    <div className="chart-tooltip-row">
      <span>
        <i className={className} />
        {label}
      </span>
      <strong>{formatPercent(value)}</strong>
    </div>
  )
}

function renderImpactLabel(props: unknown) {
  const { x, y, width, value, payload } = props as LabelContentProps
  const actualValue = payload?.totalActual ?? Number(value)
  if (!Number.isFinite(actualValue)) {
    return null
  }

  const xValue = Number(x)
  const yValue = Number(y)
  const widthValue = Number(width)
  if (![xValue, yValue, widthValue].every(Number.isFinite)) {
    return null
  }

  return (
    <text
      x={xValue + widthValue / 2}
      y={Math.max(14, yValue - 8)}
      textAnchor="middle"
      className={actualValue > (payload?.normalAxisMax ?? 80) ? 'outlier-label' : 'impact-label'}
    >
      {formatPercent(actualValue)}
    </text>
  )
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

function formatAxisLabel(value: number, brokenAxis: ManualAxisBreak) {
  const label = brokenAxis.tickLabels.get(value)
  return label === undefined ? '' : `${label}%`
}

function formatPercent(value: number) {
  return `${value.toFixed(1)}%`
}
