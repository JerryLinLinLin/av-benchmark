import { useEffect, useMemo } from 'react'
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
  CardFooter,
  CardHeader,
  CardTitle,
  CardDescription,
} from '@/components/ui/card'
import { ChartContainer, ChartTooltip, type ChartConfig } from '@/components/ui/chart'
import type { MicrobenchScenarioData, MicrobenchScenarioRow } from '../data/compilationWorkloads'
import { avLabel, microbenchTitle, text, useLocale } from '../i18n'

type Props = {
  chartId: string
  title: string
  data: MicrobenchScenarioData
  onReady: () => void
  axis?: ManualAxisBreak
  scaleNote?: string
}

export type ManualAxisBreak = {
  max: number
  ticks: number[]
  tickLabels: Map<number, number>
  transform: (value: number) => number
}

type ChartDatum = {
  avName: string
  totalActual: number
  totalPlot: number
  normalAxisMax: number
  wallMs: number
  baselineWallMs: number
  status: string
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

const microbenchSeriesConfig = {
  totalPlot: {
    label: 'Average impact',
    color: 'var(--chart-6)',
  },
} satisfies ChartConfig

const microbenchAxis: ManualAxisBreak = {
  max: 140,
  ticks: [0, 18.4, 36.8, 55.2, 73.6, 92, 104, 116, 128, 134],
  tickLabels: new Map([
    [0, 0],
    [18.4, 20],
    [36.8, 40],
    [55.2, 60],
    [73.6, 80],
    [92, 100],
    [104, 150],
    [116, 250],
    [128, 350],
    [134, 750],
  ]),
  transform: transformMicrobenchValue,
}

export function MicrobenchScenarioChart({
  chartId,
  title,
  data,
  onReady,
  axis = microbenchAxis,
  scaleNote = 'Broken y-axis emphasizes 0-100%.',
}: Props) {
  const locale = useLocale()
  const copy = text(locale)
  const displayTitle = microbenchTitle(data.id, title, locale)
  const chartData = useMemo(() => buildChartData(data.rows, axis, locale), [axis, data.rows, locale])

  useEffect(() => {
    const frame = window.requestAnimationFrame(onReady)
    return () => window.cancelAnimationFrame(frame)
  }, [onReady])

  return (
    <section className="chart-export-shell" data-chart-id={chartId}>
      <Card className="chart-card">
        <CardHeader className="chart-card-header">
          <CardTitle className="chart-card-title">{displayTitle}</CardTitle>
          <CardDescription>
            {locale === 'zh-cn'
              ? '基于所有成功运行的平均耗时计算，数值越低越好。'
              : 'Mean wall-time impact across all successful runs. Lower is better.'}
          </CardDescription>
        </CardHeader>
        <CardContent className="chart-card-content">
          <ChartContainer className="impact-chart" config={microbenchSeriesConfig}>
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
                domain={[0, axis.max]}
                ticks={axis.ticks}
                tickFormatter={(value) => formatAxisLabel(Number(value), axis)}
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
              <ChartTooltip cursor={false} content={<MicrobenchScenarioTooltip />} />
              <Bar
                dataKey="totalPlot"
                name="totalPlot"
                fill="var(--color-totalPlot)"
                radius={[4, 4, 4, 4]}
                maxBarSize={40}
              />
              <Scatter dataKey="totalPlot" legendType="none" fill="transparent">
                <LabelList dataKey="totalActual" content={renderImpactLabel} />
              </Scatter>
            </ComposedChart>
          </ChartContainer>
        </CardContent>
        <CardFooter className="chart-card-footer">
          {locale === 'zh-cn'
            ? `耗时增幅 =（杀毒软件平均耗时 - 基线 OS 平均耗时）/ 基线 OS 平均耗时。${formatScaleNote(axis, locale)}负值按 0% 显示。`
            : `Percent impact = (AV mean wall time - baseline OS mean wall time) / baseline OS mean wall time. ${formatScaleNote(axis, locale) || scaleNote} Negative values are shown as 0%.`}
        </CardFooter>
      </Card>
    </section>
  )
}

function buildChartData(rows: MicrobenchScenarioRow[], axis: ManualAxisBreak, locale: ReturnType<typeof useLocale>) {
  return rows
    .map((row): ChartDatum => {
      const totalActual = Math.max(0, row.average.value)
      return {
        avName: avLabel(row.avName, locale),
        totalActual,
        totalPlot: axis.transform(totalActual),
        normalAxisMax: 100,
        wallMs: row.average.wallMs,
        baselineWallMs: row.average.baselineWallMs,
        status: row.status,
      }
    })
    .sort((left, right) => left.totalActual - right.totalActual)
}

function MicrobenchScenarioTooltip({ active, label, payload }: TooltipProps) {
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
      <div className="chart-tooltip-total combined">
        <span>{copy.averageImpact}</span>
        <strong>{formatPercent(row.totalActual)}</strong>
      </div>
      <div className="chart-tooltip-row single">
        <span>{copy.meanWallTime}</span>
        <strong>{row.wallMs.toFixed(1)} ms</strong>
      </div>
      <div className="chart-tooltip-row single">
        <span>{copy.baselineMean}</span>
        <strong>{row.baselineWallMs.toFixed(1)} ms</strong>
      </div>
      <div className="chart-tooltip-row single">
        <span>{copy.status}</span>
        <strong>{row.status}</strong>
      </div>
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
      className={actualValue > (payload?.normalAxisMax ?? 100) ? 'outlier-label' : 'impact-label microbench'}
    >
      {formatPercent(actualValue)}
    </text>
  )
}

function transformMicrobenchValue(value: number) {
  const actualStops = [0, 20, 40, 60, 80, 100, 150, 250, 350, 750, 800]
  const plotStops = [0, 18.4, 36.8, 55.2, 73.6, 92, 104, 116, 128, 134, 140]

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

function formatAxisLabel(value: number, axis: ManualAxisBreak) {
  const label = axis.tickLabels.get(value)
  return label === undefined ? '' : `${label}%`
}

function formatScaleNote(axis: ManualAxisBreak, locale: ReturnType<typeof useLocale>) {
  if (isLinearAxis(axis)) {
    return locale === 'zh-cn' ? 'Y 轴为线性刻度。' : 'The y-axis uses a linear scale.'
  }

  return locale === 'zh-cn'
    ? 'Y 轴对高离群值做了压缩，以保留 0-100% 区间的可读性。'
    : 'The y-axis is compressed for high outliers so the 0-100% range stays readable.'
}

function isLinearAxis(axis: ManualAxisBreak) {
  return axis.ticks.every((tick) => axis.tickLabels.get(tick) === tick)
}

function formatPercent(value: number) {
  return `${value.toFixed(1)}%`
}
