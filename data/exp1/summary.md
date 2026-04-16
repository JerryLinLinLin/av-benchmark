# AV Benchmark Comparison Report

Generated: 2026-04-16 16:21:46 UTC

## ms-defender (Windows Defender v4.18.2201.11) vs baseline-os

| Scenario | Median Wall (ms) | Slowdown | p95 Slowdown | Disk Read Δ (MB) | Disk Write Δ (MB) | CV % | Baseline CV % | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| new-exe-run-motw | 68200.0 | +3422.7% | +10550.4% | +92.1 | +189.7 | 19.9% | 0.9% | noisy |
| dll-load-unique | 55649.0 | +599.4% | +540.5% | +0.9 | +12.9 | 0.6% | 1.8% | ok |
| new-exe-run | 5408.0 | +182.5% | +190.5% | +73.8 | +14.2 | 2.7% | 1.0% | ok |
| mem-map-file | 8795.0 | +123.7% | +122.5% | -0.3 | +2.3 | 1.4% | 6.7% | ok |
| file-enum-large-dir | 5258.0 | +119.0% | +112.9% | +28.1 | +0.0 | 0.9% | 0.9% | ok |
| fs-watcher | 3472.0 | +57.3% | +44.3% | -0.1 | +17.2 | 1.3% | 0.9% | ok |
| archive-extract | 19972.0 | +53.4% | +100.1% | +68.0 | +10.8 | 1.0% | 1.4% | ok |
| file-create-delete | 2236.0 | +50.1% | +53.3% | +0.9 | +13.2 | 1.3% | 1.3% | ok |
| ext-sensitivity-exe | 4453.0 | +47.9% | +45.7% | +0.0 | +13.5 | 0.6% | 0.4% | ok |
| ext-sensitivity-ps1 | 4389.0 | +47.0% | +43.2% | +0.0 | +9.1 | 0.6% | 0.2% | ok |
| ext-sensitivity-dll | 4353.0 | +44.7% | +43.1% | +0.0 | +11.9 | 1.1% | 0.7% | ok |
| ext-sensitivity-js | 4360.0 | +43.6% | +41.2% | -0.0 | +0.8 | 0.7% | 0.8% | ok |
| file-write-content | 4687.0 | +42.5% | +38.8% | +0.6 | +12.8 | 0.7% | 0.5% | ok |
| hardlink-create | 1734.0 | +38.2% | +33.5% | +0.0 | -2.5 | 0.3% | 0.5% | ok |
| registry-crud | 590.0 | +29.7% | +25.1% | -0.0 | +0.0 | 0.1% | 3.8% | ok |
| roslyn-clean-build | 260770.0 | +26.4% | — | -9162.5 | +112.0 | 2.8% | 2.7% | ok |
| junction-create | 728.0 | +24.2% | +25.4% | -0.0 | +4.6 | 0.8% | 2.0% | ok |
| ripgrep-clean-build | 18272.0 | +22.1% | — | +111.4 | +5.8 | 0.2% | 0.2% | ok |
| mem-alloc-protect | 202.0 | +20.2% | +11.1% | +0.0 | +0.2 | 1.4% | 1.0% | ok |
| process-create-wait | 15969.0 | +20.0% | +18.6% | +0.5 | +0.4 | 0.3% | 0.4% | ok |
| roslyn-incremental-build | 69593.0 | +16.5% | — | +29.4 | +4.0 | 2.2% | 3.7% | ok |
| com-create-instance | 544.0 | +15.7% | +12.1% | +0.0 | +0.0 | 0.7% | 1.8% | ok |
| net-dns-resolve | 1042.0 | +8.9% | +9.4% | +0.0 | +0.0 | 0.9% | 1.4% | ok |
| thread-create | 1248.0 | +7.1% | +10.2% | +0.0 | +0.0 | 1.5% | 1.7% | ok |
| crypto-hash-verify | 262.0 | +4.4% | +0.5% | +0.1 | -0.1 | 2.0% | 1.1% | ok |
| ripgrep-incremental-build | 6129.0 | +4.0% | — | +0.1 | +0.3 | 0.2% | 1.5% | ok |
| net-connect-loopback | 597.0 | +3.8% | +0.7% | +0.4 | +0.1 | 0.8% | 0.6% | ok |
| token-query | 55.0 | -1.8% | -7.1% | +0.0 | +0.0 | 0.9% | 2.2% | ok |
| pipe-roundtrip | 1397.0 | -8.0% | +3.5% | +0.1 | +0.0 | 40.3% | 27.9% | noisy |
| file-copy-large | 885.0 | -8.6% | -17.9% | -0.1 | +1.3 | 1.1% | 3.1% | ok |
| wmi-query | 8881.0 | -35.6% | -38.2% | +0.1 | -0.2 | 1.6% | 0.9% | anomaly |

Highest slowdown: dll-load-unique at +599.4%

Largest kernel CPU shift: roslyn-clean-build at +1.4pp (8.9% -> 10.3%)

Largest system disk write delta: new-exe-run-motw at +189.7 MB (12.0 -> 201.7 MB)

Largest system disk read delta: roslyn-clean-build at -9162.5 MB (11779.6 -> 2617.1 MB)

Noisy scenarios: new-exe-run-motw, pipe-roundtrip

Anomaly scenarios (AV appears faster - likely caching artifact): wmi-query

