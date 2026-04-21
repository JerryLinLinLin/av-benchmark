# AV Benchmark Comparison Report

Generated: 2026-04-21 19:52:01 UTC

## 360ts (360 Total Security v9, 2, 0, 1031) vs baseline-os

| Scenario | Median Wall (ms) | First-Run Wall (ms) | Slowdown | First-Run Slowdown | p95 Slowdown | Disk Read Delta (MB) | Disk Write Delta (MB) | CV % | Baseline CV % | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| file-create-delete | 3320.0 | 3343.0 | +125.0% | +126.6% | +139.8% | +11.8 | +17.6 | 1.7% | 2.0% | ok |
| archive-extract | 23290.0 | 23551.0 | +78.3% | +80.3% | +78.3% | +42.1 | +17.5 | 0.5% | 2.2% | ok |
| file-enum-large-dir | 4400.5 | 4407.0 | +83.2% | +83.5% | -1.1% | +0.1 | -4.9 | 0.8% | 0.3% | ok |
| file-copy-large | 835.0 | 821.0 | -12.3% | -13.8% | -62.7% | -0.1 | -99.3 | 0.7% | 1.4% | anomaly |
| hardlink-create | 3231.0 | 3243.0 | +155.0% | +156.0% | +121.4% | -0.0 | -4.0 | 1.2% | 0.8% | ok |
| junction-create | 0.0 | - | 0.0% | 0.0% | - | +0.0 | +0.0 | 0.0% | 0.0% | failed |
| process-create-wait | 43021.0 | 43522.0 | +220.9% | +224.7% | +212.8% | +91.1 | +127.4 | 0.2% | 0.4% | ok |
| ext-sensitivity-exe | 6616.5 | 6513.0 | +118.9% | +115.5% | +127.6% | +14.7 | +32.8 | 1.2% | 1.0% | ok |
| ext-sensitivity-dll | 6551.5 | 6531.0 | +116.3% | +115.7% | +116.1% | +0.0 | +12.2 | 0.3% | 1.4% | ok |
| ext-sensitivity-js | 6578.5 | 6866.0 | +115.7% | +125.1% | +114.0% | +0.0 | +5.5 | 0.6% | 1.0% | ok |
| ext-sensitivity-ps1 | 7580.5 | 7635.0 | +154.3% | +156.1% | +209.5% | +0.1 | +4.1 | 1.0% | 1.0% | ok |
| dll-load-unique | 10497.0 | 10677.0 | +30.9% | +33.2% | +34.7% | +0.0 | +6.5 | 0.2% | 1.1% | ok |
| file-write-content | 6883.5 | 6949.0 | +108.6% | +110.5% | +113.0% | +0.0 | -2.1 | 0.4% | 0.5% | ok |
| new-exe-run | 9393.5 | 9424.0 | +387.7% | +389.3% | +384.7% | +3.7 | +29.0 | 0.4% | 1.0% | ok |
| new-exe-run-motw | 9381.5 | 9395.0 | +386.0% | +386.7% | +369.4% | +0.0 | +25.3 | 0.8% | 0.7% | ok |
| thread-create | 1214.0 | 1229.0 | +3.6% | +4.9% | -7.5% | -0.1 | +0.1 | 1.7% | 0.8% | ok |
| mem-alloc-protect | 160.5 | 161.0 | -4.7% | -4.5% | -41.8% | +0.0 | +0.0 | 2.3% | 1.5% | anomaly |
| mem-map-file | 5766.0 | 5767.0 | +56.8% | +56.8% | +43.3% | +1.8 | +1.2 | 1.7% | 7.2% | ok |
| net-connect-loopback | 698.5 | 708.0 | +22.1% | +23.8% | +18.9% | +33.7 | +0.0 | 2.7% | 1.2% | ok |
| net-dns-resolve | 1200.0 | 1163.0 | +25.3% | +21.4% | +26.4% | +0.0 | +0.8 | 1.2% | 1.4% | ok |
| registry-crud | 925.0 | 929.0 | +103.1% | +104.0% | +90.9% | -0.0 | +0.1 | 1.3% | 4.1% | ok |
| pipe-roundtrip | 130.5 | 132.0 | -10.6% | -9.6% | -13.4% | +0.0 | -0.0 | 6.6% | 4.4% | anomaly |
| token-query | 55.0 | 55.0 | -0.9% | -0.9% | -3.7% | +0.0 | +0.0 | 1.6% | 0.9% | anomaly |
| crypto-hash-verify | 265.5 | 251.0 | +6.6% | +0.8% | -1.5% | +0.0 | +0.0 | 4.7% | 1.0% | ok |
| com-create-instance | 1525.0 | 1532.0 | +222.4% | +223.9% | +155.7% | +0.1 | +0.0 | 0.4% | 1.8% | ok |
| wmi-query | 11939.5 | 13654.0 | -14.7% | -2.4% | -13.4% | +0.5 | +0.8 | 1.2% | 23.8% | anomaly |
| fs-watcher | 5006.0 | 5016.0 | +127.0% | +127.5% | +119.5% | -0.0 | +8.9 | 1.0% | 0.9% | ok |
| ripgrep-clean-build | 18204.0 | 18244.0 | +21.4% | +21.7% | - | +99.6 | +18.3 | 0.7% | 0.3% | ok |
| ripgrep-incremental-build | 6423.0 | 6394.0 | +9.1% | +8.6% | - | -0.0 | +5.6 | 0.5% | 0.5% | ok |
| roslyn-clean-build | 214137.5 | 205471.0 | +3.9% | -0.3% | - | -7357.8 | +64.0 | 3.0% | 2.0% | ok |
| roslyn-incremental-build | 61280.5 | 58586.0 | +3.4% | -1.1% | - | +38.8 | +40.2 | 2.1% | 3.2% | ok |

Highest slowdown: new-exe-run at +387.7%

Largest kernel CPU shift: roslyn-clean-build at +1.6pp (9.0% -> 10.5%)

Largest system disk write delta: process-create-wait at +127.4 MB (1.7 -> 129.1 MB)

Largest system disk read delta: roslyn-clean-build at -7357.8 MB (9876.2 -> 2518.4 MB)

Anomaly scenarios (AV appears faster - likely caching artifact): file-copy-large, mem-alloc-protect, pipe-roundtrip, token-query, wmi-query

Failed scenarios: junction-create

## avast (Avast Antivirus v21.4.6162.0) vs baseline-os

| Scenario | Median Wall (ms) | First-Run Wall (ms) | Slowdown | First-Run Slowdown | p95 Slowdown | Disk Read Delta (MB) | Disk Write Delta (MB) | CV % | Baseline CV % | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| file-create-delete | 1834.0 | 1826.0 | +24.3% | +23.8% | +24.2% | +0.3 | +8.2 | 0.9% | 2.0% | ok |
| archive-extract | 14716.0 | 14635.0 | +12.7% | +12.0% | +12.7% | +42.7 | +3.5 | 2.1% | 2.2% | ok |
| file-enum-large-dir | 2833.5 | 2942.0 | +18.0% | +22.5% | -0.3% | +0.2 | -1.1 | 1.0% | 0.3% | ok |
| file-copy-large | 894.0 | 905.0 | -6.1% | -4.9% | -8.0% | -0.1 | +0.3 | 2.6% | 1.4% | anomaly |
| hardlink-create | 1730.5 | 1746.0 | +36.6% | +37.8% | +29.5% | -0.0 | -9.2 | 0.0% | 0.8% | ok |
| junction-create | 993.0 | 1001.0 | +65.0% | +66.3% | +90.9% | +0.1 | -2.5 | 0.7% | 1.5% | ok |
| process-create-wait | 25139.0 | 25049.0 | +87.5% | +86.9% | +71.9% | +15.7 | +11.4 | 0.5% | 0.4% | ok |
| ext-sensitivity-exe | 4105.0 | 3997.0 | +35.8% | +32.2% | +36.5% | +3.1 | -7.1 | 1.1% | 1.0% | failed |
| ext-sensitivity-dll | 3825.5 | 3780.0 | +26.3% | +24.8% | +26.6% | +12.8 | +10.9 | 2.0% | 1.4% | ok |
| ext-sensitivity-js | 3605.5 | 3552.0 | +18.2% | +16.5% | +18.0% | +0.2 | -17.3 | 0.6% | 1.0% | ok |
| ext-sensitivity-ps1 | 3771.0 | 3770.0 | +26.5% | +26.4% | +26.4% | +0.0 | +1.8 | 0.6% | 1.0% | ok |
| dll-load-unique | 8473.5 | 8502.0 | +5.7% | +6.0% | +6.0% | -0.0 | +4.2 | 1.3% | 1.1% | ok |
| file-write-content | 4247.5 | 4178.0 | +28.7% | +26.6% | +28.7% | +0.0 | -6.6 | 0.4% | 0.5% | ok |
| new-exe-run | 6140.5 | 6046.0 | +218.8% | +213.9% | +238.9% | +0.4 | -0.1 | 1.7% | 1.0% | ok |
| new-exe-run-motw | 3256.5 | 3159.0 | +68.7% | +63.6% | +66.3% | +0.8 | +1.0 | 0.7% | 0.7% | ok |
| thread-create | 1490.5 | 1481.0 | +27.2% | +26.4% | +55.6% | +34.3 | +0.1 | 3.6% | 0.8% | ok |
| mem-alloc-protect | 193.5 | 186.0 | +14.8% | +10.4% | -28.2% | +0.0 | +0.0 | 1.3% | 1.5% | ok |
| mem-map-file | 4273.0 | 4306.0 | +16.2% | +17.1% | +12.1% | +0.1 | -0.1 | 1.5% | 7.2% | ok |
| net-connect-loopback | 1226.0 | 1210.0 | +114.3% | +111.5% | +111.6% | +27.9 | +0.1 | 2.2% | 1.2% | ok |
| net-dns-resolve | 1174.0 | 1147.0 | +22.5% | +19.7% | +19.7% | +17.3 | +0.4 | 2.4% | 1.4% | ok |
| registry-crud | 1329.0 | 1345.0 | +191.8% | +195.3% | +163.2% | +0.0 | +0.0 | 49.8% | 4.1% | noisy |
| pipe-roundtrip | 154.0 | 164.0 | +5.5% | +12.3% | -35.0% | +0.3 | -0.0 | 4.0% | 4.4% | ok |
| token-query | 83.0 | 82.0 | +49.5% | +47.7% | +44.4% | +0.0 | +0.0 | 1.3% | 0.9% | ok |
| crypto-hash-verify | 255.5 | 252.0 | +2.6% | +1.2% | -2.9% | -0.1 | +0.0 | 6.8% | 1.0% | ok |
| com-create-instance | 688.5 | 675.0 | +45.6% | +42.7% | +36.8% | +0.0 | +0.0 | 1.3% | 1.8% | ok |
| wmi-query | 9625.5 | 9054.0 | -31.2% | -35.3% | -32.6% | -0.2 | +0.1 | 7.6% | 23.8% | anomaly |
| fs-watcher | 2756.0 | 2728.0 | +25.0% | +23.7% | +20.9% | -0.0 | +3.8 | 0.8% | 0.9% | ok |
| ripgrep-clean-build | 19003.0 | 18843.0 | +26.8% | +25.7% | - | +121.8 | +10.7 | 0.9% | 0.3% | ok |
| ripgrep-incremental-build | 6026.5 | 6124.0 | +2.4% | +4.0% | - | +0.1 | -0.3 | 0.6% | 0.5% | ok |
| roslyn-clean-build | 211922.0 | 216716.0 | +2.8% | +5.1% | - | -2528.7 | +87.3 | 1.9% | 2.0% | ok |
| roslyn-incremental-build | 66704.5 | 67194.0 | +12.6% | +13.4% | - | +0.4 | +14.9 | 3.6% | 3.2% | ok |

