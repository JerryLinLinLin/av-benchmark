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

type MetricKey = 'cloudCold' | 'average'
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

type WorkloadChartConfig = {
  id: string
  metric: MetricKey
  workload: WorkloadKey
  seriesConfig: ChartConfig
  normalLabelClassName: string
  title: string
  subtitle: string
  yAxisLabel: string
  footnote: string
  axisMax: number
  brokenAxis: ManualAxisBreak
}

type CombinedChartConfig = {
  id: string
  seriesConfig: ChartConfig
  normalLabelClassName: string
  valueDivisor: number
  tooltipLabel: string
  title: string
  subtitle: string
  yAxisLabel: string
  footnote: string
  axisMax: number
  brokenAxis: ManualAxisBreak
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

const ripgrepSeriesConfig = {
  cleanPlot: {
    label: 'Clean build',
    color: 'var(--chart-1)',
  },
  incrementalPlot: {
    label: 'Incremental build',
    color: 'var(--chart-2)',
  },
} satisfies ChartConfig

const roslynSeriesConfig = {
  cleanPlot: {
    label: 'Clean build',
    color: 'var(--chart-3)',
  },
  incrementalPlot: {
    label: 'Incremental build',
    color: 'var(--chart-4)',
  },
} satisfies ChartConfig

const combinedSeriesConfig = {
  totalPlot: {
    label: 'Combined average impact',
    color: 'var(--chart-5)',
  },
} satisfies ChartConfig

const cloudColdRipgrepAxis: ManualAxisBreak = {
  max: 138,
  ticks: [0, 15.3, 30.7, 46, 61.3, 76.7, 92, 104, 116, 124, 136],
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
  transform: transformCloudColdRipgrepValue,
}

const cloudColdRoslynAxis: ManualAxisBreak = {
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
  transform: transformCloudColdRoslynValue,
}

const averageRipgrepAxis: ManualAxisBreak = {
  max: 138,
  ticks: [0, 15.3, 30.7, 46, 61.3, 76.7, 92, 104, 116, 124, 136],
  tickLabels: new Map([
    [0, 0],
    [15.3, 10],
    [30.7, 20],
    [46, 30],
    [61.3, 40],
    [76.7, 50],
    [92, 60],
    [104, 180],
    [116, 200],
    [124, 850],
    [136, 900],
  ]),
  transform: transformAverageRipgrepValue,
}

const averageRoslynAxis: ManualAxisBreak = {
  max: 130,
  ticks: [0, 11.5, 23, 34.5, 46, 57.5, 69, 80.5, 92, 104, 116, 124],
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
    [116, 220],
    [124, 240],
  ]),
  transform: transformAverageRoslynValue,
}

const combinedAverageAxis: ManualAxisBreak = {
  max: 130,
  ticks: [0, 11.5, 23, 34.5, 46, 57.5, 69, 80.5, 92, 104, 116, 124],
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
    [116, 220],
    [124, 240],
  ]),
  transform: transformCombinedAverageValue,
}

const combinedAverageSumAxis: ManualAxisBreak = {
  max: 138,
  ticks: [0, 11.5, 23, 34.5, 46, 57.5, 69, 80.5, 92, 104, 116, 124, 136],
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
    [104, 180],
    [116, 250],
    [124, 900],
    [136, 950],
  ]),
  transform: transformCombinedAverageSumValue,
}

