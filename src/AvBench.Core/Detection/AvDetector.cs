using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace AvBench.Core.Detection;

[SupportedOSPlatform("windows")]
public static class AvDetector
{
    public static AvInfo Detect()
    {
        try
        {
            var products = QueryProducts();
            if (products.Count == 0)
            {
                Console.WriteLine("[detect] No AV product registered with Windows Security Center.");
                return Unknown();
            }

            foreach (var product in products)
            {
                Console.WriteLine($"[detect] WSC product: {product.DisplayName}");
            }

            var selected = products.FirstOrDefault(static product => !IsDefender(product.DisplayName))
                ?? products[0];

            var version = GetFileVersion(selected.ReportingExePath)
                ?? GetFileVersion(selected.ProductExePath)
                ?? "unknown";

            Console.WriteLine($"[detect] Selected AV: {selected.DisplayName} v{version}");
            return new AvInfo(
                string.IsNullOrWhiteSpace(selected.DisplayName) ? "unknown" : selected.DisplayName,
                string.IsNullOrWhiteSpace(version) ? "unknown" : version);
        }
        catch (ManagementException ex)
        {
            Console.WriteLine($"[detect] WSC query failed: {ex.Message}");
            return Unknown();
        }
        catch (COMException ex)
        {
            Console.WriteLine($"[detect] WSC COM query failed: {ex.Message}");
            return Unknown();
        }
    }

    private static List<WscProduct> QueryProducts()
    {
        var results = new List<WscProduct>();
        using var searcher = new ManagementObjectSearcher(
            @"root\SecurityCenter2",
            "SELECT displayName, pathToSignedProductExe, pathToSignedReportingExe FROM AntiVirusProduct");

        foreach (ManagementObject obj in searcher.Get())
        {
            var displayName = obj["displayName"]?.ToString()?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(displayName))
            {
                continue;
            }

            results.Add(new WscProduct(
                displayName,
                obj["pathToSignedProductExe"]?.ToString() ?? string.Empty,
                obj["pathToSignedReportingExe"]?.ToString() ?? string.Empty));
        }

        return results;
    }

    private static string? GetFileVersion(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var expandedPath = System.Environment.ExpandEnvironmentVariables(path.Trim().Trim('"'));
        if (!File.Exists(expandedPath))
        {
            return null;
        }

        var versionInfo = FileVersionInfo.GetVersionInfo(expandedPath);
        return string.IsNullOrWhiteSpace(versionInfo.ProductVersion)
            ? null
            : versionInfo.ProductVersion;
    }

    private static bool IsDefender(string displayName)
        => displayName.Contains("Windows Defender", StringComparison.OrdinalIgnoreCase)
            || displayName.Contains("Microsoft Defender", StringComparison.OrdinalIgnoreCase);

    private static AvInfo Unknown()
        => new("unknown", "unknown");

    private sealed record WscProduct(string DisplayName, string ProductExePath, string ReportingExePath);
}
