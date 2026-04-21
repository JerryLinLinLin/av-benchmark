import { useEffect, useMemo, useState } from 'react'
import './App.css'
import { CompilationWorkloadsChart } from './charts/CompilationWorkloadsChart'
import {
  loadCompilationWorkloadData,
  type CompilationWorkloadData,
} from './data/compilationWorkloads'

function App() {
  const experiment = useMemo(() => {
    const params = new URLSearchParams(window.location.search)
    return params.get('exp') ?? 'exp1'
  }, [])
  const [data, setData] = useState<CompilationWorkloadData | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [chartReady, setChartReady] = useState(false)

  useEffect(() => {
    loadCompilationWorkloadData(experiment)
      .then(setData)
      .catch((err: unknown) => {
        setError(err instanceof Error ? err.message : 'Unable to load chart data')
      })
  }, [experiment])

  return (
    <main className="workspace">
      <section className="export-frame" data-chart-ready={chartReady}>
        {error ? <p className="message">{error}</p> : null}
        {!data && !error ? <p className="message">Loading chart data...</p> : null}
        {data ? (
          <CompilationWorkloadsChart
            data={data}
            onReady={() => setChartReady(true)}
          />
        ) : null}
      </section>
    </main>
  )
}

export default App
