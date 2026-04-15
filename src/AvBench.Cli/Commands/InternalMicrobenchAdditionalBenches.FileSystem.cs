using System.ComponentModel;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32.SafeHandles;
using AvBench.Core.Internal;
using AvBench.Core.Models;

namespace AvBench.Cli.Commands;

[SupportedOSPlatform("windows")]
internal static partial class InternalMicrobenchAdditionalBenches
{
    private const int LargeEnumFileCount = 10_000;
    private const int LargeFileSizeMb = 100;
    private const int MemPageSize = 4096;
    private const uint MemCommit = 0x1000;
    private const uint MemReserve = 0x2000;
    private const uint MemRelease = 0x8000;
    private const uint PageReadWrite = 0x04;
    private const uint PageExecuteRead = 0x20;
    private const int FileFlagOpenReparsePoint = 0x00200000;
    private const int FileFlagBackupSemantics = 0x02000000;
    private const int OpenExisting = 3;
    private const int GenericWrite = unchecked((int)0x40000000);
    private const int FsctlSetReparsePoint = 0x000900A4;
    private const int IoReparseTagMountPoint = unchecked((int)0xA0000003);

    public static MicrobenchMetrics ExecuteFileEnumLargeDir(string root, int iterations)
    {
        var datasetDirectory = EnsureEnumDataset(root);
        var histogram = new LatencyHistogram(iterations);
        var stopwatch = Stopwatch.StartNew();

        for (var index = 0; index < iterations; index++)
        {
            var start = Stopwatch.GetTimestamp();
            var count = 0;
            foreach (var _ in Directory.EnumerateFiles(datasetDirectory))
            {
                count++;
            }

            if (count != LargeEnumFileCount)
            {
                throw new InvalidOperationException($"Expected {LargeEnumFileCount} files but enumerated {count} in {datasetDirectory}.");
            }

            histogram.Record(Stopwatch.GetTimestamp() - start);
        }

        stopwatch.Stop();
        return BuildMetrics(1, iterations, stopwatch.Elapsed, histogram);
    }

    public static MicrobenchMetrics ExecuteFileCopyLarge(string root, int iterations)
    {
        var sourcePath = EnsureLargeSourceFile(root);
        var histogram = new LatencyHistogram(iterations);
        var stopwatch = Stopwatch.StartNew();

        for (var index = 0; index < iterations; index++)
        {
            var destinationPath = Path.Combine(root, $"large_copy_{index:D2}.bin");
            var start = Stopwatch.GetTimestamp();
            File.Copy(sourcePath, destinationPath, overwrite: true);
            File.Delete(destinationPath);
            histogram.Record(Stopwatch.GetTimestamp() - start);
        }

        stopwatch.Stop();
        return BuildMetrics(1, iterations, stopwatch.Elapsed, histogram);
    }

    public static MicrobenchMetrics ExecuteHardlinkCreate(string root, int totalOperations)
    {
        var sourcePath = Path.Combine(root, "hardlink_source.dat");
        File.WriteAllBytes(sourcePath, new byte[MemPageSize]);

        var histogram = new LatencyHistogram(totalOperations);
        var stopwatch = Stopwatch.StartNew();

        for (var index = 0; index < totalOperations; index++)
        {
            var linkPath = Path.Combine(root, $"hlink_{index:D5}.dat");
            var start = Stopwatch.GetTimestamp();
            if (!CreateHardLink(linkPath, sourcePath, IntPtr.Zero))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"CreateHardLink failed for {linkPath}.");
            }

