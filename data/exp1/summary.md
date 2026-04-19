# AV Benchmark Comparison Report

Generated: 2026-04-19 04:43:58 UTC

## avira (Avira Security v1.0.2604.8504) vs baseline-os

| Scenario | Median Wall (ms) | First-Run Wall (ms) | Slowdown | First-Run Slowdown | p95 Slowdown | Disk Read Delta (MB) | Disk Write Delta (MB) | CV % | Baseline CV % | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| file-create-delete | 2552.0 | 2536.0 | +72.7% | +70.2% | +84.8% | -0.1 | +12.7 | 1.8% | 1.8% | ok |
| archive-extract | 16114.0 | 15996.0 | +23.7% | +22.8% | +32.2% | +34.4 | +8.4 | 1.0% | 2.0% | ok |
| file-enum-large-dir | 3872.0 | 3844.0 | +61.3% | +62.6% | -3.1% | -0.1 | -4.1 | 0.8% | 0.7% | ok |
| file-copy-large | 838.0 | 867.0 | -12.3% | -13.6% | -50.1% | -0.1 | -100.1 | 1.7% | 2.5% | anomaly |
| hardlink-create | 1818.0 | 1813.0 | +44.2% | +45.5% | +36.5% | -0.0 | -7.3 | 0.6% | 1.0% | ok |
| junction-create | 927.0 | 931.0 | +55.5% | +60.2% | +47.3% | +4.7 | +1.9 | 0.5% | 1.9% | ok |
| process-create-wait | 18414.0 | 18382.0 | +37.5% | +38.3% | +36.2% | +47.3 | +99.2 | 0.7% | 0.5% | ok |
| ext-sensitivity-exe | 4997.0 | 4997.0 | +66.0% | +66.1% | +83.2% | +0.0 | -5.0 | 0.7% | 0.9% | ok |
| ext-sensitivity-dll | 5020.0 | 5127.0 | +66.9% | +70.4% | +88.1% | +0.0 | -6.4 | 1.0% | 1.2% | ok |
| ext-sensitivity-js | 5011.0 | 5011.0 | +65.1% | +66.6% | +82.4% | -0.0 | -5.8 | 1.0% | 1.0% | ok |
| ext-sensitivity-ps1 | 5060.0 | 5060.0 | +69.5% | +69.5% | +88.1% | +0.0 | +6.8 | 0.8% | 0.9% | ok |
| dll-load-unique | 44339.0 | 44123.0 | +454.0% | +461.4% | +403.3% | +14.2 | +5.8 | 0.7% | 1.4% | ok |
| file-write-content | 5213.0 | 5224.0 | +58.5% | +58.8% | +69.8% | -0.0 | -13.8 | 0.4% | 0.4% | ok |
| new-exe-run | 16527.0 | 301226.0 | +761.2% | +15638.0% | +909.5% | +40.4 | +4.7 | 107.3% | 0.9% | noisy |
| new-exe-run-motw | 4405.0 | 10366.0 | +127.5% | +431.6% | +125.4% | -0.0 | +0.4 | 42.2% | 0.8% | noisy |
| thread-create | 1437.0 | 1427.0 | +21.9% | +18.7% | +30.8% | -0.1 | +0.1 | 1.5% | 1.3% | ok |
| mem-alloc-protect | 161.0 | 161.0 | -4.7% | -4.7% | -31.5% | +0.0 | +0.0 | 1.0% | 1.3% | ok |
| mem-map-file | 11102.0 | 11153.0 | +182.4% | +183.7% | +156.2% | -0.2 | +1.9 | 0.3% | 6.9% | ok |
| net-connect-loopback | 899.0 | 887.0 | +56.3% | +53.7% | +60.8% | +0.8 | +0.1 | 0.9% | 1.1% | ok |
| net-dns-resolve | 6442.0 | 6460.0 | +571.7% | +567.4% | +799.6% | +0.4 | +3.4 | 0.3% | 1.3% | ok |
| registry-crud | 5597.0 | 5597.0 | +1127.4% | +1119.4% | +904.7% | +0.2 | +0.2 | 0.6% | 3.7% | ok |
| pipe-roundtrip | 147.0 | 140.0 | +2.1% | +6.1% | -40.9% | +0.1 | -0.0 | 2.5% | 5.3% | ok |
| token-query | 56.0 | 54.0 | +0.0% | -6.9% | -7.1% | +0.0 | +0.0 | 1.4% | 2.0% | ok |
| crypto-hash-verify | 246.0 | 240.0 | -1.2% | -4.4% | -3.9% | -0.4 | -0.1 | 6.1% | 0.9% | ok |
| com-create-instance | 725.0 | 726.0 | +52.3% | +48.8% | +38.1% | +0.1 | +0.0 | 3.3% | 1.9% | ok |
| wmi-query† | 11499.0 | 11408.0 | -17.1% | -17.2% | -20.4% | -0.1 | +0.1 | 2.2% | 0.9% | anomaly |
| fs-watcher | 4106.0 | 4106.0 | +86.4% | +86.6% | +88.7% | -0.1 | +2.3 | 0.7% | 0.8% | ok |
| ripgrep-clean-build | 128448.0 | 128448.0 | +758.3% | +760.1% | - | +122.4 | +43.1 | 13.9% | 0.3% | noisy |
| ripgrep-incremental-build | 6097.0 | 6045.0 | +3.5% | -0.5% | - | -0.0 | -4.6 | 0.5% | 1.3% | ok |
| roslyn-clean-build† | 229388.5 | 479777.0 | +11.2% | +121.6% | - | -4146.8 | -12.4 | 3.0% | 2.4% | ok |
| roslyn-incremental-build† | 62391.5 | 162989.0 | +4.4% | +155.0% | - | +50.1 | +23.0 | 1.9% | 4.1% | ok |

