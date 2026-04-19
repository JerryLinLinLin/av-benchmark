# AV Benchmark Comparison Report

Generated: 2026-04-19 05:17:01 UTC

## avira (Avira Security v1.0.2604.8504) vs baseline-os

| Scenario | Median Wall (ms) | First-Run Wall (ms) | Slowdown | First-Run Slowdown | p95 Slowdown | Disk Read Delta (MB) | Disk Write Delta (MB) | CV % | Baseline CV % | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| file-create-delete | 2555.0 | 2536.0 | +73.2% | +71.9% | +86.0% | -0.1 | +14.0 | 1.9% | 2.0% | ok |
| archive-extract | 16149.0 | 15996.0 | +23.6% | +22.5% | +32.3% | +34.4 | +10.0 | 1.0% | 2.2% | ok |
| file-enum-large-dir | 3892.0 | 3844.0 | +62.1% | +60.1% | -4.2% | -0.1 | -3.3 | 0.7% | 0.3% | ok |
| file-copy-large | 833.5 | 867.0 | -12.4% | -8.9% | -52.8% | -0.1 | -100.1 | 1.1% | 1.4% | anomaly |
| hardlink-create | 1821.0 | 1813.0 | +43.7% | +43.1% | +37.6% | -0.0 | -9.2 | 0.6% | 0.8% | ok |
| junction-create | 926.5 | 931.0 | +53.9% | +54.7% | +47.6% | +5.9 | +2.4 | 0.5% | 1.5% | ok |
| process-create-wait | 18500.0 | 18382.0 | +38.0% | +37.1% | +37.6% | +58.9 | +99.4 | 0.7% | 0.4% | ok |
| ext-sensitivity-exe | 4979.5 | 4997.0 | +64.7% | +65.3% | +84.2% | +0.1 | -5.1 | 0.8% | 1.0% | ok |
| ext-sensitivity-dll | 5011.0 | 5127.0 | +65.5% | +69.3% | +84.6% | +0.0 | -6.3 | 0.4% | 1.4% | ok |
| ext-sensitivity-js | 5012.0 | 5011.0 | +64.3% | +64.3% | +80.9% | -0.0 | -4.9 | 1.1% | 1.0% | ok |
| ext-sensitivity-ps1 | 5065.0 | 5060.0 | +69.9% | +69.7% | +88.8% | +0.0 | +7.0 | 0.8% | 1.0% | ok |
| dll-load-unique | 44465.5 | 44123.0 | +454.6% | +450.3% | +402.7% | +14.3 | +5.8 | 0.7% | 1.1% | ok |
| file-write-content | 5202.0 | 5224.0 | +57.6% | +58.3% | +70.9% | -0.0 | -14.4 | 0.4% | 0.5% | ok |
| new-exe-run | 16176.0 | 301226.0 | +739.9% | +15540.0% | +905.8% | +5.4 | +3.1 | 141.0% | 1.0% | noisy |
| new-exe-run-motw | 4394.5 | 10366.0 | +127.6% | +437.0% | +125.9% | -0.0 | +0.2 | 41.3% | 0.7% | noisy |
| thread-create | 1452.5 | 1427.0 | +23.9% | +21.8% | +43.8% | -0.1 | +0.1 | 1.4% | 0.8% | ok |
| mem-alloc-protect | 162.5 | 161.0 | -3.6% | -4.5% | -33.6% | +0.0 | +0.0 | 1.1% | 1.5% | anomaly |
| mem-map-file | 11094.0 | 11153.0 | +201.7% | +203.3% | +169.5% | -0.1 | +1.6 | 0.3% | 7.2% | ok |
| net-connect-loopback | 899.5 | 887.0 | +57.3% | +55.1% | +62.2% | +0.7 | +0.1 | 0.7% | 1.2% | ok |
| net-dns-resolve | 6435.0 | 6460.0 | +571.7% | +574.3% | +794.5% | +0.4 | +4.2 | 0.3% | 1.4% | ok |
| registry-crud | 5612.5 | 5597.0 | +1132.2% | +1128.8% | +903.8% | +0.3 | +0.2 | 0.7% | 4.1% | ok |
| pipe-roundtrip | 148.0 | 140.0 | +1.4% | -4.1% | -40.2% | +0.1 | -0.0 | 1.3% | 4.4% | ok |
| token-query | 56.0 | 54.0 | +0.9% | -2.7% | -3.7% | +0.0 | +0.0 | 0.8% | 0.9% | ok |
| crypto-hash-verify | 246.5 | 240.0 | -1.0% | -3.6% | -3.2% | -0.4 | -0.0 | 6.4% | 1.0% | anomaly |
| com-create-instance | 722.0 | 726.0 | +52.6% | +53.5% | +38.8% | +0.1 | +0.0 | 3.6% | 1.8% | ok |
| wmi-query | 11312.5 | 11408.0 | -19.2% | -18.5% | -21.3% | -0.3 | -0.1 | 11.9% | 23.8% | anomaly |
| fs-watcher | 4102.0 | 4106.0 | +86.0% | +86.2% | +89.2% | -0.1 | +2.6 | 0.7% | 0.9% | ok |
| ripgrep-clean-build | 147738.5 | 128448.0 | +885.4% | +756.7% | - | +122.4 | +38.0 | 14.0% | 0.3% | noisy |
| ripgrep-incremental-build | 6101.5 | 6045.0 | +3.7% | +2.7% | - | -0.0 | -3.8 | 0.4% | 0.5% | ok |
| roslyn-clean-build | 229388.5 | 479777.0 | +11.3% | +132.7% | - | -2844.4 | +0.3 | 3.0% | 2.0% | ok |
| roslyn-incremental-build | 62391.5 | 162989.0 | +5.3% | +175.0% | - | +48.3 | +26.7 | 1.9% | 3.2% | ok |

