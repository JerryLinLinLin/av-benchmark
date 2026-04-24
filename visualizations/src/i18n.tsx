/* eslint-disable react-refresh/only-export-components */
import { createContext, useContext, type ReactNode } from 'react'

export type Locale = 'en' | 'zh-cn'

const LocaleContext = createContext<Locale>('en')

export function LocaleProvider({
  children,
  locale,
}: {
  children: ReactNode
  locale: Locale
}) {
  return <LocaleContext.Provider value={locale}>{children}</LocaleContext.Provider>
}

export function useLocale() {
  return useContext(LocaleContext)
}

export function isZh(locale: Locale) {
  return locale === 'zh-cn'
}

const avNames: Record<string, string> = {
  '360ts': '360 Total Security',
  avast: 'Avast',
  avira: '小红伞',
  bitdefender: '比特梵德',
  drweb: 'Dr.Web',
  emsisoft: 'Emsisoft',
  eset: 'ESET',
  gdata: 'G DATA',
  huorong: '火绒',
  kaspersky: '卡巴斯基',
  malwarebytes: 'Malwarebytes',
  mcafee: '迈克菲',
  'ms-defender': 'Microsoft Defender',
  sophos: 'Sophos',
  'tencent-pcmgr': '腾讯电脑管家',
  trendmicro: '趋势科技',
}

const workloadLabels: Record<string, string> = {
  'ripgrep-build': 'Ripgrep 构建',
  'roslyn-build': 'Roslyn 构建',
  'file-create-delete': '文件创建/删除',
  'file-enum-large-dir': '大目录枚举',
  'file-write-content': '文件写入',
  'archive-extract': '压缩包解压',
  'hardlink-create': '创建硬链接',
  'junction-create': '创建 Junction',
  'fs-watcher': '文件监视器',
  'process-create-wait': '进程创建/等待',
  'new-exe-sequence': '新 EXE 序列',
  'dll-load-unique': 'DLL 唯一加载',
  'com-create-instance': 'COM 实例创建',
  'thread-create': '线程创建',
  'mem-alloc-protect': '内存分配/保护',
  'mem-map-file': '内存映射文件',
  'net-connect-loopback': '回环连接',
  'net-dns-resolve': 'DNS 解析',
  'pipe-roundtrip': '管道往返',
  'registry-crud': '注册表 CRUD',
  'crypto-hash-verify': '加密哈希验证',
  'extension-sensitivity': '扩展名敏感性',
}

const microbenchTitles: Record<string, string> = {
  'file-create-delete': '文件创建/删除：平均影响',
  'archive-extract': '压缩包解压：平均影响',
  'file-enum-large-dir': '大目录枚举：平均影响',
  'hardlink-create': '创建硬链接：平均影响',
  'junction-create': '创建 Junction：平均影响',
  'process-create-wait': '进程创建/等待：平均影响',
  'dll-load-unique': 'DLL 唯一加载：平均影响',
  'file-write-content': '文件写入：平均影响',
  'thread-create': '线程创建：平均影响',
  'mem-alloc-protect': '内存分配/保护：平均影响',
  'mem-map-file': '内存映射文件：平均影响',
  'net-connect-loopback': '回环连接：平均影响',
  'net-dns-resolve': 'DNS 解析：平均影响',
  'registry-crud': '注册表 CRUD：平均影响',
  'pipe-roundtrip': '管道往返：平均影响',
  'crypto-hash-verify': '加密哈希验证：平均影响',
  'com-create-instance': 'COM 实例创建：平均影响',
  'fs-watcher': '文件系统监视器：平均影响',
}

export function avLabel(avName: string, locale: Locale) {
  return isZh(locale) ? (avNames[avName] ?? avName) : avName
}

export function workloadLabel(key: string, fallback: string, locale: Locale) {
  return isZh(locale) ? (workloadLabels[key] ?? fallback) : fallback
}

export function microbenchTitle(id: string, fallback: string, locale: Locale) {
  return isZh(locale) ? (microbenchTitles[id] ?? fallback) : fallback
}

export function text(locale: Locale) {
  const zh = isZh(locale)

  return {
    antivirusProduct: zh ? '杀毒软件产品' : 'Antivirus product',
    avProduct: zh ? 'AV 产品' : 'AV product',
    lowerIsBetter: zh ? '越低越好。' : 'Lower is better.',
    averageImpact: zh ? '平均影响' : 'Average impact',
    averageImpactPct: zh ? '平均影响（%）' : 'Average impact (%)',
    cloudColdImpactPct: zh ? '云端冷启动影响（%）' : 'Cloud-cold impact (%)',
    cleanBuild: zh ? '全量构建' : 'Clean build',
    incrementalBuild: zh ? '增量构建' : 'Incremental build',
    meanWallTime: zh ? '平均耗时' : 'Mean wall time',
    baselineMean: zh ? '基线平均耗时' : 'Baseline mean',
    status: zh ? '状态' : 'Status',
    impactScore: zh ? '影响分数' : 'Impact score',
    impactScoreAxis: zh ? '影响分数（0-100）' : 'Impact score (0-100)',
    workloads: zh ? '工作负载' : 'Workloads',
    categories: zh ? '类别' : 'Categories',
    loading: zh ? '正在加载图表数据...' : 'Loading chart data...',
    loadError: zh ? '无法加载图表数据' : 'Unable to load chart data',
  }
}
