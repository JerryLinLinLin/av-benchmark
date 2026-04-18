# AV Benchmark Comparison Report

Generated: 2026-04-18 23:30:00 UTC

## bitdefender (Bitdefender Antivirus v27.0.3.9) vs baseline-os

| Scenario | Median Wall (ms) | Slowdown | p95 Slowdown | Disk Read Delta (MB) | Disk Write Delta (MB) | CV % | Baseline CV % | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| file-create-delete | 6252.0 | +323.0% | +399.2% | +0.4 | +12.6 | 0.6% | 1.8% | ok |
| archive-extract | 73476.0 | +464.2% | +434.2% | +40.0 | +362.9 | 0.7% | 2.0% | ok |
| file-enum-large-dir | 20196.0 | +741.1% | +7.9% | +0.8 | +6.6 | 1.1% | 0.7% | ok |
| file-copy-large | 875.0 | -8.5% | -58.0% | -0.1 | -99.0 | 1.3% | 2.5% | ok |
| hardlink-create | 4412.0 | +249.9% | +229.4% | +0.1 | +3.8 | 0.8% | 1.0% | ok |
| junction-create | 1574.0 | +164.1% | +138.7% | +2.0 | +8.8 | 1.8% | 1.9% | ok |
| process-create-wait | 31310.0 | +133.8% | +126.5% | +122.5 | +243.9 | 1.5% | 0.5% | ok |
| ext-sensitivity-exe | 21279.0 | +606.9% | +552.0% | +0.1 | +14.2 | 0.8% | 0.9% | ok |
| ext-sensitivity-dll | 22299.0 | +641.3% | +559.6% | +0.1 | +8.6 | 0.6% | 1.2% | ok |
| ext-sensitivity-js | 38943.0 | +1182.7% | +880.4% | +0.2 | +2.6 | 0.4% | 1.0% | ok |
| ext-sensitivity-ps1 | 121101.0 | +3955.6% | +3771.3% | +28.6 | +10.6 | 7.3% | 0.9% | ok |
| dll-load-unique | 15723.0 | +96.5% | +73.4% | +0.4 | +4.1 | 1.1% | 1.4% | ok |
| file-write-content | 18862.0 | +473.5% | +353.2% | +0.0 | -2.3 | 0.3% | 0.4% | ok |
| new-exe-run† | 14937.5 | +678.4% | +808.7% | +3.5 | +0.5 | 0.4% | 0.9% | ok |
| new-exe-run-motw† | 12083.5 | +524.1% | +680.1% | +0.2 | +7.1 | 2.9% | 0.8% | ok |
| thread-create | 1298.0 | +10.1% | +13.6% | +0.1 | -0.0 | 0.5% | 1.3% | ok |
| mem-alloc-protect | 169.0 | +0.0% | -40.7% | +0.0 | +0.0 | 1.0% | 1.3% | ok |
| mem-map-file | 12370.0 | +214.7% | +183.4% | +0.2 | +0.8 | 1.2% | 6.9% | ok |
| net-connect-loopback | 1462.0 | +154.3% | +138.5% | +0.1 | +3.2 | 2.2% | 1.1% | ok |
| net-dns-resolve | 1235.0 | +28.8% | +31.8% | +0.0 | +0.0 | 1.5% | 1.3% | ok |
| registry-crud | 5281.0 | +1058.1% | +832.6% | -0.0 | +0.5 | 0.2% | 3.7% | ok |
| pipe-roundtrip | 176.0 | +22.2% | +14.4% | +0.1 | -0.0 | 3.2% | 5.3% | ok |
| token-query | 86.0 | +53.6% | +28.6% | +0.0 | +0.0 | 0.7% | 2.0% | ok |
| crypto-hash-verify | 269.0 | +8.0% | -4.6% | +0.0 | -0.1 | 7.7% | 0.9% | ok |
| com-create-instance | 1620.0 | +240.3% | +130.6% | +0.0 | +0.0 | 1.6% | 1.9% | ok |
| wmi-query | 11768.0 | -15.2% | -18.0% | +0.0 | -0.1 | 1.3% | 0.9% | anomaly |
| fs-watcher | 19403.0 | +780.8% | +447.5% | -0.0 | +6.0 | 0.5% | 0.8% | ok |
| ripgrep-clean-build | 17594.0 | +17.6% | - | +180.1 | +1.6 | 0.2% | 0.3% | ok |
| ripgrep-incremental-build | 6518.0 | +10.6% | - | -0.0 | +21.6 | 1.5% | 1.3% | ok |
| roslyn-clean-build | 237859.0 | +15.3% | - | -6849.8 | +132.8 | 6.8% | 2.4% | ok |
| roslyn-incremental-build | 63695.0 | +6.6% | - | +0.2 | +5.6 | 1.7% | 4.1% | ok |