Highest slowdown: registry-crud at +1132.2%

Largest kernel CPU shift: roslyn-clean-build at +1.4pp (9.0% -> 10.4%)

Largest system disk write delta: file-copy-large at -100.1 MB (100.1 -> 0.0 MB)

Largest system disk read delta: roslyn-clean-build at -2844.4 MB (9876.2 -> 7031.8 MB)

Noisy scenarios: new-exe-run, new-exe-run-motw, ripgrep-clean-build

Anomaly scenarios (AV appears faster - likely caching artifact): crypto-hash-verify, file-copy-large, mem-alloc-protect, wmi-query

## bitdefender (Bitdefender Antivirus v27.0.3.9) vs baseline-os

| Scenario | Median Wall (ms) | First-Run Wall (ms) | Slowdown | First-Run Slowdown | p95 Slowdown | Disk Read Delta (MB) | Disk Write Delta (MB) | CV % | Baseline CV % | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| file-create-delete | 6245.0 | 6329.0 | +323.2% | +328.9% | +399.1% | +0.4 | +12.7 | 0.5% | 2.0% | ok |
| archive-extract | 73276.0 | 74391.0 | +461.0% | +469.5% | +431.7% | +32.0 | +332.0 | 0.5% | 2.2% | ok |
| file-enum-large-dir | 20136.5 | 20473.0 | +738.5% | +752.5% | +6.2% | +0.8 | +10.3 | 1.0% | 0.3% | ok |
| file-copy-large | 874.0 | 876.0 | -8.2% | -8.0% | -60.1% | -0.1 | -98.8 | 1.4% | 1.4% | anomaly |
| hardlink-create | 4415.0 | 4412.0 | +248.5% | +248.2% | +228.3% | +0.1 | +4.7 | 0.9% | 0.8% | ok |
| junction-create | 1572.5 | 1589.0 | +161.2% | +164.0% | +141.3% | +2.5 | +8.6 | 2.0% | 1.5% | ok |
| process-create-wait | 31609.0 | 31152.0 | +135.8% | +132.4% | +135.3% | +152.5 | +287.5 | 1.5% | 0.4% | ok |
| ext-sensitivity-exe | 21288.5 | 20953.0 | +604.3% | +593.2% | +555.7% | +0.1 | +13.7 | 0.5% | 1.0% | ok |
| ext-sensitivity-dll | 22352.0 | 22227.0 | +638.1% | +633.9% | +552.8% | +0.1 | +8.6 | 0.6% | 1.4% | ok |
| ext-sensitivity-js | 38936.0 | 38986.0 | +1176.6% | +1178.2% | +873.8% | +0.2 | +2.5 | 0.5% | 1.0% | ok |
| ext-sensitivity-ps1 | 121172.0 | 120530.0 | +3964.1% | +3942.6% | +3794.7% | +0.2 | +9.9 | 8.0% | 1.0% | ok |
| dll-load-unique | 15708.0 | 15844.0 | +95.9% | +97.6% | +72.8% | +0.4 | +3.9 | 1.2% | 1.1% | ok |
| file-write-content | 18845.0 | 18932.0 | +471.0% | +473.6% | +356.8% | +0.0 | -2.6 | 0.3% | 0.5% | ok |
| new-exe-run | 14937.5 | 10713.0 | +675.6% | +456.2% | +806.8% | +3.5 | +0.5 | 0.4% | 1.0% | ok |
| new-exe-run-motw | 12083.5 | 8042.0 | +525.9% | +316.6% | +682.3% | +0.2 | +7.1 | 2.9% | 0.7% | ok |
| thread-create | 1301.5 | 1290.0 | +11.0% | +10.1% | +19.3% | +0.1 | +0.0 | 0.4% | 0.8% | ok |
| mem-alloc-protect | 169.0 | 166.0 | +0.3% | -1.5% | -41.8% | +0.0 | +0.0 | 0.6% | 1.5% | ok |
| mem-map-file | 12329.5 | 12370.0 | +235.3% | +236.4% | +197.7% | +0.3 | +0.6 | 1.3% | 7.2% | ok |
| net-connect-loopback | 1456.5 | 1537.0 | +154.6% | +168.7% | +137.7% | +0.1 | +4.1 | 0.6% | 1.2% | ok |
| net-dns-resolve | 1223.0 | 1252.0 | +27.7% | +30.7% | +30.1% | +0.0 | +0.1 | 1.4% | 1.4% | ok |
| registry-crud | 5276.5 | 5293.0 | +1058.4% | +1062.0% | +830.4% | -0.0 | +0.5 | 0.1% | 4.1% | ok |
| pipe-roundtrip | 175.5 | 177.0 | +20.2% | +21.2% | +14.2% | +0.1 | -0.0 | 3.5% | 4.4% | ok |
| token-query | 86.0 | 86.0 | +55.0% | +55.0% | +33.3% | +0.0 | +0.0 | 0.8% | 0.9% | ok |
| crypto-hash-verify | 264.5 | 269.0 | +6.2% | +8.0% | -3.8% | +0.0 | -0.0 | 8.6% | 1.0% | ok |
| com-create-instance | 1621.0 | 1620.0 | +242.7% | +242.5% | +132.4% | +0.0 | +0.0 | 1.8% | 1.8% | ok |
| wmi-query | 11762.0 | 12133.0 | -16.0% | -13.3% | -18.2% | -0.1 | -0.2 | 0.5% | 23.8% | anomaly |
| fs-watcher | 19381.0 | 19446.0 | +779.0% | +781.9% | +443.5% | -0.0 | +7.2 | 0.6% | 0.9% | ok |
| ripgrep-clean-build | 17582.5 | 17639.0 | +17.3% | +17.7% | - | +179.9 | +0.0 | 0.2% | 0.3% | ok |
| ripgrep-incremental-build | 6489.5 | 6537.0 | +10.2% | +11.1% | - | -0.0 | +25.9 | 1.7% | 0.5% | ok |
| roslyn-clean-build | 239808.5 | 233499.0 | +16.3% | +13.3% | - | -6347.5 | +141.5 | 7.0% | 2.0% | ok |
| roslyn-incremental-build | 63642.0 | 65869.0 | +7.4% | +11.2% | - | -4.6 | +5.1 | 1.5% | 3.2% | ok |

