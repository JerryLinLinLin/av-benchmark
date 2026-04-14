# Milestone 4 Implementation

## Scope

- Auto-detect installed AV product and version via Windows Security Center (`root\SecurityCenter2`)
- Works for **any** AV that registers with WSC — no hardcoded per-product logic
- Record detected `av_product` and `av_version` in `run.json`
- `--av-product` and `--av-version` CLI overrides
- Fallback: if WSC returns no AV and no override is provided, fields are set to `"unknown"`

## Prerequisites

- Milestone 1 complete (`avbench run` working with `--name`, `RunResult` model, JSON output)
- Windows 10 or Windows 11 (client OS — WSC is always available)

## New files

```
src/
  AvBench.Core/
    Detection/
      AvDetector.cs   → single-file: queries WSC, returns AvInfo
      AvInfo.cs        → product name + version record
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

## Detection — WSC query

Every AV product that integrates with Windows registers itself with the Windows Security Center service (`wscsvc`). The WMI class `root\SecurityCenter2\AntiVirusProduct` exposes all registered products with these properties:

| Property | Description |
|---|---|
| `displayName` | Human-readable product name (e.g., `"Windows Defender"`, `"ESET Security"`) |
| `instanceGuid` | Unique GUID for this registration |
| `pathToSignedProductExe` | Path to the product's main exe. **Caveat**: Defender sets this to `windowsdefender://` (a protocol handler, not a file path). Third-party AVs set real file paths. |
| `pathToSignedReportingExe` | Path to the reporting exe. **Always a real file path** — even for Defender (`%ProgramFiles%\Windows Defender\MsMpeng.exe`). May contain environment variables that must be expanded. |
| `productState` | Encoded bitmask: on/off/snoozed/expired |
| `timestamp` | Last update timestamp |

**Version extraction strategy:** Try `pathToSignedReportingExe` first (always a real file path), fall back to `pathToSignedProductExe`. Environment variables (`%ProgramFiles%`) must be expanded before use. Call `FileVersionInfo.GetVersionInfo()` to get the product version.

Verified on this machine:
- **Defender**: `displayName` = `"Windows Defender"`, `pathToSignedProductExe` = `"windowsdefender://"` (not a file!), `pathToSignedReportingExe` = `"%ProgramFiles%\Windows Defender\MsMpeng.exe"` → version `4.18.25080.5`
- **ESET**: `displayName` = `"ESET Security"`, `pathToSignedProductExe` = `"C:\Program Files\ESET\ESET Security\ecmds.exe"`, `pathToSignedReportingExe` = `"C:\Program Files\ESET\ESET Security\ekrn.exe"` → version `19.1.12.0`

### `AvDetector.cs`

```csharp
using System.Diagnostics;
using System.Management;

namespace AvBench.Core.Detection;

public static class AvDetector
{
    /// <summary>
    /// Query Windows Security Center for the registered AV product.
    /// Prefers third-party AV over Defender (Defender stays registered as a
    /// fallback even when third-party AV is active).
    /// </summary>
    public static AvInfo Detect()
    {
        List<WscProduct> products;

        try
        {
            products = QuerySecurityCenter2();
        }
        catch (ManagementException ex)
        {
            Console.WriteLine($"[detect] WSC query failed: {ex.Message}");
            return new AvInfo("unknown", "unknown");
        }

        if (products.Count == 0)
        {
            Console.WriteLine("[detect] No AV product registered with Windows Security Center");
            return new AvInfo("unknown", "unknown");
        }

        // Prefer third-party AV over Defender
        var pick = products.FirstOrDefault(p =>
            !p.DisplayName.Contains("Windows Defender", StringComparison.OrdinalIgnoreCase)
            && !p.DisplayName.Contains("Microsoft Defender", StringComparison.OrdinalIgnoreCase));

        if (pick is null)
            pick = products[0];

        var version = GetFileVersion(pick.ReportingExePath)
                   ?? GetFileVersion(pick.ProductExePath)
                   ?? "unknown";

        Console.WriteLine($"[detect] {pick.DisplayName} v{version}");
        return new AvInfo(pick.DisplayName, version);
    }

    private sealed record WscProduct(string DisplayName, string ProductExePath, string ReportingExePath);

    private static List<WscProduct> QuerySecurityCenter2()
    {
        var results = new List<WscProduct>();

        using var searcher = new ManagementObjectSearcher(
            @"root\SecurityCenter2",
            "SELECT displayName, pathToSignedProductExe, pathToSignedReportingExe FROM AntiVirusProduct");

        foreach (var obj in searcher.Get())
        {
            var name = obj["displayName"]?.ToString() ?? "";
            var productExe = obj["pathToSignedProductExe"]?.ToString() ?? "";
            var reportingExe = obj["pathToSignedReportingExe"]?.ToString() ?? "";
            if (!string.IsNullOrWhiteSpace(name))
                results.Add(new WscProduct(name, productExe, reportingExe));
        }

        return results;
    }

    private static string? GetFileVersion(string exePath)
    {
        if (string.IsNullOrWhiteSpace(exePath))
            return null;

        // Expand %ProgramFiles% etc., and skip protocol handlers like "windowsdefender://"
        var expanded = Environment.ExpandEnvironmentVariables(exePath);
        if (!File.Exists(expanded))
            return null;

        var fvi = FileVersionInfo.GetVersionInfo(expanded);
        return !string.IsNullOrWhiteSpace(fvi.ProductVersion)
            ? fvi.ProductVersion
            : null;
    }
}
```

