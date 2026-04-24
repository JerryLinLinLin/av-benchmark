import { useCallback, useEffect, useMemo, useState } from 'react'
import {
  Bar,
  CartesianGrid,
  ComposedChart,
  Label,
  LabelList,
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
import { avLabel, text, useLocale, type Locale } from '../i18n'

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
  cleanLabel: string | null
  incrementalLabel: string | null
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

const chartConfigs = [
  {
    id: 'ripgrep-cloud-cold',
    metric: 'cloudCold',
    workload: 'ripgrep',
    seriesConfig: ripgrepSeriesConfig,
    normalLabelClassName: 'impact-label ripgrep',
    title: 'Ripgrep Build: Cloud-Cold Impact',
    subtitle: 'Clean + incremental build impact before cloud reputation/cache warms. Lower is better.',
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
    subtitle: 'Clean + incremental build impact before cloud reputation/cache warms. Lower is better.',
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
    subtitle: 'Clean + incremental build impact using mean wall time across all runs. Lower is better.',
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
    subtitle: 'Clean + incremental build impact using mean wall time across all runs. Lower is better.',
    yAxisLabel: 'Average impact (%)',
    footnote:
      'Impact is computed from all-runs mean wall time versus baseline OS. Broken y-axis emphasizes 0-80%. Negative values are shown as 0%.',
    axisMax: 80,
    brokenAxis: averageRoslynAxis,
  },
] satisfies WorkloadChartConfig[]

export function CompilationWorkloadsChart({ data, onReady }: Props) {
  const locale = useLocale()
  const localizedConfigs = useMemo(
    () => chartConfigs.map((config) => localizeWorkloadConfig(config, locale)),
    [locale],
  )
  const [readyCount, setReadyCount] = useState(0)
  const handleChartReady = useCallback(() => {
    setReadyCount((current) => {
      const next = current + 1
      if (next === localizedConfigs.length) {
        onReady?.()
      }
      return next
    })
  }, [localizedConfigs.length, onReady])

  return (
    <div className="figure">
      <header className="figure-header">
        <div>
          <h1>{locale === 'zh-cn' ? '编译工作负载影响' : 'Compilation Workload Impact'}</h1>
          <p>
            {locale === 'zh-cn'
              ? '云端冷启动与平均影响均相对基线 OS 计算。负值显示为 0%。'
              : 'Cloud-cold and average impact vs baseline OS. Negative values are shown as 0%.'}
          </p>
        </div>
      </header>

      {localizedConfigs.map((config) => (
        <WorkloadChart
          key={config.id}
          config={config}
          data={data.rows}
          locale={locale}
          onReady={handleChartReady}
        />
      ))}
      <span hidden>{readyCount}</span>
    </div>
  )
}

function WorkloadChart({
  config,
  data,
  locale,
  onReady,
}: {
  config: WorkloadChartConfig
  data: CompilationWorkloadRow[]
  locale: Locale
  onReady: () => void
}) {
  const chartData = useMemo(() => buildChartData(data, config, locale), [data, config, locale])
  const copy = text(locale)

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
                  value={copy.antivirusProduct}
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
                fill="var(--color-cleanPlot)"
                radius={[4, 4, 4, 4]}
                maxBarSize={40}
              >
                <LabelList
                  dataKey="cleanLabel"
                  content={(props) => renderGroupedImpactLabel(props, config)}
                />
              </Bar>
              <Bar
                dataKey="incrementalPlot"
                name="incrementalPlot"
                fill="var(--color-incrementalPlot)"
                radius={[4, 4, 4, 4]}
                maxBarSize={40}
              >
                <LabelList
                  dataKey="incrementalLabel"
                  content={(props) => renderGroupedImpactLabel(props, config)}
                />
              </Bar>
            </ComposedChart>
          </ChartContainer>
        </CardContent>
        <CardFooter className="chart-card-footer">{config.footnote}</CardFooter>
      </Card>
    </section>
  )
}

function buildChartData(rows: CompilationWorkloadRow[], config: WorkloadChartConfig, locale: Locale) {
  return getSortedWorkloadRows(rows, config).map((row): ChartDatum => {
    const cleanPlot = config.brokenAxis.transform(row.clean)
    const incrementalPlot = config.brokenAxis.transform(row.incremental)
    const shouldShowOnlyHigher =
      row.clean > config.axisMax && row.incremental > config.axisMax
    const cleanLabel =
      shouldShowOnlyHigher && row.clean < row.incremental ? null : formatPercent(row.clean)
    const incrementalLabel =
      shouldShowOnlyHigher && row.incremental <= row.clean ? null : formatPercent(row.incremental)

    return {
      avName: avLabel(row.avName, locale),
      cleanActual: row.clean,
      incrementalActual: row.incremental,
      totalActual: row.total,
      normalAxisMax: config.axisMax,
      cleanPlot,
      incrementalPlot,
      totalPlot: Math.max(cleanPlot, incrementalPlot),
      cleanLabel,
      incrementalLabel,
    }
  })
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
  const locale = useLocale()
  const copy = text(locale)
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
      <TooltipRow className="clean" label={copy.cleanBuild} value={row.cleanActual} />
      <TooltipRow
        className="incremental"
        label={copy.incrementalBuild}
        value={row.incrementalActual}
      />
    </div>
  )
}

function localizeWorkloadConfig(config: WorkloadChartConfig, locale: Locale): WorkloadChartConfig {
  if (locale !== 'zh-cn') {
    return config
  }

  const isRipgrep = config.workload === 'ripgrep'
  const isAverage = config.metric === 'average'
  const axisRange = isRipgrep ? '0-60%' : '0-80%'

  return {
    ...config,
    seriesConfig: {
      cleanPlot: {
        label: '全量构建',
        color: config.seriesConfig.cleanPlot.color,
      },
      incrementalPlot: {
        label: '增量构建',
        color: config.seriesConfig.incrementalPlot.color,
      },
    },
    title: `${isRipgrep ? 'Ripgrep' : 'Roslyn'} 构建：${isAverage ? '平均影响' : '云端冷启动影响'}`,
    subtitle: isAverage
      ? '基于所有运行平均耗时的全量 + 增量构建影响。越低越好。'
      : '云端信誉/缓存预热前的全量 + 增量构建影响。越低越好。',
    yAxisLabel: isAverage ? '平均影响（%）' : '云端冷启动影响（%）',
    footnote: isAverage
      ? `影响值根据所有运行的平均耗时相对基线 OS 计算。断轴突出 ${axisRange} 区间。负值显示为 0%。`
      : `云端冷启动表示首次云端/信誉系统接触；每次运行前重置虚拟机以移除本地缓存。断轴突出 ${axisRange} 区间。负值显示为 0%。`,
  }
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

function renderGroupedImpactLabel(
  props: unknown,
  config: WorkloadChartConfig,
) {
  const { x, y, width, value } = props as LabelContentProps
  if (value === null || value === undefined || value === '') {
    return null
  }
  const label = String(value)
  const actualValue = Number.parseFloat(label)

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
        actualValue > config.axisMax
          ? 'outlier-label compact'
          : `${config.normalLabelClassName} compact`
      }
    >
      {label}
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

function formatAxisLabel(value: number, brokenAxis: ManualAxisBreak) {
  const label = brokenAxis.tickLabels.get(value)
  return label === undefined ? '' : `${label}%`
}

function formatPercent(value: number) {
  return `${value.toFixed(1)}%`
}
