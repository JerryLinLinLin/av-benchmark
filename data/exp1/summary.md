# AV Benchmark Comparison Report

Generated: 2026-04-16 21:49:00 UTC

## huorong (Huorong Internet Security v5.0.0.0) vs baseline-os

| Scenario | Median Wall (ms) | Slowdown | p95 Slowdown | Disk Read Δ (MB) | Disk Write Δ (MB) | CV % | Baseline CV % | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| file-write-content | 1044006.0 | +31642.4% | +29447.4% | +147.6 | +320.8 | 0.2% | 0.4% | ok |
| ext-sensitivity-dll | 10908.0 | +263.0% | +297.6% | +0.2 | +24.3 | 1.2% | 1.0% | ok |
| dll-load-unique | 27555.0 | +244.3% | +221.0% | +146.1 | +22.5 | 2.2% | 2.1% | ok |
| ext-sensitivity-exe | 10009.0 | +232.7% | +273.4% | +0.3 | +23.1 | 1.1% | 1.1% | ok |
| hardlink-create | 2929.0 | +133.2% | +159.5% | -0.0 | -2.1 | 1.6% | 0.7% | ok |
| com-create-instance | 900.0 | +88.7% | +69.3% | -0.0 | -0.0 | 0.4% | 1.9% | ok |
| file-enum-large-dir | 4393.0 | +82.9% | +16.0% | +1.4 | -17.6 | 56.3% | 1.0% | noisy |
| registry-crud | 810.0 | +77.6% | +75.3% | -0.1 | +0.0 | 1.0% | 3.3% | ok |
| fs-watcher | 3865.0 | +75.6% | +63.5% | +0.0 | +2.1 | 0.3% | 1.4% | ok |
| file-create-delete | 2406.0 | +61.5% | +59.6% | +1.8 | +10.9 | 0.4% | 1.1% | ok |
| ext-sensitivity-ps1 | 4794.0 | +61.3% | +56.0% | +0.3 | +0.1 | 0.3% | 0.8% | ok |
| ext-sensitivity-js | 4769.0 | +58.5% | +54.8% | +3.0 | +5.0 | 0.7% | 1.3% | ok |
| wmi-query | 21657.0 | +57.1% | +49.5% | -0.1 | +0.1 | 2.3% | 1.3% | ok |
| archive-extract | 18515.0 | +42.2% | +36.7% | +34.7 | +20.4 | 1.6% | 1.7% | ok |
| junction-create | 846.0 | +41.9% | +35.5% | -0.0 | +5.0 | 1.3% | 1.6% | ok |
| process-create-wait | 18346.0 | +37.0% | +34.9% | +1.3 | +94.1 | 0.8% | 1.2% | ok |
| new-exe-run | 2586.0 | +35.0% | +35.3% | +1.0 | +2.5 | 0.6% | 0.8% | ok |
| new-exe-run-motw | 2551.0 | +32.1% | +30.9% | +0.3 | +1.1 | 0.7% | 0.7% | ok |
| net-dns-resolve | 1118.0 | +16.8% | +17.8% | +0.4 | +0.0 | 1.4% | 1.2% | ok |
| net-connect-loopback | 653.0 | +13.6% | +13.6% | +0.8 | +0.0 | 1.5% | 0.7% | ok |
| ripgrep-clean-build | 16799.0 | +12.3% | — | +181.0 | -0.5 | 0.6% | 0.3% | ok |
| roslyn-incremental-build | 64415.0 | +7.8% | — | +408.2 | +342.9 | 2.0% | 3.8% | ok |
| crypto-hash-verify | 264.0 | +6.0% | -2.5% | -0.4 | -0.1 | 3.8% | 1.1% | ok |
| mem-map-file | 4120.0 | +4.8% | +8.9% | -0.1 | +2.3 | 1.5% | 6.9% | ok |
| ripgrep-incremental-build | 6072.0 | +3.1% | — | -0.0 | +15.0 | 1.0% | 1.3% | ok |
| roslyn-clean-build | 219665.0 | +2.4% | — | -7074.8 | +252.3 | 1.5% | 2.5% | ok |
| token-query | 55.0 | +0.0% | +0.0% | +0.0 | +0.0 | 0.7% | 2.4% | ok |
| pipe-roundtrip | 1249.0 | -0.3% | +15.7% | +0.2 | +0.0 | 23.5% | 39.5% | noisy |
| thread-create | 1175.0 | -0.4% | -3.6% | -0.1 | +0.0 | 1.6% | 1.7% | ok |
| mem-alloc-protect | 163.0 | -3.0% | -13.0% | +0.0 | +0.0 | 2.5% | 1.0% | ok |
| file-copy-large | 826.0 | -14.7% | -62.5% | -0.0 | -100.0 | 1.6% | 2.5% | anomaly |