Highest slowdown: new-exe-run at +218.8%

Largest kernel CPU shift: roslyn-incremental-build at +5.6pp (15.2% -> 20.8%)

Largest system disk read delta: roslyn-clean-build at -2528.7 MB (9876.2 -> 7347.6 MB)

Noisy scenarios: registry-crud

Anomaly scenarios (AV appears faster - likely caching artifact): file-copy-large, wmi-query

Failed scenarios: ext-sensitivity-exe

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

## drweb (Dr.Web Security Space v1.0.0.04150) vs baseline-os

| Scenario | Median Wall (ms) | First-Run Wall (ms) | Slowdown | First-Run Slowdown | p95 Slowdown | Disk Read Delta (MB) | Disk Write Delta (MB) | CV % | Baseline CV % | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| file-create-delete | 4419.5 | 5108.0 | +199.5% | +246.2% | +334.6% | +1.7 | +17.4 | 5.6% | 2.0% | ok |
| archive-extract | 16160.5 | 15556.0 | +23.7% | +19.1% | +14.4% | +61.9 | +12.9 | 1.8% | 2.2% | ok |
| file-enum-large-dir | 3297.5 | 3236.0 | +37.3% | +34.7% | +7.8% | +5.4 | -3.2 | 1.3% | 0.3% | ok |
| file-copy-large | 917.0 | 902.0 | -3.7% | -5.3% | -15.9% | -0.1 | +3.2 | 0.4% | 1.4% | anomaly |
| hardlink-create | 1938.0 | 1922.0 | +53.0% | +51.7% | +48.5% | -0.0 | -3.1 | 1.2% | 0.8% | ok |
| junction-create | 673.0 | 656.0 | +11.8% | +9.0% | +13.9% | -0.0 | -2.4 | 1.5% | 1.5% | ok |
| process-create-wait | 20120.5 | 19583.0 | +50.1% | +46.1% | +46.7% | +11.5 | -1.2 | 3.6% | 0.4% | ok |
| ext-sensitivity-exe | 14433.0 | 14738.0 | +377.5% | +387.6% | +533.7% | +0.2 | +23.2 | 10.1% | 1.0% | noisy |
| ext-sensitivity-dll | 14859.5 | 14129.0 | +390.7% | +366.5% | +558.7% | +0.9 | +9.3 | 2.4% | 1.4% | ok |
| ext-sensitivity-js | 14627.0 | 15482.0 | +379.6% | +407.6% | +520.2% | +0.6 | +6.8 | 2.2% | 1.0% | ok |
| ext-sensitivity-ps1 | 14883.0 | 14911.0 | +399.2% | +400.1% | +566.3% | +0.3 | -0.2 | 25.7% | 1.0% | noisy |
| dll-load-unique | 399853.0 | 398633.0 | +4886.9% | +4871.7% | +9632.2% | +110.9 | +35.1 | 47.9% | 1.1% | noisy |
| file-write-content | 364646.0 | 1704939.0 | +10948.2% | +51557.0% | +27941.3% | +170.0 | +141.0 | 106.9% | 0.5% | noisy |
| new-exe-run | 3356.0 | 19926.0 | +74.2% | +934.6% | +82.8% | +0.7 | +0.2 | 96.7% | 1.0% | noisy |
| new-exe-run-motw | 3390.5 | 20021.0 | +75.6% | +937.1% | +75.9% | +2.9 | +0.1 | 96.0% | 0.7% | noisy |
| thread-create | 1216.0 | 1204.0 | +3.8% | +2.7% | -5.1% | -0.1 | +0.0 | 0.9% | 0.8% | ok |
| mem-alloc-protect | 163.0 | 162.0 | -3.3% | -3.9% | -40.9% | +0.0 | +0.0 | 2.8% | 1.5% | anomaly |
| mem-map-file | 4329.0 | 4270.0 | +17.7% | +16.1% | +17.3% | +0.3 | +0.2 | 2.5% | 7.2% | ok |
| net-connect-loopback | 618.0 | 929.0 | +8.0% | +62.4% | +4.3% | +0.8 | +0.0 | 19.9% | 1.2% | noisy |
| net-dns-resolve | 1094.0 | 1107.0 | +14.2% | +15.6% | +15.2% | +0.4 | +0.0 | 1.6% | 1.4% | ok |
| registry-crud | 693.0 | 725.0 | +52.1% | +59.2% | +50.0% | -0.1 | +0.0 | 2.9% | 4.1% | ok |
| pipe-roundtrip | 169.0 | 441.0 | +15.8% | +202.1% | -4.8% | +0.1 | -0.0 | 57.7% | 4.4% | noisy |
| token-query | 56.0 | 56.0 | +0.9% | +0.9% | -3.7% | +0.0 | +0.0 | 0.8% | 0.9% | ok |
| crypto-hash-verify | 247.5 | 257.0 | -0.6% | +3.2% | +0.8% | -0.4 | +0.0 | 6.4% | 1.0% | anomaly |
| com-create-instance | 750.0 | 753.0 | +58.6% | +59.2% | +47.0% | +0.1 | +0.0 | 1.9% | 1.8% | ok |
| wmi-query | 12907.0 | 15047.0 | -7.8% | +7.5% | -10.6% | +2.1 | -0.1 | 19.2% | 23.8% | anomaly |
| fs-watcher | 5493.0 | 6016.0 | +149.1% | +172.8% | +246.9% | +0.1 | +6.8 | 3.6% | 0.9% | ok |
| ripgrep-clean-build | 17095.0 | 18260.0 | +14.0% | +21.8% | - | +185.8 | +1.0 | 2.6% | 0.3% | ok |
| ripgrep-incremental-build | 6154.5 | 6092.0 | +4.6% | +3.5% | - | +0.4 | +4.1 | 0.7% | 0.5% | ok |
| roslyn-clean-build | 283902.0 | 329191.0 | +37.7% | +59.7% | - | -1603.8 | +457.5 | 8.7% | 2.0% | ok |
| roslyn-incremental-build | 72486.0 | 69060.0 | +22.3% | +16.5% | - | +2237.1 | +71.3 | 0.9% | 3.2% | ok |

Highest slowdown: ext-sensitivity-dll at +390.7%

Largest kernel CPU shift: roslyn-incremental-build at +2.6pp (15.2% -> 17.8%)

Largest system disk write delta: roslyn-clean-build at +457.5 MB (30580.6 -> 31038.1 MB)

Largest system disk read delta: roslyn-incremental-build at +2237.1 MB (133.1 -> 2370.2 MB)

Noisy scenarios: dll-load-unique, ext-sensitivity-exe, ext-sensitivity-ps1, file-write-content, net-connect-loopback, new-exe-run, new-exe-run-motw, pipe-roundtrip

Anomaly scenarios (AV appears faster - likely caching artifact): crypto-hash-verify, file-copy-large, mem-alloc-protect, wmi-query

## emsisoft (Emsisoft Anti-Malware Home v2021.03.0.5617) vs baseline-os

