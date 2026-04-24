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
import { ChartContainer, ChartTooltip, type ChartConfig } from '@/components/ui/chart'
import { newExeRunAxis } from './newExeRunAxis'
import type { CompilationWorkloadData, MicrobenchScenarioRow } from '../data/compilationWorkloads'
import { avLabel, text, useLocale } from '../i18n'

type Props = {
  data: {
    run1: CompilationWorkloadData['microbench']['newExeRun']
    run2: CompilationWorkloadData['microbench']['newExeRunMotw']
  }
  onReady: () => void
}

type ChartDatum = {
  avName: string
  run1Actual: number
  run1Label: number | null
  run1Plot: number
  run1WallMs: number
  run1Status: string
  run2Actual: number
  run2Label: number | null
  run2Plot: number
  run2WallMs: number
  run2Status: string
  sortValue: number
  normalAxisMax: number
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

const sequenceSeriesConfig = {
  run1Plot: {
    label: 'Run 1: cold, no MOTW',
    color: 'var(--chart-6)',
  },
  run2Plot: {
    label: 'Run 2: warm cache + MOTW',
    color: 'var(--chart-7)',
  },
} satisfies ChartConfig

export function NewExeRunSequenceChart({ data, onReady }: Props) {
  const locale = useLocale()
  const copy = text(locale)
  const run1Label = locale === 'zh-cn' ? '第 1 次：冷启动，无 MOTW' : 'Run 1: cold, no MOTW'
  const run2Label = locale === 'zh-cn' ? '第 2 次：缓存已热 + MOTW' : 'Run 2: warm cache + MOTW'
  const seriesConfig = useMemo(
    () => ({
      run1Plot: {
        ...sequenceSeriesConfig.run1Plot,
        label: run1Label,
      },
      run2Plot: {
        ...sequenceSeriesConfig.run2Plot,
        label: run2Label,
      },
    }),
    [run1Label, run2Label],
  )
  const chartData = useMemo(() => buildChartData(data.run1.rows, data.run2.rows, locale), [data, locale])

  useEffect(() => {
    const frame = window.requestAnimationFrame(onReady)
    return () => window.cancelAnimationFrame(frame)
  }, [onReady])

  return (
    <section className="chart-export-shell" data-chart-id="new-exe-run-sequence-average">
      <Card className="chart-card">
        <CardHeader className="chart-card-header">
          <CardTitle className="chart-card-title">
            {locale === 'zh-cn' ? '新 EXE 运行序列：平均影响' : 'New EXE Run Sequence: Average Impact'}
          </CardTitle>
          <CardDescription>
            {locale === 'zh-cn'
              ? '先冷启动且无 MOTW，再在缓存已热时带 MOTW 运行。越低越好。'
              : 'First cold without MOTW, then warm-cache run with MOTW. Lower is better.'}
          </CardDescription>
        </CardHeader>
        <CardContent className="chart-card-content">
          <ChartContainer className="impact-chart" config={seriesConfig}>
            <ComposedChart
              accessibilityLayer
              data={chartData}
              margin={{ top: 12, right: 18, bottom: 66, left: 28 }}
              barGap={4}
              barCategoryGap="18%"
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
                domain={[0, newExeRunAxis.max]}
                ticks={newExeRunAxis.ticks}
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
              <ChartTooltip cursor={false} content={<NewExeRunSequenceTooltip />} />
              <Bar
                dataKey="run1Plot"
                name="run1Plot"
                fill="var(--color-run1Plot)"
                radius={[4, 4, 4, 4]}
                maxBarSize={22}
              >
                <LabelList dataKey="run1Label" content={renderImpactLabel('run1')} />
              </Bar>
              <Bar
                dataKey="run2Plot"
                name="run2Plot"
                fill="var(--color-run2Plot)"
                radius={[4, 4, 4, 4]}
                maxBarSize={22}
              >
                <LabelList dataKey="run2Label" content={renderImpactLabel('run2')} />
              </Bar>
            </ComposedChart>
          </ChartContainer>
          <div className="sequence-legend" aria-hidden="true">
            <span className="sequence-legend-item">
              <i className="sequence-legend-swatch primary" />
              {run1Label}
            </span>
            <span className="sequence-legend-item">
              <i className="sequence-legend-swatch secondary" />
              {run2Label}
            </span>
          </div>
        </CardContent>
        <CardFooter className="chart-card-footer">
          {locale === 'zh-cn'
            ? '影响值根据 `all_runs_mean_wall_ms` 相对基线 OS 计算。第 2 次在第 1 次之后运行，添加 MOTW 且不重置虚拟机。缺少任一运行的数据行会被排除。'
            : 'Impact is computed from `all_runs_mean_wall_ms` versus baseline OS. Run 2 runs after Run 1, with MOTW added and no VM reset. Rows missing either run are excluded.'}
        </CardFooter>
      </Card>
    </section>
  )
}

function buildChartData(run1Rows: MicrobenchScenarioRow[], run2Rows: MicrobenchScenarioRow[], locale: ReturnType<typeof useLocale>) {
  const run2ByAv = new Map(run2Rows.map((row) => [row.avName, row]))

  return run1Rows
    .flatMap((run1): ChartDatum[] => {
      const run2 = run2ByAv.get(run1.avName)
      if (!run2) {
        return []
      }

      const run1Actual = Math.max(0, run1.average.value)
      const run2Actual = Math.max(0, run2.average.value)
      const higherRun = run1Actual >= run2Actual ? 'run1' : 'run2'
      return [
        {
          avName: avLabel(run1.avName, locale),
          run1Actual,
          run1Label: higherRun === 'run1' ? run1Actual : null,
          run1Plot: newExeRunAxis.transform(run1Actual),
          run1WallMs: run1.average.wallMs,
          run1Status: run1.status,
          run2Actual,
          run2Label: higherRun === 'run2' ? run2Actual : null,
          run2Plot: newExeRunAxis.transform(run2Actual),
          run2WallMs: run2.average.wallMs,
          run2Status: run2.status,
          sortValue: (run1Actual + run2Actual) / 2,
          normalAxisMax: 100,
        },
      ]
    })
    .sort((left, right) => left.sortValue - right.sortValue)
}

function NewExeRunSequenceTooltip({ active, label, payload }: TooltipProps) {
  const locale = useLocale()
  const run1Label = locale === 'zh-cn' ? '第 1 次：冷启动，无 MOTW' : 'Run 1: cold, no MOTW'
  const run2Label = locale === 'zh-cn' ? '第 2 次：缓存已热 + MOTW' : 'Run 2: warm cache + MOTW'
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
      <div className="chart-tooltip-row">
        <span>
          <i className="microbench-primary" />
          {run1Label}
        </span>
        <strong>{formatPercent(row.run1Actual)}</strong>
      </div>
      <div className="chart-tooltip-row single">
        <span>{locale === 'zh-cn' ? '第 1 次平均耗时' : 'Run 1 mean wall time'}</span>
        <strong>{row.run1WallMs.toFixed(1)} ms</strong>
      </div>
      <div className="chart-tooltip-row">
        <span>
          <i className="microbench-secondary" />
          {run2Label}
        </span>
        <strong>{formatPercent(row.run2Actual)}</strong>
      </div>
      <div className="chart-tooltip-row single">
        <span>{locale === 'zh-cn' ? '第 2 次平均耗时' : 'Run 2 mean wall time'}</span>
        <strong>{row.run2WallMs.toFixed(1)} ms</strong>
      </div>
    </div>
  )
}

function renderImpactLabel(metric: 'run1' | 'run2') {
  return function renderMetricLabel(props: unknown) {
    const { x, y, width, value, payload } = props as LabelContentProps
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

    const isOutlier = actualValue > (payload?.normalAxisMax ?? 100)
    const className = isOutlier
      ? 'outlier-label'
      : metric === 'run1'
        ? 'impact-label microbench'
        : 'impact-label microbench-secondary'

    return (
      <text
        x={xValue + widthValue / 2}
        y={Math.max(14, yValue - 8)}
        textAnchor="middle"
        className={className}
      >
        {formatPercent(actualValue)}
      </text>
    )
  }
}

function formatAxisLabel(value: number) {
  const label = newExeRunAxis.tickLabels.get(value)
  return label === undefined ? '' : `${label}%`
}

function formatPercent(value: number) {
  return `${value.toFixed(1)}%`
}
