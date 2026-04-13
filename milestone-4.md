# Milestone 4 Implementation

## Scope

- Auto-detect installed AV product and version on Windows VMs
- Supported products: Microsoft Defender, Huorong, ESET, Bitdefender, TrendMicro
- Record detected `av_product` and `av_version` in `run.json`
- `--av-name` and `--av-version` CLI overrides for unsupported products or manual testing
- Graceful fallback: if no AV is detected and no override is provided, fields are set to `"unknown"`

## Prerequisites

- Milestone 1 complete (`avbench run` working with `--name`, `RunResult` model, JSON output)
- Admin-always policy already enforced at startup (required for WMI and registry queries)

## New files

```
src/
  AvBench.Core/
    Detection/
      AvDetector.cs             → public API: DetectAsync() returns AvInfo
      AvInfo.cs                 → product name + version record
      Detectors/
        DefenderDetector.cs     → Microsoft Defender detection
        HuorongDetector.cs      → Huorong detection
        EsetDetector.cs         → ESET detection
        BitdefenderDetector.cs  → Bitdefender detection
        TrendMicroDetector.cs   → TrendMicro detection
        IAvDetectorStrategy.cs  → per-product interface
```

## Data model

### `AvInfo.cs`

```csharp
namespace AvBench.Core.Detection;

public sealed record AvInfo(string ProductName, string ProductVersion);
```

### `RunResult` additions

Add two new optional properties to `RunResult`:

```csharp
[JsonPropertyName("av_product")]
public string? AvProduct { get; set; }

[JsonPropertyName("av_version")]
public string? AvVersion { get; set; }
```

These are populated after auto-detection (or from CLI overrides) and serialized into `run.json`. Existing fields (`av_name`, `scenario_id`, etc.) are unchanged.

Updated `run.json` example:

```json
{
  "scenario_id": "ripgrep-clean",
  "av_name": "defender-default",
  "av_product": "Microsoft Defender Antivirus",
  "av_version": "4.18.24090.11",
  "repetition": 1,
  "timestamp_utc": "2025-01-15T10:30:00Z",
  "command": "cargo build --release",
  "working_dir": "C:\\bench\\ripgrep",
  "exit_code": 0,
  "wall_ms": 42000
}
```

### CSV header additions

`CsvResultWriter` adds two columns after `av_name`:

```
scenario_id, av_name, av_product, av_version, repetition, timestamp_utc, ...
```

## Detection strategy per product

### Interface

```csharp
namespace AvBench.Core.Detection.Detectors;

public interface IAvDetectorStrategy
{
    /// <summary>
    /// Human-readable product name (e.g., "Microsoft Defender Antivirus").
    /// </summary>
    string ProductName { get; }

    /// <summary>
    /// Returns the product version string, or null if this AV is not installed.
    /// </summary>
    Task<string?> DetectVersionAsync();
}
```

### Microsoft Defender — `DefenderDetector.cs`

Detection approach:
1. Check if the `WinDefend` service exists and is running
2. Query `Get-MpComputerStatus` via PowerShell to get `AMProductVersion`
3. Fallback: read `HKLM\SOFTWARE\Microsoft\Windows Defender\Signature Updates\ASSignatureVersion`

```csharp
using System.Diagnostics;
using System.ServiceProcess;
using Microsoft.Win32;

namespace AvBench.Core.Detection.Detectors;

public sealed class DefenderDetector : IAvDetectorStrategy
{
    public string ProductName => "Microsoft Defender Antivirus";

    public async Task<string?> DetectVersionAsync()
    {
        // 1. Check WinDefend service
        try
        {
            using var sc = new ServiceController("WinDefend");
            if (sc.Status != ServiceControllerStatus.Running)
                return null;
        }
        catch (InvalidOperationException)
        {
            return null; // service not installed
        }

        // 2. Try PowerShell Get-MpComputerStatus
        var version = await RunPowerShellAsync(
            "(Get-MpComputerStatus).AMProductVersion");

        if (!string.IsNullOrWhiteSpace(version))
            return version.Trim();

        // 3. Fallback: registry
        using var key = Registry.LocalMachine.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows Defender\Signature Updates");
        return key?.GetValue("ASSignatureVersion") as string;
    }

    private static async Task<string?> RunPowerShellAsync(string command)
    {
        var psi = new ProcessStartInfo("powershell.exe", $"-NoProfile -Command \"{command}\"")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var proc = Process.Start(psi);
        if (proc is null) return null;

        var output = await proc.StandardOutput.ReadToEndAsync();
        await proc.WaitForExitAsync();

        return proc.ExitCode == 0 ? output.Trim() : null;
    }
}
```