| Scenario | Median Wall (ms) | First-Run Wall (ms) | Slowdown | First-Run Slowdown | p95 Slowdown | Disk Read Delta (MB) | Disk Write Delta (MB) | CV % | Baseline CV % | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| file-create-delete | 3925.0 | 3869.0 | +166.0% | +162.2% | +176.2% | +0.4 | +12.1 | 0.6% | 2.0% | ok |
| archive-extract | 28124.0 | 28603.0 | +115.3% | +119.0% | +107.9% | +30.5 | +15.5 | 0.5% | 2.2% | ok |
| file-enum-large-dir | 5554.0 | 5600.0 | +131.3% | +133.2% | +1.3% | +0.7 | -1.9 | 0.5% | 0.3% | ok |
| file-copy-large | 827.5 | 844.0 | -13.1% | -11.3% | -63.4% | -0.1 | -100.0 | 0.4% | 1.4% | anomaly |
| hardlink-create | 3539.0 | 3576.0 | +179.3% | +182.2% | +204.9% | +0.0 | -2.4 | 0.4% | 0.8% | ok |
| junction-create | 1274.0 | 1278.0 | +111.6% | +112.3% | +162.6% | +0.0 | -2.5 | 0.6% | 1.5% | ok |
| process-create-wait | 25837.5 | 25659.0 | +92.7% | +91.4% | +89.2% | +2.1 | +87.7 | 0.2% | 0.4% | ok |
| ext-sensitivity-exe | 7816.5 | 7594.0 | +158.6% | +151.2% | +162.9% | +0.2 | +27.6 | 0.5% | 1.0% | ok |
| ext-sensitivity-dll | 7806.5 | 7786.0 | +157.8% | +157.1% | +156.3% | +3.0 | +5.1 | 0.5% | 1.4% | ok |
| ext-sensitivity-js | 7740.5 | 7634.0 | +153.8% | +150.3% | +154.4% | +4.2 | +3.3 | 0.6% | 1.0% | ok |
| ext-sensitivity-ps1 | 7704.0 | 7736.0 | +158.4% | +159.5% | +167.7% | +39.7 | +3.0 | 1.1% | 1.0% | ok |
| dll-load-unique | 48436.0 | 48395.0 | +504.1% | +503.6% | +457.7% | +204.4 | +10.5 | 0.4% | 1.1% | ok |
| file-write-content | 8111.5 | 8519.0 | +145.8% | +158.1% | +142.8% | +0.1 | -2.1 | 0.3% | 0.5% | ok |
| new-exe-run | 5581.0 | 5767.0 | +189.8% | +199.4% | +188.7% | +3.2 | +0.2 | 0.3% | 1.0% | ok |
| new-exe-run-motw | 5620.0 | 5741.0 | +191.1% | +197.4% | +187.5% | -0.0 | -0.0 | 0.4% | 0.7% | ok |
| thread-create | 1269.0 | 1353.0 | +8.3% | +15.4% | -1.5% | +0.0 | +0.0 | 0.8% | 0.8% | ok |
| mem-alloc-protect | 226.5 | 224.0 | +34.4% | +32.9% | +24.5% | +0.0 | +0.0 | 2.0% | 1.5% | ok |
| mem-map-file | 5909.0 | 5862.0 | +60.7% | +59.4% | +51.1% | +0.0 | -0.1 | 0.6% | 7.2% | ok |
| net-connect-loopback | 1589.0 | 1475.0 | +177.8% | +157.9% | +136.4% | +0.6 | +0.0 | 6.3% | 1.2% | ok |
| net-dns-resolve | 1640.0 | 1626.0 | +71.2% | +69.7% | +66.6% | +0.3 | +0.0 | 1.4% | 1.4% | ok |
| registry-crud | 12576.0 | 12593.0 | +2660.9% | +2664.7% | +2414.7% | -0.0 | +8.9 | 1.3% | 4.1% | ok |
| pipe-roundtrip | 182.5 | 184.0 | +25.0% | +26.0% | -28.4% | +0.2 | -0.0 | 3.5% | 4.4% | ok |
| token-query | 114.0 | 113.0 | +105.4% | +103.6% | +70.4% | +0.1 | +0.0 | 4.4% | 0.9% | ok |
| crypto-hash-verify | 255.5 | 252.0 | +2.6% | +1.2% | +1.7% | -0.2 | -0.0 | 10.9% | 1.0% | noisy |
| com-create-instance | 1257.5 | 1245.0 | +165.9% | +163.2% | +115.6% | +0.1 | +0.0 | 0.9% | 1.8% | ok |
| wmi-query | 7566.5 | 7441.0 | -45.9% | -46.8% | -45.5% | -0.4 | -0.2 | 1.9% | 23.8% | anomaly |
| fs-watcher | 5625.5 | 5632.0 | +155.1% | +155.4% | +171.0% | +0.1 | +3.6 | 0.6% | 0.9% | ok |
| ripgrep-clean-build | 17188.5 | 17117.0 | +14.6% | +14.2% | - | +106.2 | +3.0 | 1.2% | 0.3% | ok |
| ripgrep-incremental-build | 6220.5 | 6259.0 | +5.7% | +6.3% | - | -0.0 | +2.3 | 0.3% | 0.5% | ok |
| roslyn-clean-build | 234333.5 | 239425.0 | +13.7% | +16.1% | - | -6750.5 | +91.3 | 5.0% | 2.0% | ok |
| roslyn-incremental-build | 73322.5 | 73520.0 | +23.7% | +24.1% | - | -6.5 | +41.9 | 3.4% | 3.2% | ok |

Highest slowdown: registry-crud at +2660.9%

Largest kernel CPU shift: roslyn-incremental-build at +2.3pp (15.2% -> 17.5%)

Largest system disk write delta: file-copy-large at -100.0 MB (100.1 -> 0.1 MB)

Largest system disk read delta: roslyn-clean-build at -6750.5 MB (9876.2 -> 3125.7 MB)

Noisy scenarios: crypto-hash-verify

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

## gdata (G DATA INTERNET SECURITY v22.0.0.0) vs baseline-os

| Scenario | Median Wall (ms) | First-Run Wall (ms) | Slowdown | First-Run Slowdown | p95 Slowdown | Disk Read Delta (MB) | Disk Write Delta (MB) | CV % | Baseline CV % | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| file-create-delete | 4169.5 | 4166.0 | +182.6% | +182.3% | +272.3% | +1.3 | +11.7 | 8.2% | 2.0% | ok |
| archive-extract | 33311.0 | 33020.0 | +155.0% | +152.8% | +190.6% | +41.6 | +25.0 | 1.1% | 2.2% | ok |
| file-enum-large-dir | 6854.5 | 6803.0 | +185.4% | +183.3% | +44.7% | -0.1 | -5.5 | 1.5% | 0.3% | ok |
| file-copy-large | 2415.5 | 2570.0 | +153.7% | +170.0% | +189.0% | +1.7 | -94.0 | 3.6% | 1.4% | ok |
| hardlink-create | 3994.0 | 4000.0 | +215.2% | +215.7% | +212.7% | -0.0 | -4.4 | 0.6% | 0.8% | ok |
| junction-create | 1302.0 | 1308.0 | +116.3% | +117.3% | +129.7% | -0.0 | +5.2 | 1.8% | 1.5% | ok |
| process-create-wait | 18436.5 | 18236.0 | +37.5% | +36.0% | +39.7% | +87.5 | +70.0 | 0.2% | 0.4% | ok |
| ext-sensitivity-exe | 8406.5 | 8262.0 | +178.1% | +173.3% | +257.3% | +17.5 | +22.6 | 0.7% | 1.0% | ok |
| ext-sensitivity-dll | 8265.5 | 8228.0 | +172.9% | +171.7% | +234.1% | +3.8 | +10.4 | 0.7% | 1.4% | ok |
| ext-sensitivity-js | 8340.0 | 8542.0 | +173.4% | +180.1% | +246.7% | +26.1 | +6.2 | 1.1% | 1.0% | ok |
| ext-sensitivity-ps1 | 8392.5 | 8513.0 | +181.5% | +185.5% | +255.1% | +5.7 | +4.4 | 0.8% | 1.0% | ok |
| dll-load-unique | 46660.5 | 46318.0 | +481.9% | +477.7% | +474.6% | +155.1 | +7.9 | 1.1% | 1.1% | ok |
| file-write-content | 40419.5 | 43625.0 | +1124.6% | +1221.8% | +4455.6% | -0.0 | +5.8 | 42.6% | 0.5% | noisy |
| new-exe-run | 3949.0 | 3951.0 | +105.0% | +105.1% | +117.2% | +2.9 | +0.1 | 2.9% | 1.0% | ok |
| new-exe-run-motw | 3838.5 | 3921.0 | +98.8% | +103.1% | +101.0% | +0.0 | +0.0 | 1.1% | 0.7% | ok |
| thread-create | 1310.0 | 1271.0 | +11.8% | +8.4% | +11.6% | -0.1 | +0.0 | 2.6% | 0.8% | ok |
| mem-alloc-protect | 160.0 | 163.0 | -5.0% | -3.3% | -34.5% | +0.0 | +0.0 | 1.0% | 1.5% | anomaly |
| mem-map-file | 7046.0 | 7040.0 | +91.6% | +91.4% | +85.8% | +18.2 | +2.4 | 2.1% | 7.2% | ok |
| net-connect-loopback | 634.5 | 592.0 | +10.9% | +3.5% | +24.4% | +11.2 | +0.2 | 8.1% | 1.2% | ok |
| net-dns-resolve | 945.0 | 957.0 | -1.4% | -0.1% | +0.7% | +6.5 | +2.8 | 1.6% | 1.4% | anomaly |
| registry-crud | 672.0 | 690.0 | +47.5% | +51.5% | +46.5% | +11.4 | +1.1 | 4.8% | 4.1% | ok |
| pipe-roundtrip | 171.5 | 158.0 | +17.5% | +8.2% | -13.1% | +0.8 | -0.0 | 2.9% | 4.4% | ok |
| token-query | 54.5 | 55.0 | -1.8% | -0.9% | -3.7% | +0.2 | +0.0 | 3.3% | 0.9% | anomaly |
| crypto-hash-verify | 249.0 | 222.0 | 0.0% | -10.8% | -1.8% | +4.0 | +0.0 | 2.9% | 1.0% | ok |
| com-create-instance | 1857.0 | 2050.0 | +292.6% | +333.4% | +308.4% | +13.4 | +5.0 | 4.7% | 1.8% | ok |
| wmi-query | 8649.5 | 7785.0 | -38.2% | -44.4% | -36.2% | +142.2 | +15.6 | 15.3% | 23.8% | anomaly |
| fs-watcher | 8988.0 | 7021.0 | +307.6% | +218.4% | +364.8% | +132.2 | +16.7 | 9.8% | 0.9% | ok |
| ripgrep-clean-build | 19199.5 | 19045.0 | +28.1% | +27.0% | - | +360.9 | +41.8 | 2.4% | 0.3% | ok |
| ripgrep-incremental-build | 6777.0 | 6817.0 | +15.1% | +15.8% | - | +68.5 | +5.3 | 0.9% | 0.5% | ok |
| roslyn-clean-build | 240329.0 | 238645.0 | +16.6% | +15.8% | - | +4541.7 | +615.7 | 1.2% | 2.0% | ok |
| roslyn-incremental-build | 69690.5 | 71399.0 | +17.6% | +20.5% | - | +600.8 | +61.1 | 1.1% | 3.2% | ok |