† 1 outlier run excluded to reduce CV

Highest slowdown: ext-sensitivity-ps1 at +3955.6%

Largest kernel CPU shift: roslyn-incremental-build at +4.1pp (15.4% -> 19.4%)

Largest system disk write delta: archive-extract at +362.9 MB (250.8 -> 613.7 MB)

Largest system disk read delta: roslyn-clean-build at -6849.8 MB (11178.6 -> 4328.8 MB)

Anomaly scenarios (AV appears faster - likely caching artifact): wmi-query

## eset (ESET Security v19.1.12.0) vs baseline-os

| Scenario | Median Wall (ms) | Slowdown | p95 Slowdown | Disk Read Delta (MB) | Disk Write Delta (MB) | CV % | Baseline CV % | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| file-create-delete | 2905.0 | +96.5% | +112.4% | -0.1 | +12.1 | 0.7% | 1.8% | ok |
| archive-extract | 16069.0 | +23.4% | +47.4% | +95.2 | +61.1 | 0.8% | 2.0% | ok |
| file-enum-large-dir | 3435.0 | +43.1% | -2.2% | +0.1 | -10.0 | 2.2% | 0.7% | ok |
| file-copy-large | 871.0 | -8.9% | -19.3% | -0.1 | -0.0 | 0.7% | 2.5% | ok |
| hardlink-create | 1802.0 | +42.9% | +33.8% | -0.0 | -1.1 | 0.4% | 1.0% | ok |
| junction-create | 705.0 | +18.3% | +14.6% | -0.0 | +1.9 | 0.8% | 1.9% | ok |
| process-create-wait | 19637.0 | +46.6% | +44.4% | +0.5 | +6.3 | 0.7% | 0.5% | ok |
| ext-sensitivity-exe | 6226.0 | +106.8% | +114.6% | +0.0 | +15.1 | 0.8% | 0.9% | ok |
| ext-sensitivity-dll | 6124.0 | +103.6% | +122.7% | +0.0 | +4.7 | 0.8% | 1.2% | ok |
| ext-sensitivity-js | 7806.0 | +157.1% | +186.6% | -0.0 | -8.4 | 2.1% | 1.0% | ok |
| ext-sensitivity-ps1 | 7738.0 | +159.1% | +170.9% | +0.0 | -9.0 | 0.8% | 0.9% | ok |
| dll-load-unique | 10446.0 | +30.5% | +31.0% | -1.9 | +2.9 | 0.6% | 1.4% | ok |
| file-write-content | 93431.0 | +2740.7% | +2719.5% | +143.9 | +23.3 | 1.8% | 0.4% | ok |
| new-exe-run† | 5695.0 | +196.8% | +231.0% | +2.4 | +1.0 | 2.2% | 0.9% | ok |
| new-exe-run-motw | 3080.0 | +59.1% | +58.1% | +0.1 | +0.5 | 0.8% | 0.8% | ok |
| thread-create | 1252.0 | +6.2% | -7.7% | -0.1 | +0.0 | 0.7% | 1.3% | ok |
| mem-alloc-protect | 172.0 | +1.8% | -33.3% | +0.8 | +0.0 | 1.0% | 1.3% | ok |
| mem-map-file | 5692.0 | +44.8% | +23.1% | +2.0 | -62.3 | 1.0% | 6.9% | ok |
| net-connect-loopback | 1565.0 | +172.2% | +155.3% | +0.9 | +0.0 | 3.5% | 1.1% | ok |
| net-dns-resolve | 1016.0 | +5.9% | +4.3% | +0.4 | +0.1 | 2.0% | 1.3% | ok |
| registry-crud | 821.0 | +80.0% | +69.1% | -0.1 | +0.2 | 1.0% | 3.7% | ok |
| pipe-roundtrip | 163.0 | +13.2% | -34.6% | +0.1 | +0.1 | 6.9% | 5.3% | ok |
| token-query | 61.0 | +8.9% | +7.1% | +0.0 | +0.0 | 0.7% | 2.0% | ok |
| crypto-hash-verify | 247.0 | -0.8% | -3.2% | -0.4 | -0.0 | 3.4% | 0.9% | ok |
| com-create-instance | 636.0 | +33.6% | +27.2% | +0.1 | +0.0 | 1.1% | 1.9% | ok |
| wmi-query | 9182.0 | -33.8% | -33.7% | -0.1 | -0.1 | 5.0% | 0.9% | anomaly |
| fs-watcher | 4947.0 | +124.6% | +140.0% | +0.0 | +1.2 | 1.6% | 0.8% | ok |
| ripgrep-clean-build | 17563.0 | +17.4% | - | +179.9 | +1.4 | 0.3% | 0.3% | ok |
| ripgrep-incremental-build | 5941.0 | +0.8% | - | -0.0 | +1.6 | 0.5% | 1.3% | ok |
| roslyn-clean-build | 253394.0 | +22.9% | - | -398.3 | +345.9 | 4.6% | 2.4% | ok |
| roslyn-incremental-build | 61571.0 | +3.1% | - | +39.5 | +38.0 | 6.8% | 4.1% | ok |

