namespace AvBench.Core;

public static class BenchmarkWorkloads
{
    public const string Ripgrep = "ripgrep";
    public const string Roslyn = "roslyn";
    public const string FileCreateDelete = "file-create-delete";
    public const string All = "all";

    private static readonly string[] AllWorkloads =
    [
        Ripgrep,
        Roslyn,
        FileCreateDelete
    ];

    public static IReadOnlyList<string> Defaults => AllWorkloads;

    public static string HelpText => string.Join(", ", AllWorkloads);

    public static IReadOnlyList<string> Normalize(IEnumerable<string>? values)
    {
        if (TryNormalize(values, out var workloads, out var error))
        {
            return workloads;
        }

        throw new InvalidOperationException(error);
    }

    public static bool TryNormalize(
        IEnumerable<string>? values,
        out IReadOnlyList<string> workloads,
        out string? error)
    {
        var normalized = new List<string>();
        foreach (var raw in values ?? [])
        {
            foreach (var part in raw.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!TryNormalizeSingle(part, out var value, out error))
                {
                    workloads = [];
                    return false;
                }

                if (string.Equals(value, All, StringComparison.OrdinalIgnoreCase))
                {
                    workloads = AllWorkloads;
                    return true;
                }

                if (!normalized.Contains(value, StringComparer.OrdinalIgnoreCase))
                {
                    normalized.Add(value);
                }
            }
        }

        workloads = normalized.Count == 0 ? AllWorkloads : normalized;
        error = null;
        return true;
    }

    public static bool Contains(IReadOnlyCollection<string> selectedWorkloads, string workload)
        => selectedWorkloads.Contains(workload, StringComparer.OrdinalIgnoreCase);

    public static bool RequiresRust(IReadOnlyCollection<string> selectedWorkloads)
        => Contains(selectedWorkloads, Ripgrep);

    public static bool RequiresVisualStudio(IReadOnlyCollection<string> selectedWorkloads)
        => Contains(selectedWorkloads, Roslyn);

    public static bool RequiresDotNetSdk(IReadOnlyCollection<string> selectedWorkloads)
        => Contains(selectedWorkloads, Roslyn);

    public static bool IncludesSourceTree(IReadOnlyCollection<string> selectedWorkloads)
        => Contains(selectedWorkloads, Ripgrep)
            || Contains(selectedWorkloads, Roslyn);

    private static bool TryNormalizeSingle(string value, out string normalized, out string? error)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            normalized = string.Empty;
            error = $"Workload names cannot be empty. Known workloads: {HelpText}.";
            return false;
        }

        switch (value.Trim().ToLowerInvariant())
        {
            case All:
                normalized = All;
                error = null;
                return true;
            case Ripgrep:
                normalized = Ripgrep;
                error = null;
                return true;
            case Roslyn:
                normalized = Roslyn;
                error = null;
                return true;
            case FileCreateDelete:
            case "microbench":
            case "file":
                normalized = FileCreateDelete;
                error = null;
                return true;
            default:
                normalized = string.Empty;
                error = $"Unknown workload '{value}'. Known workloads: {HelpText}.";
                return false;
        }
    }
}