Highest slowdown: dll-load-unique at +481.9%

Largest kernel CPU shift: roslyn-incremental-build at +3.8pp (15.2% -> 19.0%)

Largest system disk write delta: roslyn-clean-build at +615.7 MB (30580.6 -> 31196.3 MB)

Largest system disk read delta: roslyn-clean-build at +4541.7 MB (9876.2 -> 14417.9 MB)

Noisy scenarios: file-write-content

Anomaly scenarios (AV appears faster - likely caching artifact): mem-alloc-protect, net-dns-resolve, token-query, wmi-query

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

## kaspersky (Kaspersky v21.0.0.1) vs baseline-os

| Scenario | Median Wall (ms) | First-Run Wall (ms) | Slowdown | First-Run Slowdown | p95 Slowdown | Disk Read Delta (MB) | Disk Write Delta (MB) | CV % | Baseline CV % | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| file-create-delete | 2258.5 | 2263.0 | +53.1% | +53.4% | +93.1% | +0.4 | +17.6 | 1.1% | 2.0% | ok |
| archive-extract | 15159.0 | 14790.0 | +16.1% | +13.2% | +45.4% | +29.9 | +8.0 | 2.1% | 2.2% | ok |
| file-enum-large-dir | 2782.5 | 2764.0 | +15.9% | +15.1% | +8.6% | -0.1 | +9.8 | 1.4% | 0.3% | ok |
| file-copy-large | 837.0 | 878.0 | -12.1% | -7.8% | -60.5% | -0.0 | -100.1 | 0.4% | 1.4% | anomaly |
| hardlink-create | 3565.0 | 3584.0 | +181.4% | +182.9% | +190.6% | -0.0 | +0.1 | 0.7% | 0.8% | ok |
| junction-create | 1010.0 | 1010.0 | +67.8% | +67.8% | +92.2% | +0.1 | +0.8 | 1.6% | 1.5% | ok |
| process-create-wait | 27697.5 | 27574.0 | +106.6% | +105.7% | +103.4% | +0.7 | +98.7 | 0.8% | 0.4% | ok |
| ext-sensitivity-exe | 3642.0 | 3602.0 | +20.5% | +19.2% | +25.7% | +3.9 | +22.0 | 1.0% | 1.0% | ok |
| ext-sensitivity-dll | 3640.5 | 3670.0 | +20.2% | +21.2% | +22.6% | +0.1 | -2.5 | 0.9% | 1.4% | ok |
| ext-sensitivity-js | 3865.0 | 3808.0 | +26.7% | +24.9% | +28.7% | +7.6 | -6.6 | 0.6% | 1.0% | ok |
| ext-sensitivity-ps1 | 3861.0 | 3788.0 | +29.5% | +27.1% | +32.4% | +23.2 | -13.5 | 0.9% | 1.0% | ok |
| dll-load-unique | 56629.5 | 56773.0 | +606.3% | +608.1% | +547.5% | +31.1 | +23.9 | 0.8% | 1.1% | ok |
| file-write-content | 4922.5 | 4874.0 | +49.1% | +47.7% | +78.1% | -0.0 | +10.2 | 1.8% | 0.5% | ok |
| new-exe-run | 18564.5 | 18667.0 | +863.9% | +869.2% | +879.8% | +3.6 | +4.2 | 1.0% | 1.0% | ok |
| new-exe-run-motw | 5189.5 | 5096.0 | +168.8% | +164.0% | +166.4% | -0.1 | +1.0 | 1.4% | 0.7% | ok |
| thread-create | 2940.5 | 2859.0 | +150.9% | +143.9% | +148.4% | -0.1 | +0.0 | 3.3% | 0.8% | ok |
| mem-alloc-protect | 161.5 | 163.0 | -4.2% | -3.3% | -39.1% | +0.0 | +0.0 | 1.8% | 1.5% | anomaly |
| mem-map-file | 5003.5 | 5076.0 | +36.1% | +38.0% | +32.2% | +0.0 | -3.4 | 1.7% | 7.2% | ok |
| net-connect-loopback | 861.5 | 866.0 | +50.6% | +51.4% | +43.6% | +0.8 | +0.0 | 0.9% | 1.2% | ok |
| net-dns-resolve | 1102.5 | 1128.0 | +15.1% | +17.7% | +14.1% | +0.4 | +0.9 | 1.2% | 1.4% | ok |
| registry-crud | 744.5 | 789.0 | +63.4% | +73.2% | +58.1% | -0.1 | +5.1 | 3.9% | 4.1% | ok |
| pipe-roundtrip | 152.5 | 139.0 | +4.5% | -4.8% | -26.0% | +0.1 | -0.0 | 7.6% | 4.4% | ok |
| token-query | 56.5 | 58.0 | +1.8% | +4.5% | -3.7% | +0.0 | +0.0 | 1.5% | 0.9% | ok |
| crypto-hash-verify | 265.5 | 262.0 | +6.6% | +5.2% | -0.9% | -0.4 | -0.0 | 7.4% | 1.0% | ok |
| com-create-instance | 547.5 | 562.0 | +15.8% | +18.8% | +14.2% | -0.1 | +0.0 | 3.3% | 1.8% | ok |
| wmi-query | 7946.5 | 8731.0 | -43.2% | -37.6% | -42.1% | +3.0 | +0.1 | 12.3% | 23.8% | anomaly |
| fs-watcher | 2704.0 | 2686.0 | +22.6% | +21.8% | +24.7% | +1.8 | +11.2 | 1.8% | 0.9% | ok |
| ripgrep-clean-build | 17125.0 | 17053.0 | +14.2% | +13.7% | - | +141.4 | -6.1 | 3.0% | 0.3% | ok |
| ripgrep-incremental-build | 6183.0 | 6103.0 | +5.0% | +3.7% | - | +0.5 | +5.7 | 1.2% | 0.5% | ok |
| roslyn-clean-build | 243186.0 | 274613.0 | +18.0% | +33.2% | - | -4833.7 | +110.7 | 5.3% | 2.0% | ok |
| roslyn-incremental-build | 66107.5 | 70954.0 | +11.6% | +19.7% | - | +1104.2 | +47.7 | 2.2% | 3.2% | ok |

Highest slowdown: new-exe-run at +863.9%

Largest kernel CPU shift: roslyn-clean-build at +1.2pp (9.0% -> 10.1%)

Largest system disk write delta: roslyn-clean-build at +110.7 MB (30580.6 -> 30691.2 MB)

Largest system disk read delta: roslyn-clean-build at -4833.7 MB (9876.2 -> 5042.5 MB)

Anomaly scenarios (AV appears faster - likely caching artifact): file-copy-large, mem-alloc-protect, wmi-query

## malwarebytes (Malwarebytes v3.1.0.246) vs baseline-os

| Scenario | Median Wall (ms) | First-Run Wall (ms) | Slowdown | First-Run Slowdown | p95 Slowdown | Disk Read Delta (MB) | Disk Write Delta (MB) | CV % | Baseline CV % | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| file-create-delete | 5244.0 | 5209.0 | +255.4% | +253.0% | +402.0% | +6.5 | +14.8 | 2.0% | 2.0% | ok |
| archive-extract | 40760.0 | 40437.0 | +212.1% | +209.6% | +198.1% | +157.9 | +534.6 | 0.8% | 2.2% | failed |
| file-enum-large-dir | 8968.5 | 9989.0 | +273.5% | +315.9% | +940.7% | +23.7 | +39.5 | 1.4% | 0.3% | ok |
| file-copy-large | 844.0 | 852.0 | -11.3% | -10.5% | -61.0% | +1.7 | -97.1 | 4.4% | 1.4% | anomaly |
| hardlink-create | 2598.0 | 2625.0 | +105.1% | +107.2% | +91.6% | +12.7 | -8.6 | 1.2% | 0.8% | ok |
| junction-create | 1082.5 | 1067.0 | +79.8% | +77.2% | +96.3% | +4.3 | +4.4 | 4.6% | 1.5% | ok |
| process-create-wait | 22529.0 | 22277.0 | +68.1% | +66.2% | +67.6% | +70.3 | +93.4 | 0.9% | 0.4% | ok |
| ext-sensitivity-exe | 16432.5 | 16172.0 | +443.7% | +435.1% | +534.3% | +53.2 | +179.7 | 1.4% | 1.0% | ok |
| ext-sensitivity-dll | 16010.5 | 15978.0 | +428.7% | +427.6% | +522.0% | +63.8 | +175.5 | 1.2% | 1.4% | ok |
| ext-sensitivity-js | 20885.0 | 21087.0 | +584.8% | +591.4% | +665.9% | +133.5 | +175.5 | 0.5% | 1.0% | ok |
| ext-sensitivity-ps1 | 15988.5 | 16161.0 | +436.3% | +442.0% | +524.9% | +79.9 | +166.8 | 0.5% | 1.0% | ok |
| dll-load-unique | 132613.5 | 132490.0 | +1553.9% | +1552.4% | +1398.5% | +515.4 | +33.2 | 0.3% | 1.1% | ok |
| file-write-content | 19539.5 | 19787.0 | +492.0% | +499.5% | +571.2% | +271.9 | +194.8 | 0.8% | 0.5% | ok |
| new-exe-run | 14918.5 | 15617.0 | +674.6% | +710.9% | +662.4% | +53.6 | +13.1 | 1.3% | 1.0% | ok |
| new-exe-run-motw | 5120.0 | 4995.0 | +165.2% | +158.7% | +178.0% | +18.7 | +9.8 | 1.6% | 0.7% | ok |
| thread-create | 1805.5 | 1852.0 | +54.1% | +58.0% | +65.4% | +12.1 | +4.4 | 3.6% | 0.8% | ok |
| mem-alloc-protect | 164.0 | 165.0 | -2.7% | -2.1% | -41.8% | +0.0 | +0.0 | 0.8% | 1.5% | anomaly |
| mem-map-file | 8142.5 | 8084.0 | +121.4% | +119.8% | +110.3% | +45.0 | +19.2 | 2.9% | 7.2% | ok |
| net-connect-loopback | 745.0 | 727.0 | +30.2% | +27.1% | +20.1% | +1.5 | +1.0 | 1.2% | 1.2% | ok |
| net-dns-resolve | 1418.0 | 1407.0 | +48.0% | +46.9% | +46.9% | +3.9 | +1.5 | 1.9% | 1.4% | ok |
| registry-crud | 1058.5 | 1075.0 | +132.4% | +136.0% | +107.0% | +3.6 | +3.7 | 2.4% | 4.1% | ok |
| pipe-roundtrip | 153.5 | 151.0 | +5.1% | +3.4% | -41.6% | +0.4 | -0.0 | 3.5% | 4.4% | ok |
| token-query | 56.0 | 56.0 | +0.9% | +0.9% | +3.7% | +0.0 | +0.0 | 2.3% | 0.9% | ok |
| crypto-hash-verify | 249.0 | 258.0 | 0.0% | +3.6% | -2.0% | +0.3 | +1.0 | 2.7% | 1.0% | ok |
| com-create-instance | 873.0 | 869.0 | +84.6% | +83.7% | +61.1% | +1.3 | +1.0 | 1.5% | 1.8% | ok |
| wmi-query | 7777.5 | 9831.0 | -44.4% | -29.8% | -39.9% | +12.2 | +0.5 | 19.4% | 23.8% | anomaly |
| fs-watcher | 6482.0 | 6416.0 | +194.0% | +191.0% | +243.6% | +16.2 | +13.4 | 1.4% | 0.9% | ok |
| ripgrep-clean-build | 16585.0 | 17105.0 | +10.6% | +14.1% | - | +154.5 | +3.6 | 0.4% | 0.3% | ok |
| ripgrep-incremental-build | 6050.5 | 6083.0 | +2.8% | +3.3% | - | +9.7 | +6.8 | 0.2% | 0.5% | ok |
| roslyn-clean-build | 227739.5 | 223921.0 | +10.5% | +8.6% | - | -6751.6 | +50.6 | 0.9% | 2.0% | ok |
| roslyn-incremental-build | 62252.0 | 61500.0 | +5.0% | +3.8% | - | +122.4 | +22.6 | 3.2% | 3.2% | ok |

