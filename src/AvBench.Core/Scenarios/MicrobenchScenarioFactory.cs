using System.Text.Json;
using System.IO.Compression;
using AvBench.Core.Internal;
using AvBench.Core.Models;
using AvBench.Core.Serialization;

namespace AvBench.Core.Scenarios;

public static class MicrobenchScenarioFactory
{
    private const int FileCreateDeleteOperations = 5_000;
    private const int FileCreateDeleteBatchSize = 100;
    private const int ArchiveExtractIterations = 10;
    private const int ProcessCreateOperations = 500;
    private const int ExtensionSensitivityOperations = 10_000;
    private const int DllLoadOperations = 2_000;
    private const int FileWriteContentOperations = 10_000;
    private const int MotwOperations = 500;

    public static IReadOnlyList<ScenarioDefinition> Create(string executablePath, string benchDirectory)
    {
        var supportRoot = Path.Combine(benchDirectory, "microbench-support");
        var runRoot = Path.Combine(benchDirectory, "microbench");
        var archiveZipPath = Path.Combine(supportRoot, "archive", "bench_archive.zip");
        var unsignedExePath = Path.Combine(supportRoot, "procbench", "out", "noop.exe");

        return
        [
            CreateScenario(
                executablePath,
                runRoot,
                "file-create-delete",
                $"--operations {FileCreateDeleteOperations} --batch-size {FileCreateDeleteBatchSize}",
                ct => PrepareWorkingDirectoryAsync(runRoot, "file-create-delete", ct)),
            CreateScenario(
                executablePath,
                runRoot,
                "archive-extract",
                $"--iterations {ArchiveExtractIterations} --zip-path \"{archiveZipPath}\"",
                async ct =>
                {
                    await EnsureArchiveZipAsync(supportRoot, archiveZipPath, ct);
                    await PrepareWorkingDirectoryAsync(runRoot, "archive-extract", ct);
                }),
            CreateScenario(
                executablePath,
                runRoot,
                "process-create-wait",
                $"--operations {ProcessCreateOperations} --unsigned-exe \"{unsignedExePath}\"",
                async ct =>
                {
                    await EnsureUnsignedNoopExeAsync(supportRoot, unsignedExePath, ct);
                    await PrepareWorkingDirectoryAsync(runRoot, "process-create-wait", ct);
                }),
            CreateExtensionSensitivityScenario(executablePath, runRoot, ".exe"),
            CreateExtensionSensitivityScenario(executablePath, runRoot, ".dll"),
            CreateExtensionSensitivityScenario(executablePath, runRoot, ".js"),
            CreateExtensionSensitivityScenario(executablePath, runRoot, ".ps1"),
            CreateScenario(
                executablePath,
                runRoot,
                "dll-load-unique",
                $"--operations {DllLoadOperations}",
                ct => PrepareWorkingDirectoryAsync(runRoot, "dll-load-unique", ct)),
            CreateScenario(
                executablePath,
                runRoot,
                "file-write-content",
                $"--operations {FileWriteContentOperations} --unsigned-exe \"{unsignedExePath}\"",
                async ct =>
                {
                    await EnsureUnsignedNoopExeAsync(supportRoot, unsignedExePath, ct);
                    await PrepareWorkingDirectoryAsync(runRoot, "file-write-content", ct);
                }),
            CreateScenario(
                executablePath,
                runRoot,
                "motw-exe-no-motw",
                $"--operations {MotwOperations} --unsigned-exe \"{unsignedExePath}\"",
                async ct =>
                {
                    await EnsureUnsignedNoopExeAsync(supportRoot, unsignedExePath, ct);
                    await PrepareWorkingDirectoryAsync(runRoot, "motw-exe-no-motw", ct);
                }),
            CreateScenario(
                executablePath,
                runRoot,
                "motw-exe-motw-zone3",
                $"--operations {MotwOperations} --unsigned-exe \"{unsignedExePath}\" --apply-motw",
                async ct =>
                {
                    await EnsureUnsignedNoopExeAsync(supportRoot, unsignedExePath, ct);
                    await PrepareWorkingDirectoryAsync(runRoot, "motw-exe-motw-zone3", ct);
                })
        ];
    }

    private static ScenarioDefinition CreateExtensionSensitivityScenario(string executablePath, string runRoot, string extension)
    {
        var scenarioId = $"ext-sensitivity-{extension.TrimStart('.')}";
        return CreateScenario(
            executablePath,
            runRoot,
            scenarioId,
            $"--operations {ExtensionSensitivityOperations} --extension {extension}",
            ct => PrepareWorkingDirectoryAsync(runRoot, scenarioId, ct));
    }

