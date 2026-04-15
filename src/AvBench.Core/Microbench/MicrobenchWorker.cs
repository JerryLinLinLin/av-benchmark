using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using AvBench.Core.Internal;
using AvBench.Core.Models;

namespace AvBench.Core.Microbench;

[SupportedOSPlatform("windows")]
public static partial class MicrobenchWorker
{
    public static MicrobenchMetrics Execute(MicrobenchRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ScenarioId);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.RootPath);

        Directory.CreateDirectory(request.RootPath);

        return request.ScenarioId switch
        {
            "file-create-delete" => ExecuteFileCreateDelete(request.RootPath, request.Operations, request.BatchSize),
            "archive-extract" => ExecuteArchiveExtract(
                request.RootPath,
                request.ZipPath ?? throw new InvalidOperationException("--zip-path is required for archive-extract."),
                request.Iterations),
            "file-enum-large-dir" => ExecuteFileEnumLargeDir(request.RootPath, request.Iterations),
            "file-copy-large" => ExecuteFileCopyLarge(request.RootPath, request.Iterations),
            "hardlink-create" => ExecuteHardlinkCreate(request.RootPath, request.Operations),
            "junction-create" => ExecuteJunctionCreate(request.RootPath, request.Operations),
            "process-create-wait" => ExecuteProcessCreateWait(
                request.UnsignedExePath ?? throw new InvalidOperationException("--unsigned-exe is required for process-create-wait."),
                request.Operations),
            "dll-load-unique" => ExecuteDllLoadUnique(request.RootPath, request.Operations),
            "file-write-content" or "file-write-pe" => ExecuteFileWriteContent(
                request.RootPath,
                request.UnsignedExePath ?? throw new InvalidOperationException("--unsigned-exe is required for file-write-content."),
                request.Operations),
            "motw-exe-no-motw" => ExecuteMotw(
                request.RootPath,
                request.UnsignedExePath ?? throw new InvalidOperationException("--unsigned-exe is required for MOTW scenarios."),
                request.Operations,
                applyMotw: false),
            "motw-exe-motw-zone3" => ExecuteMotw(
                request.RootPath,
                request.UnsignedExePath ?? throw new InvalidOperationException("--unsigned-exe is required for MOTW scenarios."),
                request.Operations,
                applyMotw: true),
            "thread-create" => ExecuteThreadCreate(request.Operations),
            "mem-alloc-protect" => ExecuteMemAllocProtect(request.Operations),
            "mem-map-file" => ExecuteMemMapFile(request.RootPath, request.Operations),
            "net-connect-loopback" => ExecuteNetConnectLoopback(request.Operations),
            "net-dns-resolve" => ExecuteDnsResolve(request.Operations),
            "registry-crud" => ExecuteRegistryCrud(request.Operations),
            "pipe-roundtrip" => ExecutePipeRoundtrip(request.Operations),
            "token-query" => ExecuteTokenQuery(request.Operations),
            "crypto-hash-verify" => ExecuteCryptoHashVerify(request.Operations),
            "com-create-instance" => ExecuteComCreateInstance(request.Operations),
            "wmi-query" => ExecuteWmiQuery(request.Operations),
            "fs-watcher" => ExecuteFsWatcher(request.RootPath, request.Operations),
            _ when request.ScenarioId.StartsWith("ext-sensitivity-", StringComparison.OrdinalIgnoreCase)
                => ExecuteExtensionSensitivity(
                    request.RootPath,
                    request.Operations,
                    request.Extension ?? throw new InvalidOperationException("--extension is required for ext-sensitivity scenarios.")),
            _ => throw new InvalidOperationException($"Unknown internal microbench scenario '{request.ScenarioId}'.")
        };
    }

    private static MicrobenchMetrics ExecuteFileCreateDelete(string root, int totalOperations, int batchSize)
    {
        var histogram = new LatencyHistogram(totalOperations);
        var stopwatch = Stopwatch.StartNew();
        RunBatchedFileCreateDelete(root, totalOperations, batchSize, histogram);
        stopwatch.Stop();
        return BuildMetrics(batchSize, totalOperations, stopwatch.Elapsed, histogram);
    }

    private static MicrobenchMetrics ExecuteArchiveExtract(string root, string zipPath, int iterations)
    {
        var histogram = new LatencyHistogram(iterations);
        var stopwatch = Stopwatch.StartNew();

        for (var index = 0; index < iterations; index++)
        {
            var extractDirectory = Path.Combine(root, $"extract_{index:D2}");
            var start = Stopwatch.GetTimestamp();
            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractDirectory);
            FileSystemUtil.DeletePathIfExists(extractDirectory);
            histogram.Record(Stopwatch.GetTimestamp() - start);
        }

        stopwatch.Stop();
        return BuildMetrics(1, iterations, stopwatch.Elapsed, histogram);
    }

    private static MicrobenchMetrics ExecuteProcessCreateWait(string unsignedExePath, int totalOperations)
    {
        var histogram = new LatencyHistogram(totalOperations);
        var stopwatch = Stopwatch.StartNew();

        for (var index = 0; index < totalOperations; index++)
        {
            var start = Stopwatch.GetTimestamp();
            RunExecutable(unsignedExePath);
            histogram.Record(Stopwatch.GetTimestamp() - start);
        }

        stopwatch.Stop();
        return BuildMetrics(1, totalOperations, stopwatch.Elapsed, histogram);
    }

    private static MicrobenchMetrics ExecuteExtensionSensitivity(string root, int operations, string extension)
    {
        var histogram = new LatencyHistogram(operations);
        var content = new byte[4096];
        Random.Shared.NextBytes(content);
        var stopwatch = Stopwatch.StartNew();

        for (var index = 0; index < operations; index++)
        {
            var path = Path.Combine(root, $"bench_{index:D5}{extension}");
            var start = Stopwatch.GetTimestamp();
            File.WriteAllBytes(path, content);
            File.Delete(path);
            histogram.Record(Stopwatch.GetTimestamp() - start);
        }

        stopwatch.Stop();
        return BuildMetrics(1, operations, stopwatch.Elapsed, histogram);
    }

    private static MicrobenchMetrics ExecuteDllLoadUnique(string root, int totalOperations)
    {
        var histogram = new LatencyHistogram(totalOperations);
        var sourceDll = ResolveSystemDllPath();
        var stopwatch = Stopwatch.StartNew();

        for (var index = 0; index < totalOperations; index++)
        {
            var dllPath = Path.Combine(root, $"bench_{index:D5}.dll");
            var start = Stopwatch.GetTimestamp();
            File.Copy(sourceDll, dllPath, overwrite: true);
            var handle = LoadLibrary(dllPath);
            if (handle == IntPtr.Zero)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), $"LoadLibrary failed for {dllPath}");
            }

            FreeLibrary(handle);
            File.Delete(dllPath);
            histogram.Record(Stopwatch.GetTimestamp() - start);
        }

        stopwatch.Stop();
        return BuildMetrics(1, totalOperations, stopwatch.Elapsed, histogram);
    }

    private static MicrobenchMetrics ExecuteFileWriteContent(string root, string unsignedExePath, int totalOperations)
    {
        var template = File.ReadAllBytes(unsignedExePath);
        var buffer = new byte[template.Length];
        string[] extensions = [".exe", ".dll"];
        var histogram = new LatencyHistogram(totalOperations);
        var stopwatch = Stopwatch.StartNew();

        for (var index = 0; index < totalOperations; index++)
        {
            Buffer.BlockCopy(template, 0, buffer, 0, template.Length);
            buffer[0x40] = (byte)index;
            buffer[0x41] = (byte)(index >> 8);
            buffer[0x42] = (byte)(index >> 16);
            buffer[0x43] = (byte)(index >> 24);

            var path = Path.Combine(root, $"bench_{index:D5}{extensions[index % extensions.Length]}");
            var start = Stopwatch.GetTimestamp();
            File.WriteAllBytes(path, buffer);
            File.Delete(path);
            histogram.Record(Stopwatch.GetTimestamp() - start);
        }

        stopwatch.Stop();
        return BuildMetrics(1, totalOperations, stopwatch.Elapsed, histogram);
    }

    private static MicrobenchMetrics ExecuteMotw(string root, string unsignedExePath, int totalOperations, bool applyMotw)
    {
        var histogram = new LatencyHistogram(totalOperations);
        var stopwatch = Stopwatch.StartNew();
        var supportDirectory = Path.GetDirectoryName(unsignedExePath)
            ?? throw new InvalidOperationException("Unsigned executable path does not have a parent directory.");
        var baseName = Path.GetFileNameWithoutExtension(unsignedExePath);
        var supportFiles = Directory.EnumerateFiles(supportDirectory, $"{baseName}.*", SearchOption.TopDirectoryOnly).ToArray();

        for (var index = 0; index < totalOperations; index++)
        {
            var opDirectory = Path.Combine(root, $"bench_{index:D5}");
            Directory.CreateDirectory(opDirectory);
            var destExe = Path.Combine(opDirectory, Path.GetFileName(unsignedExePath));
            var start = Stopwatch.GetTimestamp();

            foreach (var supportFile in supportFiles)
            {
                File.Copy(supportFile, Path.Combine(opDirectory, Path.GetFileName(supportFile)), overwrite: true);
            }

            if (applyMotw)
            {
                File.WriteAllText(destExe + ":Zone.Identifier", "[ZoneTransfer]\r\nZoneId=3\r\n");
            }

            RunExecutable(destExe);
            FileSystemUtil.DeletePathIfExists(opDirectory);
            histogram.Record(Stopwatch.GetTimestamp() - start);
        }

        stopwatch.Stop();
        return BuildMetrics(1, totalOperations, stopwatch.Elapsed, histogram);
    }

    private static void RunBatchedFileCreateDelete(string root, int totalOperations, int batchSize, LatencyHistogram histogram)
    {
        Span<byte> data = stackalloc byte[64];
        var completed = 0;

        while (completed < totalOperations)
        {
            var currentBatch = Math.Min(batchSize, totalOperations - completed);
            for (var index = 0; index < currentBatch; index++)
            {
                var operationIndex = completed + index;
                var path = Path.Combine(root, $"bench_{operationIndex:D5}.tmp");
                var start = Stopwatch.GetTimestamp();
                using (var stream = File.Create(path))
                {
                    stream.Write(data);
                }

                File.Delete(path);
                histogram.Record(Stopwatch.GetTimestamp() - start);
            }

            completed += currentBatch;
        }
    }

    private static void RunExecutable(string exePath)
    {
        using var process = Process.Start(new ProcessStartInfo(exePath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }) ?? throw new InvalidOperationException($"Failed to start {exePath}");

        process.StandardOutput.ReadToEnd();
        process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"{exePath} exited with code {process.ExitCode}.");
        }
    }

    private static string ResolveSystemDllPath()
    {
        var systemDirectory = System.Environment.SystemDirectory;
        foreach (var candidate in new[] { "urlmon.dll", "kernel32.dll", "ntdll.dll" })
        {
            var path = Path.Combine(systemDirectory, candidate);
            if (File.Exists(path))
            {
                return path;
            }
        }

        throw new FileNotFoundException("Unable to locate a system DLL for dll-load-unique.");
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
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FreeLibrary(IntPtr hModule);
}