Highest slowdown: ext-sensitivity-ps1 at +3964.1%

Largest kernel CPU shift: roslyn-incremental-build at +4.2pp (15.2% -> 19.4%)

Largest system disk write delta: archive-extract at +332.0 MB (251.2 -> 583.2 MB)

Largest system disk read delta: roslyn-clean-build at -6347.5 MB (9876.2 -> 3528.7 MB)

Anomaly scenarios (AV appears faster - likely caching artifact): file-copy-large, wmi-query

## eset (ESET Security v19.1.12.0) vs baseline-os

| Scenario | Median Wall (ms) | First-Run Wall (ms) | Slowdown | First-Run Slowdown | p95 Slowdown | Disk Read Delta (MB) | Disk Write Delta (MB) | CV % | Baseline CV % | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| file-create-delete | 2910.5 | 2878.0 | +97.3% | +95.1% | +114.2% | -0.1 | +12.5 | 0.5% | 2.0% | ok |
| archive-extract | 16067.0 | 16084.0 | +23.0% | +23.1% | +47.5% | +65.7 | +60.1 | 0.9% | 2.2% | ok |
| file-enum-large-dir | 3471.5 | 3332.0 | +44.6% | +38.7% | +1.3% | +0.1 | +0.3 | 1.8% | 0.3% | ok |
| file-copy-large | 870.0 | 882.0 | -8.6% | -7.4% | -24.9% | -0.1 | -0.0 | 0.4% | 1.4% | anomaly |
| hardlink-create | 1804.0 | 1795.0 | +42.4% | +41.7% | +34.2% | -0.0 | -4.2 | 0.4% | 0.8% | ok |
| junction-create | 705.0 | 702.0 | +17.1% | +16.6% | +14.6% | -0.0 | +2.3 | 0.9% | 1.5% | ok |
| process-create-wait | 19685.0 | 19468.0 | +46.8% | +45.2% | +45.2% | +0.5 | +7.9 | 0.6% | 0.4% | ok |
| ext-sensitivity-exe | 6248.5 | 6155.0 | +106.7% | +103.6% | +115.5% | +0.0 | +14.7 | 0.7% | 1.0% | ok |
| ext-sensitivity-dll | 6129.5 | 6026.0 | +102.4% | +99.0% | +118.7% | +0.0 | +6.1 | 0.3% | 1.4% | ok |
| ext-sensitivity-js | 7840.5 | 7545.0 | +157.1% | +147.4% | +185.1% | -0.0 | -4.5 | 1.6% | 1.0% | ok |
| ext-sensitivity-ps1 | 7741.5 | 7577.0 | +159.7% | +154.1% | +171.6% | +0.0 | -4.6 | 0.2% | 1.0% | ok |
| dll-load-unique | 10452.0 | 10344.0 | +30.4% | +29.0% | +30.9% | -1.9 | +2.5 | 0.6% | 1.1% | ok |
| file-write-content | 94177.0 | 93431.0 | +2753.4% | +2730.8% | +2784.4% | +144.1 | +23.3 | 2.0% | 0.5% | ok |
| new-exe-run | 5695.0 | 5630.0 | +195.7% | +192.3% | +224.8% | +2.4 | +1.1 | 21.3% | 1.0% | noisy |
| new-exe-run-motw | 3084.5 | 3024.0 | +59.8% | +56.6% | +59.6% | +0.1 | +0.5 | 0.4% | 0.7% | ok |
| thread-create | 1250.0 | 1262.0 | +6.7% | +7.7% | -2.2% | -0.1 | +0.0 | 0.7% | 0.8% | ok |
| mem-alloc-protect | 171.5 | 175.0 | +1.8% | +3.9% | -33.6% | +1.0 | +0.0 | 0.5% | 1.5% | ok |
| mem-map-file | 5672.0 | 5722.0 | +54.2% | +55.6% | +28.9% | -0.0 | -62.4 | 1.0% | 7.2% | ok |
| net-connect-loopback | 1580.0 | 1499.0 | +176.2% | +162.1% | +159.4% | +0.9 | +0.0 | 3.1% | 1.2% | ok |
| net-dns-resolve | 1008.5 | 1027.0 | +5.3% | +7.2% | +3.3% | +0.4 | +0.1 | 2.1% | 1.4% | ok |
| registry-crud | 818.5 | 821.0 | +79.7% | +80.2% | +68.7% | -0.1 | +0.2 | 1.1% | 4.1% | ok |
| pipe-roundtrip | 158.0 | 163.0 | +8.2% | +11.6% | -38.8% | +0.1 | +0.1 | 7.7% | 4.4% | ok |
| token-query | 61.0 | 61.0 | +9.9% | +9.9% | +11.1% | +0.0 | +0.0 | 0.7% | 0.9% | ok |
| crypto-hash-verify | 246.5 | 247.0 | -1.0% | -0.8% | -2.4% | -0.4 | +0.0 | 3.7% | 1.0% | anomaly |
| com-create-instance | 636.0 | 640.0 | +34.5% | +35.3% | +27.9% | +0.1 | +0.0 | 1.3% | 1.8% | ok |
| wmi-query | 9372.5 | 8807.0 | -33.0% | -37.1% | -32.6% | -0.3 | -0.2 | 4.6% | 23.8% | anomaly |
| fs-watcher | 4953.5 | 4937.0 | +124.6% | +123.9% | +144.3% | +0.1 | +2.2 | 1.8% | 0.9% | ok |
| ripgrep-clean-build | 17575.5 | 17563.0 | +17.2% | +17.1% | - | +179.8 | -0.3 | 0.3% | 0.3% | ok |
| ripgrep-incremental-build | 5940.0 | 5941.0 | +0.9% | +0.9% | - | -0.0 | +1.8 | 0.5% | 0.5% | ok |
| roslyn-clean-build | 251105.0 | 263064.0 | +21.8% | +27.6% | - | -196.1 | +351.0 | 4.9% | 2.0% | ok |
| roslyn-incremental-build | 62314.0 | 60978.0 | +5.2% | +2.9% | - | +42.1 | +42.9 | 7.2% | 3.2% | ok |