    private static ScenarioDefinition CreateScenario(
        string executablePath,
        string runRoot,
        string scenarioId,
        string extraArguments,
        Func<CancellationToken, Task> prepareAsync)
    {
        var workingDirectory = Path.Combine(runRoot, scenarioId);
        Directory.CreateDirectory(workingDirectory);

        return new ScenarioDefinition
        {
            Id = scenarioId,
            FileName = executablePath,
            Arguments = $"internal-microbench --scenario {scenarioId} --root \"{workingDirectory}\" {extraArguments}",
            WorkingDirectory = workingDirectory,
            PrepareAsync = prepareAsync,
            EnrichResultFromLogs = (runResult, stdoutLogPath, _) =>
            {
                var json = File.ReadAllText(stdoutLogPath).Trim();
                if (string.IsNullOrWhiteSpace(json))
                {
                    return;
                }

                var metrics = JsonSerializer.Deserialize(json, AvBenchJsonContext.Default.MicrobenchMetrics)
                    ?? throw new InvalidOperationException($"Microbench output for {scenarioId} was not valid JSON.");

                runResult.Microbench = metrics;
                runResult.P50Us = metrics.P50Us;
                runResult.P95Us = metrics.P95Us;
                runResult.P99Us = metrics.P99Us;
                runResult.MaxUs = metrics.MaxUs;
            }
        };
    }

    private static Task PrepareWorkingDirectoryAsync(string runRoot, string scenarioId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var workingDirectory = Path.Combine(runRoot, scenarioId);
        FileSystemUtil.DeletePathIfExists(workingDirectory);
        Directory.CreateDirectory(workingDirectory);
        return Task.CompletedTask;
    }

    private static async Task EnsureArchiveZipAsync(string supportRoot, string archiveZipPath, CancellationToken cancellationToken)
    {
        if (File.Exists(archiveZipPath))
        {
            return;
        }

        var archiveDirectory = Path.GetDirectoryName(archiveZipPath)
            ?? throw new InvalidOperationException("Archive zip path does not have a parent directory.");
        Directory.CreateDirectory(archiveDirectory);

        var stageDirectory = Path.Combine(archiveDirectory, "stage");
        FileSystemUtil.DeletePathIfExists(stageDirectory);
        Directory.CreateDirectory(stageDirectory);

        try
        {
            var rng = new Random(42);
            string[] extensions = [".cs", ".js", ".json", ".xml", ".dll", ".exe", ".txt", ".md"];
            int[] sizes = [64, 256, 1024, 4096, 16384, 65536];

            for (var index = 0; index < 2_000; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var extension = extensions[index % extensions.Length];
                var size = sizes[index % sizes.Length];
                var subDirectory = Path.Combine(stageDirectory, $"pkg_{index / 100:D2}");
                Directory.CreateDirectory(subDirectory);

                var content = new byte[size];
                rng.NextBytes(content);
                if ((extension == ".dll" || extension == ".exe") && size >= 2)
                {
                    content[0] = 0x4D;
                    content[1] = 0x5A;
                }

                await File.WriteAllBytesAsync(Path.Combine(subDirectory, $"file_{index:D4}{extension}"), content, cancellationToken);
            }

            ZipFile.CreateFromDirectory(stageDirectory, archiveZipPath);
        }
        finally
        {
            FileSystemUtil.DeletePathIfExists(stageDirectory);
        }
    }

    private static async Task EnsureUnsignedNoopExeAsync(string supportRoot, string unsignedExePath, CancellationToken cancellationToken)
    {
        if (File.Exists(unsignedExePath))
        {
            return;
        }

        var projectDirectory = Path.Combine(supportRoot, "procbench");
        var outputDirectory = Path.Combine(projectDirectory, "out");
        Directory.CreateDirectory(projectDirectory);
        Directory.CreateDirectory(outputDirectory);

        var programPath = Path.Combine(projectDirectory, "Program.cs");
        var projectPath = Path.Combine(projectDirectory, "noop.csproj");

        await File.WriteAllTextAsync(programPath, "return 0;" + System.Environment.NewLine, cancellationToken);
        await File.WriteAllTextAsync(
            projectPath,
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """,
            cancellationToken);

        await ProcessUtil.EnsureSuccessAsync(
            "dotnet",
            $"build \"{projectPath}\" -c Release -o \"{outputDirectory}\"",
            projectDirectory,
            "Build unsigned noop.exe",
            cancellationToken);

        if (!File.Exists(unsignedExePath))
        {
            throw new InvalidOperationException($"Unsigned noop.exe was not produced at {unsignedExePath}.");
        }
    }
}
