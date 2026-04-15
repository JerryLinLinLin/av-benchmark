using System.Diagnostics;
using System.Runtime.Versioning;
using AvBench.Core.Microbench;
using AvBench.Core.Models;

namespace AvBench.Core.Scenarios;

[SupportedOSPlatform("windows")]
public static class MicrobenchScenarioFactory
{
    private const int FileCreateDeleteOperations = 5_000;
    private const int FileCreateDeleteBatchSize = 100;
    private const int ArchiveExtractIterations = 10;
    private const int FileEnumLargeDirIterations = 50;
    private const int FileCopyLargeIterations = 10;
    private const int HardlinkCreateOperations = 5_000;
    private const int JunctionCreateOperations = 2_000;
    private const int ProcessCreateOperations = 500;
    private const int ExtensionSensitivityOperations = 10_000;
    private const int DllLoadOperations = 2_000;
    private const int FileWriteContentOperations = 10_000;
    private const int MotwOperations = 500;
    private const int ThreadCreateOperations = 5_000;
    private const int MemAllocProtectOperations = 50_000;
    private const int MemMapFileOperations = 10_000;
    private const int NetConnectLoopbackOperations = 2_000;
    private const int NetDnsResolveOperations = 5_000;
    private const int RegistryCrudOperations = 5_000;
    private const int PipeRoundtripOperations = 2_000;
    private const int TokenQueryOperations = 50_000;
    private const int CryptoHashVerifyOperations = 5_000;
    private const int ComCreateInstanceOperations = 5_000;
    private const int WmiQueryOperations = 500;
    private const int FsWatcherOperations = 5_000;

    public static IReadOnlyList<ScenarioDefinition> Create(SuiteManifest manifest)
    {
        var support = manifest.GetRequiredMicrobenchSupport();
        MicrobenchSupport.ValidateManifestEntry(support);
        var runRoot = support.RunRoot;

        return
        [
            CreateScenario(
                runRoot,
                support,
                "file-create-delete",
                new MicrobenchRequest
                {
                    ScenarioId = "file-create-delete",
                    RootPath = Path.Combine(runRoot, "file-create-delete"),
                    Operations = FileCreateDeleteOperations,
                    BatchSize = FileCreateDeleteBatchSize
                }),
            CreateScenario(
                runRoot,
                support,
                "archive-extract",
                new MicrobenchRequest
                {
                    ScenarioId = "archive-extract",
                    RootPath = Path.Combine(runRoot, "archive-extract"),
                    Iterations = ArchiveExtractIterations,
                    ZipPath = support.ArchiveZipPath
                }),
            CreateScenario(runRoot, support, "file-enum-large-dir", new MicrobenchRequest
            {
                ScenarioId = "file-enum-large-dir",
                RootPath = Path.Combine(runRoot, "file-enum-large-dir"),
                Iterations = FileEnumLargeDirIterations
            }),
            CreateScenario(runRoot, support, "file-copy-large", new MicrobenchRequest
            {
                ScenarioId = "file-copy-large",
                RootPath = Path.Combine(runRoot, "file-copy-large"),
                Iterations = FileCopyLargeIterations
            }),
            CreateScenario(runRoot, support, "hardlink-create", new MicrobenchRequest
            {
                ScenarioId = "hardlink-create",
                RootPath = Path.Combine(runRoot, "hardlink-create"),
                Operations = HardlinkCreateOperations
            }),
            CreateScenario(runRoot, support, "junction-create", new MicrobenchRequest
            {
                ScenarioId = "junction-create",
                RootPath = Path.Combine(runRoot, "junction-create"),
                Operations = JunctionCreateOperations
            }),
            CreateScenario(runRoot, support, "process-create-wait", new MicrobenchRequest
            {
                ScenarioId = "process-create-wait",
                RootPath = Path.Combine(runRoot, "process-create-wait"),
                Operations = ProcessCreateOperations,
                UnsignedExePath = support.UnsignedExePath
            }),
            CreateExtensionSensitivityScenario(runRoot, support, ".exe"),
            CreateExtensionSensitivityScenario(runRoot, support, ".dll"),
            CreateExtensionSensitivityScenario(runRoot, support, ".js"),
            CreateExtensionSensitivityScenario(runRoot, support, ".ps1"),
            CreateScenario(runRoot, support, "dll-load-unique", new MicrobenchRequest
            {
                ScenarioId = "dll-load-unique",
                RootPath = Path.Combine(runRoot, "dll-load-unique"),
                Operations = DllLoadOperations
            }),
            CreateScenario(runRoot, support, "file-write-content", new MicrobenchRequest
            {
                ScenarioId = "file-write-content",
                RootPath = Path.Combine(runRoot, "file-write-content"),
                Operations = FileWriteContentOperations,
                UnsignedExePath = support.UnsignedExePath
            }),
            CreateScenario(runRoot, support, "motw-exe-no-motw", new MicrobenchRequest
            {
                ScenarioId = "motw-exe-no-motw",
                RootPath = Path.Combine(runRoot, "motw-exe-no-motw"),
                Operations = MotwOperations,
                UnsignedExePath = support.UnsignedExePath
            }),
            CreateScenario(runRoot, support, "motw-exe-motw-zone3", new MicrobenchRequest
            {
                ScenarioId = "motw-exe-motw-zone3",
                RootPath = Path.Combine(runRoot, "motw-exe-motw-zone3"),
                Operations = MotwOperations,
                UnsignedExePath = support.UnsignedExePath,
                ApplyMotw = true
            }),
            CreateScenario(runRoot, support, "thread-create", new MicrobenchRequest
            {
                ScenarioId = "thread-create",
                RootPath = Path.Combine(runRoot, "thread-create"),
                Operations = ThreadCreateOperations
            }),
            CreateScenario(runRoot, support, "mem-alloc-protect", new MicrobenchRequest
            {
                ScenarioId = "mem-alloc-protect",
                RootPath = Path.Combine(runRoot, "mem-alloc-protect"),
                Operations = MemAllocProtectOperations
            }),
            CreateScenario(runRoot, support, "mem-map-file", new MicrobenchRequest
            {
                ScenarioId = "mem-map-file",
                RootPath = Path.Combine(runRoot, "mem-map-file"),
                Operations = MemMapFileOperations
            }),
            CreateScenario(runRoot, support, "net-connect-loopback", new MicrobenchRequest
            {
                ScenarioId = "net-connect-loopback",
                RootPath = Path.Combine(runRoot, "net-connect-loopback"),
                Operations = NetConnectLoopbackOperations
            }),
            CreateScenario(runRoot, support, "net-dns-resolve", new MicrobenchRequest
            {
                ScenarioId = "net-dns-resolve",
                RootPath = Path.Combine(runRoot, "net-dns-resolve"),
                Operations = NetDnsResolveOperations
            }),
            CreateScenario(runRoot, support, "registry-crud", new MicrobenchRequest
            {
                ScenarioId = "registry-crud",
                RootPath = Path.Combine(runRoot, "registry-crud"),
                Operations = RegistryCrudOperations
            }),
            CreateScenario(runRoot, support, "pipe-roundtrip", new MicrobenchRequest
            {
                ScenarioId = "pipe-roundtrip",
                RootPath = Path.Combine(runRoot, "pipe-roundtrip"),
                Operations = PipeRoundtripOperations
            }),
            CreateScenario(runRoot, support, "token-query", new MicrobenchRequest
            {
                ScenarioId = "token-query",
                RootPath = Path.Combine(runRoot, "token-query"),
                Operations = TokenQueryOperations
            }),
            CreateScenario(runRoot, support, "crypto-hash-verify", new MicrobenchRequest
            {
                ScenarioId = "crypto-hash-verify",
                RootPath = Path.Combine(runRoot, "crypto-hash-verify"),
                Operations = CryptoHashVerifyOperations
            }),
            CreateScenario(runRoot, support, "com-create-instance", new MicrobenchRequest
            {
                ScenarioId = "com-create-instance",
                RootPath = Path.Combine(runRoot, "com-create-instance"),
                Operations = ComCreateInstanceOperations
            }),
            CreateScenario(runRoot, support, "wmi-query", new MicrobenchRequest
            {
                ScenarioId = "wmi-query",
                RootPath = Path.Combine(runRoot, "wmi-query"),
                Operations = WmiQueryOperations
            }),
            CreateScenario(runRoot, support, "fs-watcher", new MicrobenchRequest
            {
                ScenarioId = "fs-watcher",
                RootPath = Path.Combine(runRoot, "fs-watcher"),
                Operations = FsWatcherOperations
            })
        ];
    }

