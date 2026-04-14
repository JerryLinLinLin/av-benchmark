namespace AvBench.Core;

public static class BenchmarkWorkloads
{
    public const string Ripgrep = "ripgrep";
    public const string Roslyn = "roslyn";
    public const string Llvm = "llvm";
    public const string Files = "files";
    public const string FileCreateDelete = "file-create-delete";
    public const string All = "all";

    private static readonly string[] AllWorkloads =
    [
        Ripgrep,
        Roslyn,
        Llvm,
        Files,
        FileCreateDelete
    ];

    public static IReadOnlyList<string> Defaults => AllWorkloads;

    public static string HelpText => string.Join(", ", AllWorkloads);

    public static IReadOnlyList<string> Normalize(IEnumerable<string>? values)
    {
        var normalized = new List<string>();
        foreach (var raw in values ?? [])
        {
            foreach (var part in raw.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var value = NormalizeSingle(part);
                if (string.Equals(value, All, StringComparison.OrdinalIgnoreCase))
                {
                    return AllWorkloads;
                }

                if (!normalized.Contains(value, StringComparer.OrdinalIgnoreCase))
                {
                    normalized.Add(value);
                }
            }
        }

        return normalized.Count == 0 ? AllWorkloads : normalized;
    }

    public static bool Contains(IReadOnlyCollection<string> selectedWorkloads, string workload)
        => selectedWorkloads.Contains(workload, StringComparer.OrdinalIgnoreCase);

    public static bool RequiresRust(IReadOnlyCollection<string> selectedWorkloads)
        => Contains(selectedWorkloads, Ripgrep);

    public static bool RequiresVisualStudio(IReadOnlyCollection<string> selectedWorkloads)
        => Contains(selectedWorkloads, Roslyn)
            || Contains(selectedWorkloads, Llvm)
            || Contains(selectedWorkloads, Files);

    public static bool RequiresDotNetSdk(IReadOnlyCollection<string> selectedWorkloads)
        => Contains(selectedWorkloads, Roslyn)
            || Contains(selectedWorkloads, Files);

    public static bool RequiresCmake(IReadOnlyCollection<string> selectedWorkloads)
        => Contains(selectedWorkloads, Llvm);

    public static bool RequiresNinja(IReadOnlyCollection<string> selectedWorkloads)
        => Contains(selectedWorkloads, Llvm);

    public static bool RequiresPython(IReadOnlyCollection<string> selectedWorkloads)
        => Contains(selectedWorkloads, Llvm);

    public static bool IncludesSourceTree(IReadOnlyCollection<string> selectedWorkloads)
        => Contains(selectedWorkloads, Ripgrep)
            || Contains(selectedWorkloads, Roslyn)
            || Contains(selectedWorkloads, Llvm)
            || Contains(selectedWorkloads, Files);

    private static string NormalizeSingle(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Workload names cannot be empty. Known workloads: {HelpText}.");
        }

        return value.Trim().ToLowerInvariant() switch
        {
            All => All,
            Ripgrep => Ripgrep,
            Roslyn => Roslyn,
            Llvm => Llvm,
            Files => Files,
            FileCreateDelete => FileCreateDelete,
            "microbench" => FileCreateDelete,
            "file" => FileCreateDelete,
            _ => throw new InvalidOperationException($"Unknown workload '{value}'. Known workloads: {HelpText}.")
        };
    }
}