Highest slowdown: file-write-content at +2753.4%

Largest kernel CPU shift: roslyn-clean-build at +1.6pp (9.0% -> 10.6%)

Largest system disk write delta: roslyn-clean-build at +351.0 MB (30580.6 -> 30931.5 MB)

Largest system disk read delta: roslyn-clean-build at -196.1 MB (9876.2 -> 9680.1 MB)

Noisy scenarios: new-exe-run

Anomaly scenarios (AV appears faster - likely caching artifact): crypto-hash-verify, file-copy-large, wmi-query

## huorong (Huorong Internet Security v5.0.0.0) vs baseline-os

| Scenario | Median Wall (ms) | First-Run Wall (ms) | Slowdown | First-Run Slowdown | p95 Slowdown | Disk Read Delta (MB) | Disk Write Delta (MB) | CV % | Baseline CV % | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| file-create-delete | 2405.0 | 2406.0 | +63.0% | +63.1% | +59.5% | +0.4 | +15.9 | 0.5% | 2.0% | ok |
| archive-extract | 18553.0 | 18416.0 | +42.0% | +41.0% | +36.9% | +35.2 | +24.6 | 1.7% | 2.2% | ok |
| file-enum-large-dir | 4372.5 | 4393.0 | +82.1% | +82.9% | +9.5% | +1.7 | -16.6 | 57.1% | 0.3% | noisy |
| file-copy-large | 831.5 | 823.0 | -12.7% | -13.6% | -62.8% | -0.1 | -100.0 | 1.7% | 1.4% | anomaly |
| hardlink-create | 2919.0 | 2929.0 | +130.4% | +131.2% | +161.7% | -0.0 | -6.4 | 1.8% | 0.8% | ok |
| junction-create | 842.5 | 856.0 | +40.0% | +42.2% | +32.6% | -0.0 | -2.5 | 1.2% | 1.5% | ok |
| process-create-wait | 18280.5 | 18346.0 | +36.4% | +36.9% | +34.2% | +1.4 | +98.6 | 0.8% | 0.4% | ok |
| ext-sensitivity-exe | 10059.0 | 10009.0 | +232.8% | +231.1% | +270.9% | +0.2 | +24.9 | 1.3% | 1.0% | ok |
| ext-sensitivity-dll | 10900.5 | 10973.0 | +259.9% | +262.3% | +289.1% | +0.2 | +21.7 | 1.4% | 1.4% | ok |
| ext-sensitivity-js | 4768.0 | 4809.0 | +56.3% | +57.7% | +52.7% | +3.7 | +0.8 | 0.7% | 1.0% | ok |
| ext-sensitivity-ps1 | 4790.0 | 4827.0 | +60.7% | +61.9% | +55.5% | +0.2 | -2.3 | 0.2% | 1.0% | ok |
| dll-load-unique | 27572.5 | 27286.0 | +243.9% | +240.3% | +223.0% | +170.0 | +23.1 | 2.2% | 1.1% | ok |
| file-write-content | 1043680.5 | 1047793.0 | +31521.9% | +31646.5% | +29319.2% | +140.7 | +316.7 | 0.1% | 0.5% | ok |
| new-exe-run | 2581.5 | 2586.0 | +34.0% | +34.3% | +35.4% | +1.0 | +2.6 | 0.7% | 1.0% | ok |
| new-exe-run-motw | 2550.5 | 2572.0 | +32.1% | +33.2% | +31.5% | +0.3 | +1.0 | 0.6% | 0.7% | ok |
| thread-create | 1181.5 | 1175.0 | +0.8% | +0.3% | 0.0% | -0.1 | +0.0 | 1.8% | 0.8% | ok |
| mem-alloc-protect | 162.0 | 163.0 | -3.9% | -3.3% | -24.5% | +0.0 | +0.0 | 2.7% | 1.5% | anomaly |
| mem-map-file | 4146.0 | 4066.0 | +12.7% | +10.6% | +14.5% | +0.0 | +2.4 | 1.4% | 7.2% | ok |
| net-connect-loopback | 650.0 | 655.0 | +13.6% | +14.5% | +15.3% | +0.8 | +0.0 | 1.6% | 1.2% | ok |
| net-dns-resolve | 1111.5 | 1133.0 | +16.0% | +18.3% | +17.0% | +0.4 | +0.0 | 1.2% | 1.4% | ok |
| registry-crud | 807.5 | 811.0 | +77.3% | +78.0% | +73.5% | -0.1 | +0.0 | 1.1% | 4.1% | ok |
| pipe-roundtrip | 159.0 | 156.0 | +8.9% | +6.8% | +2.8% | +1.1 | +0.0 | 1.3% | 4.4% | ok |
| token-query | 55.0 | 56.0 | -0.9% | +0.9% | -3.7% | +0.0 | +0.0 | 0.0% | 0.9% | anomaly |
| crypto-hash-verify | 266.0 | 264.0 | +6.8% | +6.0% | -1.0% | -0.4 | -0.0 | 4.2% | 1.0% | ok |
| com-create-instance | 899.5 | 900.0 | +90.2% | +90.3% | +66.6% | -0.0 | +0.0 | 0.4% | 1.8% | ok |
| wmi-query | 21802.5 | 21357.0 | +55.8% | +52.6% | +50.1% | -0.1 | +0.0 | 2.4% | 23.8% | noisy |
| fs-watcher | 3864.0 | 3865.0 | +75.2% | +75.3% | +63.2% | +0.0 | +4.0 | 0.4% | 0.9% | ok |
| ripgrep-clean-build | 16748.5 | 16897.0 | +11.7% | +12.7% | - | +180.8 | -1.1 | 0.6% | 0.3% | ok |
| ripgrep-incremental-build | 6073.0 | 6027.0 | +3.2% | +2.4% | - | -0.0 | +16.0 | 1.0% | 0.5% | ok |
| roslyn-clean-build | 219804.0 | 211433.0 | +6.6% | +2.6% | - | -4655.9 | +248.5 | 0.6% | 2.0% | ok |
| roslyn-incremental-build | 63939.0 | 64900.0 | +7.9% | +9.5% | - | +408.4 | +340.4 | 2.1% | 3.2% | ok |

