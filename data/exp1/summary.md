# AV Benchmark Comparison Report

Generated: 2026-04-16 19:07:37 UTC

## huorong (Huorong Internet Security v5.0.0.0) vs baseline-os

| Scenario | Median Wall (ms) | Slowdown | p95 Slowdown | Disk Read Δ (MB) | Disk Write Δ (MB) | CV % | Baseline CV % | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| file-write-content | 1043355.0 | +31622.6% | +29031.8% | +152.2 | +319.8 | 0.3% | 0.5% | ok |
| ext-sensitivity-dll | 10973.0 | +264.8% | +296.8% | +0.1 | +22.9 | 1.3% | 0.7% | ok |
| dll-load-unique | 27392.0 | +244.3% | +224.5% | +130.3 | +21.1 | 0.4% | 1.8% | ok |
| ext-sensitivity-exe | 10009.0 | +232.5% | +270.4% | +0.4 | +21.7 | 0.5% | 0.4% | ok |
| hardlink-create | 2888.0 | +130.1% | +157.5% | +0.0 | -7.2 | 1.4% | 0.5% | ok |
| com-create-instance | 900.0 | +91.5% | +69.6% | -0.0 | +0.0 | 0.5% | 1.8% | ok |
| file-enum-large-dir | 4393.0 | +83.0% | +20.5% | +1.6 | -22.8 | 0.7% | 0.9% | ok |
| registry-crud | 810.0 | +78.0% | +75.6% | -0.1 | +0.0 | 0.8% | 3.8% | ok |
| fs-watcher | 3865.0 | +75.1% | +60.8% | +0.0 | +1.7 | 0.2% | 0.9% | ok |
| file-create-delete | 2404.0 | +61.3% | +59.3% | +2.4 | +10.6 | 0.5% | 1.3% | ok |
| ext-sensitivity-ps1 | 4794.0 | +60.5% | +55.3% | +0.4 | +9.5 | 0.4% | 0.2% | ok |
| wmi-query | 21657.0 | +57.1% | +48.0% | +0.2 | +0.1 | 1.1% | 0.9% | ok |
| ext-sensitivity-js | 4767.0 | +57.0% | +54.5% | +4.8 | +13.9 | 0.8% | 0.8% | ok |
| junction-create | 847.0 | +44.5% | +45.2% | -0.0 | +8.3 | 0.8% | 2.0% | ok |
| archive-extract | 18416.0 | +41.4% | +36.0% | +33.8 | +17.6 | 0.5% | 1.4% | ok |
| process-create-wait | 18346.0 | +37.9% | +34.9% | +1.2 | +100.2 | 0.4% | 0.4% | ok |
| new-exe-run | 2595.0 | +35.6% | +38.9% | +1.1 | +2.7 | 0.3% | 1.0% | ok |
| new-exe-run-motw | 2553.0 | +31.9% | +31.5% | +0.3 | +1.1 | 0.4% | 0.9% | ok |
| net-dns-resolve | 1123.0 | +17.3% | +19.2% | +0.4 | +0.0 | 1.0% | 1.4% | ok |
| net-connect-loopback | 647.0 | +12.5% | +13.5% | +0.8 | +0.0 | 1.6% | 0.6% | ok |
| ripgrep-clean-build | 16837.0 | +12.5% | — | +181.6 | -0.1 | 0.5% | 0.2% | ok |
| roslyn-clean-build | 219943.0 | +6.6% | — | -6682.0 | +237.2 | 1.9% | 2.7% | ok |
| roslyn-incremental-build | 63463.0 | +6.2% | — | +365.8 | +320.8 | 2.1% | 3.7% | ok |
| crypto-hash-verify | 264.0 | +5.2% | -2.5% | -0.4 | -0.1 | 4.1% | 1.1% | ok |
| mem-map-file | 4120.0 | +4.8% | +7.9% | -0.2 | +2.8 | 1.1% | 6.7% | ok |
| ripgrep-incremental-build | 6072.0 | +3.1% | — | -0.0 | +15.2 | 1.2% | 1.5% | ok |
| thread-create | 1164.0 | -0.1% | -6.4% | -0.1 | -0.0 | 0.5% | 1.7% | ok |
| token-query | 55.0 | -1.8% | -7.1% | +0.0 | +0.0 | 0.9% | 2.2% | ok |
| mem-alloc-protect | 164.0 | -2.4% | -13.0% | +0.0 | +0.0 | 0.5% | 1.0% | ok |
| file-copy-large | 823.0 | -15.0% | -54.3% | -0.1 | -100.0 | 2.0% | 3.1% | anomaly |
| pipe-roundtrip | 1249.0 | -17.8% | -11.6% | +0.2 | +0.0 | 13.5% | 27.9% | noisy |