† 1 outlier run excluded to reduce CV

Highest slowdown: file-write-content at +2740.7%

Largest kernel CPU shift: roslyn-clean-build at +1.6pp (8.9% -> 10.6%)

Largest system disk write delta: roslyn-clean-build at +345.9 MB (30593.3 -> 30939.2 MB)

Largest system disk read delta: roslyn-clean-build at -398.3 MB (11178.6 -> 10780.3 MB)

Anomaly scenarios (AV appears faster - likely caching artifact): wmi-query

## huorong (Huorong Internet Security v5.0.0.0) vs baseline-os

| Scenario | Median Wall (ms) | Slowdown | p95 Slowdown | Disk Read Delta (MB) | Disk Write Delta (MB) | CV % | Baseline CV % | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| file-create-delete | 2406.0 | +62.8% | +59.6% | +1.8 | +14.5 | 0.4% | 1.8% | ok |
| archive-extract | 18515.0 | +42.2% | +36.7% | +34.8 | +21.3 | 1.6% | 2.0% | ok |
| file-enum-large-dir† | 4366.0 | +81.8% | +13.2% | +1.7 | -21.0 | 0.9% | 0.7% | ok |
| file-copy-large | 826.0 | -13.6% | -60.4% | -0.1 | -100.0 | 1.6% | 2.5% | anomaly |
| hardlink-create | 2929.0 | +132.3% | +159.5% | -0.0 | -4.8 | 1.6% | 1.0% | ok |
| junction-create | 846.0 | +41.9% | +32.9% | -0.0 | +3.0 | 1.3% | 1.9% | ok |
| process-create-wait | 18346.0 | +37.0% | +34.9% | +1.3 | +98.9 | 0.8% | 0.5% | ok |
| ext-sensitivity-exe | 10009.0 | +232.5% | +270.4% | +0.3 | +24.0 | 1.1% | 0.9% | ok |
| ext-sensitivity-dll | 10908.0 | +262.6% | +296.8% | +0.2 | +22.1 | 1.2% | 1.2% | ok |
| ext-sensitivity-js | 4769.0 | +57.1% | +54.5% | +3.0 | +3.1 | 0.7% | 1.0% | ok |
| ext-sensitivity-ps1 | 4794.0 | +60.5% | +55.3% | +0.3 | +1.0 | 0.3% | 0.9% | ok |
| dll-load-unique | 27555.0 | +244.3% | +221.8% | +146.1 | +22.3 | 2.2% | 1.4% | ok |
| file-write-content | 1044006.0 | +31642.4% | +29076.1% | +147.6 | +318.2 | 0.2% | 0.4% | ok |
| new-exe-run | 2586.0 | +34.8% | +35.3% | +1.0 | +2.5 | 0.6% | 0.9% | ok |
| new-exe-run-motw | 2551.0 | +31.8% | +30.9% | +0.3 | +1.0 | 0.7% | 0.8% | ok |
| thread-create | 1175.0 | -0.3% | -3.6% | -0.1 | +0.0 | 1.6% | 1.3% | ok |
| mem-alloc-protect | 163.0 | -3.6% | -13.0% | +0.0 | +0.0 | 2.5% | 1.3% | ok |
| mem-map-file | 4120.0 | +4.8% | +8.9% | -0.1 | +2.3 | 1.5% | 6.9% | ok |
| net-connect-loopback | 653.0 | +13.6% | +13.5% | +0.8 | +0.0 | 1.5% | 1.1% | ok |
| net-dns-resolve | 1118.0 | +16.6% | +17.8% | +0.4 | +0.0 | 1.4% | 1.3% | ok |
| registry-crud | 810.0 | +77.6% | +75.2% | -0.1 | +0.0 | 1.0% | 3.7% | ok |
| pipe-roundtrip | 158.0 | +9.7% | +3.0% | +1.0 | +0.0 | 1.3% | 5.3% | ok |
| token-query | 55.0 | -1.8% | -7.1% | +0.0 | +0.0 | 0.7% | 2.0% | ok |
| crypto-hash-verify | 264.0 | +6.0% | -1.8% | -0.4 | -0.1 | 3.8% | 0.9% | ok |
| com-create-instance | 900.0 | +89.1% | +65.7% | -0.0 | +0.0 | 0.4% | 1.9% | ok |
| wmi-query | 21657.0 | +56.0% | +48.8% | +0.1 | +0.1 | 2.3% | 0.9% | ok |
| fs-watcher | 3865.0 | +75.4% | +63.5% | +0.0 | +2.1 | 0.3% | 0.8% | ok |
| ripgrep-clean-build | 16799.0 | +12.3% | - | +181.1 | +0.5 | 0.6% | 0.3% | ok |
| ripgrep-incremental-build | 6072.0 | +3.1% | - | -0.0 | +15.8 | 1.0% | 1.3% | ok |
| roslyn-clean-build | 219665.0 | +6.5% | - | -6156.6 | +230.8 | 1.5% | 2.4% | ok |
| roslyn-incremental-build | 64415.0 | +7.8% | - | +405.5 | +346.9 | 2.0% | 4.1% | ok |

