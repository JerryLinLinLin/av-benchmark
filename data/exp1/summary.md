# AV Benchmark Comparison Report

Generated: 2026-04-16 22:34:43 UTC

## huorong (Huorong Internet Security v5.0.0.0) vs baseline-os

| Scenario | Median Wall (ms) | Slowdown | p95 Slowdown | Disk Read Delta (MB) | Disk Write Delta (MB) | CV % | Baseline CV % | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| file-write-content | 1044006.0 | +31642.4% | +29076.1% | +147.6 | +318.2 | 0.2% | 0.4% | ok |
| ext-sensitivity-dll | 10908.0 | +262.6% | +296.8% | +0.2 | +22.1 | 1.2% | 1.2% | ok |
| dll-load-unique | 27555.0 | +244.3% | +221.8% | +146.1 | +22.3 | 2.2% | 1.4% | ok |
| ext-sensitivity-exe | 10009.0 | +232.5% | +270.4% | +0.3 | +24.0 | 1.1% | 0.9% | ok |
| hardlink-create | 2929.0 | +132.3% | +159.5% | -0.0 | -4.8 | 1.6% | 1.0% | ok |
| com-create-instance | 900.0 | +89.1% | +65.7% | -0.0 | +0.0 | 0.4% | 1.9% | ok |
| file-enum-large-dir† | 4366.0 | +81.8% | +13.2% | +1.7 | -21.0 | 0.9% | 0.7% | ok |
| registry-crud | 810.0 | +77.6% | +75.2% | -0.1 | +0.0 | 1.0% | 3.7% | ok |
| fs-watcher | 3865.0 | +75.4% | +63.5% | +0.0 | +2.1 | 0.3% | 0.8% | ok |
| file-create-delete | 2406.0 | +62.8% | +59.6% | +1.8 | +14.5 | 0.4% | 1.8% | ok |
| ext-sensitivity-ps1 | 4794.0 | +60.5% | +55.3% | +0.3 | +1.0 | 0.3% | 0.9% | ok |
| ext-sensitivity-js | 4769.0 | +57.1% | +54.5% | +3.0 | +3.1 | 0.7% | 1.0% | ok |
| wmi-query | 21657.0 | +56.0% | +48.8% | +0.1 | +0.1 | 2.3% | 0.9% | ok |
| archive-extract | 18515.0 | +42.2% | +36.7% | +34.8 | +21.3 | 1.6% | 2.0% | ok |
| junction-create | 846.0 | +41.9% | +32.9% | -0.0 | +3.0 | 1.3% | 1.9% | ok |
| process-create-wait | 18346.0 | +37.0% | +34.9% | +1.3 | +98.9 | 0.8% | 0.5% | ok |
| new-exe-run | 2586.0 | +34.8% | +35.3% | +1.0 | +2.5 | 0.6% | 0.9% | ok |
| new-exe-run-motw | 2551.0 | +31.8% | +30.9% | +0.3 | +1.0 | 0.7% | 0.8% | ok |
| net-dns-resolve | 1118.0 | +16.6% | +17.8% | +0.4 | +0.0 | 1.4% | 1.3% | ok |
| net-connect-loopback | 653.0 | +13.6% | +13.5% | +0.8 | +0.0 | 1.5% | 1.1% | ok |
| ripgrep-clean-build | 16799.0 | +12.3% | - | +181.1 | +0.5 | 0.6% | 0.3% | ok |
| roslyn-incremental-build | 64415.0 | +7.8% | - | +405.5 | +346.9 | 2.0% | 4.1% | ok |
| roslyn-clean-build | 219665.0 | +6.5% | - | -6156.6 | +230.8 | 1.5% | 2.4% | ok |
| crypto-hash-verify | 264.0 | +6.0% | -1.8% | -0.4 | -0.1 | 3.8% | 0.9% | ok |
| mem-map-file | 4120.0 | +4.8% | +8.9% | -0.1 | +2.3 | 1.5% | 6.9% | ok |
| ripgrep-incremental-build | 6072.0 | +3.1% | - | -0.0 | +15.8 | 1.0% | 1.3% | ok |
| thread-create | 1175.0 | -0.3% | -3.6% | -0.1 | +0.0 | 1.6% | 1.3% | ok |
| token-query | 55.0 | -1.8% | -7.1% | +0.0 | +0.0 | 0.7% | 2.0% | ok |
| mem-alloc-protect | 163.0 | -3.6% | -13.0% | +0.0 | +0.0 | 2.5% | 1.3% | ok |
| file-copy-large | 826.0 | -13.6% | -60.4% | -0.1 | -100.0 | 1.6% | 2.5% | anomaly |
| pipe-roundtrip | 1249.0 | -17.8% | -11.6% | +0.2 | +0.0 | 23.5% | 24.2% | noisy |