† 1 outlier run excluded to reduce CV

Highest slowdown: registry-crud at +1127.4%

Largest kernel CPU shift: roslyn-clean-build at +1.5pp (8.9% -> 10.4%)

Largest system disk write delta: file-copy-large at -100.1 MB (100.1 -> 0.0 MB)

Largest system disk read delta: roslyn-clean-build at -4146.8 MB (11178.6 -> 7031.8 MB)

Noisy scenarios: new-exe-run, new-exe-run-motw, ripgrep-clean-build

Anomaly scenarios (AV appears faster - likely caching artifact): file-copy-large, wmi-query

## bitdefender (Bitdefender Antivirus v27.0.3.9) vs baseline-os

| Scenario | Median Wall (ms) | First-Run Wall (ms) | Slowdown | First-Run Slowdown | p95 Slowdown | Disk Read Delta (MB) | Disk Write Delta (MB) | CV % | Baseline CV % | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| file-create-delete | 6252.0 | 6329.0 | +323.0% | +324.8% | +399.2% | +0.4 | +12.6 | 0.6% | 1.8% | ok |
| archive-extract | 73476.0 | 74391.0 | +464.2% | +471.2% | +434.2% | +40.0 | +362.9 | 0.7% | 2.0% | ok |
| file-enum-large-dir | 20196.0 | 20473.0 | +741.1% | +766.0% | +7.9% | +0.8 | +6.6 | 1.1% | 0.7% | ok |
| file-copy-large | 875.0 | 876.0 | -8.5% | -12.7% | -58.0% | -0.1 | -99.0 | 1.3% | 2.5% | ok |
| hardlink-create | 4412.0 | 4412.0 | +249.9% | +254.1% | +229.4% | +0.1 | +3.8 | 0.8% | 1.0% | ok |
| junction-create | 1574.0 | 1589.0 | +164.1% | +173.5% | +138.7% | +2.0 | +8.8 | 1.8% | 1.9% | ok |
| process-create-wait | 31310.0 | 31152.0 | +133.8% | +134.4% | +126.5% | +122.5 | +243.9 | 1.5% | 0.5% | ok |
| ext-sensitivity-exe | 21279.0 | 20953.0 | +606.9% | +596.6% | +552.0% | +0.1 | +14.2 | 0.8% | 0.9% | ok |
| ext-sensitivity-dll | 22299.0 | 22227.0 | +641.3% | +638.9% | +559.6% | +0.1 | +8.6 | 0.6% | 1.2% | ok |
| ext-sensitivity-js | 38943.0 | 38986.0 | +1182.7% | +1196.1% | +880.4% | +0.2 | +2.6 | 0.4% | 1.0% | ok |
| ext-sensitivity-ps1 | 121101.0 | 120530.0 | +3955.6% | +3936.5% | +3771.3% | +28.6 | +10.6 | 7.3% | 0.9% | ok |
| dll-load-unique | 15723.0 | 15844.0 | +96.5% | +101.6% | +73.4% | +0.4 | +4.1 | 1.1% | 1.4% | ok |
| file-write-content | 18862.0 | 18932.0 | +473.5% | +475.6% | +353.2% | +0.0 | -2.3 | 0.3% | 0.4% | ok |
| new-exe-run† | 14937.5 | 10713.0 | +678.4% | +459.7% | +808.7% | +3.5 | +0.5 | 0.4% | 0.9% | ok |
| new-exe-run-motw† | 12083.5 | 8042.0 | +524.1% | +312.4% | +680.1% | +0.2 | +7.1 | 2.9% | 0.8% | ok |
| thread-create | 1298.0 | 1290.0 | +10.1% | +7.3% | +13.6% | +0.1 | -0.0 | 0.5% | 1.3% | ok |
| mem-alloc-protect | 169.0 | 166.0 | +0.0% | -1.8% | -40.7% | +0.0 | +0.0 | 1.0% | 1.3% | ok |
| mem-map-file | 12370.0 | 12370.0 | +214.7% | +214.7% | +183.4% | +0.2 | +0.8 | 1.2% | 6.9% | ok |
| net-connect-loopback | 1462.0 | 1537.0 | +154.3% | +166.4% | +138.5% | +0.1 | +3.2 | 2.2% | 1.1% | ok |
| net-dns-resolve | 1235.0 | 1252.0 | +28.8% | +29.3% | +31.8% | +0.0 | +0.0 | 1.5% | 1.3% | ok |
| registry-crud | 5281.0 | 5293.0 | +1058.1% | +1053.2% | +832.6% | -0.0 | +0.5 | 0.2% | 3.7% | ok |
| pipe-roundtrip | 176.0 | 177.0 | +22.2% | +34.1% | +14.4% | +0.1 | -0.0 | 3.2% | 5.3% | ok |
| token-query | 86.0 | 86.0 | +53.6% | +48.3% | +28.6% | +0.0 | +0.0 | 0.7% | 2.0% | ok |
| crypto-hash-verify | 269.0 | 269.0 | +8.0% | +7.2% | -4.6% | +0.0 | -0.1 | 7.7% | 0.9% | ok |
| com-create-instance | 1620.0 | 1620.0 | +240.3% | +232.0% | +130.6% | +0.0 | +0.0 | 1.6% | 1.9% | ok |
| wmi-query | 11768.0 | 12133.0 | -15.2% | -12.0% | -18.0% | +0.0 | -0.1 | 1.3% | 0.9% | anomaly |
| fs-watcher | 19403.0 | 19446.0 | +780.8% | +783.5% | +447.5% | -0.0 | +6.0 | 0.5% | 0.8% | ok |
| ripgrep-clean-build | 17594.0 | 17639.0 | +17.6% | +18.1% | - | +180.1 | +1.6 | 0.2% | 0.3% | ok |
| ripgrep-incremental-build | 6518.0 | 6537.0 | +10.6% | +7.6% | - | -0.0 | +21.6 | 1.5% | 1.3% | ok |
| roslyn-clean-build | 237859.0 | 233499.0 | +15.3% | +7.8% | - | -6849.8 | +132.8 | 6.8% | 2.4% | ok |
| roslyn-incremental-build | 63695.0 | 65869.0 | +6.6% | +3.1% | - | +0.2 | +5.6 | 1.7% | 4.1% | ok |

