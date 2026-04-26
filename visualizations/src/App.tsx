import { useEffect, useMemo, useState } from 'react'
import './App.css'
import { CompilationWorkloadsChart } from './charts/CompilationWorkloadsChart'
import { ArchiveExtractChart } from './charts/ArchiveExtractChart'
import { ComCreateInstanceChart } from './charts/ComCreateInstanceChart'
import { CryptoHashVerifyChart } from './charts/CryptoHashVerifyChart'
import { DllLoadUniqueChart } from './charts/DllLoadUniqueChart'
import { ExtensionSensitivityChart } from './charts/ExtensionSensitivityChart'
import { FileEnumLargeDirChart } from './charts/FileEnumLargeDirChart'
import { FileCreateDeleteChart } from './charts/FileCreateDeleteChart'
import { FileWriteContentChart } from './charts/FileWriteContentChart'
import { FsWatcherChart } from './charts/FsWatcherChart'
import { HardlinkCreateChart } from './charts/HardlinkCreateChart'
import { JunctionCreateChart } from './charts/JunctionCreateChart'
import { MemAllocProtectChart } from './charts/MemAllocProtectChart'
import { MemMapFileChart } from './charts/MemMapFileChart'
import { NetConnectLoopbackChart } from './charts/NetConnectLoopbackChart'
import { NetDnsResolveChart } from './charts/NetDnsResolveChart'
import { NewExeRunSequenceChart } from './charts/NewExeRunSequenceChart'
import { OverallImpactScoreCharts } from './charts/OverallImpactScoreCharts'
import { PipeRoundtripChart } from './charts/PipeRoundtripChart'
import { ProcessCreateWaitChart } from './charts/ProcessCreateWaitChart'
import { RegistryCrudChart } from './charts/RegistryCrudChart'
import { ThreadCreateChart } from './charts/ThreadCreateChart'
import { WorkloadImpactHeatmap } from './charts/WorkloadImpactHeatmap'
import {
  loadCompilationWorkloadData,
  type CompilationWorkloadData,
} from './data/compilationWorkloads'
import { LocaleProvider, type Locale, text } from './i18n'

function App() {
  const experiment = useMemo(() => {
    const params = new URLSearchParams(window.location.search)
    return params.get('exp') ?? 'exp1'
  }, [])
  const locale = useMemo<Locale>(() => {
    const params = new URLSearchParams(window.location.search)
    return params.get('lang') === 'zh-cn' ? 'zh-cn' : 'en'
  }, [])
  const copy = text(locale)
  const [data, setData] = useState<CompilationWorkloadData | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [chartReady, setChartReady] = useState(false)

  useEffect(() => {
    loadCompilationWorkloadData(experiment)
      .then(setData)
      .catch((err: unknown) => {
        setError(err instanceof Error ? err.message : copy.loadError)
      })
  }, [copy.loadError, experiment])

  return (
    <LocaleProvider locale={locale}>
      <main className="workspace" lang={locale}>
        <section className="export-frame" data-chart-ready={chartReady}>
          {error ? <p className="message">{error}</p> : null}
          {!data && !error ? <p className="message">{copy.loading}</p> : null}
          {data ? (
            <>
              <CompilationWorkloadsChart
                data={data}
                onReady={() => setChartReady(true)}
              />
              <WorkloadImpactHeatmap
                data={data}
                onReady={() => setChartReady(true)}
              />
              <OverallImpactScoreCharts
                data={data}
                onReady={() => setChartReady(true)}
              />
              <FileCreateDeleteChart
                data={data.microbench.fileCreateDelete}
                onReady={() => setChartReady(true)}
              />
              <ArchiveExtractChart
                data={data.microbench.archiveExtract}
                onReady={() => setChartReady(true)}
              />
              <FileEnumLargeDirChart
                data={data.microbench.fileEnumLargeDir}
                onReady={() => setChartReady(true)}
              />
              <HardlinkCreateChart
                data={data.microbench.hardlinkCreate}
                onReady={() => setChartReady(true)}
              />
              <JunctionCreateChart
                data={data.microbench.junctionCreate}
                onReady={() => setChartReady(true)}
              />
              <ProcessCreateWaitChart
                data={data.microbench.processCreateWait}
                onReady={() => setChartReady(true)}
              />
              <DllLoadUniqueChart
                data={data.microbench.dllLoadUnique}
                onReady={() => setChartReady(true)}
              />
              <FileWriteContentChart
                data={data.microbench.fileWriteContent}
                onReady={() => setChartReady(true)}
              />
              <ThreadCreateChart
                data={data.microbench.threadCreate}
                onReady={() => setChartReady(true)}
              />
              <MemAllocProtectChart
                data={data.microbench.memAllocProtect}
                onReady={() => setChartReady(true)}
              />
              <MemMapFileChart
                data={data.microbench.memMapFile}
                onReady={() => setChartReady(true)}
              />
              <NetConnectLoopbackChart
                data={data.microbench.netConnectLoopback}
                onReady={() => setChartReady(true)}
              />
              <NetDnsResolveChart
                data={data.microbench.netDnsResolve}
                onReady={() => setChartReady(true)}
              />
              <RegistryCrudChart
                data={data.microbench.registryCrud}
                onReady={() => setChartReady(true)}
              />
              <PipeRoundtripChart
                data={data.microbench.pipeRoundtrip}
                onReady={() => setChartReady(true)}
              />
              <CryptoHashVerifyChart
                data={data.microbench.cryptoHashVerify}
                onReady={() => setChartReady(true)}
              />
              <ComCreateInstanceChart
                data={data.microbench.comCreateInstance}
                onReady={() => setChartReady(true)}
              />
              <FsWatcherChart
                data={data.microbench.fsWatcher}
                onReady={() => setChartReady(true)}
              />
              <NewExeRunSequenceChart
                data={{
                  run1: data.microbench.newExeRun,
                  run2: data.microbench.newExeRunMotw,
                }}
                onReady={() => setChartReady(true)}
              />
              <ExtensionSensitivityChart
                data={data.microbench.extensionSensitivity}
                onReady={() => setChartReady(true)}
              />
            </>
          ) : null}
        </section>
      </main>
    </LocaleProvider>
  )
}

export default App