† 1 outlier run excluded to reduce CV

Highest slowdown: file-write-content at +31642.4%

Largest kernel CPU shift: roslyn-clean-build at +2.3pp (8.9% -> 11.2%)

Largest system disk write delta: roslyn-incremental-build at +346.9 MB (1202.8 -> 1549.7 MB)

Largest system disk read delta: roslyn-clean-build at -6156.6 MB (11178.6 -> 5022.0 MB)

Noisy scenarios: pipe-roundtrip

Anomaly scenarios (AV appears faster - likely caching artifact): file-copy-large

## ms-defender (Windows Defender v4.18.2201.11) vs baseline-os

| Scenario | Median Wall (ms) | Slowdown | p95 Slowdown | Disk Read Delta (MB) | Disk Write Delta (MB) | CV % | Baseline CV % | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| new-exe-run-motw† | 60074.0 | +3003.0% | +4052.9% | +92.9 | +140.1 | 8.9% | 0.8% | ok |
| dll-load-unique | 56054.0 | +600.4% | +540.2% | +1.0 | +13.7 | 0.5% | 1.4% | ok |
| new-exe-run | 5182.0 | +170.0% | +174.3% | +48.7 | +8.8 | 2.8% | 0.9% | ok |
| mem-map-file | 8795.0 | +123.7% | +124.3% | -0.2 | +2.1 | 1.2% | 6.9% | ok |
| file-enum-large-dir | 5258.0 | +119.0% | +112.9% | +16.9 | +1.7 | 0.7% | 0.7% | ok |
| fs-watcher | 3472.0 | +57.6% | +47.7% | -0.0 | +13.0 | 1.7% | 0.8% | ok |
| archive-extract | 20087.0 | +54.2% | +100.1% | +52.5 | +12.2 | 1.0% | 2.0% | ok |
| file-create-delete | 2236.0 | +51.3% | +54.8% | +0.8 | +17.8 | 1.2% | 1.8% | ok |
| ext-sensitivity-exe | 4453.0 | +47.9% | +45.7% | +0.0 | +12.6 | 0.7% | 0.9% | ok |
| ext-sensitivity-ps1 | 4389.0 | +47.0% | +43.2% | +0.0 | +8.9 | 0.5% | 0.9% | ok |
| ext-sensitivity-dll | 4397.0 | +46.2% | +43.7% | +0.0 | +12.6 | 0.9% | 1.2% | ok |
| ext-sensitivity-js | 4360.0 | +43.6% | +41.0% | +0.0 | -1.5 | 0.6% | 1.0% | ok |
| file-write-content | 4699.0 | +42.9% | +38.8% | +0.5 | +9.0 | 0.6% | 0.4% | ok |
| hardlink-create | 1739.0 | +37.9% | +34.5% | -0.0 | +0.1 | 7.3% | 1.0% | ok |
| registry-crud | 590.0 | +29.4% | +24.7% | -0.0 | +0.0 | 0.9% | 3.7% | ok |
| roslyn-clean-build | 258176.0 | +25.2% | - | -8494.6 | +90.7 | 3.2% | 2.4% | ok |
| ripgrep-clean-build | 18306.0 | +22.3% | - | +111.4 | +5.4 | 0.6% | 0.3% | ok |
| junction-create | 724.0 | +21.5% | +15.7% | -0.0 | +3.2 | 0.7% | 1.9% | ok |
| process-create-wait | 15969.0 | +19.3% | +18.6% | +0.5 | -0.9 | 0.7% | 0.5% | ok |
| mem-alloc-protect | 197.0 | +16.6% | +7.4% | +0.0 | +0.1 | 2.3% | 1.3% | ok |
| roslyn-incremental-build | 69593.0 | +16.5% | - | +23.0 | +23.8 | 2.1% | 4.1% | ok |
| com-create-instance | 552.0 | +16.0% | +9.9% | +0.0 | +0.0 | 1.1% | 1.9% | ok |
| net-dns-resolve | 1056.0 | +10.1% | +10.5% | +0.0 | +0.0 | 1.2% | 1.3% | ok |
| thread-create | 1257.0 | +6.6% | +10.2% | +0.0 | +0.0 | 1.5% | 1.3% | ok |
| crypto-hash-verify | 262.0 | +5.2% | +1.1% | +0.1 | -0.1 | 2.1% | 0.9% | ok |
| ripgrep-incremental-build | 6129.0 | +4.0% | - | +0.1 | +2.1 | 0.7% | 1.3% | ok |
| net-connect-loopback | 597.0 | +3.8% | +1.3% | +0.4 | +0.0 | 0.8% | 1.1% | ok |
| token-query | 55.0 | -1.8% | -7.1% | +0.0 | +0.0 | 1.4% | 2.0% | ok |
| file-copy-large | 885.0 | -7.4% | -11.8% | -0.1 | +0.8 | 1.2% | 2.5% | ok |
| wmi-query | 9066.0 | -34.7% | -37.0% | -0.1 | -0.2 | 4.2% | 0.9% | anomaly |
| pipe-roundtrip | 868.0 | -42.9% | -42.8% | +0.1 | +0.0 | 37.7% | 24.2% | noisy |

