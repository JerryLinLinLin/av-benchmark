using System.ComponentModel;
using System.Runtime.InteropServices;

namespace AvBench.Core.Runner;

public sealed class JobObject : IDisposable
{
    private readonly IntPtr _handle;
    private bool _disposed;

    public JobObject()
    {
        _handle = NativeMethods.CreateJobObject(IntPtr.Zero, null);
        if (_handle == IntPtr.Zero)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "CreateJobObject failed.");
        }

        var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
            {
                LimitFlags = JobObjectLimitFlags.JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
            }
        };

        var length = Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();
        var buffer = Marshal.AllocHGlobal(length);
        try
        {
            Marshal.StructureToPtr(info, buffer, false);
            if (!NativeMethods.SetInformationJobObject(_handle, JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation, buffer, (uint)length))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "SetInformationJobObject failed.");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    public void AssignProcess(IntPtr processHandle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!NativeMethods.AssignProcessToJobObject(_handle, processHandle))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), "AssignProcessToJobObject failed.");
        }
    }

    public JobAccountingSnapshot QueryAccounting()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var basicAndIo = Query<JOBOBJECT_BASIC_AND_IO_ACCOUNTING_INFORMATION>(JOBOBJECTINFOCLASS.JobObjectBasicAndIoAccountingInformation);
        var extended = Query<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>(JOBOBJECTINFOCLASS.JobObjectExtendedLimitInformation);

        return new JobAccountingSnapshot
        {
            TotalUserTimeMs = (long)TimeSpan.FromTicks(basicAndIo.BasicInfo.TotalUserTime).TotalMilliseconds,
            TotalKernelTimeMs = (long)TimeSpan.FromTicks(basicAndIo.BasicInfo.TotalKernelTime).TotalMilliseconds,
            TotalProcesses = basicAndIo.BasicInfo.TotalProcesses,
            PeakJobMemoryBytes = extended.PeakJobMemoryUsed.ToUInt64(),
            IoReadBytes = basicAndIo.IoInfo.ReadTransferCount,
            IoWriteBytes = basicAndIo.IoInfo.WriteTransferCount,
            IoReadOps = basicAndIo.IoInfo.ReadOperationCount,
            IoWriteOps = basicAndIo.IoInfo.WriteOperationCount
        };
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        NativeMethods.CloseHandle(_handle);
        _disposed = true;
    }

    private T Query<T>(JOBOBJECTINFOCLASS infoClass) where T : struct
    {
        var length = Marshal.SizeOf<T>();
        var buffer = Marshal.AllocHGlobal(length);
        try
        {
            if (!NativeMethods.QueryInformationJobObject(_handle, infoClass, buffer, (uint)length, out _))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"QueryInformationJobObject failed for {infoClass}.");
            }

            return Marshal.PtrToStructure<T>(buffer);
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static class NativeMethods
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr CreateJobObject(IntPtr lpJobAttributes, string? lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetInformationJobObject(
            IntPtr hJob,
            JOBOBJECTINFOCLASS jobObjectInfoClass,
            IntPtr lpJobObjectInfo,
            uint cbJobObjectInfoLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool QueryInformationJobObject(
            IntPtr hJob,
            JOBOBJECTINFOCLASS jobObjectInfoClass,
            IntPtr lpJobObjectInfo,
            uint cbJobObjectInfoLength,
            out uint lpReturnLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(IntPtr hObject);
    }
}

public sealed class JobAccountingSnapshot
{
    public long TotalUserTimeMs { get; init; }

    public long TotalKernelTimeMs { get; init; }

    public uint TotalProcesses { get; init; }

    public ulong PeakJobMemoryBytes { get; init; }

    public ulong IoReadBytes { get; init; }

    public ulong IoWriteBytes { get; init; }

    public ulong IoReadOps { get; init; }

    public ulong IoWriteOps { get; init; }
}

[Flags]
internal enum JobObjectLimitFlags : uint
{
    JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x00002000
}

internal enum JOBOBJECTINFOCLASS
{
    JobObjectBasicAndIoAccountingInformation = 8,
    JobObjectExtendedLimitInformation = 9
}

[StructLayout(LayoutKind.Sequential)]
internal struct JOBOBJECT_BASIC_AND_IO_ACCOUNTING_INFORMATION
{
    public JOBOBJECT_BASIC_ACCOUNTING_INFORMATION BasicInfo;
    public IO_COUNTERS IoInfo;
}

[StructLayout(LayoutKind.Sequential)]
internal struct JOBOBJECT_BASIC_ACCOUNTING_INFORMATION
{
    public long TotalUserTime;
    public long TotalKernelTime;
    public long ThisPeriodTotalUserTime;
    public long ThisPeriodTotalKernelTime;
    public uint TotalPageFaultCount;
    public uint TotalProcesses;
    public uint ActiveProcesses;
    public uint TotalTerminatedProcesses;
}

[StructLayout(LayoutKind.Sequential)]
internal struct JOBOBJECT_BASIC_LIMIT_INFORMATION
{
    public long PerProcessUserTimeLimit;
    public long PerJobUserTimeLimit;
    public JobObjectLimitFlags LimitFlags;
    public UIntPtr MinimumWorkingSetSize;
    public UIntPtr MaximumWorkingSetSize;
    public uint ActiveProcessLimit;
    public UIntPtr Affinity;
    public uint PriorityClass;
    public uint SchedulingClass;
}

[StructLayout(LayoutKind.Sequential)]
internal struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
{
    public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
    public IO_COUNTERS IoInfo;
    public UIntPtr ProcessMemoryLimit;
    public UIntPtr JobMemoryLimit;
    public UIntPtr PeakProcessMemoryUsed;
    public UIntPtr PeakJobMemoryUsed;
}

[StructLayout(LayoutKind.Sequential)]
internal struct IO_COUNTERS
{
    public ulong ReadOperationCount;
    public ulong WriteOperationCount;
    public ulong OtherOperationCount;
    public ulong ReadTransferCount;
    public ulong WriteTransferCount;
    public ulong OtherTransferCount;
}

