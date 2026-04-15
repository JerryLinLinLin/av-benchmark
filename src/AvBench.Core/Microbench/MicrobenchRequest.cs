namespace AvBench.Core.Microbench;

public sealed class MicrobenchRequest
{
    public required string ScenarioId { get; init; }

    public required string RootPath { get; init; }

    public int Operations { get; init; }

    public int BatchSize { get; init; } = 100;

    public string? Extension { get; init; }

    public string? ZipPath { get; init; }

    public string? UnsignedExePath { get; init; }

    public int Iterations { get; init; }

    public bool ApplyMotw { get; init; }
}
