# AV Benchmark Comparison Report

Generated: 2026-04-16 06:18:28 UTC

## ms-defender (Windows Defender v4.18.2201.11) vs baseline-os

| Scenario | Mean Wall (ms) | Slowdown | Kernel CPU % | Baseline Kernel % | Kernel Shift | CV % | Status |
|---|---:|---:|---:|---:|---:|---:|---|
| new-exe-run-motw | 70796.7 | +3566.3% | 0.0% | 0.0% | +0.0pp | 19.9% | noisy |
| dll-load-unique | 55792.3 | +597.1% | 0.0% | 0.0% | +0.0pp | 0.6% | ok |
| new-exe-run | 5315.7 | +178.2% | 0.0% | 0.0% | +0.0pp | 2.7% | ok |
| mem-map-file | 8876.3 | +135.7% | 0.0% | 0.0% | +0.0pp | 1.4% | ok |
| file-enum-large-dir | 5256.3 | +119.7% | 0.0% | 0.0% | +0.0pp | 0.9% | ok |
| fs-watcher | 3501.7 | +57.9% | 0.0% | 0.0% | +0.0pp | 1.3% | ok |
| archive-extract | 20106.7 | +54.1% | 0.0% | 0.0% | +0.0pp | 1.0% | ok |
| file-create-delete | 2245.3 | +50.3% | 0.0% | 0.0% | +0.0pp | 1.3% | ok |
| ext-sensitivity-exe | 4459.0 | +47.8% | 0.0% | 0.0% | +0.0pp | 0.6% | ok |
| ext-sensitivity-ps1 | 4404.3 | +47.6% | 0.0% | 0.0% | +0.0pp | 0.6% | ok |
| ext-sensitivity-dll | 4387.0 | +45.2% | 0.0% | 0.0% | +0.0pp | 1.1% | ok |
| ext-sensitivity-js | 4376.0 | +44.1% | 0.0% | 0.0% | +0.0pp | 0.7% | ok |
| file-write-content | 4690.7 | +42.2% | 0.0% | 0.0% | +0.0pp | 0.7% | ok |
| hardlink-create | 1733.3 | +38.2% | 0.0% | 0.0% | +0.0pp | 0.3% | ok |
| registry-crud | 590.0 | +32.6% | 0.0% | 0.0% | +0.0pp | 0.1% | ok |
| roslyn-clean-build | 264578.3 | +26.7% | 10.3% | 8.9% | +1.4pp | 2.8% | ok |
| junction-create | 725.3 | +22.6% | 0.0% | 0.0% | +0.0pp | 0.8% | ok |
| ripgrep-clean-build | 18265.7 | +22.0% | 4.3% | 3.9% | +0.5pp | 0.2% | ok |
| process-create-wait | 15961.7 | +19.7% | 0.0% | 0.0% | +0.0pp | 0.3% | ok |
| mem-alloc-protect | 200.0 | +19.5% | 0.0% | 0.0% | +0.0pp | 1.4% | ok |
| com-create-instance | 546.3 | +14.9% | 0.0% | 0.0% | +0.0pp | 0.7% | ok |
| roslyn-incremental-build | 69579.0 | +14.4% | 15.8% | 15.1% | +0.7pp | 2.2% | ok |
| net-dns-resolve | 1044.0 | +9.5% | 0.0% | 0.0% | +0.0pp | 0.9% | ok |
| thread-create | 1239.3 | +5.5% | 0.0% | 0.0% | +0.0pp | 1.5% | ok |
| net-connect-loopback | 595.3 | +3.8% | 0.0% | 0.0% | +0.0pp | 0.8% | ok |
| crypto-hash-verify | 259.3 | +3.5% | 0.0% | 0.0% | +0.0pp | 2.0% | ok |
| ripgrep-incremental-build | 6121.0 | +2.9% | 2.4% | 2.3% | +0.0pp | 0.2% | ok |
| token-query | 55.3 | -1.8% | 0.0% | 0.0% | +0.0pp | 0.9% | ok |
| file-copy-large | 880.7 | -9.0% | 0.0% | 0.0% | +0.0pp | 1.1% | ok |
| pipe-roundtrip | 1113.3 | -28.2% | 0.0% | 0.0% | +0.0pp | 40.3% | noisy |
| wmi-query | 8887.3 | -35.8% | 0.0% | 0.0% | +0.0pp | 1.6% | ok |

Highest slowdown: dll-load-unique at +597.1%

Largest kernel CPU shift: roslyn-clean-build at +1.4pp (8.9% -> 10.3%)

Largest system disk write delta: new-exe-run-motw at +189.7 MB (12.0 -> 201.7 MB)

Largest system disk read delta: roslyn-clean-build at -9162.5 MB (11779.6 -> 2617.1 MB)

Noisy scenarios: new-exe-run-motw, pipe-roundtrip