Highest slowdown: file-write-content at +31521.9%

Largest kernel CPU shift: roslyn-clean-build at +2.2pp (9.0% -> 11.2%)

Largest system disk write delta: roslyn-incremental-build at +340.4 MB (1199.1 -> 1539.5 MB)

Largest system disk read delta: roslyn-clean-build at -4655.9 MB (9876.2 -> 5220.3 MB)

Noisy scenarios: file-enum-large-dir, wmi-query

Anomaly scenarios (AV appears faster - likely caching artifact): file-copy-large, mem-alloc-protect, token-query

## ms-defender (Windows Defender v4.18.2201.11) vs baseline-os

| Scenario | Median Wall (ms) | First-Run Wall (ms) | Slowdown | First-Run Slowdown | p95 Slowdown | Disk Read Delta (MB) | Disk Write Delta (MB) | CV % | Baseline CV % | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| file-create-delete | 2256.5 | 2215.0 | +52.9% | +50.1% | +57.7% | +0.8 | +19.1 | 1.0% | 2.0% | ok |
| archive-extract | 20029.5 | 20392.0 | +53.3% | +56.1% | +98.4% | +49.3 | +14.0 | 0.9% | 2.2% | ok |
| file-enum-large-dir | 5262.5 | 5200.0 | +119.1% | +116.5% | +107.5% | +14.1 | +3.5 | 0.5% | 0.3% | ok |
| file-copy-large | 880.0 | 890.0 | -7.6% | -6.5% | -19.1% | -0.1 | +1.0 | 1.3% | 1.4% | anomaly |
| hardlink-create | 1873.5 | 1727.0 | +47.9% | +36.3% | +41.0% | -0.0 | +0.1 | 7.3% | 0.8% | ok |
| junction-create | 726.0 | 717.0 | +20.6% | +19.1% | +15.6% | +0.0 | +0.9 | 0.5% | 1.5% | ok |
| process-create-wait | 15958.0 | 15969.0 | +19.0% | +19.1% | +18.7% | +0.5 | -1.3 | 0.8% | 0.4% | ok |
| ext-sensitivity-exe | 4451.0 | 4453.0 | +47.3% | +47.3% | +46.6% | +0.0 | +13.7 | 0.8% | 1.0% | ok |
| ext-sensitivity-dll | 4399.5 | 4353.0 | +45.3% | +43.7% | +41.3% | +0.0 | +15.2 | 0.8% | 1.4% | ok |
| ext-sensitivity-js | 4365.5 | 4360.0 | +43.1% | +43.0% | +39.9% | +0.0 | -0.6 | 0.6% | 1.0% | ok |
| ext-sensitivity-ps1 | 4394.5 | 4389.0 | +47.4% | +47.2% | +44.1% | +0.0 | +7.4 | 0.5% | 1.0% | ok |
| dll-load-unique | 55851.5 | 56260.0 | +596.6% | +601.7% | +536.9% | +1.0 | +13.9 | 0.5% | 1.1% | ok |
| file-write-content | 4714.5 | 4655.0 | +42.8% | +41.0% | +40.1% | +0.6 | +8.8 | 0.4% | 0.5% | ok |
| new-exe-run | 5149.0 | 5423.0 | +167.3% | +181.6% | +170.2% | +7.6 | +0.6 | 2.5% | 1.0% | ok |
| new-exe-run-motw | 60074.0 | 89210.0 | +3011.8% | +4521.1% | +4064.3% | +92.9 | +140.1 | 8.9% | 0.7% | ok |
| thread-create | 1259.0 | 1213.0 | +7.4% | +3.5% | +13.3% | +0.0 | +0.0 | 0.5% | 0.8% | ok |
| mem-alloc-protect | 196.5 | 202.0 | +16.6% | +19.9% | +4.5% | +0.0 | +0.1 | 2.2% | 1.5% | ok |
| mem-map-file | 8831.5 | 8795.0 | +140.1% | +139.2% | +142.1% | -0.0 | +2.1 | 1.3% | 7.2% | ok |
| net-connect-loopback | 598.5 | 589.0 | +4.6% | +3.0% | +3.7% | +0.4 | +0.1 | 0.5% | 1.2% | ok |
| net-dns-resolve | 1059.0 | 1034.0 | +10.5% | +7.9% | +10.5% | +0.0 | +0.0 | 1.0% | 1.4% | ok |
| registry-crud | 590.5 | 589.0 | +29.6% | +29.3% | +24.6% | -0.0 | +0.0 | 0.9% | 4.1% | ok |
| pipe-roundtrip | 147.5 | 128.0 | +1.0% | -12.3% | -19.9% | +0.3 | -0.0 | 9.2% | 4.4% | ok |
| token-query | 55.0 | 56.0 | -0.9% | +0.9% | -3.7% | +0.0 | +0.0 | 1.6% | 0.9% | anomaly |
| crypto-hash-verify | 257.5 | 264.0 | +3.4% | +6.0% | +2.0% | +0.1 | -0.0 | 2.1% | 1.0% | ok |
| com-create-instance | 550.0 | 552.0 | +16.3% | +16.7% | +11.6% | +0.0 | +0.0 | 1.2% | 1.8% | ok |
| wmi-query | 9068.0 | 8881.0 | -35.2% | -36.5% | -36.4% | -0.3 | -0.2 | 4.4% | 23.8% | anomaly |
| fs-watcher | 3519.0 | 3467.0 | +59.6% | +57.2% | +47.6% | -0.0 | +13.2 | 1.8% | 0.9% | ok |
| ripgrep-clean-build | 18289.5 | 18306.0 | +22.0% | +22.1% | - | +111.3 | +5.3 | 0.7% | 0.3% | ok |
| ripgrep-incremental-build | 6133.0 | 6105.0 | +4.2% | +3.7% | - | +0.0 | +3.0 | 0.7% | 0.5% | ok |
| roslyn-clean-build | 257738.0 | 260770.0 | +25.0% | +26.5% | - | -7227.1 | +101.0 | 3.6% | 2.0% | ok |
| roslyn-incremental-build | 69763.0 | 69593.0 | +17.7% | +17.4% | - | +24.3 | +24.3 | 2.3% | 3.2% | ok |