Highest slowdown: file-write-content at +31642.4%

Largest kernel CPU shift: roslyn-clean-build at +2.3pp (8.9% -> 11.2%)

Largest system disk write delta: roslyn-incremental-build at +342.9 MB (1206.7 -> 1549.7 MB)

Largest system disk read delta: roslyn-clean-build at -7074.8 MB (12096.8 -> 5022.0 MB)

Noisy scenarios: file-enum-large-dir, pipe-roundtrip

Anomaly scenarios (AV appears faster - likely caching artifact): file-copy-large

## ms-defender (Windows Defender v4.18.2201.11) vs baseline-os

| Scenario | Median Wall (ms) | Slowdown | p95 Slowdown | Disk Read Δ (MB) | Disk Write Δ (MB) | CV % | Baseline CV % | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| new-exe-run-motw | 63715.0 | +3199.6% | +4743.0% | +100.0 | +114.2 | 18.5% | 0.7% | noisy |
| dll-load-unique | 56054.0 | +600.4% | +538.5% | +1.0 | +13.9 | 0.5% | 2.1% | ok |
| new-exe-run | 5182.0 | +170.5% | +174.3% | +48.7 | +8.8 | 2.8% | 0.8% | ok |
| mem-map-file | 8795.0 | +123.7% | +124.3% | -0.2 | +2.1 | 1.2% | 6.9% | ok |
| file-enum-large-dir | 5258.0 | +118.9% | +112.9% | +16.9 | +2.0 | 0.7% | 1.0% | ok |
| fs-watcher | 3472.0 | +57.7% | +47.7% | -0.0 | +13.0 | 1.7% | 1.4% | ok |
| archive-extract | 20087.0 | +54.2% | +100.1% | +52.5 | +11.4 | 1.0% | 1.7% | ok |
| file-create-delete | 2236.0 | +50.1% | +54.8% | +0.8 | +14.1 | 1.2% | 1.1% | ok |
| ext-sensitivity-exe | 4453.0 | +48.0% | +46.9% | +0.0 | +11.8 | 0.7% | 1.1% | ok |
| ext-sensitivity-ps1 | 4389.0 | +47.6% | +43.9% | +0.0 | +8.0 | 0.5% | 0.8% | ok |
| ext-sensitivity-dll | 4397.0 | +46.3% | +44.0% | +0.0 | +14.8 | 0.9% | 1.0% | ok |
| ext-sensitivity-js | 4360.0 | +44.9% | +41.3% | +0.0 | +0.4 | 0.6% | 1.3% | ok |
| file-write-content | 4699.0 | +42.9% | +40.6% | +0.5 | +11.6 | 0.6% | 0.4% | ok |
| hardlink-create | 1739.0 | +38.5% | +34.5% | -0.0 | +2.8 | 7.3% | 0.7% | ok |
| registry-crud | 590.0 | +29.4% | +24.9% | -0.0 | +0.0 | 0.9% | 3.3% | ok |
| ripgrep-clean-build | 18306.0 | +22.3% | — | +111.3 | +4.4 | 0.6% | 0.3% | ok |
| junction-create | 724.0 | +21.5% | +18.0% | -0.0 | +5.2 | 0.7% | 1.6% | ok |
| roslyn-clean-build | 258176.0 | +20.4% | — | -9412.8 | +112.2 | 3.2% | 2.5% | ok |
| process-create-wait | 15969.0 | +19.3% | +18.6% | +0.5 | -5.7 | 0.7% | 1.2% | ok |
| mem-alloc-protect | 197.0 | +17.3% | +7.4% | +0.0 | +0.1 | 2.3% | 1.0% | ok |
| roslyn-incremental-build | 69593.0 | +16.5% | — | +25.6 | +19.9 | 2.1% | 3.8% | ok |
| com-create-instance | 552.0 | +15.7% | +12.3% | +0.0 | +0.0 | 1.1% | 1.9% | ok |
| net-dns-resolve | 1056.0 | +10.3% | +10.5% | +0.0 | +0.0 | 1.2% | 1.2% | ok |
| thread-create | 1257.0 | +6.5% | +10.2% | +0.0 | +0.0 | 1.5% | 1.7% | ok |
| crypto-hash-verify | 262.0 | +5.2% | +0.5% | +0.0 | -0.1 | 2.1% | 1.1% | ok |
| ripgrep-incremental-build | 6129.0 | +4.0% | — | +0.1 | +1.3 | 0.7% | 1.3% | ok |
| net-connect-loopback | 597.0 | +3.8% | +1.4% | +0.4 | +0.0 | 0.8% | 0.7% | ok |
| token-query | 55.0 | +0.0% | +0.0% | +0.0 | +0.0 | 1.4% | 2.4% | ok |
| file-copy-large | 885.0 | -8.6% | -16.6% | -0.0 | +0.8 | 1.2% | 2.5% | ok |
| pipe-roundtrip | 868.0 | -30.7% | -25.1% | +0.1 | +0.0 | 37.7% | 39.5% | noisy |
| wmi-query | 9066.0 | -34.2% | -36.6% | -0.2 | -0.2 | 4.2% | 1.3% | anomaly |

