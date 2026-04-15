using AvBench.Core.Collectors;
using AvBench.Core.Detection;
using AvBench.Core.Environment;
using AvBench.Core.Internal;
using AvBench.Core.Models;
using AvBench.Core.Output;
using AvBench.Core.Runner;

namespace AvBench.Core.Scenarios;

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
        foreach (var scenario in scenarios)
        {
            var scenarioDirectory = Path.Combine(_outputRoot, scenario.Id);
            FileSystemUtil.DeletePathIfExists(scenarioDirectory);
            Directory.CreateDirectory(scenarioDirectory);

            Console.WriteLine($"[run] {scenario.Id}");
            var result = await RunOnceAsync(scenario, scenarioDirectory, cancellationToken);
            await JsonResultWriter.WriteAsync(result, Path.Combine(scenarioDirectory, "run.json"), cancellationToken);
            results.Add(result);

            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException($"Scenario {scenario.Id} failed with exit code {result.ExitCode}. See logs in {scenarioDirectory}.");
            }

            if (scenario.ValidateAsync is not null)
            {
                await scenario.ValidateAsync(cancellationToken);
            }
        }

        return results;
    }

    private async Task<RunResult> RunOnceAsync(ScenarioDefinition scenario, string? outputDirectory, CancellationToken cancellationToken)
    {
        await scenario.PrepareAsync(cancellationToken);

        var stdoutPath = outputDirectory is null
            ? Path.Combine(Path.GetTempPath(), "avbench", $"{Guid.NewGuid():N}.stdout.log")
            : Path.Combine(outputDirectory, "stdout.log");
        var stderrPath = outputDirectory is null
            ? Path.Combine(Path.GetTempPath(), "avbench", $"{Guid.NewGuid():N}.stderr.log")
            : Path.Combine(outputDirectory, "stderr.log");

        Directory.CreateDirectory(Path.GetDirectoryName(stdoutPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(stderrPath)!);

        TypeperfCollector? collector = null;
        try
        {
            if (outputDirectory is not null)
            {
                collector = new TypeperfCollector();
                collector.Start(outputDirectory);
            }

            ScenarioExecutionResult execution;
            if (scenario.ExecuteInProcessAsync is not null)
            {
                execution = await scenario.ExecuteInProcessAsync(cancellationToken);
                await File.WriteAllTextAsync(stdoutPath, execution.Stdout, cancellationToken);
                await File.WriteAllTextAsync(stderrPath, execution.Stderr, cancellationToken);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(scenario.FileName))
                {
                    throw new InvalidOperationException($"Scenario {scenario.Id} does not define a process command or an in-process executor.");
                }

                var processExecution = await ProcessTreeRunner.RunAsync(
                    scenario.FileName,
                    scenario.Arguments,
                    scenario.WorkingDirectory,
                    stdoutPath,
                    stderrPath,
                    TimeSpan.FromHours(2),
                    cancellationToken);

                execution = new ScenarioExecutionResult
                {
                    Command = string.IsNullOrWhiteSpace(scenario.Arguments) ? scenario.FileName : $"{scenario.FileName} {scenario.Arguments}",
                    WorkingDirectory = scenario.WorkingDirectory,
                    ExitCode = processExecution.ExitCode,
                    WallMs = processExecution.WallMs,
                    UserCpuMs = processExecution.Accounting.TotalUserTimeMs,
                    KernelCpuMs = processExecution.Accounting.TotalKernelTimeMs,
                    PeakJobMemoryMb = (long)(processExecution.Accounting.PeakJobMemoryBytes / (1024 * 1024)),
                    IoReadBytes = processExecution.Accounting.IoReadBytes,
                    IoWriteBytes = processExecution.Accounting.IoWriteBytes,
                    IoReadOps = processExecution.Accounting.IoReadOps,
                    IoWriteOps = processExecution.Accounting.IoWriteOps,
                    TotalProcesses = processExecution.Accounting.TotalProcesses
                };
            }

            var result = CreateRunResult(scenario, execution);
            scenario.EnrichResultFromLogs?.Invoke(result, stdoutPath, stderrPath);
            return result;
        }
        finally
        {
            if (collector is not null)
            {
                try
                {
                    collector.Stop();
                }
                finally
                {
                    collector.Dispose();
                }
            }

            if (outputDirectory is null)
            {
                TryDelete(stdoutPath);
                TryDelete(stderrPath);
            }
        }
    }

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
            IoReadBytes = execution.IoReadBytes,
            IoWriteBytes = execution.IoWriteBytes,
            IoReadOps = execution.IoReadOps,
            IoWriteOps = execution.IoWriteOps,
            TotalProcesses = execution.TotalProcesses,
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