const chartConfigs = [
  {
    id: 'ripgrep-cloud-cold',
    metric: 'cloudCold',
    workload: 'ripgrep',
    seriesConfig: ripgrepSeriesConfig,
    normalLabelClassName: 'impact-label ripgrep',
    title: 'Ripgrep Build: Cloud-Cold Impact',
    subtitle: 'Clean + incremental build impact before cloud reputation/cache warms',
    yAxisLabel: 'Cloud-cold impact (%)',
    footnote:
      'Cloud-cold means first cloud/reputation exposure; VM reset removes local cache between runs. Broken y-axis emphasizes 0-60%. Negative values are shown as 0%.',
    axisMax: 60,
    brokenAxis: cloudColdRipgrepAxis,
  },
  {
    id: 'roslyn-cloud-cold',
    metric: 'cloudCold',
    workload: 'roslyn',
    seriesConfig: roslynSeriesConfig,
    normalLabelClassName: 'impact-label roslyn',
    title: 'Roslyn Build: Cloud-Cold Impact',
    subtitle: 'Clean + incremental build impact before cloud reputation/cache warms',
    yAxisLabel: 'Cloud-cold impact (%)',
    footnote:
      'Cloud-cold means first cloud/reputation exposure; VM reset removes local cache between runs. Broken y-axis emphasizes 0-80%. Negative values are shown as 0%.',
    axisMax: 80,
    brokenAxis: cloudColdRoslynAxis,
  },
  {
    id: 'ripgrep-average',
    metric: 'average',
    workload: 'ripgrep',
    seriesConfig: ripgrepSeriesConfig,
    normalLabelClassName: 'impact-label ripgrep',
    title: 'Ripgrep Build: Average Impact',
    subtitle: 'Clean + incremental build impact using mean wall time across all runs',
    yAxisLabel: 'Average impact (%)',
    footnote:
      'Impact is computed from all-runs mean wall time versus baseline OS. Broken y-axis emphasizes 0-60%. Negative values are shown as 0%.',
    axisMax: 60,
    brokenAxis: averageRipgrepAxis,
  },
  {
    id: 'roslyn-average',
    metric: 'average',
    workload: 'roslyn',
    seriesConfig: roslynSeriesConfig,
    normalLabelClassName: 'impact-label roslyn',
    title: 'Roslyn Build: Average Impact',
    subtitle: 'Clean + incremental build impact using mean wall time across all runs',
    yAxisLabel: 'Average impact (%)',
    footnote:
      'Impact is computed from all-runs mean wall time versus baseline OS. Broken y-axis emphasizes 0-80%. Negative values are shown as 0%.',
    axisMax: 80,
    brokenAxis: averageRoslynAxis,
  },
] satisfies WorkloadChartConfig[]

const combinedAverageConfig = {
  id: 'compilation-average-combined',
  seriesConfig: combinedSeriesConfig,
  normalLabelClassName: 'impact-label combined',
  valueDivisor: 4,
  tooltipLabel: 'Average compile impact',
  title: 'Compilation Builds: Average Impact',
  subtitle: 'Mean impact across Ripgrep and Roslyn clean + incremental builds',
  yAxisLabel: 'Average compile impact (%)',
  footnote:
    'Average compile impact is the mean slowdown across Ripgrep and Roslyn clean + incremental builds. Broken y-axis emphasizes 0-80%. Negative values are shown as 0%.',
  axisMax: 80,
  brokenAxis: combinedAverageAxis,
} satisfies CombinedChartConfig

const combinedAverageSumConfig = {
  id: 'compilation-average-total',
  seriesConfig: combinedSeriesConfig,
  normalLabelClassName: 'impact-label combined',
  valueDivisor: 1,
  tooltipLabel: 'Total compile impact score',
  title: 'Compilation Builds: Total Average Impact',
  subtitle: 'Summed impact across Ripgrep and Roslyn clean + incremental builds',
  yAxisLabel: 'Total average impact (%)',
  footnote:
    'Total average impact sums average slowdown across Ripgrep and Roslyn clean + incremental builds. Broken y-axis emphasizes 0-80%. Negative values are shown as 0%.',
  axisMax: 80,
  brokenAxis: combinedAverageSumAxis,
} satisfies CombinedChartConfig

export function CompilationWorkloadsChart({ data, onReady }: Props) {
  const [readyCount, setReadyCount] = useState(0)
  const handleChartReady = useCallback(() => {
    setReadyCount((current) => {
      const next = current + 1
      if (next === chartConfigs.length + 2) {
        onReady?.()
      }
      return next
    })
  }, [onReady])

  return (
    <div className="figure">
      <header className="figure-header">
        <div>
          <h1>Compilation Workload Impact</h1>
          <p>
            Cloud-cold and average impact vs baseline OS. Negative values are
            shown as 0%.
          </p>
        </div>
      </header>

      {chartConfigs.map((config) => (
        <WorkloadChart
          key={config.id}
          config={config}
          data={data.rows}
          onReady={handleChartReady}
        />
      ))}
      <CombinedAverageChart
        config={combinedAverageConfig}
        data={data.rows}
        onReady={handleChartReady}
      />
      <CombinedAverageChart
        config={combinedAverageSumConfig}
        data={data.rows}
        onReady={handleChartReady}
      />
      <span hidden>{readyCount}</span>
    </div>
  )
}