Highest slowdown: dll-load-unique at +600.4%

Largest kernel CPU shift: roslyn-clean-build at +1.5pp (8.9% -> 10.4%)

Largest system disk write delta: new-exe-run-motw at +114.2 MB (12.0 -> 126.3 MB)

Largest system disk read delta: roslyn-clean-build at -9412.8 MB (12096.8 -> 2684.0 MB)

Noisy scenarios: new-exe-run-motw, pipe-roundtrip

Anomaly scenarios (AV appears faster - likely caching artifact): wmi-query

## Cross-AV comparison

| Scenario | baseline (ms) | huorong | ms-defender |
|---|---|---|---|
| file-write-content | 3289.0 | +31642.4% | +42.9% |
| new-exe-run-motw | 1931.0 | +32.1% | +3199.6%* |
| dll-load-unique | 8003.0 | +244.3% | +600.4% |
| ext-sensitivity-dll | 3005.0 | +263.0% | +46.3% |
| ext-sensitivity-exe | 3008.0 | +232.7% | +48.0% |
| new-exe-run | 1916.0 | +35.0% | +170.5% |
| hardlink-create | 1256.0 | +133.2% | +38.5% |
| mem-map-file | 3931.0 | +4.8% | +123.7% |
| file-enum-large-dir | 2402.0 | +82.9%* | +118.9% |
| com-create-instance | 477.0 | +88.7% | +15.7% |
| registry-crud | 456.0 | +77.6% | +29.4% |
| fs-watcher | 2201.0 | +75.6% | +57.7% |
| file-create-delete | 1490.0 | +61.5% | +50.1% |
| ext-sensitivity-ps1 | 2973.0 | +61.3% | +47.6% |
| ext-sensitivity-js | 3008.0 | +58.5% | +44.9% |
| wmi-query | 13783.0 | +57.1% | -34.2%* |
| archive-extract | 13023.0 | +42.2% | +54.2% |
| junction-create | 596.0 | +41.9% | +21.5% |
| process-create-wait | 13391.0 | +37.0% | +19.3% |
| ripgrep-clean-build | 14965.0 | +12.3% | +22.3% |
| roslyn-clean-build | 214489.0 | +2.4% | +20.4% |
| mem-alloc-protect | 168.0 | -3.0% | +17.3% |
| net-dns-resolve | 957.0 | +16.8% | +10.3% |
| roslyn-incremental-build | 59744.0 | +7.8% | +16.5% |
| net-connect-loopback | 575.0 | +13.6% | +3.8% |
| thread-create | 1180.0 | -0.4% | +6.5% |
| crypto-hash-verify | 249.0 | +6.0% | +5.2% |
| ripgrep-incremental-build | 5891.0 | +3.1% | +4.0% |
| token-query | 55.0 | +0.0% | +0.0% |
| pipe-roundtrip | 1253.0 | -0.3%* | -30.7%* |
| file-copy-large | 968.0 | -14.7%* | -8.6% |

`*` marks a non-ok result (`failed`, `insufficient`, `noisy`, or `anomaly`).