† 1 outlier run excluded to reduce CV

Highest slowdown: file-write-content at +31642.4%

Largest kernel CPU shift: roslyn-clean-build at +2.3pp (8.9% -> 11.2%)

Largest system disk write delta: roslyn-incremental-build at +346.9 MB (1202.8 -> 1549.7 MB)

Largest system disk read delta: roslyn-clean-build at -6156.6 MB (11178.6 -> 5022.0 MB)

Anomaly scenarios (AV appears faster - likely caching artifact): file-copy-large

## ms-defender (Windows Defender v4.18.2201.11) vs baseline-os

| Scenario | Median Wall (ms) | Slowdown | p95 Slowdown | Disk Read Delta (MB) | Disk Write Delta (MB) | CV % | Baseline CV % | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| file-create-delete | 2236.0 | +51.3% | +54.8% | +0.8 | +17.8 | 1.2% | 1.8% | ok |
| archive-extract | 20087.0 | +54.2% | +100.1% | +52.5 | +12.2 | 1.0% | 2.0% | ok |
| file-enum-large-dir | 5258.0 | +119.0% | +112.9% | +16.9 | +1.7 | 0.7% | 0.7% | ok |
| file-copy-large | 885.0 | -7.4% | -11.8% | -0.1 | +0.8 | 1.2% | 2.5% | ok |
| hardlink-create | 1739.0 | +37.9% | +34.5% | -0.0 | +0.1 | 7.3% | 1.0% | ok |
| junction-create | 724.0 | +21.5% | +15.7% | -0.0 | +3.2 | 0.7% | 1.9% | ok |
| process-create-wait | 15969.0 | +19.3% | +18.6% | +0.5 | -0.9 | 0.7% | 0.5% | ok |
| ext-sensitivity-exe | 4453.0 | +47.9% | +45.7% | +0.0 | +12.6 | 0.7% | 0.9% | ok |
| ext-sensitivity-dll | 4397.0 | +46.2% | +43.7% | +0.0 | +12.6 | 0.9% | 1.2% | ok |
| ext-sensitivity-js | 4360.0 | +43.6% | +41.0% | +0.0 | -1.5 | 0.6% | 1.0% | ok |
| ext-sensitivity-ps1 | 4389.0 | +47.0% | +43.2% | +0.0 | +8.9 | 0.5% | 0.9% | ok |
| dll-load-unique | 56054.0 | +600.4% | +540.2% | +1.0 | +13.7 | 0.5% | 1.4% | ok |
| file-write-content | 4699.0 | +42.9% | +38.8% | +0.5 | +9.0 | 0.6% | 0.4% | ok |
| new-exe-run | 5182.0 | +170.0% | +174.3% | +48.7 | +8.8 | 2.8% | 0.9% | ok |
| new-exe-run-motw† | 60074.0 | +3003.0% | +4052.9% | +92.9 | +140.1 | 8.9% | 0.8% | ok |
| thread-create | 1257.0 | +6.6% | +10.2% | +0.0 | +0.0 | 1.5% | 1.3% | ok |
| mem-alloc-protect | 197.0 | +16.6% | +7.4% | +0.0 | +0.1 | 2.3% | 1.3% | ok |
| mem-map-file | 8795.0 | +123.7% | +124.3% | -0.2 | +2.1 | 1.2% | 6.9% | ok |
| net-connect-loopback | 597.0 | +3.8% | +1.3% | +0.4 | +0.0 | 0.8% | 1.1% | ok |
| net-dns-resolve | 1056.0 | +10.1% | +10.5% | +0.0 | +0.0 | 1.2% | 1.3% | ok |
| registry-crud | 590.0 | +29.4% | +24.7% | -0.0 | +0.0 | 0.9% | 3.7% | ok |
| pipe-roundtrip† | 134.5 | -6.6% | -39.5% | +0.3 | -0.0 | 8.6% | 5.3% | ok |
| token-query | 55.0 | -1.8% | -7.1% | +0.0 | +0.0 | 1.4% | 2.0% | ok |
| crypto-hash-verify | 262.0 | +5.2% | +1.1% | +0.1 | -0.1 | 2.1% | 0.9% | ok |
| com-create-instance | 552.0 | +16.0% | +9.9% | +0.0 | +0.0 | 1.1% | 1.9% | ok |
| wmi-query | 9066.0 | -34.7% | -37.0% | -0.1 | -0.2 | 4.2% | 0.9% | anomaly |
| fs-watcher | 3472.0 | +57.6% | +47.7% | -0.0 | +13.0 | 1.7% | 0.8% | ok |
| ripgrep-clean-build | 18306.0 | +22.3% | - | +111.4 | +5.4 | 0.6% | 0.3% | ok |
| ripgrep-incremental-build | 6129.0 | +4.0% | - | +0.1 | +2.1 | 0.7% | 1.3% | ok |
| roslyn-clean-build | 258176.0 | +25.2% | - | -8494.6 | +90.7 | 3.2% | 2.4% | ok |
| roslyn-incremental-build | 69593.0 | +16.5% | - | +23.0 | +23.8 | 2.1% | 4.1% | ok |