† 1 outlier run excluded to reduce CV

Highest slowdown: ext-sensitivity-ps1 at +3955.6%

Largest kernel CPU shift: roslyn-incremental-build at +4.1pp (15.4% -> 19.4%)

Largest system disk write delta: archive-extract at +362.9 MB (250.8 -> 613.7 MB)

Largest system disk read delta: roslyn-clean-build at -6849.8 MB (11178.6 -> 4328.8 MB)

Anomaly scenarios (AV appears faster - likely caching artifact): wmi-query

## eset (ESET Security v19.1.12.0) vs baseline-os

| Scenario | Median Wall (ms) | First-Run Wall (ms) | Slowdown | First-Run Slowdown | p95 Slowdown | Disk Read Delta (MB) | Disk Write Delta (MB) | CV % | Baseline CV % | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| file-create-delete | 2905.0 | 2878.0 | +96.5% | +93.2% | +112.4% | -0.1 | +12.1 | 0.7% | 1.8% | ok |
| archive-extract | 16069.0 | 16084.0 | +23.4% | +23.5% | +47.4% | +95.2 | +61.1 | 0.8% | 2.0% | ok |
| file-enum-large-dir | 3435.0 | 3332.0 | +43.1% | +40.9% | -2.2% | +0.1 | -10.0 | 2.2% | 0.7% | ok |
| file-copy-large | 871.0 | 882.0 | -8.9% | -12.2% | -19.3% | -0.1 | -0.0 | 0.7% | 2.5% | ok |
| hardlink-create | 1802.0 | 1795.0 | +42.9% | +44.1% | +33.8% | -0.0 | -1.1 | 0.4% | 1.0% | ok |
| junction-create | 705.0 | 702.0 | +18.3% | +20.8% | +14.6% | -0.0 | +1.9 | 0.8% | 1.9% | ok |
| process-create-wait | 19637.0 | 19468.0 | +46.6% | +46.5% | +44.4% | +0.5 | +6.3 | 0.7% | 0.5% | ok |
| ext-sensitivity-exe | 6226.0 | 6155.0 | +106.8% | +104.6% | +114.6% | +0.0 | +15.1 | 0.8% | 0.9% | ok |
| ext-sensitivity-dll | 6124.0 | 6026.0 | +103.6% | +100.3% | +122.7% | +0.0 | +4.7 | 0.8% | 1.2% | ok |
| ext-sensitivity-js | 7806.0 | 7545.0 | +157.1% | +150.8% | +186.6% | -0.0 | -8.4 | 2.1% | 1.0% | ok |
| ext-sensitivity-ps1 | 7738.0 | 7577.0 | +159.1% | +153.8% | +170.9% | +0.0 | -9.0 | 0.8% | 0.9% | ok |
| dll-load-unique | 10446.0 | 10344.0 | +30.5% | +31.6% | +31.0% | -1.9 | +2.9 | 0.6% | 1.4% | ok |
| file-write-content | 93431.0 | 93431.0 | +2740.7% | +2740.7% | +2719.5% | +143.9 | +23.3 | 1.8% | 0.4% | ok |
| new-exe-run† | 5695.0 | 5630.0 | +196.8% | +194.1% | +231.0% | +2.4 | +1.0 | 2.2% | 0.9% | ok |
| new-exe-run-motw | 3080.0 | 3024.0 | +59.1% | +55.1% | +58.1% | +0.1 | +0.5 | 0.8% | 0.8% | ok |
| thread-create | 1252.0 | 1262.0 | +6.2% | +5.0% | -7.7% | -0.1 | +0.0 | 0.7% | 1.3% | ok |
| mem-alloc-protect | 172.0 | 175.0 | +1.8% | +3.6% | -33.3% | +0.8 | +0.0 | 1.0% | 1.3% | ok |
| mem-map-file | 5692.0 | 5722.0 | +44.8% | +45.6% | +23.1% | +2.0 | -62.3 | 1.0% | 6.9% | ok |
| net-connect-loopback | 1565.0 | 1499.0 | +172.2% | +159.8% | +155.3% | +0.9 | +0.0 | 3.5% | 1.1% | ok |
| net-dns-resolve | 1016.0 | 1027.0 | +5.9% | +6.1% | +4.3% | +0.4 | +0.1 | 2.0% | 1.3% | ok |
| registry-crud | 821.0 | 821.0 | +80.0% | +78.9% | +69.1% | -0.1 | +0.2 | 1.0% | 3.7% | ok |
| pipe-roundtrip | 163.0 | 163.0 | +13.2% | +23.5% | -34.6% | +0.1 | +0.1 | 6.9% | 5.3% | ok |
| token-query | 61.0 | 61.0 | +8.9% | +5.2% | +7.1% | +0.0 | +0.0 | 0.7% | 2.0% | ok |
| crypto-hash-verify | 247.0 | 247.0 | -0.8% | -1.6% | -3.2% | -0.4 | -0.0 | 3.4% | 0.9% | ok |
| com-create-instance | 636.0 | 640.0 | +33.6% | +31.1% | +27.2% | +0.1 | +0.0 | 1.1% | 1.9% | ok |
| wmi-query | 9182.0 | 8807.0 | -33.8% | -36.1% | -33.7% | -0.1 | -0.1 | 5.0% | 0.9% | anomaly |
| fs-watcher | 4947.0 | 4937.0 | +124.6% | +124.3% | +140.0% | +0.0 | +1.2 | 1.6% | 0.8% | ok |
| ripgrep-clean-build | 17563.0 | 17563.0 | +17.4% | +17.6% | - | +179.9 | +1.4 | 0.3% | 0.3% | ok |
| ripgrep-incremental-build | 5941.0 | 5941.0 | +0.8% | -2.3% | - | -0.0 | +1.6 | 0.5% | 1.3% | ok |
| roslyn-clean-build | 253394.0 | 263064.0 | +22.9% | +21.5% | - | -398.3 | +345.9 | 4.6% | 2.4% | ok |
| roslyn-incremental-build | 61571.0 | 60978.0 | +3.1% | -4.6% | - | +39.5 | +38.0 | 6.8% | 4.1% | ok |

