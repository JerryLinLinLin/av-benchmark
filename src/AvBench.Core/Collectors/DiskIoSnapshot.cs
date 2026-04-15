using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading;

namespace AvBench.Core.Collectors;

[SupportedOSPlatform("windows")]
public sealed class DiskIoSnapshot : IDisposable
{
    private static int s_loggedUnavailableWarning;

    private readonly PerformanceCounter? _readCounter;
    private readonly PerformanceCounter? _writeCounter;
    private long _startReadBytes;
    private long _startWriteBytes;

    public DiskIoSnapshot()
    {
        try
        {
            _readCounter = new PerformanceCounter("PhysicalDisk", "Disk Read Bytes/sec", "_Total", readOnly: true);
            _writeCounter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total", readOnly: true);
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or PlatformNotSupportedException)
        {
            LogUnavailableWarning(ex.Message);
        }
    }

    public void SnapshotBefore()
    {
        if (_readCounter is null || _writeCounter is null)
        {
            return;
        }

        _startReadBytes = _readCounter.NextSample().RawValue;
        _startWriteBytes = _writeCounter.NextSample().RawValue;
    }

    public (long ReadBytes, long WriteBytes) SnapshotAfter()
    {
        if (_readCounter is null || _writeCounter is null)
        {
            return (0, 0);
        }

        var endReadBytes = _readCounter.NextSample().RawValue;
        var endWriteBytes = _writeCounter.NextSample().RawValue;

        return (ClampNonNegative(endReadBytes - _startReadBytes), ClampNonNegative(endWriteBytes - _startWriteBytes));
    }

    public void Dispose()
    {
        _readCounter?.Dispose();
        _writeCounter?.Dispose();
    }

    private static long ClampNonNegative(long value)
        => value < 0 ? 0 : value;

    private static void LogUnavailableWarning(string message)
    {
        if (Interlocked.Exchange(ref s_loggedUnavailableWarning, 1) == 0)
        {
            Console.WriteLine($"[disk] WARNING: system-wide disk counters are unavailable; disk byte metrics will be recorded as 0. {message}");
        }
    }
}
