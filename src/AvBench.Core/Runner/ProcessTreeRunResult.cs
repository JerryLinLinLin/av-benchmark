namespace AvBench.Core.Runner;

public sealed class ProcessTreeRunResult
{
    public int ExitCode { get; init; }

    public long WallMs { get; init; }

    public required JobAccountingSnapshot Accounting { get; init; }
}

