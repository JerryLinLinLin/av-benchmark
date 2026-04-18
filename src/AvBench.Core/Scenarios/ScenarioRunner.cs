using AvBench.Core.Collectors;
using AvBench.Core.Detection;
using AvBench.Core.Environment;
using AvBench.Core.Internal;
using AvBench.Core.Models;
using AvBench.Core.Output;
using AvBench.Core.Runner;
using System.Runtime.Versioning;

namespace AvBench.Core.Scenarios;

[SupportedOSPlatform("windows")]
public sealed class ScenarioRunner
{
    private readonly string _avName;
    private readonly string _outputRoot;
    private readonly string _runnerVersion;
    private readonly string _suiteManifestSha;
    private readonly AvInfo _avInfo;

    public ScenarioRunner(string avName, string outputRoot, string runnerVersion, string suiteManifestSha, AvInfo avInfo)
    {
        _avName = avName;
        _outputRoot = outputRoot;
        _runnerVersion = runnerVersion;
        _suiteManifestSha = suiteManifestSha;
        _avInfo = avInfo;
    }

    public async Task<List<RunResult>> ExecuteScenariosAsync(
        IReadOnlyList<ScenarioDefinition> scenarios,
        CancellationToken cancellationToken)
    {
        var results = new List<RunResult>(scenarios.Count);
        for (var index = 0; index < scenarios.Count; index++)
        {
            var scenario = scenarios[index];
            var scenarioDirectory = Path.Combine(_outputRoot, scenario.Id);
            FileSystemUtil.DeletePathIfExists(scenarioDirectory);
            Directory.CreateDirectory(scenarioDirectory);

            Console.WriteLine($"[run] {scenario.Id}");
            var result = await RunOnceAsync(scenario, scenarioDirectory, cancellationToken);
            await JsonResultWriter.WriteAsync(result, Path.Combine(scenarioDirectory, "run.json"), cancellationToken);
            results.Add(result);

            var succeeded = result.ExitCode == 0;
            if (!succeeded)
            {
                var message = $"Scenario {scenario.Id} failed with exit code {result.ExitCode}. See logs in {scenarioDirectory}.";
                if (!scenario.ContinueOnFailure)
                {
                    throw new InvalidOperationException(message);
                }

                Console.WriteLine($"[run] WARNING: {message} Continuing with remaining scenarios.");
            }

            if (succeeded && scenario.ValidateAsync is not null)
            {
                await scenario.ValidateAsync(cancellationToken);
            }

            if (index < scenarios.Count - 1)
            {
                var nextScenario = scenarios[index + 1];
                var cooldownMilliseconds = ScenarioSupport.GetCooldownMilliseconds(scenario.Id, nextScenario.Id);
                if (cooldownMilliseconds > 0)
                {
                    Console.WriteLine($"[run] Cooldown: waiting {cooldownMilliseconds / 1000}s before {nextScenario.Id}");
                    await Task.Delay(cooldownMilliseconds, cancellationToken);
                }
            }
        }

        return results;
    }

