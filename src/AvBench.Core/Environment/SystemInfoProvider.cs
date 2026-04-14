using System.Reflection;
using System.Runtime.InteropServices;
using AvBench.Core.Internal;
using AvBench.Core.Models;

namespace AvBench.Core.Environment;

public static class SystemInfoProvider
{
    public static string GetRunnerVersion()
    {
        return Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3)
            ?? Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)
            ?? "0.1.0";
    }

    public static MachineInfo CollectMachineInfo()
    {
        var memory = GetPhysicalMemoryBytes();

        return new MachineInfo
        {
            Os = System.Environment.OSVersion.VersionString,
            Cpu = $"{System.Environment.ProcessorCount} vCPU",
            RamGb = (int)Math.Max(1, memory / (1024d * 1024d * 1024d)),
            Storage = "unknown"
        };
    }

    public static AvInfo CollectAvInfo()
    {
        var workingDirectory = Directory.GetCurrentDirectory();

        var defenderProduct = TryGetPowerShellValue(
            "(Get-MpComputerStatus).AMProductVersion",
            workingDirectory);

        if (!string.IsNullOrWhiteSpace(defenderProduct))
        {
            return new AvInfo
            {
                Product = "Microsoft Defender Antivirus",
                Version = defenderProduct
            };
        }

        var productName = TryGetPowerShellValue(
            "(Get-CimInstance -Namespace root/SecurityCenter2 -ClassName AntiVirusProduct | Select-Object -First 1 -ExpandProperty displayName)",
            workingDirectory);

        return new AvInfo
        {
            Product = string.IsNullOrWhiteSpace(productName) ? "unknown" : productName,
            Version = "unknown"
        };
    }

    private static string? TryGetPowerShellValue(string script, string workingDirectory)
    {
        try
        {
            var escapedScript = script.Replace("\"", "`\"", StringComparison.Ordinal);
            var result = ProcessUtil.RunAsync(
                "powershell",
                $"-NoProfile -NonInteractive -Command \"{escapedScript}\"",
                workingDirectory,
                CancellationToken.None).GetAwaiter().GetResult();

            return result.ExitCode == 0
                ? result.Stdout.Trim()
                : null;
        }
        catch
        {
            return null;
        }
    }

    private static ulong GetPhysicalMemoryBytes()
    {
        var status = new MEMORYSTATUSEX();
        if (!GlobalMemoryStatusEx(status))
        {
            return 0;
        }

        return status.ullTotalPhys;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX buffer);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private sealed class MEMORYSTATUSEX
    {
        public uint dwLength = (uint)Marshal.SizeOf<MEMORYSTATUSEX>();
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }
}

public sealed class AvInfo
{
    public string Product { get; init; } = "unknown";

    public string Version { get; init; } = "unknown";
}