Highest slowdown: new-exe-run-motw at +3011.8%

Largest kernel CPU shift: roslyn-clean-build at +1.5pp (9.0% -> 10.4%)

Largest system disk write delta: new-exe-run-motw at +140.1 MB (12.1 -> 152.1 MB)

Largest system disk read delta: roslyn-clean-build at -7227.1 MB (9876.2 -> 2649.1 MB)

Anomaly scenarios (AV appears faster - likely caching artifact): file-copy-large, token-query, wmi-query

## trendmicro (Trend Micro Maximum Security v7.7) vs baseline-os

| Scenario | Median Wall (ms) | First-Run Wall (ms) | Slowdown | First-Run Slowdown | p95 Slowdown | Disk Read Delta (MB) | Disk Write Delta (MB) | CV % | Baseline CV % | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| file-create-delete | 12578.0 | 12662.0 | +752.5% | +758.1% | +843.9% | -0.0 | +16.9 | 0.5% | 2.0% | ok |
| archive-extract | 68852.0 | 69021.0 | +427.1% | +428.4% | +405.6% | +34.3 | +269.5 | 0.6% | 2.2% | ok |
| file-enum-large-dir | 18166.5 | 17552.0 | +656.5% | +630.9% | -3.7% | +44.7 | +66.7 | 1.6% | 0.3% | ok |
| file-copy-large | 892.5 | 862.0 | -6.2% | -9.5% | -23.3% | -0.1 | +0.4 | 0.8% | 1.4% | anomaly |
| hardlink-create | 7662.0 | 7766.0 | +504.7% | +512.9% | +498.1% | +42.0 | +3.2 | 2.3% | 0.8% | ok |
| junction-create | 2034.5 | 2027.0 | +238.0% | +236.7% | +244.5% | +0.1 | +5.0 | 0.4% | 1.5% | ok |
| process-create-wait | 24896.0 | 24981.0 | +85.7% | +86.4% | +79.5% | +1.1 | -0.6 | 0.3% | 0.4% | ok |
| ext-sensitivity-exe | 26228.5 | 26248.0 | +767.8% | +768.4% | +839.6% | +0.2 | +162.2 | 0.4% | 1.0% | ok |
| ext-sensitivity-dll | 40132.0 | 38123.0 | +1225.1% | +1158.8% | +1283.1% | +0.0 | +71.7 | 5.5% | 1.4% | ok |
| ext-sensitivity-js | 62025.5 | 60901.0 | +1933.6% | +1896.8% | +2484.9% | +0.1 | +292.5 | 0.8% | 1.0% | ok |
| ext-sensitivity-ps1 | 40563.0 | 39883.0 | +1260.5% | +1237.7% | +1319.9% | +4.6 | +125.9 | 0.2% | 1.0% | ok |
| dll-load-unique | 45616.0 | 45621.0 | +468.9% | +469.0% | +422.4% | +0.1 | +9.5 | 0.3% | 1.1% | ok |
| file-write-content | 88823.0 | 89653.0 | +2591.2% | +2616.3% | +3834.7% | +0.9 | +829.2 | 1.1% | 0.5% | ok |
| new-exe-run | 25001.0 | 33467.0 | +1198.1% | +1637.6% | +1736.7% | +2.5 | +3.4 | 4.1% | 1.0% | ok |
| new-exe-run-motw | 0.0 | - | 0.0% | 0.0% | - | +0.0 | +0.0 | 0.0% | 0.0% | failed |
| thread-create | 1874.5 | 1878.0 | +59.9% | +60.2% | +72.9% | -0.1 | +0.2 | 0.9% | 0.8% | ok |
| mem-alloc-protect | 392.0 | 392.0 | +132.6% | +132.6% | +41.8% | +0.0 | +0.0 | 0.8% | 1.5% | ok |
| mem-map-file | 13742.0 | 13817.0 | +273.7% | +275.7% | +235.0% | -0.1 | +2.8 | 0.4% | 7.2% | ok |
| net-connect-loopback | 680.0 | 693.0 | +18.9% | +21.2% | +15.1% | +0.8 | +0.1 | 0.5% | 1.2% | ok |
| net-dns-resolve | 1071.5 | 1089.0 | +11.8% | +13.7% | +10.5% | +0.4 | +0.1 | 1.5% | 1.4% | ok |
| registry-crud | 10126.0 | 10216.0 | +2123.1% | +2142.8% | +2025.7% | -0.0 | +1.1 | 1.1% | 4.1% | ok |
| pipe-roundtrip | 153.5 | 146.0 | +5.1% | 0.0% | -22.9% | +0.1 | -0.0 | 7.5% | 4.4% | ok |
| token-query | 56.0 | 55.0 | +0.9% | -0.9% | 0.0% | +0.0 | +0.0 | 0.8% | 0.9% | ok |
| crypto-hash-verify | 256.0 | 260.0 | +2.8% | +4.4% | -2.4% | -0.4 | +0.0 | 2.3% | 1.0% | ok |
| com-create-instance | 1891.0 | 1854.0 | +299.8% | +292.0% | +168.0% | +0.1 | +0.1 | 1.4% | 1.8% | ok |
| wmi-query | 13176.0 | 12804.0 | -5.9% | -8.5% | -6.9% | -0.3 | +0.7 | 0.9% | 23.8% | anomaly |
| fs-watcher | 20223.5 | 20608.0 | +817.2% | +834.6% | +865.4% | +0.0 | +12.8 | 0.6% | 0.9% | ok |
| ripgrep-clean-build | 18205.5 | 19651.0 | +21.4% | +31.1% | - | +100.6 | +3.5 | 2.0% | 0.3% | ok |
| ripgrep-incremental-build | 5846.0 | 6044.0 | -0.7% | +2.7% | - | -0.0 | +0.6 | 2.7% | 0.5% | anomaly |
| roslyn-clean-build | 247467.5 | 254204.0 | +20.1% | +23.3% | - | -3671.4 | +317.2 | 1.3% | 2.0% | ok |
| roslyn-incremental-build | 68906.5 | 69286.0 | +16.3% | +16.9% | - | +243.4 | +62.0 | 1.1% | 3.2% | ok |

