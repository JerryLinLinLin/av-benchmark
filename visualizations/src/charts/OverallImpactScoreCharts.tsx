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
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import { ChartContainer, ChartTooltip, type ChartConfig } from '@/components/ui/chart'
import type { CompilationWorkloadData } from '../data/compilationWorkloads'
import { avLabel, text, useLocale, type Locale } from '../i18n'
import {
  average,
  buildWorkloadColumns,
  getAvNames,
  levelPenaltyScore,
  normalizedLevel,
  normalizedLogScore,
  valuesForColumn,
  workloadCategories,
} from './workloadImpactModel'

type Props = {
  data: CompilationWorkloadData
  onReady: () => void
}

type ScoreMode = 'equal-workload' | 'category-balanced' | 'severity-weighted'

type ScoreDatum = {
  avName: string
  score: number
  workloadCount: number
  categoryCount: number
}

type ScoreChartConfig = {
  chartId: string
  mode: ScoreMode
  title: string
  subtitle: string
  footnote: string
  seriesConfig: ChartConfig
  labelClassName: string
}

type LabelContentProps = {
  x?: number | string
  y?: number | string
  width?: number | string
  height?: number | string
  value?: number | string
}

type TooltipProps = {
  active?: boolean
  label?: string
  payload?: Array<{ payload?: ScoreDatum }>
}

const equalWorkloadConfig = {
  chartId: 'overall-impact-equal-workload',
  mode: 'equal-workload',
  title: 'Overall Impact Score: Equal Workload Weight',
  subtitle: 'Average log-normalized score across workload columns. Lower is better.',
  footnote:
    'Each workload column contributes equally after log-normalization across AV products. Scores are relative 0-100 values, not raw slowdown percentages.',
  labelClassName: 'impact-label overall-primary',
  seriesConfig: {
    score: {
      label: 'Impact score',
      color: 'var(--chart-5)',
    },
  },
} satisfies ScoreChartConfig

const categoryBalancedConfig = {
  chartId: 'overall-impact-category-balanced',
  mode: 'category-balanced',
  title: 'Overall Impact Score: Category-Balanced',
  subtitle: 'Workloads are averaged within categories, then categories are weighted equally. Lower is better.',
  footnote:
    'Category-balanced score prevents categories with more test cases from dominating. Build uses 25% clean and 75% incremental; New EXE uses the worse sequence step; extension sensitivity averages EXE/DLL/JS/PS1.',
  labelClassName: 'impact-label overall-secondary',
  seriesConfig: {
    score: {
      label: 'Impact score',
      color: 'var(--chart-penalty)',
    },
  },
} satisfies ScoreChartConfig

const severityWeightedConfig = {
  chartId: 'overall-impact-severity-weighted',
  mode: 'severity-weighted',
  title: 'Overall Performance Impact Score: Severity-Weighted',
  subtitle: 'Workload severity levels are scored exponentially, so severe individual slowdowns count more. Lower is better.',
  footnote:
    'Each workload uses its heatmap level: lowest=1, very low=2, low=4, mid=8, high=16, very high=32, highest=64. The chart shows an availability-adjusted total, so missing cells do not make a product look artificially better.',
  labelClassName: 'impact-label overall-tertiary',
  seriesConfig: {
    score: {
      label: 'Severity-weighted score',
      color: 'var(--chart-8)',
    },
  },
} satisfies ScoreChartConfig

const chartConfigs = [equalWorkloadConfig, categoryBalancedConfig, severityWeightedConfig]

export function OverallImpactScoreCharts({ data, onReady }: Props) {
  const locale = useLocale()
  const localizedConfigs = useMemo(
    () => chartConfigs.map((config) => localizeScoreConfig(config, locale)),
    [locale],
  )
  return (
    <>
      {localizedConfigs.map((config) => (
        <OverallImpactScoreChart
          config={config}
          data={data}
          key={config.chartId}
          locale={locale}
          onReady={onReady}
        />
      ))}
    </>
  )
}