    private async Task<RunResult> RunOnceAsync(ScenarioDefinition scenario, string? outputDirectory, CancellationToken cancellationToken)
    {
        var stdoutPath = outputDirectory is null
            ? Path.Combine(Path.GetTempPath(), "avbench", $"{Guid.NewGuid():N}.stdout.log")
            : Path.Combine(outputDirectory, "stdout.log");
        var stderrPath = outputDirectory is null
            ? Path.Combine(Path.GetTempPath(), "avbench", $"{Guid.NewGuid():N}.stderr.log")
            : Path.Combine(outputDirectory, "stderr.log");

        Directory.CreateDirectory(Path.GetDirectoryName(stdoutPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(stderrPath)!);

        using var diskIoSnapshot = new DiskIoSnapshot();
        try
        {
            await scenario.PrepareAsync(cancellationToken);

            ScenarioExecutionResult execution;
            if (scenario.ExecuteInProcessAsync is not null)
            {
                diskIoSnapshot.SnapshotBefore();
                execution = await scenario.ExecuteInProcessAsync(cancellationToken);
                var (readBytes, writeBytes) = diskIoSnapshot.SnapshotAfter();
                execution = execution with
                {
                    SystemDiskReadBytes = readBytes,
                    SystemDiskWriteBytes = writeBytes
                };
                await File.WriteAllTextAsync(stdoutPath, execution.Stdout, cancellationToken);
                await File.WriteAllTextAsync(stderrPath, execution.Stderr, cancellationToken);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(scenario.FileName))
                {
                    throw new InvalidOperationException($"Scenario {scenario.Id} does not define a process command or an in-process executor.");
                }

                diskIoSnapshot.SnapshotBefore();
                var processExecution = await ProcessTreeRunner.RunAsync(
                    scenario.FileName,
                    scenario.Arguments,
                    scenario.WorkingDirectory,
                    stdoutPath,
                    stderrPath,
                    TimeSpan.FromHours(2),
                    cancellationToken);
                var (readBytes, writeBytes) = diskIoSnapshot.SnapshotAfter();

                execution = new ScenarioExecutionResult
                {
                    Command = string.IsNullOrWhiteSpace(scenario.Arguments) ? scenario.FileName : $"{scenario.FileName} {scenario.Arguments}",
                    WorkingDirectory = scenario.WorkingDirectory,
                    ExitCode = processExecution.ExitCode,
                    WallMs = processExecution.WallMs,
                    UserCpuMs = processExecution.Accounting.TotalUserTimeMs,
                    KernelCpuMs = processExecution.Accounting.TotalKernelTimeMs,
                    PeakJobMemoryMb = (long)(processExecution.Accounting.PeakJobMemoryBytes / (1024 * 1024)),
                    SystemDiskReadBytes = readBytes,
                    SystemDiskWriteBytes = writeBytes
                };
            }

            var result = CreateRunResult(scenario, execution);
            scenario.EnrichResultFromLogs?.Invoke(result, stdoutPath, stderrPath);
            return result;
        }
        catch (Exception ex) when (scenario.ContinueOnFailure && ex is not OperationCanceledException)
        {
            var execution = await CreateFailedExecutionAsync(scenario, stdoutPath, stderrPath, ex, cancellationToken);
            return CreateRunResult(scenario, execution);
        }
        finally
        {
            if (outputDirectory is null)
            {
                TryDelete(stdoutPath);
                TryDelete(stderrPath);
            }
        }
    }

    private static async Task<ScenarioExecutionResult> CreateFailedExecutionAsync(
        ScenarioDefinition scenario,
        string stdoutPath,
        string stderrPath,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var stderr = FormatException(exception);
        await File.WriteAllTextAsync(stdoutPath, string.Empty, cancellationToken);
        await File.WriteAllTextAsync(stderrPath, stderr, cancellationToken);

        return new ScenarioExecutionResult
        {
            Command = scenario.ExecuteInProcessAsync is not null
                ? scenario.Id
                : string.IsNullOrWhiteSpace(scenario.Arguments) ? scenario.FileName ?? scenario.Id : $"{scenario.FileName} {scenario.Arguments}",
            WorkingDirectory = scenario.WorkingDirectory,
            ExitCode = 1,
            WallMs = 0,
            Stdout = string.Empty,
            Stderr = stderr
        };
    }

    private static string FormatException(Exception exception)
        => $"{exception.GetType().FullName}: {exception.Message}{System.Environment.NewLine}{exception.StackTrace}{System.Environment.NewLine}";

    private RunResult CreateRunResult(ScenarioDefinition scenario, ScenarioExecutionResult execution)
    {
        var result = new RunResult
        {
            ScenarioId = scenario.Id,
            AvName = _avName,
            AvProduct = _avInfo.ProductName,
            AvVersion = _avInfo.ProductVersion,
            TimestampUtc = DateTime.UtcNow,
            Command = execution.Command,
            WorkingDir = execution.WorkingDirectory,
            ExitCode = execution.ExitCode,
            WallMs = execution.WallMs,
            UserCpuMs = execution.UserCpuMs,
            KernelCpuMs = execution.KernelCpuMs,
            PeakJobMemoryMb = execution.PeakJobMemoryMb,
            SystemDiskReadBytes = execution.SystemDiskReadBytes,
            SystemDiskWriteBytes = execution.SystemDiskWriteBytes,
            Machine = SystemInfoProvider.CollectMachineInfo(),
            RunnerVersion = _runnerVersion,
            SuiteManifestSha = _suiteManifestSha,
            Microbench = execution.Microbench
        };

        if (execution.Microbench is not null)
        {
            result.P50Us = execution.Microbench.P50Us;
            result.P95Us = execution.Microbench.P95Us;
            result.P99Us = execution.Microbench.P99Us;
            result.MaxUs = execution.Microbench.MaxUs;
        }

        return result;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Temp log cleanup is best effort.
        }
    }
}