† 1 outlier run excluded to reduce CV

Highest slowdown: file-write-content at +2740.7%

Largest kernel CPU shift: roslyn-clean-build at +1.6pp (8.9% -> 10.6%)

Largest system disk write delta: roslyn-clean-build at +345.9 MB (30593.3 -> 30939.2 MB)

Largest system disk read delta: roslyn-clean-build at -398.3 MB (11178.6 -> 10780.3 MB)

Anomaly scenarios (AV appears faster - likely caching artifact): wmi-query

## huorong (Huorong Internet Security v5.0.0.0) vs baseline-os

| Scenario | Median Wall (ms) | First-Run Wall (ms) | Slowdown | First-Run Slowdown | p95 Slowdown | Disk Read Delta (MB) | Disk Write Delta (MB) | CV % | Baseline CV % | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| file-create-delete | 2406.0 | 2406.0 | +62.8% | +61.5% | +59.6% | +1.8 | +14.5 | 0.4% | 1.8% | ok |
| archive-extract | 18515.0 | 18416.0 | +42.2% | +41.4% | +36.7% | +34.8 | +21.3 | 1.6% | 2.0% | ok |
| file-enum-large-dir† | 4366.0 | 4393.0 | +81.8% | +85.8% | +13.2% | +1.7 | -21.0 | 0.9% | 0.7% | ok |
| file-copy-large | 826.0 | 823.0 | -13.6% | -18.0% | -60.4% | -0.1 | -100.0 | 1.6% | 2.5% | anomaly |
| hardlink-create | 2929.0 | 2929.0 | +132.3% | +135.1% | +159.5% | -0.0 | -4.8 | 1.6% | 1.0% | ok |
| junction-create | 846.0 | 856.0 | +41.9% | +47.3% | +32.9% | -0.0 | +3.0 | 1.3% | 1.9% | ok |
| process-create-wait | 18346.0 | 18346.0 | +37.0% | +38.0% | +34.9% | +1.3 | +98.9 | 0.8% | 0.5% | ok |
| ext-sensitivity-exe | 10009.0 | 10009.0 | +232.5% | +232.7% | +270.4% | +0.3 | +24.0 | 1.1% | 0.9% | ok |
| ext-sensitivity-dll | 10908.0 | 10973.0 | +262.6% | +264.8% | +296.8% | +0.2 | +22.1 | 1.2% | 1.2% | ok |
| ext-sensitivity-js | 4769.0 | 4809.0 | +57.1% | +59.9% | +54.5% | +3.0 | +3.1 | 0.7% | 1.0% | ok |
| ext-sensitivity-ps1 | 4794.0 | 4827.0 | +60.5% | +61.7% | +55.3% | +0.3 | +1.0 | 0.3% | 0.9% | ok |
| dll-load-unique | 27555.0 | 27286.0 | +244.3% | +247.2% | +221.8% | +146.1 | +22.3 | 2.2% | 1.4% | ok |
| file-write-content | 1044006.0 | 1047793.0 | +31642.4% | +31757.5% | +29076.1% | +147.6 | +318.2 | 0.2% | 0.4% | ok |
| new-exe-run | 2586.0 | 2586.0 | +34.8% | +35.1% | +35.3% | +1.0 | +2.5 | 0.6% | 0.9% | ok |
| new-exe-run-motw | 2551.0 | 2572.0 | +31.8% | +31.9% | +30.9% | +0.3 | +1.0 | 0.7% | 0.8% | ok |
| thread-create | 1175.0 | 1175.0 | -0.3% | -2.2% | -3.6% | -0.1 | +0.0 | 1.6% | 1.3% | ok |
| mem-alloc-protect | 163.0 | 163.0 | -3.6% | -3.6% | -13.0% | +0.0 | +0.0 | 2.5% | 1.3% | ok |
| mem-map-file | 4120.0 | 4066.0 | +4.8% | +3.4% | +8.9% | -0.1 | +2.3 | 1.5% | 6.9% | ok |
| net-connect-loopback | 653.0 | 655.0 | +13.6% | +13.5% | +13.5% | +0.8 | +0.0 | 1.5% | 1.1% | ok |
| net-dns-resolve | 1118.0 | 1133.0 | +16.6% | +17.0% | +17.8% | +0.4 | +0.0 | 1.4% | 1.3% | ok |
| registry-crud | 810.0 | 811.0 | +77.6% | +76.7% | +75.2% | -0.1 | +0.0 | 1.0% | 3.7% | ok |
| pipe-roundtrip | 158.0 | 156.0 | +9.7% | +18.2% | +3.0% | +1.0 | +0.0 | 1.3% | 5.3% | ok |
| token-query | 55.0 | 56.0 | -1.8% | -3.4% | -7.1% | +0.0 | +0.0 | 0.7% | 2.0% | ok |
| crypto-hash-verify | 264.0 | 264.0 | +6.0% | +5.2% | -1.8% | -0.4 | -0.1 | 3.8% | 0.9% | ok |
| com-create-instance | 900.0 | 900.0 | +89.1% | +84.4% | +65.7% | -0.0 | +0.0 | 0.4% | 1.9% | ok |
| wmi-query | 21657.0 | 21357.0 | +56.0% | +55.0% | +48.8% | +0.1 | +0.1 | 2.3% | 0.9% | ok |
| fs-watcher | 3865.0 | 3865.0 | +75.4% | +75.6% | +63.5% | +0.0 | +2.1 | 0.3% | 0.8% | ok |
| ripgrep-clean-build | 16799.0 | 16897.0 | +12.3% | +13.1% | - | +181.1 | +0.5 | 0.6% | 0.3% | ok |
| ripgrep-incremental-build | 6072.0 | 6027.0 | +3.1% | -0.8% | - | -0.0 | +15.8 | 1.0% | 1.3% | ok |
| roslyn-clean-build | 219665.0 | 211433.0 | +6.5% | -2.4% | - | -6156.6 | +230.8 | 1.5% | 2.4% | ok |
| roslyn-incremental-build | 64415.0 | 64900.0 | +7.8% | +1.5% | - | +405.5 | +346.9 | 2.0% | 4.1% | ok |