Highest slowdown: dll-load-unique at +1553.9%

Largest kernel CPU shift: roslyn-clean-build at +1.1pp (9.0% -> 10.1%)

Largest system disk write delta: archive-extract at +534.6 MB (251.2 -> 785.8 MB)

Largest system disk read delta: roslyn-clean-build at -6751.6 MB (9876.2 -> 3124.7 MB)

Anomaly scenarios (AV appears faster - likely caching artifact): file-copy-large, mem-alloc-protect, wmi-query

Failed scenarios: archive-extract

## mcafee (McAfee v16.100.0 0) vs baseline-os

| Scenario | Median Wall (ms) | First-Run Wall (ms) | Slowdown | First-Run Slowdown | p95 Slowdown | Disk Read Delta (MB) | Disk Write Delta (MB) | CV % | Baseline CV % | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| file-create-delete | 1572.5 | 1584.0 | +6.6% | +7.4% | +9.2% | -0.1 | +6.7 | 0.6% | 2.0% | ok |
| archive-extract | 13493.5 | 13392.0 | +3.3% | +2.5% | +3.5% | +45.1 | +17.2 | 1.5% | 2.2% | ok |
| file-enum-large-dir | 2431.5 | 2516.0 | +1.2% | +4.8% | +1.8% | -0.0 | -4.4 | 1.9% | 0.3% | ok |
| file-copy-large | 813.0 | 801.0 | -14.6% | -15.9% | -60.3% | -0.1 | -100.0 | 2.7% | 1.4% | anomaly |
| hardlink-create | 1502.0 | 1497.0 | +18.5% | +18.2% | +22.1% | -0.0 | -3.4 | 1.4% | 0.8% | ok |
| junction-create | 607.0 | 610.0 | +0.8% | +1.3% | -0.9% | -0.0 | -2.5 | 1.4% | 1.5% | ok |
| process-create-wait | 22097.0 | 21288.0 | +64.8% | +58.8% | +49.0% | +24.6 | +125.3 | 1.2% | 0.4% | ok |
| ext-sensitivity-exe | 3110.0 | 3181.0 | +2.9% | +5.2% | +5.5% | -0.0 | +5.0 | 2.8% | 1.0% | ok |
| ext-sensitivity-dll | 3120.0 | 3152.0 | +3.0% | +4.1% | +3.9% | +0.0 | +9.2 | 2.6% | 1.4% | ok |
| ext-sensitivity-js | 3115.0 | 3074.0 | +2.1% | +0.8% | +8.7% | +17.0 | -6.8 | 2.8% | 1.0% | ok |
| ext-sensitivity-ps1 | 3129.5 | 2997.0 | +5.0% | +0.5% | +7.5% | +0.0 | +2.4 | 3.8% | 1.0% | ok |
| dll-load-unique | 48067.5 | 48343.0 | +499.5% | +502.9% | +470.8% | +62.9 | +60.5 | 0.4% | 1.1% | ok |
| file-write-content | 3352.0 | 3336.0 | +1.6% | +1.1% | +0.3% | -0.0 | -5.0 | 1.4% | 0.5% | ok |
| new-exe-run | 55190.5 | 56003.0 | +2765.6% | +2807.7% | +2700.6% | +1.9 | +11.4 | 1.6% | 1.0% | ok |
| new-exe-run-motw | 48049.0 | 46143.0 | +2388.9% | +2290.2% | +2401.2% | +35.8 | +22.2 | 1.3% | 0.7% | ok |
| thread-create | 1220.5 | 1225.0 | +4.1% | +4.5% | +23.5% | -0.1 | +0.0 | 1.2% | 0.8% | ok |
| mem-alloc-protect | 156.5 | 156.0 | -7.1% | -7.4% | -42.7% | +0.0 | +0.0 | 2.3% | 1.5% | anomaly |
| mem-map-file | 3955.0 | 3839.0 | +7.5% | +4.4% | +12.9% | -0.0 | +1.6 | 0.7% | 7.2% | ok |
| net-connect-loopback | 638.5 | 653.0 | +11.6% | +14.2% | +12.6% | +1.0 | +0.2 | 2.8% | 1.2% | ok |
| net-dns-resolve | 1030.5 | 957.0 | +7.6% | -0.1% | +5.5% | +0.5 | +0.1 | 2.3% | 1.4% | ok |
| registry-crud | 554.0 | 520.0 | +21.6% | +14.2% | +19.2% | -0.1 | +0.0 | 1.0% | 4.1% | ok |
| pipe-roundtrip | 145.0 | 172.0 | -0.7% | +17.8% | -44.0% | +0.2 | +0.0 | 9.8% | 4.4% | anomaly |
| token-query | 56.0 | 53.0 | +0.9% | -4.5% | 0.0% | +0.0 | +0.0 | 1.3% | 0.9% | ok |
| crypto-hash-verify | 267.0 | 238.0 | +7.2% | -4.4% | -2.5% | -0.3 | +0.0 | 7.8% | 1.0% | ok |
| com-create-instance | 570.0 | 499.0 | +20.5% | +5.5% | +9.5% | +0.1 | +0.0 | 0.7% | 1.8% | ok |
| wmi-query | 7589.5 | 6213.0 | -45.8% | -55.6% | -43.9% | +1.0 | +1.1 | 16.9% | 23.8% | anomaly |
| fs-watcher | 2365.0 | 2406.0 | +7.3% | +9.1% | +6.4% | -0.0 | -8.3 | 0.9% | 0.9% | ok |
| ripgrep-clean-build | 41367.0 | 54218.0 | +175.9% | +261.6% | - | +173.1 | +33.6 | 14.0% | 0.3% | noisy |
| ripgrep-incremental-build | 5972.5 | 5979.0 | +1.5% | +1.6% | - | -0.0 | +5.9 | 2.6% | 0.5% | ok |
| roslyn-clean-build | 218822.5 | 214531.0 | +6.2% | +4.1% | - | -6992.0 | +43.3 | 3.2% | 2.0% | ok |
| roslyn-incremental-build | 60034.0 | 60289.0 | +1.3% | +1.7% | - | -3.7 | -4.2 | 2.1% | 3.2% | ok |

Highest slowdown: new-exe-run at +2765.6%

Largest system disk write delta: process-create-wait at +125.3 MB (1.7 -> 127.0 MB)

Largest system disk read delta: roslyn-clean-build at -6992.0 MB (9876.2 -> 2884.3 MB)

Noisy scenarios: ripgrep-clean-build

Anomaly scenarios (AV appears faster - likely caching artifact): file-copy-large, mem-alloc-protect, pipe-roundtrip, wmi-query

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

## sophos (Sophos Home v4.0.0) vs baseline-os

