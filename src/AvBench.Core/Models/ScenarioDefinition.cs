namespace AvBench.Core.Models;

public sealed class ScenarioDefinition
{
    public required string Id { get; init; }

    public required string FileName { get; init; }

    public string Arguments { get; init; } = string.Empty;

    public required string WorkingDirectory { get; init; }

    public required Func<CancellationToken, Task> PrepareAsync { get; init; }

    public Func<CancellationToken, Task>? ValidateAsync { get; init; }

    public Action<RunResult, string, string>? EnrichResultFromLogs { get; init; }
}