Highest slowdown: file-write-content at +2591.2%

Largest kernel CPU shift: roslyn-clean-build at +2.3pp (9.0% -> 11.3%)

Largest system disk write delta: file-write-content at +829.2 MB (58.0 -> 887.2 MB)

Largest system disk read delta: roslyn-clean-build at -3671.4 MB (9876.2 -> 6204.8 MB)

Anomaly scenarios (AV appears faster - likely caching artifact): file-copy-large, ripgrep-incremental-build, wmi-query

Failed scenarios: new-exe-run-motw

## Cross-AV steady-state comparison

Cells are slowdown vs baseline using median wall time after excluding each side's earliest successful run.

| Scenario | baseline median (ms) | avira | bitdefender | eset | huorong | ms-defender | trendmicro |
|---|---|---|---|---|---|---|---|
| file-create-delete | 1475.5 | +73.2% | +323.2% | +97.3% | +63.0% | +52.9% | +752.5% |
| archive-extract | 13062.0 | +23.6% | +461.0% | +23.0% | +42.0% | +53.3% | +427.1% |
| file-enum-large-dir | 2401.5 | +62.1% | +738.5% | +44.6% | +82.1%* | +119.1% | +656.5% |
| file-copy-large | 952.0 | -12.4%* | -8.2%* | -8.6%* | -12.7%* | -7.6%* | -6.2%* |
| hardlink-create | 1267.0 | +43.7% | +248.5% | +42.4% | +130.4% | +47.9% | +504.7% |
| junction-create | 602.0 | +53.9% | +161.2% | +17.1% | +40.0% | +20.6% | +238.0% |
| process-create-wait | 13405.0 | +38.0% | +135.8% | +46.8% | +36.4% | +19.0% | +85.7% |
| ext-sensitivity-exe | 3022.5 | +64.7% | +604.3% | +106.7% | +232.8% | +47.3% | +767.8% |
| ext-sensitivity-dll | 3028.5 | +65.5% | +638.1% | +102.4% | +259.9% | +45.3% | +1225.1% |
| ext-sensitivity-js | 3050.0 | +64.3% | +1176.6% | +157.1% | +56.3% | +43.1% | +1933.6% |
| ext-sensitivity-ps1 | 2981.5 | +69.9% | +3964.1% | +159.7% | +60.7% | +47.4% | +1260.5% |
| dll-load-unique | 8018.0 | +454.6% | +95.9% | +30.4% | +243.9% | +596.6% | +468.9% |
| file-write-content | 3300.5 | +57.6% | +471.0% | +2753.4% | +31521.9% | +42.8% | +2591.2% |
| new-exe-run | 1926.0 | +739.9%* | +675.6% | +195.7%* | +34.0% | +167.3% | +1198.1% |
| new-exe-run-motw | 1930.5 | +127.6%* | +525.9% | +59.8% | +32.1% | +3011.8% | failed* |
| thread-create | 1172.0 | +23.9% | +11.0% | +6.7% | +0.8% | +7.4% | +59.9% |
| mem-alloc-protect | 168.5 | -3.6%* | +0.3% | +1.8% | -3.9%* | +16.6% | +132.6% |
| mem-map-file | 3677.5 | +201.7% | +235.3% | +54.2% | +12.7% | +140.1% | +273.7% |
| net-connect-loopback | 572.0 | +57.3% | +154.6% | +176.2% | +13.6% | +4.6% | +18.9% |
| net-dns-resolve | 958.0 | +571.7% | +27.7% | +5.3% | +16.0% | +10.5% | +11.8% |
| registry-crud | 455.5 | +1132.2% | +1058.4% | +79.7% | +77.3% | +29.6% | +2123.1% |
| pipe-roundtrip | 146.0 | +1.4% | +20.2% | +8.2% | +8.9% | +1.0% | +5.1% |
| token-query | 55.5 | +0.9% | +55.0% | +9.9% | -0.9%* | -0.9%* | +0.9% |
| crypto-hash-verify | 249.0 | -1.0%* | +6.2% | -1.0%* | +6.8% | +3.4% | +2.8% |
| com-create-instance | 473.0 | +52.6% | +242.7% | +34.5% | +90.2% | +16.3% | +299.8% |
| wmi-query | 13995.5 | -19.2%* | -16.0%* | -33.0%* | +55.8%* | -35.2%* | -5.9%* |
| fs-watcher | 2205.0 | +86.0% | +779.0% | +124.6% | +75.2% | +59.6% | +817.2% |
| ripgrep-clean-build | 14992.5 | +885.4%* | +17.3% | +17.2% | +11.7% | +22.0% | +21.4% |
| ripgrep-incremental-build | 5886.5 | +3.7% | +10.2% | +0.9% | +3.2% | +4.2% | -0.7%* |
| roslyn-clean-build | 206134.5 | +11.3% | +16.3% | +21.8% | +6.6% | +25.0% | +20.1% |
| roslyn-incremental-build | 59259.5 | +5.3% | +7.4% | +5.2% | +7.9% | +17.7% | +16.3% |