| Scenario | Median Wall (ms) | First-Run Wall (ms) | Slowdown | First-Run Slowdown | p95 Slowdown | Disk Read Delta (MB) | Disk Write Delta (MB) | CV % | Baseline CV % | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| file-create-delete | 4396.5 | 4253.0 | +198.0% | +188.2% | +231.6% | -0.1 | +11.3 | 2.2% | 2.0% | ok |
| archive-extract | 31027.0 | 30742.0 | +137.5% | +135.4% | +131.3% | +29.7 | +32.2 | 1.8% | 2.2% | ok |
| file-enum-large-dir | 9552.5 | 9620.0 | +297.8% | +300.6% | +6.3% | +0.1 | +3.7 | 0.3% | 0.3% | ok |
| file-copy-large | 848.5 | 847.0 | -10.9% | -11.0% | -58.2% | -0.1 | -99.8 | 1.2% | 1.4% | anomaly |
| hardlink-create | 3193.5 | 3190.0 | +152.1% | +151.8% | +127.4% | -0.0 | -3.0 | 0.2% | 0.8% | ok |
| junction-create | 955.5 | 960.0 | +58.7% | +59.5% | +49.3% | -0.0 | -2.5 | 1.2% | 1.5% | ok |
| process-create-wait | 27790.0 | 28552.0 | +107.3% | +113.0% | +113.4% | +37.2 | +112.3 | 0.2% | 0.4% | ok |
| ext-sensitivity-exe | 10226.0 | 10298.0 | +238.3% | +240.7% | +325.2% | +34.1 | +30.8 | 0.6% | 1.0% | ok |
| ext-sensitivity-dll | 10121.0 | 10322.0 | +234.2% | +240.8% | +319.8% | +0.0 | +19.4 | 0.2% | 1.4% | ok |
| ext-sensitivity-js | 10324.5 | 10518.0 | +238.5% | +244.9% | +318.6% | +0.2 | +10.9 | 0.8% | 1.0% | ok |
| ext-sensitivity-ps1 | 10398.0 | 10538.0 | +248.8% | +253.4% | +336.2% | +0.0 | +19.1 | 0.6% | 1.0% | ok |
| dll-load-unique | 66770.0 | 66224.0 | +732.8% | +725.9% | +679.2% | +103.4 | +7.3 | 0.4% | 1.1% | ok |
| file-write-content | 11462.5 | 11610.0 | +247.3% | +251.8% | +359.1% | -0.0 | +11.7 | 0.8% | 0.5% | ok |
| new-exe-run | 9656.5 | 9941.0 | +401.4% | +416.1% | +420.1% | +7.9 | +0.3 | 3.0% | 1.0% | ok |
| new-exe-run-motw | 7035.5 | 7065.0 | +264.4% | +266.0% | +283.8% | +5.6 | +0.3 | 0.6% | 0.7% | ok |
| thread-create | 1431.0 | 1444.0 | +22.1% | +23.2% | +35.5% | -0.1 | +0.0 | 1.6% | 0.8% | ok |
| mem-alloc-protect | 252.0 | 263.0 | +49.6% | +56.1% | -9.1% | +0.0 | +0.0 | 1.5% | 1.5% | ok |
| mem-map-file | 8017.5 | 8354.0 | +118.0% | +127.2% | +117.6% | -0.1 | +0.7 | 4.0% | 7.2% | ok |
| net-connect-loopback | 1130.0 | 1128.0 | +97.6% | +97.2% | +118.8% | +0.7 | +0.0 | 4.7% | 1.2% | ok |
| net-dns-resolve | 1399.0 | 1405.0 | +46.0% | +46.7% | +43.2% | +0.4 | +0.0 | 0.7% | 1.4% | ok |
| registry-crud | 6395.5 | 6399.0 | +1304.1% | +1304.8% | +1409.7% | -0.1 | +10.1 | 1.8% | 4.1% | ok |
| pipe-roundtrip | 182.0 | 173.0 | +24.7% | +18.5% | +1.9% | +0.1 | -0.0 | 4.2% | 4.4% | ok |
| token-query | 56.0 | 56.0 | +0.9% | +0.9% | -3.7% | +0.0 | +0.0 | 0.8% | 0.9% | ok |
| crypto-hash-verify | 249.5 | 241.0 | +0.2% | -3.2% | -2.0% | -0.4 | -0.0 | 4.2% | 1.0% | ok |
| com-create-instance | 2539.0 | 2546.0 | +436.8% | +438.3% | +444.4% | -0.0 | +0.2 | 0.4% | 1.8% | ok |
| wmi-query | 12346.0 | 8940.0 | -11.8% | -36.1% | -10.7% | -0.4 | +73.3 | 16.3% | 23.8% | anomaly |
| fs-watcher | 7437.0 | 7375.0 | +237.3% | +234.5% | +258.5% | -0.1 | +19.8 | 0.9% | 0.9% | ok |
| ripgrep-clean-build | 18398.0 | 18468.0 | +22.7% | +23.2% | - | +177.8 | -10.3 | 0.2% | 0.3% | ok |
| ripgrep-incremental-build | 6663.5 | 6651.0 | +13.2% | +13.0% | - | -0.0 | -5.0 | 3.3% | 0.5% | ok |
| roslyn-clean-build | 485092.0 | 503495.0 | +135.3% | +144.3% | - | +34684.0 | +573.2 | 3.1% | 2.0% | ok |
| roslyn-incremental-build | 109275.0 | 107123.0 | +84.4% | +80.8% | - | +7873.4 | +127.5 | 1.5% | 3.2% | ok |

Highest slowdown: registry-crud at +1304.1%

Largest kernel CPU shift: roslyn-incremental-build at +7.3pp (15.2% -> 22.5%)

Largest system disk write delta: roslyn-clean-build at +573.2 MB (30580.6 -> 31153.8 MB)

Largest system disk read delta: roslyn-clean-build at +34684.0 MB (9876.2 -> 44560.2 MB)

Anomaly scenarios (AV appears faster - likely caching artifact): file-copy-large, wmi-query

## tencent-pcmgr (腾讯电脑管家系统防护 v17,1,20399,201) vs baseline-os

| Scenario | Median Wall (ms) | First-Run Wall (ms) | Slowdown | First-Run Slowdown | p95 Slowdown | Disk Read Delta (MB) | Disk Write Delta (MB) | CV % | Baseline CV % | Status |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|
| file-create-delete | 2481.5 | 2520.0 | +68.2% | +70.8% | +64.7% | +0.6 | +7.1 | 0.3% | 2.0% | ok |
| archive-extract | 18540.5 | 18590.0 | +41.9% | +42.3% | +43.5% | +28.5 | +15.2 | 0.4% | 2.2% | ok |
| file-enum-large-dir | 3684.0 | 3760.0 | +53.4% | +56.6% | +5.9% | +41.2 | -10.1 | 1.4% | 0.3% | ok |
| file-copy-large | 866.0 | 857.0 | -9.0% | -10.0% | -26.0% | +0.0 | -0.0 | 1.4% | 1.4% | anomaly |
| hardlink-create | 2291.5 | 2329.0 | +80.9% | +83.8% | +69.1% | +0.1 | +4.2 | 1.1% | 0.8% | ok |
| junction-create | 864.5 | 881.0 | +43.6% | +46.3% | +35.6% | +0.1 | +2.0 | 1.5% | 1.5% | ok |
| process-create-wait | 20807.0 | 20658.0 | +55.2% | +54.1% | +46.8% | +0.6 | -1.3 | 0.4% | 0.4% | ok |
| ext-sensitivity-exe | 5113.5 | 5060.0 | +69.2% | +67.4% | +98.4% | +7.8 | +15.6 | 0.7% | 1.0% | ok |
| ext-sensitivity-dll | 5100.5 | 5097.0 | +68.4% | +68.3% | +94.9% | +0.0 | +14.7 | 0.1% | 1.4% | ok |
| ext-sensitivity-js | 5090.0 | 5085.0 | +66.9% | +66.7% | +93.2% | -0.0 | +10.3 | 0.2% | 1.0% | ok |
| ext-sensitivity-ps1 | 5131.5 | 5159.0 | +72.1% | +73.0% | +94.9% | +21.9 | +6.6 | 0.5% | 1.0% | ok |
| dll-load-unique | 10198.5 | 10046.0 | +27.2% | +25.3% | +31.3% | -1.9 | +2.2 | 1.4% | 1.1% | ok |
| file-write-content | 5349.0 | 5362.0 | +62.1% | +62.5% | +79.1% | -0.0 | -23.6 | 0.7% | 0.5% | ok |
| new-exe-run | 48541.0 | 48588.0 | +2420.3% | +2422.7% | +2945.0% | +142.2 | +7.0 | 0.7% | 1.0% | ok |
| new-exe-run-motw | 48990.5 | 49213.0 | +2437.7% | +2449.2% | +2976.9% | +0.0 | +5.3 | 2.6% | 0.7% | ok |
| thread-create | 1219.5 | 1205.0 | +4.1% | +2.8% | +4.7% | +0.1 | +0.0 | 1.8% | 0.8% | ok |
| mem-alloc-protect | 164.0 | 166.0 | -2.7% | -1.5% | -20.0% | +0.0 | +0.0 | 2.8% | 1.5% | anomaly |
| mem-map-file | 5311.5 | 5316.0 | +44.4% | +44.6% | +38.7% | +0.0 | +1.6 | 0.3% | 7.2% | ok |
| net-connect-loopback | 787.5 | 797.0 | +37.7% | +39.3% | +37.1% | +0.0 | +0.1 | 3.6% | 1.2% | ok |
| net-dns-resolve | 1241.5 | 1254.0 | +29.6% | +30.9% | +30.1% | +0.0 | +0.0 | 0.8% | 1.4% | ok |
| registry-crud | 1030.5 | 1015.0 | +126.2% | +122.8% | +107.1% | -0.0 | +0.0 | 0.2% | 4.1% | ok |
| pipe-roundtrip | 129.5 | 127.0 | -11.3% | -13.0% | -33.1% | +0.1 | -0.0 | 1.6% | 4.4% | anomaly |
| token-query | 64.5 | 57.0 | +16.2% | +2.7% | -3.7% | +0.1 | +0.0 | 6.0% | 0.9% | ok |
| crypto-hash-verify | 253.5 | 257.0 | +1.8% | +3.2% | -1.2% | +0.2 | +0.0 | 5.3% | 1.0% | ok |
| com-create-instance | 655.5 | 666.0 | +38.6% | +40.8% | +29.4% | +0.1 | +0.0 | 3.2% | 1.8% | ok |
| wmi-query | 14451.5 | 14320.0 | +3.3% | +2.3% | +3.8% | -0.4 | +0.2 | 1.1% | 23.8% | noisy |
| fs-watcher | 3679.5 | 3680.0 | +66.9% | +66.9% | +52.9% | -0.0 | +5.1 | 0.9% | 0.9% | ok |
| ripgrep-clean-build | 20389.5 | 20400.0 | +36.0% | +36.1% | - | +142.5 | +6.7 | 1.2% | 0.3% | ok |
| ripgrep-incremental-build | 6013.0 | 6011.0 | +2.1% | +2.1% | - | -0.0 | +1.1 | 0.6% | 0.5% | ok |
| roslyn-clean-build | 216484.0 | 216567.0 | +5.0% | +5.1% | - | +2970.4 | +102.0 | 2.4% | 2.0% | ok |
| roslyn-incremental-build | 61210.5 | 57462.0 | +3.3% | -3.0% | - | +8.1 | +20.4 | 3.4% | 3.2% | ok |