## CLI integration

### New options on `RunCommand`

```csharp
var avProductOption = new Option<string?>("--av-product",
    "Override detected AV product name")
{
    IsRequired = false
};

var avVersionOption = new Option<string?>("--av-version",
    "Override detected AV product version")
{
    IsRequired = false
};

runCommand.Add(avProductOption);
runCommand.Add(avVersionOption);
```

### Usage in `RunCommand.SetAction`

```csharp
// Auto-detect AV (or use CLI overrides)
var detected = AvDetector.Detect();
string avProduct = parseResult.GetValue(avProductOption) ?? detected.ProductName;
string avVersion = parseResult.GetValue(avVersionOption) ?? detected.ProductVersion;

Console.WriteLine($"[run] AV: {avProduct} v{avVersion}");
```

### Updated ScenarioRunner

`ScenarioRunner` receives `avProduct` and `avVersion` and stamps them on every `RunResult`:

```csharp
private RunResult RunOnce(ScenarioDefinition scenario)
{
    // ... existing Job Object measurement code ...

    return new RunResult
    {
        ScenarioId = scenario.Id,
        AvName = _avName,
        AvProduct = _avProduct,
        AvVersion = _avVersion,
        Repetition = rep,
        // ... remaining fields ...
    };
}
```

## NuGet dependency

```xml
<PackageReference Include="System.Management" Version="8.0.0" />
```

## Updated `CompareCsvWriter.cs` and `CompareEngine.cs`

Add `av_product` and `av_version` columns to `CompareCsvWriter.Headers` after `av_name`:

```csharp
private static readonly string[] Headers =
[
    "scenario_id", "av_name", "av_product", "av_version", "baseline_name", "repetitions",
    "mean_wall_ms", "median_wall_ms", "mean_cpu_ms",
    "kernel_cpu_pct", "baseline_kernel_cpu_pct", "kernel_cpu_slowdown_pct",
    "peak_memory_mb", "slowdown_pct", "cv_pct", "status"
];
```

Add matching fields to `ComparisonRow`:

```csharp
public string AvProduct { get; init; } = "";
public string AvVersion { get; init; } = "";
```

In `CompareEngine.Compare()`, populate from the first run in each group:

```csharp
var firstRun = scenarioRuns[0];
rows.Add(new ComparisonRow
{
    ScenarioId = scenarioId,
    AvName = avName,
    AvProduct = firstRun.AvProduct ?? "",
    AvVersion = firstRun.AvVersion ?? "",
    // ... existing fields ...
});
```

Update `SummaryRenderer` section header to include product info:

```csharp
sb.AppendLine($"## {nameGroup.Key} ({nameGroup.First().AvProduct} v{nameGroup.First().AvVersion}) vs {nameGroup.First().BaselineName}");
```

## Test commands

```powershell
# Auto-detect AV (no overrides)
avbench run --name defender-default --bench-dir C:\bench --output results -n 1

# Expected:
# [detect] Microsoft Defender Antivirus v4.18.24090.11
# run.json → av_product: "Microsoft Defender Antivirus", av_version: "4.18.24090.11"
```

```powershell
# Override detection
avbench run --name custom --bench-dir C:\bench --output results -n 1 \
    --av-product "Custom AV" --av-version "1.0.0"

# Expected:
# [detect] Microsoft Defender Antivirus v4.18.24090.11
# [run] AV: Custom AV v1.0.0   (overrides applied)
```

```powershell
# Baseline (AV disabled/uninstalled)
avbench run --name baseline-os --bench-dir C:\bench --output results -n 1

# Expected:
# [detect] No AV product registered with Windows Security Center
# run.json → av_product: "unknown", av_version: "unknown"
```

## Implementation steps (ordered)

### Step 1: Data model

Add `AvProduct` and `AvVersion` to `RunResult`. Add `AvInfo` record. Update `CsvResultWriter` headers.

### Step 2: AvDetector

Create `AvDetector.cs` — single WSC query + file version. Test on a VM with Defender, and on a VM with third-party AV.

### Step 3: CLI integration

Add `--av-product` and `--av-version` options. Wire `AvDetector.Detect()` into `RunCommand`. Apply overrides.

### Step 4: Compare output

Update `ComparisonRow`, `CompareCsvWriter`, `SummaryRenderer` with `av_product`/`av_version` fields.

### Step 5: End-to-end test

Verify `run.json` and `compare.csv` contain correct values across Defender, third-party AV, and baseline (no AV) VMs.

## Risks

| Risk | Impact | Mitigation |
|------|--------|------------|
| AV product doesn't register with WSC | `unknown` in output | Use `--av-product` / `--av-version` CLI overrides |
| `pathToSignedProductExe` is empty or missing | Version shows "unknown" | Product name still detected via `displayName`; version is secondary |
| Multiple AV products registered | Wrong one picked | Prefer non-Defender entry; log all found products for debugging |
