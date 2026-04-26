import { useEffect, useMemo } from 'react'
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import type { CompilationWorkloadData } from '../data/compilationWorkloads'
import { avLabel, text, useLocale, workloadLabel } from '../i18n'
import {
  buildWorkloadColumns,
  formatImpactPercent,
  getAvNames,
  normalizedLevel,
  valuesForColumn,
} from './workloadImpactModel'

type Props = {
  data: CompilationWorkloadData
  onReady: () => void
}

type HeatmapCell = {
  key: string
  label: string
  value: number | null
  level: number
}

type HeatmapRow = {
  avName: string
  cells: HeatmapCell[]
}

const levelLabels = [
  'Lowest in workload',
  'Very low',
  'Low',
  'Mid',
  'High',
  'Very high',
  'Highest in workload',
]

const zhLevelLabels = [
  '本项最低',
  '很低',
  '低',
  '中等',
  '高',
  '很高',
  '本项最高',
]

export function WorkloadImpactHeatmap({ data, onReady }: Props) {
  const locale = useLocale()
  const copy = text(locale)
  const legendLabels = locale === 'zh-cn' ? zhLevelLabels : levelLabels
  const { columns, rows } = useMemo(() => buildHeatmap(data), [data])

  useEffect(() => {
    const frame = window.requestAnimationFrame(onReady)
    return () => window.cancelAnimationFrame(frame)
  }, [onReady])

  return (
    <section className="chart-export-shell heatmap-shell" data-chart-id="workload-impact-heatmap">
      <Card className="chart-card heatmap-card">
        <CardHeader className="chart-card-header">
          <CardTitle className="chart-card-title">
            {locale === 'zh-cn' ? '杀毒软件性能影响热力图' : 'Antivirus Workload Impact Heatmap'}
          </CardTitle>
          <CardDescription>
            {locale === 'zh-cn'
              ? '平均耗时增幅相对基线 OS 计算，数值越低越好。颜色按测试项分别做对数归一化。'
              : 'Mean wall-time impact versus baseline OS. Lower is better. Color is log-normalized within each workload column.'}
          </CardDescription>
        </CardHeader>
        <CardContent className="chart-card-content heatmap-content">
          <div className="heatmap-legend" aria-label="Heatmap color legend">
            {legendLabels.map((label, index) => (
              <div className="heatmap-legend-item" key={label}>
                <span className={`heatmap-legend-swatch heatmap-level-${index}`} />
                <span>{label}</span>
              </div>
            ))}
          </div>
          <div
            className="heatmap-grid"
            style={{
              gridTemplateColumns: `130px repeat(${columns.length}, minmax(50px, 1fr))`,
            }}
          >
            <div className="heatmap-corner">{copy.avProduct}</div>
            {columns.map((column) => (
              <div className="heatmap-column-header" key={column.key}>
                <span>{workloadLabel(column.key, column.label, locale)}</span>
              </div>
            ))}
            {rows.map((row) => (
              <RowFragment key={row.avName} row={row} />
            ))}
          </div>
        </CardContent>
        <CardFooter className="chart-card-footer">
          {locale === 'zh-cn'
            ? '构建测试按全量 25%、增量 75% 加权。新 EXE 运行序列取两步中较高的耗时增幅。扩展名敏感度取 EXE、DLL、JS、PS1 的平均值。颜色按测试项分别归一化；每个格子显示实际耗时增幅。'
            : 'Build columns weight clean at 25% and incremental at 75%. New EXE sequence uses the worse of the two sequence steps. Extension sensitivity averages EXE, DLL, JS, and PS1. Color is log-normalized per column; cell text shows actual impact.'}
        </CardFooter>
      </Card>
    </section>
  )
}

function RowFragment({ row }: { row: HeatmapRow }) {
  const locale = useLocale()
  return (
    <>
      <div className="heatmap-row-header">{avLabel(row.avName, locale)}</div>
      {row.cells.map((cell) => (
        <div
          className={
            cell.value === null
              ? 'heatmap-cell heatmap-missing'
              : `heatmap-cell heatmap-level-${cell.level}`
          }
          key={`${row.avName}-${cell.key}`}
          title={`${avLabel(row.avName, locale)} / ${workloadLabel(cell.key, cell.label, locale)}: ${formatImpactPercent(cell.value)}`}
        >
          {formatImpactPercent(cell.value)}
        </div>
      ))}
    </>
  )
}

function buildHeatmap(data: CompilationWorkloadData) {
  const avNames = getAvNames(data)
  const columns = buildWorkloadColumns(data)

  const valuesByColumn = new Map(
    columns.map((column) => [column.key, valuesForColumn(column, avNames)]),
  )

  const rows = avNames.map((avName): HeatmapRow => {
    const cells = columns.map((column): HeatmapCell => {
      const value = column.value(avName)
      return {
        key: column.key,
        label: column.label,
        value,
        level: normalizedLevel(value, valuesByColumn.get(column.key) ?? []) ?? 0,
      }
    })

    return { avName, cells }
  })

  return { columns, rows }
}