### Huorong — `HuorongDetector.cs`

Detection approach:
1. Check if the `HipsTray` service exists and is running
2. Read product version from `HKLM\SOFTWARE\Huorong\Sysdiag\Scan Engine` → `EngineVersion`
3. Fallback: read file version from `C:\Program Files\Huorong\Sysdiag\bin\HipsTray.exe`

```csharp
using System.Diagnostics;
using System.ServiceProcess;
using Microsoft.Win32;

namespace AvBench.Core.Detection.Detectors;

public sealed class HuorongDetector : IAvDetectorStrategy
{
    public string ProductName => "Huorong Internet Security";

    public Task<string?> DetectVersionAsync()
    {
        // 1. Check HipsTray service
        try
        {
            using var sc = new ServiceController("HipsTray");
            if (sc.Status != ServiceControllerStatus.Running)
                return Task.FromResult<string?>(null);
        }
        catch (InvalidOperationException)
        {
            return Task.FromResult<string?>(null);
        }

        // 2. Try registry
        using var key = Registry.LocalMachine.OpenSubKey(
            @"SOFTWARE\Huorong\Sysdiag\Scan Engine");
        var regVersion = key?.GetValue("EngineVersion") as string;
        if (!string.IsNullOrWhiteSpace(regVersion))
            return Task.FromResult<string?>(regVersion);

        // 3. Fallback: file version
        const string exePath = @"C:\Program Files\Huorong\Sysdiag\bin\HipsTray.exe";
        if (File.Exists(exePath))
        {
            var fvi = FileVersionInfo.GetVersionInfo(exePath);
            return Task.FromResult(fvi.ProductVersion);
        }

        return Task.FromResult<string?>("installed");
    }
}
```

### ESET — `EsetDetector.cs`

Detection approach:
1. Check if the `ekrn` service (ESET Kernel Service) exists and is running
2. Read product version from `HKLM\SOFTWARE\ESET\ESET Security\CurrentVersion\Info` → `ProductVersion`
3. Fallback: file version from `C:\Program Files\ESET\ESET Security\ekrn.exe`

```csharp
using System.Diagnostics;
using System.ServiceProcess;
using Microsoft.Win32;

namespace AvBench.Core.Detection.Detectors;

public sealed class EsetDetector : IAvDetectorStrategy
{
    public string ProductName => "ESET Security";

    public Task<string?> DetectVersionAsync()
    {
        // 1. Check ekrn service
        try
        {
            using var sc = new ServiceController("ekrn");
            if (sc.Status != ServiceControllerStatus.Running)
                return Task.FromResult<string?>(null);
        }
        catch (InvalidOperationException)
        {
            return Task.FromResult<string?>(null);
        }

        // 2. Try registry
        using var key = Registry.LocalMachine.OpenSubKey(
            @"SOFTWARE\ESET\ESET Security\CurrentVersion\Info");
        var regVersion = key?.GetValue("ProductVersion") as string;
        if (!string.IsNullOrWhiteSpace(regVersion))
            return Task.FromResult<string?>(regVersion);

        // 3. Fallback: file version
        const string exePath = @"C:\Program Files\ESET\ESET Security\ekrn.exe";
        if (File.Exists(exePath))
        {
            var fvi = FileVersionInfo.GetVersionInfo(exePath);
            return Task.FromResult(fvi.ProductVersion);
        }

        return Task.FromResult<string?>("installed");
    }
}
```

### Bitdefender — `BitdefenderDetector.cs`