function WorkloadChart({
  config,
  data,
  onReady,
}: {
  config: WorkloadChartConfig
  data: CompilationWorkloadRow[]
  onReady: () => void
}) {
  const chartData = useMemo(() => buildChartData(data, config), [data, config])

  useEffect(() => {
    const frame = window.requestAnimationFrame(onReady)
    return () => window.cancelAnimationFrame(frame)
  }, [onReady])

  return (
    <section className="chart-export-shell" data-chart-id={config.id}>
      <Card className="chart-card">
        <CardHeader className="chart-card-header">
          <CardTitle className="chart-card-title">{config.title}</CardTitle>
          <CardDescription>{config.subtitle}</CardDescription>
        </CardHeader>
        <CardContent className="chart-card-content">
          <ChartContainer className="impact-chart" config={config.seriesConfig}>
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
                  value={config.yAxisLabel}
                  angle={-90}
                  position="insideLeft"
                  offset={-14}
                  className="axis-label"
                />
              </YAxis>
              <ChartTooltip cursor={false} content={<ImpactTooltip />} />
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
              />
              <Scatter dataKey="totalPlot" legendType="none" fill="transparent">
                <LabelList
                  dataKey="totalActual"
                  content={(props) => renderImpactLabel(props, config)}
                />
              </Scatter>
            </ComposedChart>
          </ChartContainer>
        </CardContent>
        <CardFooter className="chart-card-footer">{config.footnote}</CardFooter>
      </Card>
    </section>
  )
}

function CombinedAverageChart({
  config,
  data,
  onReady,
}: {
  config: CombinedChartConfig
  data: CompilationWorkloadRow[]
  onReady: () => void
}) {
  const chartData = useMemo(() => buildCombinedAverageChartData(data, config), [data, config])

  useEffect(() => {
    const frame = window.requestAnimationFrame(onReady)
    return () => window.cancelAnimationFrame(frame)
  }, [onReady])

  return (
    <section className="chart-export-shell" data-chart-id={config.id}>
      <Card className="chart-card">
        <CardHeader className="chart-card-header">
          <CardTitle className="chart-card-title">{config.title}</CardTitle>
          <CardDescription>{config.subtitle}</CardDescription>
        </CardHeader>
        <CardContent className="chart-card-content">
          <ChartContainer className="impact-chart" config={config.seriesConfig}>
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
                  value={config.yAxisLabel}
                  angle={-90}
                  position="insideLeft"
                  offset={-14}
                  className="axis-label"
                />
              </YAxis>
              <ChartTooltip
                cursor={false}
                content={<CombinedImpactTooltip labelText={config.tooltipLabel} />}
              />
              <Bar
                dataKey="totalPlot"
                name="totalPlot"
                fill="var(--color-totalPlot)"
                radius={[4, 4, 4, 4]}
                maxBarSize={40}
              />
              <Scatter dataKey="totalPlot" legendType="none" fill="transparent">
                <LabelList
                  dataKey="totalActual"
                  content={(props) => renderImpactLabel(props, config)}
                />
              </Scatter>
            </ComposedChart>
          </ChartContainer>
        </CardContent>
        <CardFooter className="chart-card-footer">{config.footnote}</CardFooter>
      </Card>
    </section>
  )
}

