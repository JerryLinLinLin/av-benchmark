import { useEffect, useMemo } from 'react'
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
  CardFooter,
  CardHeader,
  CardTitle,
  CardDescription,
} from '@/components/ui/card'
import {
  ChartContainer,
  ChartLegend,
  ChartLegendContent,
  ChartTooltip,
  type ChartConfig,
} from '@/components/ui/chart'
import type {
  CompilationWorkloadData,
  ExtensionSensitivityRow,
  MicrobenchScenarioRow,
} from '../data/compilationWorkloads'
import { avLabel, text, useLocale } from '../i18n'

type Props = {
  data: CompilationWorkloadData['microbench']['extensionSensitivity']
  onReady: () => void
}

type ExtensionKey = 'exe' | 'dll' | 'js' | 'ps1'

type ManualAxisBreak = {
  max: number
  ticks: number[]
  tickLabels: Map<number, number>
  transform: (value: number) => number
}

type ChartDatum = {
  avName: string
  sortValue: number
  exeActual: number
  exeLabel: number | null
  exePlot: number
  exeWallMs: number
  exeBaselineWallMs: number
  exeStatus: string
  dllActual: number
  dllLabel: number | null
  dllPlot: number
  dllWallMs: number
  dllBaselineWallMs: number
  dllStatus: string
  jsActual: number
  jsLabel: number | null
  jsPlot: number
  jsWallMs: number
  jsBaselineWallMs: number
  jsStatus: string
  ps1Actual: number
  ps1Label: number | null
  ps1Plot: number
  ps1WallMs: number
  ps1BaselineWallMs: number
  ps1Status: string
}

type LabelContentProps = {
  x?: number | string
  y?: number | string
  width?: number | string
  value?: number | string
  payload?: ChartDatum
}

type TooltipProps = {
  active?: boolean
  label?: string
  payload?: Array<{ payload?: ChartDatum }>
}

const extensionKeys: ExtensionKey[] = ['exe', 'dll', 'js', 'ps1']

const extensionLabels = {
  exe: '.exe',
  dll: '.dll',
  js: '.js',
  ps1: '.ps1',
} satisfies Record<ExtensionKey, string>

const extensionSensitivitySeriesConfig = {
  exePlot: {
    label: '.exe',
    color: 'var(--chart-6)',
  },
  dllPlot: {
    label: '.dll',
    color: 'var(--chart-7)',
  },
  jsPlot: {
    label: '.js',
    color: 'var(--chart-8)',
  },
  ps1Plot: {
    label: '.ps1',
    color: 'var(--chart-9)',
  },
} satisfies ChartConfig

const extensionAxis: ManualAxisBreak = {
  max: 160,
  ticks: [0, 18.4, 36.8, 55.2, 73.6, 92, 108, 124, 136, 148, 160],
  tickLabels: new Map([
    [0, 0],
    [18.4, 20],
    [36.8, 40],
    [55.2, 60],
    [73.6, 80],
    [92, 100],
    [108, 200],
    [124, 500],
    [136, 1000],
    [148, 2000],
    [160, 4000],
  ]),
  transform: transformExtensionValue,
}

export function ExtensionSensitivityChart({ data, onReady }: Props) {
  const locale = useLocale()
  const copy = text(locale)
  const chartData = useMemo(() => buildChartData(data.rows, locale), [data.rows, locale])

  useEffect(() => {
    const frame = window.requestAnimationFrame(onReady)
    return () => window.cancelAnimationFrame(frame)
  }, [onReady])

  return (
    <section className="chart-export-shell" data-chart-id="extension-sensitivity-average">
      <Card className="chart-card">
        <CardHeader className="chart-card-header">
          <CardTitle className="chart-card-title">
            {locale === 'zh-cn' ? '扩展名敏感度：平均耗时增幅' : 'Extension Sensitivity: Average Impact'}
          </CardTitle>
          <CardDescription>
            {locale === 'zh-cn'
              ? '按文件扩展名统计所有成功运行的平均耗时增幅。数值越低越好。'
              : 'Mean wall-time impact by file extension across all successful runs. Lower is better.'}
          </CardDescription>
        </CardHeader>
        <CardContent className="chart-card-content">
          <ChartContainer className="impact-chart" config={extensionSensitivitySeriesConfig}>
            <ComposedChart
              accessibilityLayer
              data={chartData}
              margin={{ top: 12, right: 18, bottom: 66, left: 28 }}
              barGap={2}
              barCategoryGap="12%"
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
                domain={[0, extensionAxis.max]}
                ticks={extensionAxis.ticks}
                tickFormatter={(value) => formatAxisLabel(Number(value))}
                tickLine={false}
                axisLine={false}
                width={74}
              >
                <Label
                  value={copy.averageImpactPct}
                  angle={-90}
                  position="insideLeft"
                  offset={-14}
                  className="axis-label"
                />
              </YAxis>
              <ChartLegend
                verticalAlign="top"
                align="left"
                content={<ChartLegendContent className="chart-legend" />}
              />
              <ChartTooltip cursor={false} content={<ExtensionSensitivityTooltip />} />
              {extensionKeys.map((key) => (
                <Bar
                  key={key}
                  dataKey={`${key}Plot`}
                  name={`${key}Plot`}
                  fill={`var(--color-${key}Plot)`}
                  radius={[3, 3, 3, 3]}
                  maxBarSize={12}
                >
                  <LabelList dataKey={`${key}Label`} content={renderExtremeLabel} />
                </Bar>
              ))}
            </ComposedChart>
          </ChartContainer>
        </CardContent>
        <CardFooter className="chart-card-footer">
          {locale === 'zh-cn'
            ? '耗时增幅根据扩展名敏感度测试的 `all_runs_mean_wall_ms` 相对基线 OS 计算。断轴用于突出 0-100% 区间。详细数值可在提示框查看；每个产品超过 200% 的最高值会以红色标注。'
            : "Impact is computed from `all_runs_mean_wall_ms` versus baseline OS for the extension sensitivity microbenchmarks. Broken y-axis emphasizes 0-100%. Values are available in the tooltip; each AV's highest value above 200% is labeled in red."}
        </CardFooter>
      </Card>
    </section>
  )
}