function OverallImpactScoreChart({
  config,
  data,
  locale,
  onReady,
}: {
  config: ScoreChartConfig
  data: CompilationWorkloadData
  locale: Locale
  onReady: () => void
}) {
  const chartData = useMemo(() => buildScoreRows(data, config.mode, locale), [config.mode, data, locale])
  const copy = text(locale)
  const axisMax = config.mode === 'severity-weighted' ? niceAxisMax(Math.max(...chartData.map((row) => row.score))) : 100
  const ticks = config.mode === 'severity-weighted'
    ? createTicks(axisMax)
    : [0, 20, 40, 60, 80, 100]

  useEffect(() => {
    const frame = window.requestAnimationFrame(onReady)
    return () => window.cancelAnimationFrame(frame)
  }, [onReady])

  return (
    <section className="chart-export-shell" data-chart-id={config.chartId}>
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
              layout="vertical"
              margin={{ top: 14, right: 62, bottom: 38, left: 12 }}
            >
              <CartesianGrid vertical={false} strokeDasharray="3 3" />
              <XAxis
                type="number"
                domain={[0, axisMax]}
                ticks={ticks}
                tickFormatter={(value) => `${value}`}
                tickLine={false}
                axisLine={false}
                tickMargin={8}
              >
                <Label
                  value={config.mode === 'severity-weighted' ? copy.severityWeightedScoreAxis : copy.impactScoreAxis}
                  position="insideBottom"
                  offset={-24}
                  className="axis-label"
                />
              </XAxis>
              <YAxis
                dataKey="avName"
                type="category"
                interval={0}
                tickLine={false}
                axisLine={false}
                tickMargin={8}
                width={150}
              />
              <ChartTooltip cursor={false} content={<ScoreTooltip mode={config.mode} />} />
              <Bar
                dataKey="score"
                name="score"
                fill="var(--color-score)"
                radius={[4, 4, 4, 4]}
                maxBarSize={40}
              >
                <LabelList
                  dataKey="score"
                  content={(props) => renderScoreLabel(props, config.labelClassName, config.mode)}
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

function buildScoreRows(data: CompilationWorkloadData, mode: ScoreMode, locale: Locale) {
  const avNames = getAvNames(data)
  const columns = buildWorkloadColumns(data)
  const valuesByColumn = new Map(
    columns.map((column) => [column.key, valuesForColumn(column, avNames)]),
  )

  return avNames
    .map((avName): ScoreDatum => {
      const workloadScores = columns.map((column) =>
        normalizedLogScore(column.value(avName), valuesByColumn.get(column.key) ?? []),
      )
      const categoryScores = workloadCategories.map((category) =>
        average(
          columns
            .filter((column) => column.category === category)
            .map((column) =>
              normalizedLogScore(column.value(avName), valuesByColumn.get(column.key) ?? []),
            ),
        ),
      )
      const levelScores = columns.map((column) => {
        const level = normalizedLevel(column.value(avName), valuesByColumn.get(column.key) ?? [])
        return level === null ? null : levelPenaltyScore(level)
      })
      const score =
        mode === 'equal-workload'
          ? average(workloadScores)
          : mode === 'category-balanced'
            ? average(categoryScores)
            : availabilityAdjustedTotal(levelScores, columns.length)

      return {
        avName: avLabel(avName, locale),
        score: score ?? 0,
        workloadCount: workloadScores.filter((value) => value !== null).length,
        categoryCount: categoryScores.filter((value) => value !== null).length,
      }
    })
    .sort((left, right) => left.score - right.score)
}

function ScoreTooltip({ active, label, mode, payload }: TooltipProps & { mode: ScoreMode }) {
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
        <span>{mode === 'severity-weighted' ? copy.severityWeightedScore : copy.impactScore}</span>
        <strong>{formatScore(row.score, mode)}</strong>
      </div>
      <div className="chart-tooltip-row single">
        <span>{mode === 'equal-workload' ? copy.workloads : copy.categories}</span>
        <strong>{mode === 'category-balanced' ? row.categoryCount : row.workloadCount}</strong>
      </div>
    </div>
  )
}

function renderScoreLabel(props: unknown, className: string, mode: ScoreMode) {
  const { x, y, width, height, value } = props as LabelContentProps
  const score = Number(value)
  if (!Number.isFinite(score)) {
    return null
  }

  const xValue = Number(x)
  const yValue = Number(y)
  const widthValue = Number(width)
  const heightValue = Number(height)
  if (![xValue, yValue, widthValue, heightValue].every(Number.isFinite)) {
    return null
  }

  return (
    <text
      x={xValue + widthValue + 8}
      y={yValue + heightValue / 2 + 4}
      textAnchor="start"
      className={className}
    >
      {formatScore(score, mode)}
    </text>
  )
}

function availabilityAdjustedTotal(scores: Array<number | null>, totalWorkloads: number) {
  const meanScore = average(scores)
  return meanScore === null ? null : meanScore * totalWorkloads
}

function niceAxisMax(value: number) {
  if (!Number.isFinite(value) || value <= 0) {
    return 100
  }

  const magnitude = 10 ** Math.floor(Math.log10(value))
  const normalized = value / magnitude
  const niceNormalized = normalized <= 2 ? 2 : normalized <= 5 ? 5 : 10
  return niceNormalized * magnitude
}

function createTicks(max: number) {
  return Array.from({ length: 6 }, (_, index) => Math.round((max / 5) * index))
}

function formatScore(score: number, mode?: ScoreMode) {
  return mode === 'severity-weighted' ? score.toFixed(0) : score.toFixed(1)
}

function localizeScoreConfig(config: ScoreChartConfig, locale: Locale): ScoreChartConfig {
  if (locale !== 'zh-cn') {
    return config
  }

  if (config.mode === 'equal-workload') {
    return {
      ...config,
      title: '总体影响分数：测试项等权',
      subtitle: '对各测试项的对数归一化分数取平均。数值越低越好。',
      footnote:
        '每个测试项先在各产品之间做对数归一化，再等权计入。分数为 0-100 的相对值，不是原始变慢百分比。',
      seriesConfig: {
        score: {
          label: '影响分数',
          color: config.seriesConfig.score.color,
        },
      },
    }
  }

  if (config.mode === 'severity-weighted') {
    return {
      ...config,
      title: '总体性能影响分数：严重度加权',
      subtitle: '按测试项严重程度等级指数计分，让单项严重变慢获得更高权重。数值越低越好。',
      footnote:
        '每个测试项按热力图等级计分：本项最低=1、很低=2、低=4、中等=8、高=16、很高=32、本项最高=64。若某些测试项缺少数据，会根据已有结果估算完整总分，避免产品因缺项而得到偏低的总分。',
      seriesConfig: {
        score: {
          label: '严重度加权分数',
          color: config.seriesConfig.score.color,
        },
      },
    }
  }

  return {
    ...config,
    title: '总体影响分数：类别均衡',
    subtitle: '先在类别内平均各测试项，再让各类别等权。数值越低越好。',
    footnote:
      '类别均衡分数可避免测试项数量较多的类别主导结果。构建按全量 25%、增量 75% 加权；新 EXE 取两步中较高的耗时增幅；扩展名敏感度取 EXE/DLL/JS/PS1 平均值。',
    seriesConfig: {
      score: {
        label: '影响分数',
        color: config.seriesConfig.score.color,
      },
    },
  }
}