† 1 outlier run excluded to reduce CV

Highest slowdown: file-write-content at +31642.4%

Largest kernel CPU shift: roslyn-clean-build at +2.3pp (8.9% -> 11.2%)

Largest system disk write delta: roslyn-incremental-build at +346.9 MB (1202.8 -> 1549.7 MB)

Largest system disk read delta: roslyn-clean-build at -6156.6 MB (11178.6 -> 5022.0 MB)

Anomaly scenarios (AV appears faster - likely caching artifact): file-copy-large

## ms-defender (Windows Defender v4.18.2201.11) vs baseline-os

| Scenario | Median Wall (ms) | First-Run Wall (ms) | Slowdown | First-Run Slowdown | p95 Slowdown | Disk Read Delta (MB) | Disk Write Delta (MB) | CV % | Baseline CV % | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| file-create-delete | 2236.0 | 2215.0 | +51.3% | +48.7% | +54.8% | +0.8 | +17.8 | 1.2% | 1.8% | ok |
| archive-extract | 20087.0 | 20392.0 | +54.2% | +56.6% | +100.1% | +52.5 | +12.2 | 1.0% | 2.0% | ok |
| file-enum-large-dir | 5258.0 | 5200.0 | +119.0% | +120.0% | +112.9% | +16.9 | +1.7 | 0.7% | 0.7% | ok |
| file-copy-large | 885.0 | 890.0 | -7.4% | -11.4% | -11.8% | -0.1 | +0.8 | 1.2% | 2.5% | ok |
| hardlink-create | 1739.0 | 1727.0 | +37.9% | +38.6% | +34.5% | -0.0 | +0.1 | 7.3% | 1.0% | ok |
| junction-create | 724.0 | 717.0 | +21.5% | +23.4% | +15.7% | -0.0 | +3.2 | 0.7% | 1.9% | ok |
| process-create-wait | 15969.0 | 15969.0 | +19.3% | +20.2% | +18.6% | +0.5 | -0.9 | 0.7% | 0.5% | ok |
| ext-sensitivity-exe | 4453.0 | 4453.0 | +47.9% | +48.0% | +45.7% | +0.0 | +12.6 | 0.7% | 0.9% | ok |
| ext-sensitivity-dll | 4397.0 | 4353.0 | +46.2% | +44.7% | +43.7% | +0.0 | +12.6 | 0.9% | 1.2% | ok |
| ext-sensitivity-js | 4360.0 | 4360.0 | +43.6% | +44.9% | +41.0% | +0.0 | -1.5 | 0.6% | 1.0% | ok |
| ext-sensitivity-ps1 | 4389.0 | 4389.0 | +47.0% | +47.0% | +43.2% | +0.0 | +8.9 | 0.5% | 0.9% | ok |
| dll-load-unique | 56054.0 | 56260.0 | +600.4% | +615.9% | +540.2% | +1.0 | +13.7 | 0.5% | 1.4% | ok |
| file-write-content | 4699.0 | 4655.0 | +42.9% | +41.5% | +38.8% | +0.5 | +9.0 | 0.6% | 0.4% | ok |
| new-exe-run | 5182.0 | 5423.0 | +170.0% | +183.3% | +174.3% | +48.7 | +8.8 | 2.8% | 0.9% | ok |
| new-exe-run-motw† | 60074.0 | 89210.0 | +3003.0% | +4474.9% | +4052.9% | +92.9 | +140.1 | 8.9% | 0.8% | ok |
| thread-create | 1257.0 | 1213.0 | +6.6% | +0.9% | +10.2% | +0.0 | +0.0 | 1.5% | 1.3% | ok |
| mem-alloc-protect | 197.0 | 202.0 | +16.6% | +19.5% | +7.4% | +0.0 | +0.1 | 2.3% | 1.3% | ok |
| mem-map-file | 8795.0 | 8795.0 | +123.7% | +123.7% | +124.3% | -0.2 | +2.1 | 1.2% | 6.9% | ok |
| net-connect-loopback | 597.0 | 589.0 | +3.8% | +2.1% | +1.3% | +0.4 | +0.0 | 0.8% | 1.1% | ok |
| net-dns-resolve | 1056.0 | 1034.0 | +10.1% | +6.8% | +10.5% | +0.0 | +0.0 | 1.2% | 1.3% | ok |
| registry-crud | 590.0 | 589.0 | +29.4% | +28.3% | +24.7% | -0.0 | +0.0 | 0.9% | 3.7% | ok |
| pipe-roundtrip† | 134.5 | 128.0 | -6.6% | -3.0% | -39.5% | +0.3 | -0.0 | 8.6% | 5.3% | ok |
| token-query | 55.0 | 56.0 | -1.8% | -3.4% | -7.1% | +0.0 | +0.0 | 1.4% | 2.0% | ok |
| crypto-hash-verify | 262.0 | 264.0 | +5.2% | +5.2% | +1.1% | +0.1 | -0.1 | 2.1% | 0.9% | ok |
| com-create-instance | 552.0 | 552.0 | +16.0% | +13.1% | +9.9% | +0.0 | +0.0 | 1.1% | 1.9% | ok |
| wmi-query | 9066.0 | 8881.0 | -34.7% | -35.6% | -37.0% | -0.1 | -0.2 | 4.2% | 0.9% | anomaly |
| fs-watcher | 3472.0 | 3467.0 | +57.6% | +57.5% | +47.7% | -0.0 | +13.0 | 1.7% | 0.8% | ok |
| ripgrep-clean-build | 18306.0 | 18306.0 | +22.3% | +22.6% | - | +111.4 | +5.4 | 0.6% | 0.3% | ok |
| ripgrep-incremental-build | 6129.0 | 6105.0 | +4.0% | +0.4% | - | +0.1 | +2.1 | 0.7% | 1.3% | ok |
| roslyn-clean-build | 258176.0 | 260770.0 | +25.2% | +20.4% | - | -8494.6 | +90.7 | 3.2% | 2.4% | ok |
| roslyn-incremental-build | 69593.0 | 69593.0 | +16.5% | +8.9% | - | +23.0 | +23.8 | 2.1% | 4.1% | ok |