            File.Delete(linkPath);
            histogram.Record(Stopwatch.GetTimestamp() - start);
        }

        stopwatch.Stop();
        File.Delete(sourcePath);
        return BuildMetrics(1, totalOperations, stopwatch.Elapsed, histogram);
    }

    public static MicrobenchMetrics ExecuteJunctionCreate(string root, int totalOperations)
    {
        var targetPath = Path.Combine(root, "junction_target");
        Directory.CreateDirectory(targetPath);

        var histogram = new LatencyHistogram(totalOperations);
        var stopwatch = Stopwatch.StartNew();

        for (var index = 0; index < totalOperations; index++)
        {
            var junctionPath = Path.Combine(root, $"junction_{index:D5}");
            var start = Stopwatch.GetTimestamp();
            Directory.CreateDirectory(junctionPath);
            CreateMountPointJunction(junctionPath, targetPath);
            Directory.Delete(junctionPath);
            histogram.Record(Stopwatch.GetTimestamp() - start);
        }

        stopwatch.Stop();
        Directory.Delete(targetPath);
        return BuildMetrics(1, totalOperations, stopwatch.Elapsed, histogram);
    }

    public static MicrobenchMetrics ExecuteThreadCreate(int totalOperations)
    {
        var histogram = new LatencyHistogram(totalOperations);
        var stopwatch = Stopwatch.StartNew();

        for (var index = 0; index < totalOperations; index++)
        {
            var start = Stopwatch.GetTimestamp();
            var thread = new Thread(NoOpThreadStart)
            {
                IsBackground = true
            };
            thread.Start();
            thread.Join();
            histogram.Record(Stopwatch.GetTimestamp() - start);
        }

        stopwatch.Stop();
        return BuildMetrics(1, totalOperations, stopwatch.Elapsed, histogram);
    }

    public static MicrobenchMetrics ExecuteMemAllocProtect(int totalOperations)
    {
        var histogram = new LatencyHistogram(totalOperations);
        var stopwatch = Stopwatch.StartNew();

        for (var index = 0; index < totalOperations; index++)
        {
            var start = Stopwatch.GetTimestamp();
            var pointer = VirtualAlloc(IntPtr.Zero, (nuint)MemPageSize, MemCommit | MemReserve, PageReadWrite);
            if (pointer == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "VirtualAlloc failed.");
            }

            Marshal.WriteByte(pointer, 0, 0x41);
            if (!VirtualProtect(pointer, (nuint)MemPageSize, PageExecuteRead, out _))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "VirtualProtect failed.");
            }

            if (!VirtualFree(pointer, 0, MemRelease))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "VirtualFree failed.");
            }

            histogram.Record(Stopwatch.GetTimestamp() - start);
        }

        stopwatch.Stop();
        return BuildMetrics(1, totalOperations, stopwatch.Elapsed, histogram);
    }

    public static MicrobenchMetrics ExecuteMemMapFile(string root, int totalOperations)
    {
        var backingPath = Path.Combine(root, "mmap-backing.bin");
        if (!File.Exists(backingPath))
        {
            File.WriteAllBytes(backingPath, new byte[MemPageSize]);
        }

        var histogram = new LatencyHistogram(totalOperations);
        var stopwatch = Stopwatch.StartNew();

        for (var index = 0; index < totalOperations; index++)
        {
            var start = Stopwatch.GetTimestamp();
            using (var mappedFile = MemoryMappedFile.CreateFromFile(backingPath, FileMode.Open, null, MemPageSize, MemoryMappedFileAccess.ReadWrite))
            using (var accessor = mappedFile.CreateViewAccessor(0, MemPageSize, MemoryMappedFileAccess.ReadWrite))
            {
                accessor.Write(0, (byte)(index & 0xFF));
                _ = accessor.ReadByte(0);
            }
            histogram.Record(Stopwatch.GetTimestamp() - start);
        }

        stopwatch.Stop();
        return BuildMetrics(1, totalOperations, stopwatch.Elapsed, histogram);
    }

    private static string EnsureEnumDataset(string root)
    {
        var datasetDirectory = Path.Combine(root, "enum_dataset");
        if (Directory.Exists(datasetDirectory) && Directory.EnumerateFiles(datasetDirectory).Take(LargeEnumFileCount + 1).Count() == LargeEnumFileCount)
        {
            return datasetDirectory;
        }

        DeletePathIfExists(datasetDirectory);
        Directory.CreateDirectory(datasetDirectory);

        var rng = new Random(42);
        string[] extensions = [".cs", ".js", ".json", ".xml", ".dll", ".exe", ".txt", ".md", ".config", ".props"];
        var buffer = new byte[256];

        for (var index = 0; index < LargeEnumFileCount; index++)
        {
            rng.NextBytes(buffer);
            var extension = extensions[index % extensions.Length];
            File.WriteAllBytes(Path.Combine(datasetDirectory, $"file_{index:D5}{extension}"), buffer);
        }

        return datasetDirectory;
    }

    private static string EnsureLargeSourceFile(string root)
    {
        var sourcePath = Path.Combine(root, "large_source.bin");
        if (File.Exists(sourcePath) && new FileInfo(sourcePath).Length == LargeFileSizeMb * 1024L * 1024L)
        {
            return sourcePath;
        }

        DeletePathIfExists(sourcePath);
        var buffer = new byte[1024 * 1024];
        var rng = new Random(42);
        using var stream = File.Create(sourcePath);
        for (var index = 0; index < LargeFileSizeMb; index++)
        {
            rng.NextBytes(buffer);
            stream.Write(buffer, 0, buffer.Length);
        }

        return sourcePath;
    }

    private static void CreateMountPointJunction(string junctionPath, string targetPath)
    {
        var printName = Path.GetFullPath(targetPath);
        var substituteName = $@"\??\{printName}";
        var substituteBytes = Encoding.Unicode.GetBytes(substituteName);
        var printBytes = Encoding.Unicode.GetBytes(printName);
        var reparseDataLength = 8 + substituteBytes.Length + 2 + printBytes.Length + 2;
        var buffer = new byte[8 + reparseDataLength];

        using (var stream = new MemoryStream(buffer))
        using (var writer = new BinaryWriter(stream, Encoding.Unicode, leaveOpen: true))
        {
            writer.Write(IoReparseTagMountPoint);
            writer.Write((ushort)reparseDataLength);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write((ushort)substituteBytes.Length);
            writer.Write((ushort)(substituteBytes.Length + 2));
            writer.Write((ushort)printBytes.Length);
            writer.Write(substituteBytes);
            writer.Write((ushort)0);
            writer.Write(printBytes);
            writer.Write((ushort)0);
        }

        using var handle = CreateFile(
            junctionPath,
            GenericWrite,
            0,
            IntPtr.Zero,
            OpenExisting,
            FileFlagOpenReparsePoint | FileFlagBackupSemantics,
            IntPtr.Zero);

        if (handle.IsInvalid)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"CreateFile failed for junction path {junctionPath}.");
        }

        if (!DeviceIoControl(handle, FsctlSetReparsePoint, buffer, buffer.Length, null, 0, out _, IntPtr.Zero))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"DeviceIoControl(FSCTL_SET_REPARSE_POINT) failed for {junctionPath}.");
        }
    }

    private static void NoOpThreadStart()
    {
    }

    private static void DeletePathIfExists(string path)
    {
        if (Directory.Exists(path))
        {
            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            foreach (var directory in Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(directory, FileAttributes.Normal);
            }

            Directory.Delete(path, recursive: true);
            return;
        }

        if (File.Exists(path))
        {
            File.SetAttributes(path, FileAttributes.Normal);
            File.Delete(path);
        }
    }

    private static MicrobenchMetrics BuildMetrics(int batchSize, int totalOperations, TimeSpan elapsed, LatencyHistogram histogram)
    {
        return new MicrobenchMetrics
        {
            BatchSize = batchSize,
            TotalOperations = totalOperations,
            OpsPerSec = totalOperations / Math.Max(elapsed.TotalSeconds, 0.000001),
            MeanLatencyUs = histogram.MeanUs,
            P50Us = histogram.GetPercentile(50),
            P95Us = histogram.GetPercentile(95),
            P99Us = histogram.GetPercentile(99),
            MaxUs = histogram.MaxUs
        };
    }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr VirtualAlloc(IntPtr lpAddress, nuint dwSize, uint flAllocationType, uint flProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool VirtualProtect(IntPtr lpAddress, nuint dwSize, uint flNewProtect, out uint lpflOldProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool VirtualFree(IntPtr lpAddress, nuint dwSize, uint dwFreeType);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern SafeFileHandle CreateFile(
        string lpFileName,
        int dwDesiredAccess,
        int dwShareMode,
        IntPtr lpSecurityAttributes,
        int dwCreationDisposition,
        int dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(
        SafeFileHandle hDevice,
        int dwIoControlCode,
        byte[] lpInBuffer,
        int nInBufferSize,
        byte[]? lpOutBuffer,
        int nOutBufferSize,
        out int lpBytesReturned,
        IntPtr lpOverlapped);
}