Highest slowdown: file-write-content at +31622.6%

Largest kernel CPU shift: roslyn-clean-build at +2.3pp (8.9% -> 11.2%)

Largest system disk write delta: roslyn-incremental-build at +320.8 MB (1216.5 -> 1537.3 MB)

Largest system disk read delta: roslyn-clean-build at -6682.0 MB (11779.6 -> 5097.6 MB)

Noisy scenarios: pipe-roundtrip

Anomaly scenarios (AV appears faster - likely caching artifact): file-copy-large

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

## Cross-AV comparison

| Scenario | baseline (ms) | huorong | ms-defender |
|---|---|---|---|
| file-write-content | 3289.0 | +31622.6% | +42.5% |
| new-exe-run-motw | 1936.0 | +31.9% | +3422.7%* |
| dll-load-unique | 7957.0 | +244.3% | +599.4% |
| ext-sensitivity-dll | 3008.0 | +264.8% | +44.7% |
| ext-sensitivity-exe | 3010.0 | +232.5% | +47.9% |
| new-exe-run | 1914.0 | +35.6% | +182.5% |
| hardlink-create | 1255.0 | +130.1% | +38.2% |
| mem-map-file | 3931.0 | +4.8% | +123.7% |
| file-enum-large-dir | 2401.0 | +83.0% | +119.0% |
| com-create-instance | 470.0 | +91.5% | +15.7% |
| registry-crud | 455.0 | +78.0% | +29.7% |
| fs-watcher | 2207.0 | +75.1% | +57.3% |
| file-create-delete | 1490.0 | +61.3% | +50.1% |
| ext-sensitivity-ps1 | 2986.0 | +60.5% | +47.0% |
| wmi-query | 13783.0 | +57.1% | -35.6%* |
| ext-sensitivity-js | 3036.0 | +57.0% | +43.6% |
| archive-extract | 13023.0 | +41.4% | +53.4% |
| junction-create | 586.0 | +44.5% | +24.2% |
| process-create-wait | 13302.0 | +37.9% | +20.0% |
| roslyn-clean-build | 206248.0 | +6.6% | +26.4% |
| ripgrep-clean-build | 14965.0 | +12.5% | +22.1% |
| mem-alloc-protect | 168.0 | -2.4% | +20.2% |
| net-dns-resolve | 957.0 | +17.3% | +8.9% |
| roslyn-incremental-build | 59744.0 | +6.2% | +16.5% |
| net-connect-loopback | 575.0 | +12.5% | +3.8% |
| thread-create | 1165.0 | -0.1% | +7.1% |
| crypto-hash-verify | 251.0 | +5.2% | +4.4% |
| ripgrep-incremental-build | 5891.0 | +3.1% | +4.0% |
| token-query | 56.0 | -1.8% | -1.8% |
| pipe-roundtrip | 1519.0 | -17.8%* | -8.0%* |
| file-copy-large | 968.0 | -15.0%* | -8.6% |

`*` marks a non-ok result (`failed`, `insufficient`, `noisy`, or `anomaly`).