Highest slowdown: new-exe-run-motw at +2437.7%

Largest kernel CPU shift: roslyn-clean-build at +1.4pp (9.0% -> 10.3%)

Largest system disk write delta: roslyn-clean-build at +102.0 MB (30580.6 -> 30682.5 MB)

Largest system disk read delta: roslyn-clean-build at +2970.4 MB (9876.2 -> 12846.6 MB)

Noisy scenarios: wmi-query

Anomaly scenarios (AV appears faster - likely caching artifact): file-copy-large, mem-alloc-protect, pipe-roundtrip

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

| Scenario | baseline median (ms) | 360ts | avast | avira | bitdefender | drweb | emsisoft | eset | gdata | huorong | kaspersky | malwarebytes | mcafee | ms-defender | sophos | tencent-pcmgr | trendmicro |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| file-create-delete | 1475.5 | +125.0% | +24.3% | +73.2% | +323.2% | +199.5% | +166.0% | +97.3% | +182.6% | +63.0% | +53.1% | +255.4% | +6.6% | +52.9% | +198.0% | +68.2% | +752.5% |
| archive-extract | 13062.0 | +78.3% | +12.7% | +23.6% | +461.0% | +23.7% | +115.3% | +23.0% | +155.0% | +42.0% | +16.1% | +212.1%* | +3.3% | +53.3% | +137.5% | +41.9% | +427.1% |
| file-enum-large-dir | 2401.5 | +83.2% | +18.0% | +62.1% | +738.5% | +37.3% | +131.3% | +44.6% | +185.4% | +82.1%* | +15.9% | +273.5% | +1.2% | +119.1% | +297.8% | +53.4% | +656.5% |
| file-copy-large | 952.0 | -12.3%* | -6.1%* | -12.4%* | -8.2%* | -3.7%* | -13.1%* | -8.6%* | +153.7% | -12.7%* | -12.1%* | -11.3%* | -14.6%* | -7.6%* | -10.9%* | -9.0%* | -6.2%* |
| hardlink-create | 1267.0 | +155.0% | +36.6% | +43.7% | +248.5% | +53.0% | +179.3% | +42.4% | +215.2% | +130.4% | +181.4% | +105.1% | +18.5% | +47.9% | +152.1% | +80.9% | +504.7% |
| junction-create | - | failed* | +65.0% | +53.9% | +161.2% | +11.8% | +111.6% | +17.1% | +116.3% | +40.0% | +67.8% | +79.8% | +0.8% | +20.6% | +58.7% | +43.6% | +238.0% |
| process-create-wait | 13405.0 | +220.9% | +87.5% | +38.0% | +135.8% | +50.1% | +92.7% | +46.8% | +37.5% | +36.4% | +106.6% | +68.1% | +64.8% | +19.0% | +107.3% | +55.2% | +85.7% |
| ext-sensitivity-exe | 3022.5 | +118.9% | +35.8%* | +64.7% | +604.3% | +377.5%* | +158.6% | +106.7% | +178.1% | +232.8% | +20.5% | +443.7% | +2.9% | +47.3% | +238.3% | +69.2% | +767.8% |
| ext-sensitivity-dll | 3028.5 | +116.3% | +26.3% | +65.5% | +638.1% | +390.7% | +157.8% | +102.4% | +172.9% | +259.9% | +20.2% | +428.7% | +3.0% | +45.3% | +234.2% | +68.4% | +1225.1% |
| ext-sensitivity-js | 3050.0 | +115.7% | +18.2% | +64.3% | +1176.6% | +379.6% | +153.8% | +157.1% | +173.4% | +56.3% | +26.7% | +584.8% | +2.1% | +43.1% | +238.5% | +66.9% | +1933.6% |
| ext-sensitivity-ps1 | 2981.5 | +154.3% | +26.5% | +69.9% | +3964.1% | +399.2%* | +158.4% | +159.7% | +181.5% | +60.7% | +29.5% | +436.3% | +5.0% | +47.4% | +248.8% | +72.1% | +1260.5% |
| dll-load-unique | 8018.0 | +30.9% | +5.7% | +454.6% | +95.9% | +4886.9%* | +504.1% | +30.4% | +481.9% | +243.9% | +606.3% | +1553.9% | +499.5% | +596.6% | +732.8% | +27.2% | +468.9% |
| file-write-content | 3300.5 | +108.6% | +28.7% | +57.6% | +471.0% | +10948.2%* | +145.8% | +2753.4% | +1124.6%* | +31521.9% | +49.1% | +492.0% | +1.6% | +42.8% | +247.3% | +62.1% | +2591.2% |
| new-exe-run | 1926.0 | +387.7% | +218.8% | +739.9%* | +675.6% | +74.2%* | +189.8% | +195.7%* | +105.0% | +34.0% | +863.9% | +674.6% | +2765.6% | +167.3% | +401.4% | +2420.3% | +1198.1% |
| new-exe-run-motw | 1930.5 | +386.0% | +68.7% | +127.6%* | +525.9% | +75.6%* | +191.1% | +59.8% | +98.8% | +32.1% | +168.8% | +165.2% | +2388.9% | +3011.8% | +264.4% | +2437.7% | failed* |
| thread-create | 1172.0 | +3.6% | +27.2% | +23.9% | +11.0% | +3.8% | +8.3% | +6.7% | +11.8% | +0.8% | +150.9% | +54.1% | +4.1% | +7.4% | +22.1% | +4.1% | +59.9% |
| mem-alloc-protect | 168.5 | -4.7%* | +14.8% | -3.6%* | +0.3% | -3.3%* | +34.4% | +1.8% | -5.0%* | -3.9%* | -4.2%* | -2.7%* | -7.1%* | +16.6% | +49.6% | -2.7%* | +132.6% |
| mem-map-file | 3677.5 | +56.8% | +16.2% | +201.7% | +235.3% | +17.7% | +60.7% | +54.2% | +91.6% | +12.7% | +36.1% | +121.4% | +7.5% | +140.1% | +118.0% | +44.4% | +273.7% |
| net-connect-loopback | 572.0 | +22.1% | +114.3% | +57.3% | +154.6% | +8.0%* | +177.8% | +176.2% | +10.9% | +13.6% | +50.6% | +30.2% | +11.6% | +4.6% | +97.6% | +37.7% | +18.9% |
| net-dns-resolve | 958.0 | +25.3% | +22.5% | +571.7% | +27.7% | +14.2% | +71.2% | +5.3% | -1.4%* | +16.0% | +15.1% | +48.0% | +7.6% | +10.5% | +46.0% | +29.6% | +11.8% |
| registry-crud | 455.5 | +103.1% | +191.8%* | +1132.2% | +1058.4% | +52.1% | +2660.9% | +79.7% | +47.5% | +77.3% | +63.4% | +132.4% | +21.6% | +29.6% | +1304.1% | +126.2% | +2123.1% |
| pipe-roundtrip | 146.0 | -10.6%* | +5.5% | +1.4% | +20.2% | +15.8%* | +25.0% | +8.2% | +17.5% | +8.9% | +4.5% | +5.1% | -0.7%* | +1.0% | +24.7% | -11.3%* | +5.1% |
| token-query | 55.5 | -0.9%* | +49.5% | +0.9% | +55.0% | +0.9% | +105.4% | +9.9% | -1.8%* | -0.9%* | +1.8% | +0.9% | +0.9% | -0.9%* | +0.9% | +16.2% | +0.9% |
| crypto-hash-verify | 249.0 | +6.6% | +2.6% | -1.0%* | +6.2% | -0.6%* | +2.6%* | -1.0%* | 0.0% | +6.8% | +6.6% | 0.0% | +7.2% | +3.4% | +0.2% | +1.8% | +2.8% |
| com-create-instance | 473.0 | +222.4% | +45.6% | +52.6% | +242.7% | +58.6% | +165.9% | +34.5% | +292.6% | +90.2% | +15.8% | +84.6% | +20.5% | +16.3% | +436.8% | +38.6% | +299.8% |
| wmi-query | 13995.5 | -14.7%* | -31.2%* | -19.2%* | -16.0%* | -7.8%* | -45.9%* | -33.0%* | -38.2%* | +55.8%* | -43.2%* | -44.4%* | -45.8%* | -35.2%* | -11.8%* | +3.3%* | -5.9%* |
| fs-watcher | 2205.0 | +127.0% | +25.0% | +86.0% | +779.0% | +149.1% | +155.1% | +124.6% | +307.6% | +75.2% | +22.6% | +194.0% | +7.3% | +59.6% | +237.3% | +66.9% | +817.2% |
| ripgrep-clean-build | 14992.5 | +21.4% | +26.8% | +885.4%* | +17.3% | +14.0% | +14.6% | +17.2% | +28.1% | +11.7% | +14.2% | +10.6% | +175.9%* | +22.0% | +22.7% | +36.0% | +21.4% |
| ripgrep-incremental-build | 5886.5 | +9.1% | +2.4% | +3.7% | +10.2% | +4.6% | +5.7% | +0.9% | +15.1% | +3.2% | +5.0% | +2.8% | +1.5% | +4.2% | +13.2% | +2.1% | -0.7%* |
| roslyn-clean-build | 206134.5 | +3.9% | +2.8% | +11.3% | +16.3% | +37.7% | +13.7% | +21.8% | +16.6% | +6.6% | +18.0% | +10.5% | +6.2% | +25.0% | +135.3% | +5.0% | +20.1% |
| roslyn-incremental-build | 59259.5 | +3.4% | +12.6% | +5.3% | +7.4% | +22.3% | +23.7% | +5.2% | +17.6% | +7.9% | +11.6% | +5.0% | +1.3% | +17.7% | +84.4% | +3.3% | +16.3% |

