using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using AvBench.Core.Internal;
using AvBench.Core.Models;

namespace AvBench.Core.Microbench;

public static partial class MicrobenchWorker
{
    private static MicrobenchMetrics ExecuteThreadCreate(int totalOperations)
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

    private static MicrobenchMetrics ExecuteComCreateInstance(int totalOperations)
    {
        var progIdType = Type.GetTypeFromProgID("Scripting.FileSystemObject", throwOnError: false)
            ?? throw new InvalidOperationException("COM ProgID 'Scripting.FileSystemObject' is not registered on this VM.");

        var histogram = new LatencyHistogram(totalOperations);
        var stopwatch = Stopwatch.StartNew();

        for (var index = 0; index < totalOperations; index++)
        {
            var start = Stopwatch.GetTimestamp();
            var comObject = Activator.CreateInstance(progIdType)
                ?? throw new InvalidOperationException("Activator.CreateInstance returned null for Scripting.FileSystemObject.");

            if (Marshal.IsComObject(comObject))
            {
                Marshal.FinalReleaseComObject(comObject);
            }

            histogram.Record(Stopwatch.GetTimestamp() - start);
        }

        stopwatch.Stop();
        return BuildMetrics(1, totalOperations, stopwatch.Elapsed, histogram);
    }

    private static MicrobenchMetrics ExecuteWmiQuery(int totalOperations)
    {
        var processId = System.Environment.ProcessId;
        var query = $"SELECT ProcessId, Name FROM Win32_Process WHERE ProcessId = {processId}";
        var histogram = new LatencyHistogram(totalOperations);
        var stopwatch = Stopwatch.StartNew();

        for (var index = 0; index < totalOperations; index++)
        {
            var start = Stopwatch.GetTimestamp();
            using var searcher = new ManagementObjectSearcher(query);
            using var results = searcher.Get();
            foreach (ManagementObject item in results)
            {
                _ = item["Name"];
                item.Dispose();
            }

            histogram.Record(Stopwatch.GetTimestamp() - start);
        }

        stopwatch.Stop();
        return BuildMetrics(1, totalOperations, stopwatch.Elapsed, histogram);
    }

    private static MicrobenchMetrics ExecuteFsWatcher(string root, int totalOperations)
    {
        var watchDirectory = Path.Combine(root, "watched");
        Directory.CreateDirectory(watchDirectory);
        var payload = new byte[64];
        Random.Shared.NextBytes(payload);

        var notificationsReceived = 0;
        using var watcher = new FileSystemWatcher(watchDirectory)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
            Filter = "*.*",
            IncludeSubdirectories = false,
            EnableRaisingEvents = true
        };
        watcher.Created += (_, _) => Interlocked.Increment(ref notificationsReceived);
        watcher.Changed += (_, _) => Interlocked.Increment(ref notificationsReceived);
        watcher.Deleted += (_, _) => Interlocked.Increment(ref notificationsReceived);

        var histogram = new LatencyHistogram(totalOperations);
        var stopwatch = Stopwatch.StartNew();

        for (var index = 0; index < totalOperations; index++)
        {
            var path = Path.Combine(watchDirectory, $"watch_{index:D5}.tmp");
            var start = Stopwatch.GetTimestamp();
            File.WriteAllBytes(path, payload);
            File.AppendAllText(path, "x");
            File.Delete(path);
            histogram.Record(Stopwatch.GetTimestamp() - start);
        }

        stopwatch.Stop();
        Thread.Sleep(100);
        GC.KeepAlive(notificationsReceived);
        Directory.Delete(watchDirectory, recursive: true);
        return BuildMetrics(1, totalOperations, stopwatch.Elapsed, histogram);
    }

    private static void NoOpThreadStart()
    {
    }
}