function buildChartData(rows: ExtensionSensitivityRow[], locale: ReturnType<typeof useLocale>) {
  return rows
    .map((row): ChartDatum => {
      const exe = metricValues(row.exe)
      const dll = metricValues(row.dll)
      const js = metricValues(row.js)
      const ps1 = metricValues(row.ps1)
      const values = [exe.actual, dll.actual, js.actual, ps1.actual]
      const worstValue = Math.max(...values)

      return {
        avName: avLabel(row.avName, locale),
        sortValue: values.reduce((sum, value) => sum + value, 0) / values.length,
        exeActual: exe.actual,
        exeLabel: extremeLabelValue(exe.actual, worstValue),
        exePlot: extensionAxis.transform(exe.actual),
        exeWallMs: exe.wallMs,
        exeBaselineWallMs: exe.baselineWallMs,
        exeStatus: exe.status,
        dllActual: dll.actual,
        dllLabel: extremeLabelValue(dll.actual, worstValue),
        dllPlot: extensionAxis.transform(dll.actual),
        dllWallMs: dll.wallMs,
        dllBaselineWallMs: dll.baselineWallMs,
        dllStatus: dll.status,
        jsActual: js.actual,
        jsLabel: extremeLabelValue(js.actual, worstValue),
        jsPlot: extensionAxis.transform(js.actual),
        jsWallMs: js.wallMs,
        jsBaselineWallMs: js.baselineWallMs,
        jsStatus: js.status,
        ps1Actual: ps1.actual,
        ps1Label: extremeLabelValue(ps1.actual, worstValue),
        ps1Plot: extensionAxis.transform(ps1.actual),
        ps1WallMs: ps1.wallMs,
        ps1BaselineWallMs: ps1.baselineWallMs,
        ps1Status: ps1.status,
      }
    })
    .sort((left, right) => left.sortValue - right.sortValue)
}

function extremeLabelValue(value: number, worstValue: number) {
  return value > 200 && value === worstValue ? value : null
}

function metricValues(row: MicrobenchScenarioRow | null) {
  const actual = row ? Math.max(0, row.average.value) : 0
  return {
    actual,
    wallMs: row?.average.wallMs ?? 0,
    baselineWallMs: row?.average.baselineWallMs ?? 0,
    status: row?.status ?? 'missing',
  }
}

function ExtensionSensitivityTooltip({ active, label, payload }: TooltipProps) {
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
      {extensionKeys.map((key) => (
        <div className="chart-tooltip-row" key={key}>
          <span>
            <i className={`extension-${key}`} />
            {extensionLabels[key]}
          </span>
          <strong>{formatPercent(row[`${key}Actual`])}</strong>
        </div>
      ))}
    </div>
  )
}

function renderExtremeLabel(props: unknown) {
  const { x, y, width, value } = props as LabelContentProps
  if (value == null) {
    return null
  }

  const actualValue = Number(value)
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
      y={Math.max(14, yValue - 7)}
      textAnchor="middle"
      className="outlier-label compact"
    >
      {formatPercent(actualValue)}
    </text>
  )
}

function transformExtensionValue(value: number) {
  const actualStops = [0, 20, 40, 60, 80, 100, 200, 500, 1000, 2000, 4000]
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

function formatAxisLabel(value: number) {
  const label = extensionAxis.tickLabels.get(value)
  return label === undefined ? '' : `${label}%`
}

function formatPercent(value: number) {
  return `${value.toFixed(1)}%`
}
