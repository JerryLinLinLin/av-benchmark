import { mkdir } from 'node:fs/promises'
import path from 'node:path'
import { spawn, spawnSync } from 'node:child_process'
import { chromium } from 'playwright'

const experiment = getArgumentValue('--exp') ?? 'exp1'
const root = path.resolve(import.meta.dirname, '..')
const outputDir = path.join(root, 'exports', experiment)
const zhOutputDir = path.join(outputDir, 'zh-cn')
const outputs = [
  {
    chartId: 'workload-impact-heatmap',
    path: path.join(outputDir, 'av-workload-impact-heatmap.png'),
    waitForSvg: false,
  },
  {
    chartId: 'overall-impact-equal-workload',
    path: path.join(outputDir, 'overall-impact-score-equal-workload.png'),
  },
  {
    chartId: 'overall-impact-category-balanced',
    path: path.join(outputDir, 'overall-impact-score-category-balanced.png'),
  },
  {
    chartId: 'ripgrep-cloud-cold',
    path: path.join(outputDir, 'ripgrep-build-cloud-cold-impact.png'),
  },
  {
    chartId: 'roslyn-cloud-cold',
    path: path.join(outputDir, 'roslyn-build-cloud-cold-impact.png'),
  },
  {
    chartId: 'ripgrep-average',
    path: path.join(outputDir, 'ripgrep-build-average-impact.png'),
  },
  {
    chartId: 'roslyn-average',
    path: path.join(outputDir, 'roslyn-build-average-impact.png'),
  },
  {
    chartId: 'file-create-delete-average',
    path: path.join(outputDir, 'file-create-delete-average-impact.png'),
  },
  {
    chartId: 'archive-extract-average',
    path: path.join(outputDir, 'archive-extract-average-impact.png'),
  },
  {
    chartId: 'file-enum-large-dir-average',
    path: path.join(outputDir, 'file-enum-large-dir-average-impact.png'),
  },
  {
    chartId: 'hardlink-create-average',
    path: path.join(outputDir, 'hardlink-create-average-impact.png'),
  },
  {
    chartId: 'junction-create-average',
    path: path.join(outputDir, 'junction-create-average-impact.png'),
  },
  {
    chartId: 'process-create-wait-average',
    path: path.join(outputDir, 'process-create-wait-average-impact.png'),
  },
  {
    chartId: 'dll-load-unique-average',
    path: path.join(outputDir, 'dll-load-unique-average-impact.png'),
  },
  {
    chartId: 'file-write-content-average',
    path: path.join(outputDir, 'file-write-content-average-impact.png'),
  },
  {
    chartId: 'thread-create-average',
    path: path.join(outputDir, 'thread-create-average-impact.png'),
  },
  {
    chartId: 'mem-alloc-protect-average',
    path: path.join(outputDir, 'mem-alloc-protect-average-impact.png'),
  },
  {
    chartId: 'mem-map-file-average',
    path: path.join(outputDir, 'mem-map-file-average-impact.png'),
  },
  {
    chartId: 'net-connect-loopback-average',
    path: path.join(outputDir, 'net-connect-loopback-average-impact.png'),
  },
  {
    chartId: 'net-dns-resolve-average',
    path: path.join(outputDir, 'net-dns-resolve-average-impact.png'),
  },
  {
    chartId: 'registry-crud-average',
    path: path.join(outputDir, 'registry-crud-average-impact.png'),
  },
  {
    chartId: 'pipe-roundtrip-average',
    path: path.join(outputDir, 'pipe-roundtrip-average-impact.png'),
  },
  {
    chartId: 'crypto-hash-verify-average',
    path: path.join(outputDir, 'crypto-hash-verify-average-impact.png'),
  },
  {
    chartId: 'com-create-instance-average',
    path: path.join(outputDir, 'com-create-instance-average-impact.png'),
  },
  {
    chartId: 'fs-watcher-average',
    path: path.join(outputDir, 'fs-watcher-average-impact.png'),
  },
  {
    chartId: 'new-exe-run-sequence-average',
    path: path.join(outputDir, 'new-exe-run-sequence-average-impact.png'),
  },
  {
    chartId: 'extension-sensitivity-average',
    path: path.join(outputDir, 'extension-sensitivity-average-impact.png'),
  },
]
await mkdir(outputDir, { recursive: true })
await mkdir(zhOutputDir, { recursive: true })

const port = 5178

const serverCommand = process.platform === 'win32' ? 'cmd.exe' : 'pnpm'
const serverArgs =
  process.platform === 'win32'
    ? ['/d', '/s', '/c', 'pnpm', 'exec', 'vite', '--host', '127.0.0.1', '--port', String(port)]
    : ['exec', 'vite', '--host', '127.0.0.1', '--port', String(port)]
const server = spawn(serverCommand, serverArgs, {
  cwd: root,
  stdio: 'pipe',
})

try {
  await waitForServer(`http://127.0.0.1:${port}/?exp=${encodeURIComponent(experiment)}`)

  const browser = await chromium.launch()
  await exportLocale(browser, 'en', outputDir)
  await exportLocale(browser, 'zh-cn', zhOutputDir)
  await browser.close()
} finally {
  if (server.pid && process.platform === 'win32') {
    spawnSync('taskkill', ['/pid', String(server.pid), '/T', '/F'])
  } else {
    server.kill()
  }
}

async function exportLocale(
  browser: Awaited<ReturnType<typeof chromium.launch>>,
  locale: 'en' | 'zh-cn',
  targetDir: string,
) {
  const page = await browser.newPage({ viewport: { width: 1700, height: 1200 }, deviceScaleFactor: 1 })
  const params = new URLSearchParams({ exp: experiment })
  if (locale === 'zh-cn') {
    params.set('lang', 'zh-cn')
  }

  await page.goto(`http://127.0.0.1:${port}/?${params.toString()}`)
  for (const output of outputs) {
    const selector = output.waitForSvg === false
      ? `[data-chart-id="${output.chartId}"]`
      : `[data-chart-id="${output.chartId}"] svg.recharts-surface`
    await page.locator(selector).waitFor({ timeout: 15_000 })
  }
  await page.waitForTimeout(300)
  for (const output of outputs) {
    const outputPath = path.join(targetDir, path.basename(output.path))
    await page
      .locator(`[data-chart-id="${output.chartId}"]`)
      .screenshot({ path: outputPath })
    console.log(`Wrote ${path.relative(process.cwd(), outputPath)}`)
  }
  await page.close()
}

async function waitForServer(targetUrl: string) {
  const deadline = Date.now() + 20_000
  while (Date.now() < deadline) {
    try {
      const response = await fetch(targetUrl)
      if (response.ok) {
        return
      }
    } catch {
      await new Promise((resolve) => setTimeout(resolve, 250))
    }
  }
  throw new Error(`Timed out waiting for ${targetUrl}`)
}

function getArgumentValue(name: string) {
  const index = process.argv.indexOf(name)
  return index >= 0 ? process.argv[index + 1] : undefined
}