† 1 outlier run excluded to reduce CV

Highest slowdown: new-exe-run-motw at +3003.0%

Largest kernel CPU shift: roslyn-clean-build at +1.5pp (8.9% -> 10.4%)

Largest system disk write delta: new-exe-run-motw at +140.1 MB (12.1 -> 152.1 MB)

Largest system disk read delta: roslyn-clean-build at -8494.6 MB (11178.6 -> 2684.0 MB)

Anomaly scenarios (AV appears faster - likely caching artifact): wmi-query

## Cross-AV comparison

| Scenario | baseline (ms) | bitdefender | eset | huorong | ms-defender |
|---|---|---|---|---|---|
| file-create-delete | 1478.0 | +323.0% | +96.5% | +62.8% | +51.3% |
| archive-extract | 13023.0 | +464.2% | +23.4% | +42.2% | +54.2% |
| file-enum-large-dir | 2401.0 | +741.1% | +43.1% | +81.8% | +119.0% |
| file-copy-large | 956.0 | -8.5% | -8.9% | -13.6%* | -7.4% |
| hardlink-create | 1261.0 | +249.9% | +42.9% | +132.3% | +37.9% |
| junction-create | 596.0 | +164.1% | +18.3% | +41.9% | +21.5% |
| process-create-wait | 13391.0 | +133.8% | +46.6% | +37.0% | +19.3% |
| ext-sensitivity-exe | 3010.0 | +606.9% | +106.8% | +232.5% | +47.9% |
| ext-sensitivity-dll | 3008.0 | +641.3% | +103.6% | +262.6% | +46.2% |
| ext-sensitivity-js | 3036.0 | +1182.7% | +157.1% | +57.1% | +43.6% |
| ext-sensitivity-ps1 | 2986.0 | +3955.6% | +159.1% | +60.5% | +47.0% |
| dll-load-unique | 8003.0 | +96.5% | +30.5% | +244.3% | +600.4% |
| file-write-content | 3289.0 | +473.5% | +2740.7% | +31642.4% | +42.9% |
| new-exe-run | 1919.0 | +678.4% | +196.8% | +34.8% | +170.0% |
| new-exe-run-motw | 1936.0 | +524.1% | +59.1% | +31.8% | +3003.0% |
| thread-create | 1179.0 | +10.1% | +6.2% | -0.3% | +6.6% |
| mem-alloc-protect | 169.0 | +0.0% | +1.8% | -3.6% | +16.6% |
| mem-map-file | 3931.0 | +214.7% | +44.8% | +4.8% | +123.7% |
| net-connect-loopback | 575.0 | +154.3% | +172.2% | +13.6% | +3.8% |
| net-dns-resolve | 959.0 | +28.8% | +5.9% | +16.6% | +10.1% |
| registry-crud | 456.0 | +1058.1% | +80.0% | +77.6% | +29.4% |
| pipe-roundtrip | 144.0 | +22.2% | +13.2% | +9.7% | -6.6% |
| token-query | 56.0 | +53.6% | +8.9% | -1.8% | -1.8% |
| crypto-hash-verify | 249.0 | +8.0% | -0.8% | +6.0% | +5.2% |
| com-create-instance | 476.0 | +240.3% | +33.6% | +89.1% | +16.0% |
| wmi-query | 13879.0 | -15.2%* | -33.8%* | +56.0% | -34.7%* |
| fs-watcher | 2203.0 | +780.8% | +124.6% | +75.4% | +57.6% |
| ripgrep-clean-build | 14965.0 | +17.6% | +17.4% | +12.3% | +22.3% |
| ripgrep-incremental-build | 5891.0 | +10.6% | +0.8% | +3.1% | +4.0% |
| roslyn-clean-build | 206248.0 | +15.3% | +22.9% | +6.5% | +25.2% |
| roslyn-incremental-build | 59744.0 | +6.6% | +3.1% | +7.8% | +16.5% |

`*` marks a non-ok result (`failed`, `insufficient`, `noisy`, or `anomaly`).