## Cross-AV first-run comparison

Cells are slowdown vs baseline using the AV's earliest successful run against the baseline steady-state median.

| Scenario | baseline median (ms) | avira | bitdefender | eset | huorong | ms-defender | trendmicro |
|---|---|---|---|---|---|---|---|
| file-create-delete | 1475.5 | +71.9% | +328.9% | +95.1% | +63.1% | +50.1% | +758.1% |
| archive-extract | 13062.0 | +22.5% | +469.5% | +23.1% | +41.0% | +56.1% | +428.4% |
| file-enum-large-dir | 2401.5 | +60.1% | +752.5% | +38.7% | +82.9% | +116.5% | +630.9% |
| file-copy-large | 952.0 | -8.9%* | -8.0%* | -7.4%* | -13.6%* | -6.5%* | -9.5%* |
| hardlink-create | 1267.0 | +43.1% | +248.2% | +41.7% | +131.2% | +36.3% | +512.9% |
| junction-create | 602.0 | +54.7% | +164.0% | +16.6% | +42.2% | +19.1% | +236.7% |
| process-create-wait | 13405.0 | +37.1% | +132.4% | +45.2% | +36.9% | +19.1% | +86.4% |
| ext-sensitivity-exe | 3022.5 | +65.3% | +593.2% | +103.6% | +231.1% | +47.3% | +768.4% |
| ext-sensitivity-dll | 3028.5 | +69.3% | +633.9% | +99.0% | +262.3% | +43.7% | +1158.8% |
| ext-sensitivity-js | 3050.0 | +64.3% | +1178.2% | +147.4% | +57.7% | +43.0% | +1896.8% |
| ext-sensitivity-ps1 | 2981.5 | +69.7% | +3942.6% | +154.1% | +61.9% | +47.2% | +1237.7% |
| dll-load-unique | 8018.0 | +450.3% | +97.6% | +29.0% | +240.3% | +601.7% | +469.0% |
| file-write-content | 3300.5 | +58.3% | +473.6% | +2730.8% | +31646.5% | +41.0% | +2616.3% |
| new-exe-run | 1926.0 | +15540.0% | +456.2% | +192.3% | +34.3% | +181.6% | +1637.6% |
| new-exe-run-motw | 1930.5 | +437.0% | +316.6% | +56.6% | +33.2% | +4521.1% | failed* |
| thread-create | 1172.0 | +21.8% | +10.1% | +7.7% | +0.3% | +3.5% | +60.2% |
| mem-alloc-protect | 168.5 | -4.5%* | -1.5%* | +3.9% | -3.3%* | +19.9% | +132.6% |
| mem-map-file | 3677.5 | +203.3% | +236.4% | +55.6% | +10.6% | +139.2% | +275.7% |
| net-connect-loopback | 572.0 | +55.1% | +168.7% | +162.1% | +14.5% | +3.0% | +21.2% |
| net-dns-resolve | 958.0 | +574.3% | +30.7% | +7.2% | +18.3% | +7.9% | +13.7% |
| registry-crud | 455.5 | +1128.8% | +1062.0% | +80.2% | +78.0% | +29.3% | +2142.8% |
| pipe-roundtrip | 146.0 | -4.1%* | +21.2% | +11.6% | +6.8% | -12.3%* | 0.0% |
| token-query | 55.5 | -2.7%* | +55.0% | +9.9% | +0.9% | +0.9% | -0.9%* |
| crypto-hash-verify | 249.0 | -3.6%* | +8.0% | -0.8%* | +6.0% | +6.0% | +4.4% |
| com-create-instance | 473.0 | +53.5% | +242.5% | +35.3% | +90.3% | +16.7% | +292.0% |
| wmi-query | 13995.5 | -18.5%* | -13.3%* | -37.1%* | +52.6% | -36.5%* | -8.5%* |
| fs-watcher | 2205.0 | +86.2% | +781.9% | +123.9% | +75.3% | +57.2% | +834.6% |
| ripgrep-clean-build | 14992.5 | +756.7% | +17.7% | +17.1% | +12.7% | +22.1% | +31.1% |
| ripgrep-incremental-build | 5886.5 | +2.7% | +11.1% | +0.9% | +2.4% | +3.7% | +2.7% |
| roslyn-clean-build | 206134.5 | +132.7% | +13.3% | +27.6% | +2.6% | +26.5% | +23.3% |
| roslyn-incremental-build | 59259.5 | +175.0% | +11.2% | +2.9% | +9.5% | +17.4% | +16.9% |

`*` in the steady-state table marks a non-ok result (`failed`, `insufficient`, `noisy`, or `anomaly`).
First-run cells do not inherit `noisy` or `insufficient` markers because CV and steady-state sample count are not meaningful for a single first-run sample; `failed*` means no successful first run was available, and a negative first-run slowdown is marked as an anomaly.

