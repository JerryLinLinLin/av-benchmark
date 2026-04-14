using AvBench.Core.Collectors;
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
    private readonly bool _enableCounters;

    public ScenarioRunner(string avName, string outputRoot, string runnerVersion, string suiteManifestSha, bool enableCounters = false)
    {
        _avName = avName;
        _outputRoot = outputRoot;
        _runnerVersion = runnerVersion;
        _suiteManifestSha = suiteManifestSha;
        _avInfo = SystemInfoProvider.CollectAvInfo();
        _enableCounters = enableCounters;
    }

    public async Task<List<RunResult>> ExecuteScenarioAsync(
        ScenarioDefinition scenario,
        int repetitions,
        CancellationToken cancellationToken)
    {
        FileSystemUtil.DeletePathIfExists(Path.Combine(_outputRoot, scenario.Id));

        var results = new List<RunResult>(repetitions);
        for (var repetition = 1; repetition <= repetitions; repetition++)
        {
            var scenarioDirectory = Path.Combine(_outputRoot, scenario.Id, $"rep-{repetition:D2}");
            Directory.CreateDirectory(scenarioDirectory);

            Console.WriteLine($"[run] {scenario.Id} rep {repetition}/{repetitions}");
            var result = await RunOnceAsync(scenario, scenarioDirectory, cancellationToken);
            result.Repetition = repetition;

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

        var collectors = new List<IOptInCollector>();
        try
        {
            if (_enableCounters && outputDirectory is not null)
            {
                var collector = new TypeperfCollector();
                collector.Start(outputDirectory);
                collectors.Add(collector);
            }

            var execution = await ProcessTreeRunner.RunAsync(
                scenario.FileName,
                scenario.Arguments,
                scenario.WorkingDirectory,
                stdoutPath,
                stderrPath,
                TimeSpan.FromHours(2),
                cancellationToken);

            var result = new RunResult
            {
                ScenarioId = scenario.Id,
                AvName = _avName,
                AvProduct = _avInfo.Product,
                AvVersion = _avInfo.Version,
                TimestampUtc = DateTime.UtcNow,
                Command = string.IsNullOrWhiteSpace(scenario.Arguments) ? scenario.FileName : $"{scenario.FileName} {scenario.Arguments}",
                WorkingDir = scenario.WorkingDirectory,
                ExitCode = execution.ExitCode,
                WallMs = execution.WallMs,
                UserCpuMs = execution.Accounting.TotalUserTimeMs,
                KernelCpuMs = execution.Accounting.TotalKernelTimeMs,
                PeakJobMemoryMb = (long)(execution.Accounting.PeakJobMemoryBytes / (1024 * 1024)),
                IoReadBytes = execution.Accounting.IoReadBytes,
                IoWriteBytes = execution.Accounting.IoWriteBytes,
                IoReadOps = execution.Accounting.IoReadOps,
                IoWriteOps = execution.Accounting.IoWriteOps,
                TotalProcesses = execution.Accounting.TotalProcesses,
                Machine = SystemInfoProvider.CollectMachineInfo(),
                RunnerVersion = _runnerVersion,
                SuiteManifestSha = _suiteManifestSha
            };

            scenario.EnrichResultFromLogs?.Invoke(result, stdoutPath, stderrPath);
            return result;
        }
        finally
        {
            foreach (var collector in collectors)
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