function buildChartData(rows: CompilationWorkloadRow[], config: WorkloadChartConfig) {
  return getSortedWorkloadRows(rows, config).map((row): ChartDatum => {
    const cleanPlot = config.brokenAxis.transform(row.clean)
    const totalPlot = config.brokenAxis.transform(row.total)

    return {
      avName: row.avName,
      cleanActual: row.clean,
      incrementalActual: row.incremental,
      totalActual: row.total,
      normalAxisMax: config.axisMax,
      cleanPlot,
      incrementalPlot: totalPlot - cleanPlot,
      totalPlot,
    }
  })
}

function buildCombinedAverageChartData(rows: CompilationWorkloadRow[], config: CombinedChartConfig) {
  return rows
    .map((row): ChartDatum => {
      const total =
        (metricValue(row.ripgrep.clean, 'average') +
          metricValue(row.ripgrep.incremental, 'average') +
          metricValue(row.roslyn.clean, 'average') +
          metricValue(row.roslyn.incremental, 'average')) /
        config.valueDivisor
      const totalPlot = config.brokenAxis.transform(total)

      return {
        avName: row.avName,
        cleanActual: total,
        incrementalActual: 0,
        totalActual: total,
        normalAxisMax: config.axisMax,
        cleanPlot: totalPlot,
        incrementalPlot: 0,
        totalPlot,
      }
    })
    .sort((left, right) => left.totalActual - right.totalActual)
}

function getSortedWorkloadRows(rows: CompilationWorkloadRow[], config: WorkloadChartConfig) {
  return rows
    .map((row): SortedWorkloadRow => {
      const clean = metricValue(row[config.workload].clean, config.metric)
      const incremental = metricValue(row[config.workload].incremental, config.metric)
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
      <TooltipRow className="clean" label="Clean build" value={row.cleanActual} />
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

function CombinedImpactTooltip({
  active,
  label,
  labelText,
  payload,
}: ImpactTooltipProps & { labelText: string }) {
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
      <div className="chart-tooltip-total combined">
        <span>{labelText}</span>
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

function renderImpactLabel(props: unknown, config: WorkloadChartConfig | CombinedChartConfig) {
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
      className={
        actualValue > (payload?.normalAxisMax ?? 80)
          ? 'outlier-label'
          : config.normalLabelClassName
      }
    >
      {formatPercent(actualValue)}
    </text>
  )
}

function metricValue(metric: BuildMetric | null, metricKey: MetricKey) {
  return metric ? Math.max(0, metric[metricKey].value) : 0
}

function transformCloudColdRipgrepValue(value: number) {
  if (value <= 60) {
    return (value / 60) * 92
  }

  if (value <= 300) {
    return 104 + ((Math.max(value, 200) - 200) / 100) * 12
  }

  return 124 + ((Math.min(value, 800) - 700) / 100) * 12
}

function transformCloudColdRoslynValue(value: number) {
  if (value <= 80) {
    return (value / 80) * 92
  }

  return 104 + ((Math.min(Math.max(value, 200), 320) - 200) / 120) * 24
}

function transformAverageRipgrepValue(value: number) {
  if (value <= 60) {
    return (value / 60) * 92
  }

  if (value <= 200) {
    return 104 + ((Math.max(value, 180) - 180) / 20) * 12
  }

  return 124 + ((Math.min(value, 900) - 850) / 50) * 12
}

function transformAverageRoslynValue(value: number) {
  if (value <= 80) {
    return (value / 80) * 92
  }

  return 104 + ((Math.min(Math.max(value, 200), 240) - 200) / 40) * 24
}

function transformCombinedAverageValue(value: number) {
  if (value <= 80) {
    return (value / 80) * 92
  }

  return 104 + ((Math.min(Math.max(value, 200), 240) - 200) / 40) * 24
}

function transformCombinedAverageSumValue(value: number) {
  if (value <= 80) {
    return (value / 80) * 92
  }

  if (value <= 250) {
    return 104 + ((Math.max(value, 180) - 180) / 70) * 12
  }

  return 124 + ((Math.min(value, 950) - 900) / 50) * 12
}

function formatAxisLabel(value: number, brokenAxis: ManualAxisBreak) {
  const label = brokenAxis.tickLabels.get(value)
  return label === undefined ? '' : `${label}%`
}

function formatPercent(value: number) {
  return `${value.toFixed(1)}%`
}
