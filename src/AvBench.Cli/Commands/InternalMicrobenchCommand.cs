using System.CommandLine;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text.Json;
using AvBench.Core.Internal;
using AvBench.Core.Models;
using AvBench.Core.Serialization;

namespace AvBench.Cli.Commands;

[SupportedOSPlatform("windows")]
public static class InternalMicrobenchCommand
{
    public static Command Create()
    {
        var scenarioOption = new Option<string>("--scenario")
        {
            Description = "Internal microbenchmark scenario id."
        };
        scenarioOption.Required = true;

        var rootOption = new Option<DirectoryInfo>("--root")
        {
            Description = "Working directory for the internal microbenchmark."
        };
        rootOption.Required = true;

        var operationsOption = new Option<int>("--operations")
        {
            Description = "Total operations or iterations for the scenario.",
            DefaultValueFactory = _ => 0
        };

        var batchSizeOption = new Option<int>("--batch-size")
        {
            Description = "Batch size for file-create-delete.",
            DefaultValueFactory = _ => 100
        };

        var extensionOption = new Option<string?>("--extension")
        {
            Description = "Extension used by ext-sensitivity scenarios."
        };

        var zipPathOption = new Option<FileInfo?>("--zip-path")
        {
            Description = "Path to the prepared archive zip for archive-extract."
        };

        var unsignedExeOption = new Option<FileInfo?>("--unsigned-exe")
        {
            Description = "Path to the prepared unsigned executable."
        };

        var iterationsOption = new Option<int>("--iterations")
        {
            Description = "Archive extract iterations.",
            DefaultValueFactory = _ => 0
        };

        var applyMotwOption = new Option<bool>("--apply-motw")
        {
            Description = "Apply Mark of the Web before execution.",
            DefaultValueFactory = _ => false
        };

        var command = new Command("internal-microbench")
        {
            Description = "Internal worker for M3 API microbenchmarks.",
            Hidden = true
        };

        command.Options.Add(scenarioOption);
        command.Options.Add(rootOption);
        command.Options.Add(operationsOption);
        command.Options.Add(batchSizeOption);
        command.Options.Add(extensionOption);
        command.Options.Add(zipPathOption);
        command.Options.Add(unsignedExeOption);
        command.Options.Add(iterationsOption);
        command.Options.Add(applyMotwOption);

        command.SetAction(parseResult =>
        {
            var scenario = parseResult.GetValue(scenarioOption)!;
            var root = parseResult.GetValue(rootOption)!;
            var operations = parseResult.GetValue(operationsOption);
            var batchSize = parseResult.GetValue(batchSizeOption);
            var extension = parseResult.GetValue(extensionOption);
            var zipPath = parseResult.GetValue(zipPathOption);
            var unsignedExe = parseResult.GetValue(unsignedExeOption);
            var iterations = parseResult.GetValue(iterationsOption);
            var applyMotw = parseResult.GetValue(applyMotwOption);

            Directory.CreateDirectory(root.FullName);

            var metrics = scenario switch
            {
                "file-create-delete" => ExecuteFileCreateDelete(root.FullName, operations, batchSize),
                "archive-extract" => ExecuteArchiveExtract(root.FullName, zipPath?.FullName ?? throw new InvalidOperationException("--zip-path is required for archive-extract."), iterations),
                "file-enum-large-dir" => InternalMicrobenchAdditionalBenches.ExecuteFileEnumLargeDir(root.FullName, iterations),
                "file-copy-large" => InternalMicrobenchAdditionalBenches.ExecuteFileCopyLarge(root.FullName, iterations),
                "hardlink-create" => InternalMicrobenchAdditionalBenches.ExecuteHardlinkCreate(root.FullName, operations),
                "junction-create" => InternalMicrobenchAdditionalBenches.ExecuteJunctionCreate(root.FullName, operations),
                "process-create-wait" => ExecuteProcessCreateWait(unsignedExe?.FullName ?? throw new InvalidOperationException("--unsigned-exe is required for process-create-wait."), operations),
                "dll-load-unique" => ExecuteDllLoadUnique(root.FullName, operations),
                "file-write-content" or "file-write-pe" => ExecuteFileWriteContent(root.FullName, unsignedExe?.FullName ?? throw new InvalidOperationException("--unsigned-exe is required for file-write-content."), operations),
                "motw-exe-no-motw" => ExecuteMotw(root.FullName, unsignedExe?.FullName ?? throw new InvalidOperationException("--unsigned-exe is required for MOTW scenarios."), operations, applyMotw: false),
                "motw-exe-motw-zone3" => ExecuteMotw(root.FullName, unsignedExe?.FullName ?? throw new InvalidOperationException("--unsigned-exe is required for MOTW scenarios."), operations, applyMotw: true),
                "thread-create" => InternalMicrobenchAdditionalBenches.ExecuteThreadCreate(operations),
                "mem-alloc-protect" => InternalMicrobenchAdditionalBenches.ExecuteMemAllocProtect(operations),
                "mem-map-file" => InternalMicrobenchAdditionalBenches.ExecuteMemMapFile(root.FullName, operations),
                "net-connect-loopback" => InternalMicrobenchAdditionalBenches.ExecuteNetConnectLoopback(operations),
                "net-dns-resolve" => InternalMicrobenchAdditionalBenches.ExecuteDnsResolve(operations),
                "registry-crud" => InternalMicrobenchAdditionalBenches.ExecuteRegistryCrud(operations),
                "pipe-roundtrip" => InternalMicrobenchAdditionalBenches.ExecutePipeRoundtrip(operations),
                "token-query" => InternalMicrobenchAdditionalBenches.ExecuteTokenQuery(operations),
                "crypto-hash-verify" => InternalMicrobenchAdditionalBenches.ExecuteCryptoHashVerify(operations),
                "com-create-instance" => InternalMicrobenchAdditionalBenches.ExecuteComCreateInstance(operations),
                "wmi-query" => InternalMicrobenchAdditionalBenches.ExecuteWmiQuery(operations),
                "fs-watcher" => InternalMicrobenchAdditionalBenches.ExecuteFsWatcher(root.FullName, operations),
                _ when scenario.StartsWith("ext-sensitivity-", StringComparison.OrdinalIgnoreCase)
                    => ExecuteExtensionSensitivity(root.FullName, operations, extension ?? throw new InvalidOperationException("--extension is required for ext-sensitivity scenarios.")),
                _ => throw new InvalidOperationException($"Unknown internal microbench scenario '{scenario}'.")
            };

            Console.Out.Write(JsonSerializer.Serialize(metrics, AvBenchJsonContext.Default.MicrobenchMetrics));
            return 0;
        });

        return command;
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
            DeletePathIfExists(extractDirectory);
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
        var supportFiles = Directory.EnumerateFiles(supportDirectory, $"{baseName}.*", SearchOption.TopDirectoryOnly)
            .ToArray();

        for (var index = 0; index < totalOperations; index++)
        {
            var opDirectory = Path.Combine(root, $"bench_{index:D5}");
            Directory.CreateDirectory(opDirectory);
            var destExe = Path.Combine(opDirectory, Path.GetFileName(unsignedExePath));
            var start = Stopwatch.GetTimestamp();
            foreach (var supportFile in supportFiles)
            {
                File.Copy(
                    supportFile,
                    Path.Combine(opDirectory, Path.GetFileName(supportFile)),
                    overwrite: true);
            }

            if (applyMotw)
            {
                File.WriteAllText(destExe + ":Zone.Identifier", "[ZoneTransfer]\r\nZoneId=3\r\n");
            }

            RunExecutable(destExe);
            DeletePathIfExists(opDirectory);
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
        var systemDirectory = Environment.SystemDirectory;
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
    private static extern IntPtr LoadLibrary(string lpFileName);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool FreeLibrary(IntPtr hModule);
}