† 1 outlier run excluded to reduce CV

Highest slowdown: new-exe-run-motw at +3003.0%

Largest kernel CPU shift: roslyn-clean-build at +1.5pp (8.9% -> 10.4%)

Largest system disk write delta: new-exe-run-motw at +140.1 MB (12.1 -> 152.1 MB)

Largest system disk read delta: roslyn-clean-build at -8494.6 MB (11178.6 -> 2684.0 MB)

Anomaly scenarios (AV appears faster - likely caching artifact): wmi-query

## trendmicro (Trend Micro Maximum Security v7.7) vs baseline-os

| Scenario | Median Wall (ms) | First-Run Wall (ms) | Slowdown | First-Run Slowdown | p95 Slowdown | Disk Read Delta (MB) | Disk Write Delta (MB) | CV % | Baseline CV % | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| file-create-delete | 12615.0 | 12662.0 | +753.5% | +749.8% | +844.0% | +0.5 | +16.8 | 0.6% | 1.8% | ok |
| archive-extract | 69021.0 | 69021.0 | +430.0% | +430.0% | +406.6% | +34.5 | +269.7 | 0.5% | 2.0% | ok |
| file-enum-large-dir | 18123.0 | 17552.0 | +654.8% | +642.5% | -3.3% | +36.9 | +64.8 | 1.8% | 0.7% | ok |
| file-copy-large | 890.0 | 862.0 | -6.9% | -14.1% | -19.6% | -0.1 | +0.3 | 1.7% | 2.5% | ok |
| hardlink-create | 7690.0 | 7766.0 | +509.8% | +523.3% | +497.6% | +33.6 | +6.0 | 2.2% | 1.0% | ok |
| junction-create | 2031.0 | 2027.0 | +240.8% | +248.9% | +242.9% | +0.0 | +6.1 | 0.4% | 1.9% | ok |
| process-create-wait | 24913.0 | 24981.0 | +86.0% | +88.0% | +79.8% | +1.1 | -0.2 | 0.3% | 0.5% | ok |
| ext-sensitivity-exe | 26248.0 | 26248.0 | +772.0% | +772.6% | +831.4% | +0.1 | +163.0 | 0.4% | 0.9% | ok |
| ext-sensitivity-dll | 40129.0 | 38123.0 | +1234.1% | +1167.4% | +1308.0% | +0.0 | +74.8 | 5.0% | 1.2% | ok |
| ext-sensitivity-js | 61879.0 | 60901.0 | +1938.2% | +1924.6% | +2593.6% | +0.1 | +294.3 | 1.0% | 1.0% | ok |
| ext-sensitivity-ps1 | 40541.0 | 39883.0 | +1257.7% | +1235.7% | +1313.2% | +33.5 | +127.0 | 0.7% | 0.9% | ok |
| dll-load-unique | 45621.0 | 45621.0 | +470.0% | +480.5% | +423.4% | +1.5 | +10.1 | 0.3% | 1.4% | ok |
| file-write-content | 89146.0 | 89653.0 | +2610.4% | +2625.8% | +3809.8% | +0.9 | +829.2 | 1.0% | 0.4% | ok |
| new-exe-run† | 25001.0 | 33467.0 | +1202.8% | +1648.5% | +1740.6% | +2.5 | +3.4 | 4.1% | 0.9% | ok |
| new-exe-run-motw | 0.0 | - | +0.0% | +0.0% | - | +0.0 | +0.0 | 0.0% | 0.0% | failed |
| thread-create | 1878.0 | 1878.0 | +59.3% | +56.2% | +67.8% | -0.1 | +0.1 | 0.8% | 1.3% | ok |
| mem-alloc-protect | 392.0 | 392.0 | +132.0% | +132.0% | +44.4% | +0.0 | +0.0 | 0.7% | 1.3% | ok |
| mem-map-file | 13750.0 | 13817.0 | +249.8% | +251.5% | +219.2% | +1.6 | +2.9 | 0.5% | 6.9% | ok |
| net-connect-loopback | 680.0 | 693.0 | +18.3% | +20.1% | +16.1% | +0.9 | +0.1 | 0.8% | 1.1% | ok |
| net-dns-resolve | 1080.0 | 1089.0 | +12.6% | +12.5% | +11.4% | +0.4 | +0.1 | 1.6% | 1.3% | ok |
| registry-crud | 10185.0 | 10216.0 | +2133.6% | +2125.7% | +2019.8% | -0.0 | +1.0 | 1.0% | 3.7% | ok |
| pipe-roundtrip | 153.0 | 146.0 | +6.2% | +10.6% | -3.6% | +0.1 | -0.0 | 7.3% | 5.3% | ok |
| token-query | 56.0 | 55.0 | +0.0% | -5.2% | -7.1% | +0.0 | +0.0 | 1.1% | 2.0% | ok |
| crypto-hash-verify | 258.0 | 260.0 | +3.6% | +3.6% | -3.7% | -0.4 | -0.0 | 2.1% | 0.9% | ok |
| com-create-instance | 1877.0 | 1854.0 | +294.3% | +279.9% | +166.0% | +0.1 | +0.1 | 1.6% | 1.9% | ok |
| wmi-query | 13132.0 | 12804.0 | -5.4% | -7.1% | -7.1% | -0.1 | +0.8 | 1.5% | 0.9% | ok |
| fs-watcher | 20278.0 | 20608.0 | +820.5% | +836.3% | +865.9% | -0.0 | +11.8 | 1.0% | 0.8% | ok |
| ripgrep-clean-build | 18249.0 | 19651.0 | +21.9% | +31.6% | - | +100.6 | +3.5 | 3.3% | 0.3% | ok |
| ripgrep-incremental-build | 6002.0 | 6044.0 | +1.9% | -0.6% | - | -0.0 | +1.2 | 2.8% | 1.3% | ok |
| roslyn-clean-build | 247502.0 | 254204.0 | +20.0% | +17.4% | - | -5291.1 | +298.4 | 1.5% | 2.4% | ok |
| roslyn-incremental-build | 69286.0 | 69286.0 | +16.0% | +8.4% | - | +214.4 | +65.4 | 1.0% | 4.1% | ok |

