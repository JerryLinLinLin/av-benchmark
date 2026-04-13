using AvBench.Core.Environment;
using AvBench.Core.Models;
using AvBench.Core.Output;
using AvBench.Core.Runner;

namespace AvBench.Core.Scenarios;

public sealed class ScenarioRunner
{
    private readonly AvProfile _profile;
    private readonly string _outputRoot;
    private readonly string _runnerVersion;
    private readonly string _suiteManifestSha;

    public ScenarioRunner(AvProfile profile, string outputRoot, string runnerVersion, string suiteManifestSha)
    {
        _profile = profile;
        _outputRoot = outputRoot;
        _runnerVersion = runnerVersion;
        _suiteManifestSha = suiteManifestSha;
    }

    public async Task<List<RunResult>> ExecuteScenarioAsync(
        ScenarioDefinition scenario,
        int repetitions,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"[run] Warmup: {scenario.Id}");
        await RunOnceAsync(scenario, null, cancellationToken);

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

        using var sampler = new AvProcessSampler(_profile.ProcessNames);
        sampler.Start();

        var execution = await ProcessTreeRunner.RunAsync(
            scenario.FileName,
            scenario.Arguments,
            scenario.WorkingDirectory,
            stdoutPath,
            stderrPath,
            TimeSpan.FromHours(2),
            cancellationToken);

        var avSamples = await sampler.StopAsync();

        var result = new RunResult
        {
            ScenarioId = scenario.Id,
            AvProfile = _profile.Name,
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
            AvSamples = avSamples,
            Machine = SystemInfoProvider.CollectMachineInfo(),
            RunnerVersion = _runnerVersion,
            SuiteManifestSha = _suiteManifestSha
        };

        scenario.EnrichResultFromLogs?.Invoke(result, stdoutPath, stderrPath);

        if (outputDirectory is null)
        {
            TryDelete(stdoutPath);
            TryDelete(stderrPath);
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
            // Warmup cleanup is best effort.
        }
    }
}

