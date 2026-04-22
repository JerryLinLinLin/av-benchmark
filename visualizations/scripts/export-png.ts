import { mkdir } from 'node:fs/promises'
import path from 'node:path'
import { spawn, spawnSync } from 'node:child_process'
import { chromium } from 'playwright'

const experiment = getArgumentValue('--exp') ?? 'exp1'
const root = path.resolve(import.meta.dirname, '..')
const outputDir = path.join(root, 'exports', experiment)
const outputs = [
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
    chartId: 'compilation-average-combined',
    path: path.join(outputDir, 'compilation-builds-average-impact.png'),
  },
  {
    chartId: 'compilation-average-total',
    path: path.join(outputDir, 'compilation-builds-total-average-impact.png'),
  },
]
const port = 5178
const url = `http://127.0.0.1:${port}/?exp=${encodeURIComponent(experiment)}`

await mkdir(outputDir, { recursive: true })

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
  await waitForServer(url)

  const browser = await chromium.launch()
  const page = await browser.newPage({ viewport: { width: 1700, height: 1200 }, deviceScaleFactor: 1 })
  await page.goto(url)
  for (const output of outputs) {
    await page
      .locator(`[data-chart-id="${output.chartId}"] svg.recharts-surface`)
      .waitFor({ timeout: 15_000 })
  }
  await page.waitForTimeout(300)
  for (const output of outputs) {
    await page
      .locator(`[data-chart-id="${output.chartId}"]`)
      .screenshot({ path: output.path })
    console.log(`Wrote ${path.relative(process.cwd(), output.path)}`)
  }
  await browser.close()
} finally {
  if (server.pid && process.platform === 'win32') {
    spawnSync('taskkill', ['/pid', String(server.pid), '/T', '/F'])
  } else {
    server.kill()
  }
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