† 1 outlier run excluded to reduce CV

Highest slowdown: file-write-content at +2610.4%

Largest kernel CPU shift: roslyn-clean-build at +2.4pp (8.9% -> 11.3%)

Largest system disk write delta: file-write-content at +829.2 MB (58.5 -> 887.7 MB)

Largest system disk read delta: roslyn-clean-build at -5291.1 MB (11178.6 -> 5887.5 MB)

Failed scenarios: new-exe-run-motw

## Cross-AV steady-state comparison

Cells are slowdown vs baseline using median wall time after compare-time outlier handling.

| Scenario | baseline median (ms) | avira | bitdefender | eset | huorong | ms-defender | trendmicro |
|---|---|---|---|---|---|---|---|
| file-create-delete | 1478.0 | +72.7% | +323.0% | +96.5% | +62.8% | +51.3% | +753.5% |
| archive-extract | 13023.0 | +23.7% | +464.2% | +23.4% | +42.2% | +54.2% | +430.0% |
| file-enum-large-dir | 2401.0 | +61.3% | +741.1% | +43.1% | +81.8% | +119.0% | +654.8% |
| file-copy-large | 956.0 | -12.3%* | -8.5% | -8.9% | -13.6%* | -7.4% | -6.9% |
| hardlink-create | 1261.0 | +44.2% | +249.9% | +42.9% | +132.3% | +37.9% | +509.8% |
| junction-create | 596.0 | +55.5% | +164.1% | +18.3% | +41.9% | +21.5% | +240.8% |
| process-create-wait | 13391.0 | +37.5% | +133.8% | +46.6% | +37.0% | +19.3% | +86.0% |
| ext-sensitivity-exe | 3010.0 | +66.0% | +606.9% | +106.8% | +232.5% | +47.9% | +772.0% |
| ext-sensitivity-dll | 3008.0 | +66.9% | +641.3% | +103.6% | +262.6% | +46.2% | +1234.1% |
| ext-sensitivity-js | 3036.0 | +65.1% | +1182.7% | +157.1% | +57.1% | +43.6% | +1938.2% |
| ext-sensitivity-ps1 | 2986.0 | +69.5% | +3955.6% | +159.1% | +60.5% | +47.0% | +1257.7% |
| dll-load-unique | 8003.0 | +454.0% | +96.5% | +30.5% | +244.3% | +600.4% | +470.0% |
| file-write-content | 3289.0 | +58.5% | +473.5% | +2740.7% | +31642.4% | +42.9% | +2610.4% |
| new-exe-run | 1919.0 | +761.2%* | +678.4% | +196.8% | +34.8% | +170.0% | +1202.8% |
| new-exe-run-motw | 1936.0 | +127.5%* | +524.1% | +59.1% | +31.8% | +3003.0% | failed* |
| thread-create | 1179.0 | +21.9% | +10.1% | +6.2% | -0.3% | +6.6% | +59.3% |
| mem-alloc-protect | 169.0 | -4.7% | +0.0% | +1.8% | -3.6% | +16.6% | +132.0% |
| mem-map-file | 3931.0 | +182.4% | +214.7% | +44.8% | +4.8% | +123.7% | +249.8% |
| net-connect-loopback | 575.0 | +56.3% | +154.3% | +172.2% | +13.6% | +3.8% | +18.3% |
| net-dns-resolve | 959.0 | +571.7% | +28.8% | +5.9% | +16.6% | +10.1% | +12.6% |
| registry-crud | 456.0 | +1127.4% | +1058.1% | +80.0% | +77.6% | +29.4% | +2133.6% |
| pipe-roundtrip | 144.0 | +2.1% | +22.2% | +13.2% | +9.7% | -6.6% | +6.2% |
| token-query | 56.0 | +0.0% | +53.6% | +8.9% | -1.8% | -1.8% | +0.0% |
| crypto-hash-verify | 249.0 | -1.2% | +8.0% | -0.8% | +6.0% | +5.2% | +3.6% |
| com-create-instance | 476.0 | +52.3% | +240.3% | +33.6% | +89.1% | +16.0% | +294.3% |
| wmi-query | 13879.0 | -17.1%* | -15.2%* | -33.8%* | +56.0% | -34.7%* | -5.4% |
| fs-watcher | 2203.0 | +86.4% | +780.8% | +124.6% | +75.4% | +57.6% | +820.5% |
| ripgrep-clean-build | 14965.0 | +758.3%* | +17.6% | +17.4% | +12.3% | +22.3% | +21.9% |
| ripgrep-incremental-build | 5891.0 | +3.5% | +10.6% | +0.8% | +3.1% | +4.0% | +1.9% |
| roslyn-clean-build | 206248.0 | +11.2% | +15.3% | +22.9% | +6.5% | +25.2% | +20.0% |
| roslyn-incremental-build | 59744.0 | +4.4% | +6.6% | +3.1% | +7.8% | +16.5% | +16.0% |