† 1 outlier run excluded to reduce CV

Highest slowdown: new-exe-run-motw at +3003.0%

Largest kernel CPU shift: roslyn-clean-build at +1.5pp (8.9% -> 10.4%)

Largest system disk write delta: new-exe-run-motw at +140.1 MB (12.1 -> 152.1 MB)

Largest system disk read delta: roslyn-clean-build at -8494.6 MB (11178.6 -> 2684.0 MB)

Noisy scenarios: pipe-roundtrip

Anomaly scenarios (AV appears faster - likely caching artifact): wmi-query

## Cross-AV comparison

| Scenario | baseline (ms) | huorong | ms-defender |
|---|---|---|---|
| file-write-content | 3289.0 | +31642.4% | +42.9% |
| new-exe-run-motw | 1936.0 | +31.8% | +3003.0% |
| dll-load-unique | 8003.0 | +244.3% | +600.4% |
| ext-sensitivity-dll | 3008.0 | +262.6% | +46.2% |
| ext-sensitivity-exe | 3010.0 | +232.5% | +47.9% |
| new-exe-run | 1919.0 | +34.8% | +170.0% |
| hardlink-create | 1261.0 | +132.3% | +37.9% |
| mem-map-file | 3931.0 | +4.8% | +123.7% |
| file-enum-large-dir | 2401.0 | +81.8% | +119.0% |
| com-create-instance | 476.0 | +89.1% | +16.0% |
| registry-crud | 456.0 | +77.6% | +29.4% |
| fs-watcher | 2203.0 | +75.4% | +57.6% |
| file-create-delete | 1478.0 | +62.8% | +51.3% |
| ext-sensitivity-ps1 | 2986.0 | +60.5% | +47.0% |
| ext-sensitivity-js | 3036.0 | +57.1% | +43.6% |
| wmi-query | 13879.0 | +56.0% | -34.7%* |
| archive-extract | 13023.0 | +42.2% | +54.2% |
| junction-create | 596.0 | +41.9% | +21.5% |
| process-create-wait | 13391.0 | +37.0% | +19.3% |
| roslyn-clean-build | 206248.0 | +6.5% | +25.2% |
| ripgrep-clean-build | 14965.0 | +12.3% | +22.3% |
| mem-alloc-protect | 169.0 | -3.6% | +16.6% |
| net-dns-resolve | 959.0 | +16.6% | +10.1% |
| roslyn-incremental-build | 59744.0 | +7.8% | +16.5% |
| net-connect-loopback | 575.0 | +13.6% | +3.8% |
| thread-create | 1179.0 | -0.3% | +6.6% |
| crypto-hash-verify | 249.0 | +6.0% | +5.2% |
| ripgrep-incremental-build | 5891.0 | +3.1% | +4.0% |
| token-query | 56.0 | -1.8% | -1.8% |
| file-copy-large | 956.0 | -13.6%* | -7.4% |
| pipe-roundtrip | 1519.0 | -17.8%* | -42.9%* |

`*` marks a non-ok result (`failed`, `insufficient`, `noisy`, or `anomaly`).