Detection approach:
1. Check if the `bdservicehost` service exists and is running (or `VSSERV` for older versions)
2. Read product version from `HKLM\SOFTWARE\Bitdefender\Bitdefender Security\About` → `ProductVersion`
3. Fallback: file version from `C:\Program Files\Bitdefender\Bitdefender Security\bdservicehost.exe`

```csharp
using System.Diagnostics;
using System.ServiceProcess;
using Microsoft.Win32;

namespace AvBench.Core.Detection.Detectors;

public sealed class BitdefenderDetector : IAvDetectorStrategy
{
    private static readonly string[] ServiceNames = ["bdservicehost", "VSSERV"];

    public string ProductName => "Bitdefender";

    public Task<string?> DetectVersionAsync()
    {
        // 1. Check known services
        bool serviceFound = false;
        foreach (var name in ServiceNames)
        {
            try
            {
                using var sc = new ServiceController(name);
                if (sc.Status == ServiceControllerStatus.Running)
                {
                    serviceFound = true;
                    break;
                }
            }
            catch (InvalidOperationException) { }
        }

        if (!serviceFound)
            return Task.FromResult<string?>(null);

        // 2. Try registry
        using var key = Registry.LocalMachine.OpenSubKey(
            @"SOFTWARE\Bitdefender\Bitdefender Security\About");
        var regVersion = key?.GetValue("ProductVersion") as string;
        if (!string.IsNullOrWhiteSpace(regVersion))
            return Task.FromResult<string?>(regVersion);

        // 3. Fallback: file version
        const string exePath = @"C:\Program Files\Bitdefender\Bitdefender Security\bdservicehost.exe";
        if (File.Exists(exePath))
        {
            var fvi = FileVersionInfo.GetVersionInfo(exePath);
            return Task.FromResult(fvi.ProductVersion);
        }

        return Task.FromResult<string?>("installed");
    }
}
```

### TrendMicro — `TrendMicroDetector.cs`

Detection approach:
1. Check if the `Ntrtscan` service (TrendMicro Real-Time Scan) or `PccNTUpd` (update service) exists and is running
2. Read product version from `HKLM\SOFTWARE\TrendMicro\PC-cillinNTCorp\CurrentVersion` → `EngineVersion`
3. Fallback: file version from common install paths

```csharp
using System.Diagnostics;
using System.ServiceProcess;
using Microsoft.Win32;

namespace AvBench.Core.Detection.Detectors;

public sealed class TrendMicroDetector : IAvDetectorStrategy
{
    private static readonly string[] ServiceNames = ["Ntrtscan", "PccNTUpd", "TmListen"];

    public string ProductName => "Trend Micro";

    public Task<string?> DetectVersionAsync()
    {
        // 1. Check known services
        bool serviceFound = false;
        foreach (var name in ServiceNames)
        {
            try
            {
                using var sc = new ServiceController(name);
                if (sc.Status == ServiceControllerStatus.Running)
                {
                    serviceFound = true;
                    break;
                }
            }
            catch (InvalidOperationException) { }
        }

        if (!serviceFound)
            return Task.FromResult<string?>(null);

        // 2. Try registry
        using var key = Registry.LocalMachine.OpenSubKey(
            @"SOFTWARE\TrendMicro\PC-cillinNTCorp\CurrentVersion");
        var regVersion = key?.GetValue("EngineVersion") as string;
        if (!string.IsNullOrWhiteSpace(regVersion))
            return Task.FromResult<string?>(regVersion);

        // 3. Fallback: file version
        string[] searchPaths =
        [
            @"C:\Program Files\Trend Micro\Security Agent\Ntrtscan.exe",
            @"C:\Program Files (x86)\Trend Micro\OfficeScan Client\Ntrtscan.exe"
        ];

        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
            {
                var fvi = FileVersionInfo.GetVersionInfo(path);
                return Task.FromResult(fvi.ProductVersion);
            }
        }

        return Task.FromResult<string?>("installed");
    }
}
```

## WMI fallback — `SecurityCenter2`