## Cross-AV first-run comparison

Cells are slowdown vs baseline using the earliest successful run for each scenario before outlier trimming.

| Scenario | baseline first-run (ms) | avira | bitdefender | eset | huorong | ms-defender | trendmicro |
|---|---|---|---|---|---|---|---|
| file-create-delete | 1490.0 | +70.2% | +324.8% | +93.2% | +61.5% | +48.7% | +749.8% |
| archive-extract | 13023.0 | +22.8% | +471.2% | +23.5% | +41.4% | +56.6% | +430.0% |
| file-enum-large-dir | 2364.0 | +62.6% | +766.0% | +40.9% | +85.8% | +120.0% | +642.5% |
| file-copy-large | 1004.0 | -13.6%* | -12.7%* | -12.2%* | -18.0%* | -11.4%* | -14.1%* |
| hardlink-create | 1246.0 | +45.5% | +254.1% | +44.1% | +135.1% | +38.6% | +523.3% |
| junction-create | 581.0 | +60.2% | +173.5% | +20.8% | +47.3% | +23.4% | +248.9% |
| process-create-wait | 13290.0 | +38.3% | +134.4% | +46.5% | +38.0% | +20.2% | +88.0% |
| ext-sensitivity-exe | 3008.0 | +66.1% | +596.6% | +104.6% | +232.7% | +48.0% | +772.6% |
| ext-sensitivity-dll | 3008.0 | +70.4% | +638.9% | +100.3% | +264.8% | +44.7% | +1167.4% |
| ext-sensitivity-js | 3008.0 | +66.6% | +1196.1% | +150.8% | +59.9% | +44.9% | +1924.6% |
| ext-sensitivity-ps1 | 2986.0 | +69.5% | +3936.5% | +153.8% | +61.7% | +47.0% | +1235.7% |
| dll-load-unique | 7859.0 | +461.4% | +101.6% | +31.6% | +247.2% | +615.9% | +480.5% |
| file-write-content | 3289.0 | +58.8% | +475.6% | +2740.7% | +31757.5% | +41.5% | +2625.8% |
| new-exe-run | 1914.0 | +15638.0% | +459.7% | +194.1% | +35.1% | +183.3% | +1648.5% |
| new-exe-run-motw | 1950.0 | +431.6% | +312.4% | +55.1% | +31.9% | +4474.9% | failed* |
| thread-create | 1202.0 | +18.7% | +7.3% | +5.0% | -2.2%* | +0.9% | +56.2% |
| mem-alloc-protect | 169.0 | -4.7%* | -1.8%* | +3.6% | -3.6%* | +19.5% | +132.0% |
| mem-map-file | 3931.0 | +183.7% | +214.7% | +45.6% | +3.4% | +123.7% | +251.5% |
| net-connect-loopback | 577.0 | +53.7% | +166.4% | +159.8% | +13.5% | +2.1% | +20.1% |
| net-dns-resolve | 968.0 | +567.4% | +29.3% | +6.1% | +17.0% | +6.8% | +12.5% |
| registry-crud | 459.0 | +1119.4% | +1053.2% | +78.9% | +76.7% | +28.3% | +2125.7% |
| pipe-roundtrip | 132.0 | +6.1% | +34.1% | +23.5% | +18.2% | -3.0%* | +10.6% |
| token-query | 58.0 | -6.9%* | +48.3% | +5.2% | -3.4%* | -3.4%* | -5.2%* |
| crypto-hash-verify | 251.0 | -4.4%* | +7.2% | -1.6%* | +5.2% | +5.2% | +3.6% |
| com-create-instance | 488.0 | +48.8% | +232.0% | +31.1% | +84.4% | +13.1% | +279.9% |
| wmi-query | 13783.0 | -17.2%* | -12.0%* | -36.1%* | +55.0% | -35.6%* | -7.1%* |
| fs-watcher | 2201.0 | +86.6% | +783.5% | +124.3% | +75.6% | +57.5% | +836.3% |
| ripgrep-clean-build | 14934.0 | +760.1% | +18.1% | +17.6% | +13.1% | +22.6% | +31.6% |
| ripgrep-incremental-build | 6078.0 | -0.5%* | +7.6% | -2.3%* | -0.8%* | +0.4% | -0.6%* |
| roslyn-clean-build | 216530.0 | +121.6% | +7.8% | +21.5% | -2.4%* | +20.4% | +17.4% |
| roslyn-incremental-build | 63913.0 | +155.0% | +3.1% | -4.6%* | +1.5% | +8.9% | +8.4% |

`*` in the steady-state table marks a non-ok result (`failed`, `insufficient`, `noisy`, or `anomaly`).
First-run cells do not inherit `noisy` or `insufficient` markers because CV and repeat-run status are not meaningful for a single first-run sample; `failed*` means no successful first run was available, and a negative first-run slowdown is marked as an anomaly.