## Cross-AV first-run comparison

Cells are slowdown vs baseline using the AV's earliest successful run against the baseline steady-state median.

| Scenario | baseline median (ms) | 360ts | avast | avira | bitdefender | drweb | emsisoft | eset | gdata | huorong | kaspersky | malwarebytes | mcafee | ms-defender | sophos | tencent-pcmgr | trendmicro |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| file-create-delete | 1475.5 | +126.6% | +23.8% | +71.9% | +328.9% | +246.2% | +162.2% | +95.1% | +182.3% | +63.1% | +53.4% | +253.0% | +7.4% | +50.1% | +188.2% | +70.8% | +758.1% |
| archive-extract | 13062.0 | +80.3% | +12.0% | +22.5% | +469.5% | +19.1% | +119.0% | +23.1% | +152.8% | +41.0% | +13.2% | +209.6% | +2.5% | +56.1% | +135.4% | +42.3% | +428.4% |
| file-enum-large-dir | 2401.5 | +83.5% | +22.5% | +60.1% | +752.5% | +34.7% | +133.2% | +38.7% | +183.3% | +82.9% | +15.1% | +315.9% | +4.8% | +116.5% | +300.6% | +56.6% | +630.9% |
| file-copy-large | 952.0 | -13.8%* | -4.9%* | -8.9%* | -8.0%* | -5.3%* | -11.3%* | -7.4%* | +170.0% | -13.6%* | -7.8%* | -10.5%* | -15.9%* | -6.5%* | -11.0%* | -10.0%* | -9.5%* |
| hardlink-create | 1267.0 | +156.0% | +37.8% | +43.1% | +248.2% | +51.7% | +182.2% | +41.7% | +215.7% | +131.2% | +182.9% | +107.2% | +18.2% | +36.3% | +151.8% | +83.8% | +512.9% |
| junction-create | - | failed* | +66.3% | +54.7% | +164.0% | +9.0% | +112.3% | +16.6% | +117.3% | +42.2% | +67.8% | +77.2% | +1.3% | +19.1% | +59.5% | +46.3% | +236.7% |
| process-create-wait | 13405.0 | +224.7% | +86.9% | +37.1% | +132.4% | +46.1% | +91.4% | +45.2% | +36.0% | +36.9% | +105.7% | +66.2% | +58.8% | +19.1% | +113.0% | +54.1% | +86.4% |
| ext-sensitivity-exe | 3022.5 | +115.5% | +32.2% | +65.3% | +593.2% | +387.6% | +151.2% | +103.6% | +173.3% | +231.1% | +19.2% | +435.1% | +5.2% | +47.3% | +240.7% | +67.4% | +768.4% |
| ext-sensitivity-dll | 3028.5 | +115.7% | +24.8% | +69.3% | +633.9% | +366.5% | +157.1% | +99.0% | +171.7% | +262.3% | +21.2% | +427.6% | +4.1% | +43.7% | +240.8% | +68.3% | +1158.8% |
| ext-sensitivity-js | 3050.0 | +125.1% | +16.5% | +64.3% | +1178.2% | +407.6% | +150.3% | +147.4% | +180.1% | +57.7% | +24.9% | +591.4% | +0.8% | +43.0% | +244.9% | +66.7% | +1896.8% |
| ext-sensitivity-ps1 | 2981.5 | +156.1% | +26.4% | +69.7% | +3942.6% | +400.1% | +159.5% | +154.1% | +185.5% | +61.9% | +27.1% | +442.0% | +0.5% | +47.2% | +253.4% | +73.0% | +1237.7% |
| dll-load-unique | 8018.0 | +33.2% | +6.0% | +450.3% | +97.6% | +4871.7% | +503.6% | +29.0% | +477.7% | +240.3% | +608.1% | +1552.4% | +502.9% | +601.7% | +725.9% | +25.3% | +469.0% |
| file-write-content | 3300.5 | +110.5% | +26.6% | +58.3% | +473.6% | +51557.0% | +158.1% | +2730.8% | +1221.8% | +31646.5% | +47.7% | +499.5% | +1.1% | +41.0% | +251.8% | +62.5% | +2616.3% |
| new-exe-run | 1926.0 | +389.3% | +213.9% | +15540.0% | +456.2% | +934.6% | +199.4% | +192.3% | +105.1% | +34.3% | +869.2% | +710.9% | +2807.7% | +181.6% | +416.1% | +2422.7% | +1637.6% |
| new-exe-run-motw | 1930.5 | +386.7% | +63.6% | +437.0% | +316.6% | +937.1% | +197.4% | +56.6% | +103.1% | +33.2% | +164.0% | +158.7% | +2290.2% | +4521.1% | +266.0% | +2449.2% | failed* |
| thread-create | 1172.0 | +4.9% | +26.4% | +21.8% | +10.1% | +2.7% | +15.4% | +7.7% | +8.4% | +0.3% | +143.9% | +58.0% | +4.5% | +3.5% | +23.2% | +2.8% | +60.2% |
| mem-alloc-protect | 168.5 | -4.5%* | +10.4% | -4.5%* | -1.5%* | -3.9%* | +32.9% | +3.9% | -3.3%* | -3.3%* | -3.3%* | -2.1%* | -7.4%* | +19.9% | +56.1% | -1.5%* | +132.6% |
| mem-map-file | 3677.5 | +56.8% | +17.1% | +203.3% | +236.4% | +16.1% | +59.4% | +55.6% | +91.4% | +10.6% | +38.0% | +119.8% | +4.4% | +139.2% | +127.2% | +44.6% | +275.7% |
| net-connect-loopback | 572.0 | +23.8% | +111.5% | +55.1% | +168.7% | +62.4% | +157.9% | +162.1% | +3.5% | +14.5% | +51.4% | +27.1% | +14.2% | +3.0% | +97.2% | +39.3% | +21.2% |
| net-dns-resolve | 958.0 | +21.4% | +19.7% | +574.3% | +30.7% | +15.6% | +69.7% | +7.2% | -0.1%* | +18.3% | +17.7% | +46.9% | -0.1%* | +7.9% | +46.7% | +30.9% | +13.7% |
| registry-crud | 455.5 | +104.0% | +195.3% | +1128.8% | +1062.0% | +59.2% | +2664.7% | +80.2% | +51.5% | +78.0% | +73.2% | +136.0% | +14.2% | +29.3% | +1304.8% | +122.8% | +2142.8% |
| pipe-roundtrip | 146.0 | -9.6%* | +12.3% | -4.1%* | +21.2% | +202.1% | +26.0% | +11.6% | +8.2% | +6.8% | -4.8%* | +3.4% | +17.8% | -12.3%* | +18.5% | -13.0%* | 0.0% |
| token-query | 55.5 | -0.9%* | +47.7% | -2.7%* | +55.0% | +0.9% | +103.6% | +9.9% | -0.9%* | +0.9% | +4.5% | +0.9% | -4.5%* | +0.9% | +0.9% | +2.7% | -0.9%* |
| crypto-hash-verify | 249.0 | +0.8% | +1.2% | -3.6%* | +8.0% | +3.2% | +1.2% | -0.8%* | -10.8%* | +6.0% | +5.2% | +3.6% | -4.4%* | +6.0% | -3.2%* | +3.2% | +4.4% |
| com-create-instance | 473.0 | +223.9% | +42.7% | +53.5% | +242.5% | +59.2% | +163.2% | +35.3% | +333.4% | +90.3% | +18.8% | +83.7% | +5.5% | +16.7% | +438.3% | +40.8% | +292.0% |
| wmi-query | 13995.5 | -2.4%* | -35.3%* | -18.5%* | -13.3%* | +7.5% | -46.8%* | -37.1%* | -44.4%* | +52.6% | -37.6%* | -29.8%* | -55.6%* | -36.5%* | -36.1%* | +2.3% | -8.5%* |
| fs-watcher | 2205.0 | +127.5% | +23.7% | +86.2% | +781.9% | +172.8% | +155.4% | +123.9% | +218.4% | +75.3% | +21.8% | +191.0% | +9.1% | +57.2% | +234.5% | +66.9% | +834.6% |
| ripgrep-clean-build | 14992.5 | +21.7% | +25.7% | +756.7% | +17.7% | +21.8% | +14.2% | +17.1% | +27.0% | +12.7% | +13.7% | +14.1% | +261.6% | +22.1% | +23.2% | +36.1% | +31.1% |
| ripgrep-incremental-build | 5886.5 | +8.6% | +4.0% | +2.7% | +11.1% | +3.5% | +6.3% | +0.9% | +15.8% | +2.4% | +3.7% | +3.3% | +1.6% | +3.7% | +13.0% | +2.1% | +2.7% |
| roslyn-clean-build | 206134.5 | -0.3%* | +5.1% | +132.7% | +13.3% | +59.7% | +16.1% | +27.6% | +15.8% | +2.6% | +33.2% | +8.6% | +4.1% | +26.5% | +144.3% | +5.1% | +23.3% |
| roslyn-incremental-build | 59259.5 | -1.1%* | +13.4% | +175.0% | +11.2% | +16.5% | +24.1% | +2.9% | +20.5% | +9.5% | +19.7% | +3.8% | +1.7% | +17.4% | +80.8% | -3.0%* | +16.9% |

`*` in the steady-state table marks a non-ok result (`failed`, `insufficient`, `noisy`, or `anomaly`).
First-run cells do not inherit `noisy` or `insufficient` markers because CV and steady-state sample count are not meaningful for a single first-run sample; `failed*` means no successful first run was available, and a negative first-run slowdown is marked as an anomaly.

