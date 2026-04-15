namespace AvBench.Core.Models;

public sealed class ScenarioDefinition
{
    public required string Id { get; init; }

    public string? FileName { get; init; }

    public string Arguments { get; init; } = string.Empty;

    public required string WorkingDirectory { get; init; }

    public required Func<CancellationToken, Task> PrepareAsync { get; init; }

    public Func<CancellationToken, Task<ScenarioExecutionResult>>? ExecuteInProcessAsync { get; init; }

    public Func<CancellationToken, Task>? ValidateAsync { get; init; }

    public Action<RunResult, string, string>? EnrichResultFromLogs { get; init; }
}

public sealed class ScenarioExecutionResult
{
    public string Command { get; init; } = string.Empty;

    public string WorkingDirectory { get; init; } = string.Empty;

    public int ExitCode { get; init; }

    public long WallMs { get; init; }

    public long UserCpuMs { get; init; }

    public long KernelCpuMs { get; init; }

    public long PeakJobMemoryMb { get; init; }

    public ulong IoReadBytes { get; init; }

    public ulong IoWriteBytes { get; init; }

    public ulong IoReadOps { get; init; }

    public ulong IoWriteOps { get; init; }

    public uint TotalProcesses { get; init; }

    public string Stdout { get; init; } = string.Empty;

    public string Stderr { get; init; } = string.Empty;

    public MicrobenchMetrics? Microbench { get; init; }
}
