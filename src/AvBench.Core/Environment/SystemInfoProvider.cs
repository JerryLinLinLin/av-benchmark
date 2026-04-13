using System.Reflection;
using System.Runtime.InteropServices;
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