    private static ScenarioDefinition CreateExtensionSensitivityScenario(string runRoot, MicrobenchSupportEntry support, string extension)
    {
        var scenarioId = $"ext-sensitivity-{extension.TrimStart('.')}";
        return CreateScenario(runRoot, support, scenarioId, new MicrobenchRequest
        {
            ScenarioId = scenarioId,
            RootPath = Path.Combine(runRoot, scenarioId),
            Operations = ExtensionSensitivityOperations,
            Extension = extension
        });
    }

    private static ScenarioDefinition CreateScenario(
        string runRoot,
        MicrobenchSupportEntry support,
        string scenarioId,
        MicrobenchRequest request)
    {
        var workingDirectory = request.RootPath;
        Directory.CreateDirectory(workingDirectory);

        return new ScenarioDefinition
        {
            Id = scenarioId,
            WorkingDirectory = workingDirectory,
            PrepareAsync = ct => MicrobenchSupport.PrepareWorkingDirectoryAsync(runRoot, scenarioId, ct),
            ExecuteInProcessAsync = cancellationToken =>
                ExecuteInProcessAsync(request, support, cancellationToken)
        };
    }

    private static Task<ScenarioExecutionResult> ExecuteInProcessAsync(
        MicrobenchRequest request,
        MicrobenchSupportEntry support,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        MicrobenchSupport.ValidateManifestEntry(support);

        var stopwatch = Stopwatch.StartNew();
        var metrics = MicrobenchWorker.Execute(request);
        stopwatch.Stop();

        return Task.FromResult(new ScenarioExecutionResult
        {
            Command = request.ScenarioId,
            WorkingDirectory = request.RootPath,
            ExitCode = 0,
            WallMs = stopwatch.ElapsedMilliseconds,
            Microbench = metrics
        });
    }
}