On **client** editions of Windows (Windows 10/11), `root\SecurityCenter2\AntiVirusProduct` provides a generic list of registered AV products. This is used as a secondary signal when none of the per-product detectors match.

> **Note:** `SecurityCenter2` is **not available** on Windows Server editions. The per-product service/registry checks above work on both client and server.

```csharp
using System.Management;

namespace AvBench.Core.Detection;

internal static class WmiAvQuery
{
    /// <summary>
    /// Query WMI SecurityCenter2 for registered AV products.
    /// Returns list of (displayName, pathToSignedProductExe) tuples.
    /// Returns empty list on Server editions where SecurityCenter2 is unavailable.
    /// </summary>
    public static List<(string DisplayName, string ExePath)> QueryRegisteredProducts()
    {
        var results = new List<(string, string)>();

        try
        {
            using var searcher = new ManagementObjectSearcher(
                @"root\SecurityCenter2",
                "SELECT displayName, pathToSignedProductExe FROM AntiVirusProduct");

            foreach (var obj in searcher.Get())
            {
                var name = obj["displayName"]?.ToString() ?? "";
                var path = obj["pathToSignedProductExe"]?.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(name))
                    results.Add((name, path));
            }
        }
        catch (ManagementException)
        {
            // SecurityCenter2 not available (e.g., Windows Server)
        }

        return results;
    }
}
```

## Public API — `AvDetector.cs`

Orchestrates all per-product detectors and the WMI fallback. Returns the first successful match.

```csharp
using AvBench.Core.Detection.Detectors;

namespace AvBench.Core.Detection;

public static class AvDetector
{
    private static readonly IAvDetectorStrategy[] Detectors =
    [
        new DefenderDetector(),
        new HuorongDetector(),
        new EsetDetector(),
        new BitdefenderDetector(),
        new TrendMicroDetector()
    ];

    /// <summary>
    /// Auto-detect the installed AV product and version.
    /// Returns AvInfo with product name and version, or ("unknown", "unknown") if undetected.
    /// </summary>
    public static async Task<AvInfo> DetectAsync()
    {
        // 1. Try each known detector
        foreach (var detector in Detectors)
        {
            var version = await detector.DetectVersionAsync();
            if (version is not null)
            {
                Console.WriteLine($"[detect] Found: {detector.ProductName} v{version}");
                return new AvInfo(detector.ProductName, version);
            }
        }

        // 2. Fallback: WMI SecurityCenter2 (client OS only)
        var wmiProducts = WmiAvQuery.QueryRegisteredProducts();
        if (wmiProducts.Count > 0)
        {
            // Use the first non-Windows-Defender entry, or the first entry if only Defender
            var product = wmiProducts.FirstOrDefault(p =>
                !p.DisplayName.Contains("Windows Defender", StringComparison.OrdinalIgnoreCase));

            if (product == default)
                product = wmiProducts[0];

            Console.WriteLine($"[detect] WMI found: {product.DisplayName}");

            // Try to get version from the EXE path
            string version = "unknown";
            if (!string.IsNullOrWhiteSpace(product.ExePath) && File.Exists(product.ExePath))
            {
                var fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(product.ExePath);
                if (!string.IsNullOrWhiteSpace(fvi.ProductVersion))
                    version = fvi.ProductVersion;
            }

            return new AvInfo(product.DisplayName, version);
        }

        Console.WriteLine("[detect] No AV product detected");
        return new AvInfo("unknown", "unknown");
    }
}
```

## CLI integration

### New options on `RunCommand`

Add `--av-name` and `--av-version` as optional string options:

```csharp
var avNameOption = new Option<string?>("--av-name",
    "Override detected AV product name (e.g., \"ESET Security\")")
{
    IsRequired = false
};

var avVersionOption = new Option<string?>("--av-version",
    "Override detected AV product version (e.g., \"10.1.2152.0\")")
{
    IsRequired = false
};

runCommand.Add(avNameOption);
runCommand.Add(avVersionOption);
```

### Usage in `RunCommand.SetAction`

After parsing CLI args, detect AV and apply overrides:

```csharp
// Auto-detect AV (or use CLI overrides)
var detected = await AvDetector.DetectAsync();
string avProduct = parseResult.GetValue(avNameOption) ?? detected.ProductName;
string avVersion = parseResult.GetValue(avVersionOption) ?? detected.ProductVersion;

Console.WriteLine($"[run] AV: {avProduct} v{avVersion}");

// Pass to ScenarioRunner (avProduct and avVersion stamped on every RunResult)
```

### Updated ScenarioRunner

`ScenarioRunner` now receives `avProduct` and `avVersion` in addition to `avName` and stamps them on every `RunResult`:

```csharp
private RunResult RunOnce(ScenarioDefinition scenario, bool isWarmup)
{
    // ... existing Job Object measurement code ...

    return new RunResult
    {
        ScenarioId = scenario.Id,
        AvName = _avName,
        AvProduct = _avProduct,   // NEW
        AvVersion = _avVersion,   // NEW
        Repetition = rep,
        // ... remaining fields ...
    };
}
```

## NuGet dependency

Add `System.Management` package for WMI queries:

```xml
<PackageReference Include="System.Management" Version="8.0.0" />
```

This package is only used by `WmiAvQuery.cs` and is already included in the Windows targeting pack.

## Test commands

```powershell
# Auto-detect AV (no overrides)
avbench run --name defender-default --bench-dir C:\bench --output results -n 1

# Expected console output:
# [detect] Found: Microsoft Defender Antivirus v4.18.24090.11
# [run] AV: Microsoft Defender Antivirus v4.18.24090.11

# Check run.json includes av_product and av_version
Get-Content results\ripgrep-clean\rep-01\run.json | ConvertFrom-Json | Select-Object av_product, av_version
# av_product: Microsoft Defender Antivirus
# av_version: 4.18.24090.11
```

```powershell
# Override detection manually
avbench run --name custom-av --bench-dir C:\bench --output results -n 1 --av-name "Custom AV" --av-version "1.0.0"

# Expected console output:
# [detect] Found: Microsoft Defender Antivirus v4.18.24090.11
# [run] AV: Custom AV v1.0.0   (overrides applied)
```

```powershell
# Baseline (no AV) - auto-detect returns unknown
avbench run --name baseline-os --bench-dir C:\bench --output results -n 1

# Expected console output:
# [detect] No AV product detected
# [run] AV: unknown vunknown
```

## Implementation steps (ordered)

### Step 1: Data model additions

Add `AvProduct` and `AvVersion` properties to `RunResult`. Add `AvInfo` record. Update `CsvResultWriter` headers.

### Step 2: Detection interface and per-product detectors

Create `IAvDetectorStrategy` and all five detector implementations (`DefenderDetector`, `HuorongDetector`, `EsetDetector`, `BitdefenderDetector`, `TrendMicroDetector`).

Test each detector individually on VMs with the corresponding AV installed. On VMs without that AV, `DetectVersionAsync()` should return `null`.

### Step 3: WMI fallback

Create `WmiAvQuery.cs`. Test on a Windows 10/11 client (should return registered AV products) and on Windows Server (should return empty list without errors).

### Step 4: AvDetector orchestrator

Create `AvDetector.cs` with the prioritized detection chain. Test on each target AV VM.

### Step 5: CLI integration

Add `--av-name` and `--av-version` options to `RunCommand`. Wire up `AvDetector.DetectAsync()` in the run action. Apply overrides when present.

### Step 6: End-to-end test

Run `avbench run` on each target VM and verify `run.json` contains correct `av_product` and `av_version` values. Test override behavior with `--av-name` and `--av-version`.

## Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| Registry paths vary across AV versions | Wrong version or false negative | File version fallback; log which detection method succeeded |
| Service names change in future AV releases | Detection miss | WMI SecurityCenter2 as catch-all on client OS |
| WMI unavailable on Server editions | No fallback | Per-product service/registry checks work on all editions |
| PowerShell cmdlet (Defender) slow | Startup delay ~1-2s | Only runs if WinDefend service is detected first |
| Multiple AVs installed simultaneously | Ambiguous detection | First match wins; use `--av-name` override if needed |
