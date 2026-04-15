using AvBench.Core.Scenarios;
using System.Runtime.Versioning;

namespace AvBench.Core;

[SupportedOSPlatform("windows")]
public static class BenchmarkWorkloads
{
    public const string Ripgrep = "ripgrep";
    public const string Roslyn = "roslyn";
    public const string Microbench = "microbench";
    public const string All = "all";

    private static readonly string[] SetupWorkloads =
    [
        Ripgrep,
        Roslyn,
        Microbench
    ];

    public static IReadOnlyList<string> Defaults => SetupWorkloads;

    public static string HelpText => string.Join(", ", SetupWorkloads);

    public static IReadOnlyList<string> NormalizeSetup(IEnumerable<string>? values)
    {
        if (TryNormalizeSetup(values, out var workloads, out var error))
        {
            return workloads;
        }

        throw new InvalidOperationException(error);
    }

    public static bool TryNormalizeSetup(
        IEnumerable<string>? values,
        out IReadOnlyList<string> workloads,
        out string? error)
    {
        var normalized = new List<string>();
        foreach (var raw in values ?? [])
        {
            foreach (var part in raw.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!TryNormalizeSetupSingle(part, out var value, out error))
                {
                    workloads = [];
                    return false;
                }

                if (string.Equals(value, All, StringComparison.OrdinalIgnoreCase))
                {
                    workloads = SetupWorkloads;
                    return true;
                }

                if (!normalized.Contains(value, StringComparer.OrdinalIgnoreCase))
                {
                    normalized.Add(value);
                }
            }
        }

        workloads = normalized.Count == 0 ? SetupWorkloads : normalized;
        error = null;
        return true;
    }

    public static bool TryNormalizeRun(
        IEnumerable<string>? values,
        out BenchmarkRunSelection selection,
        out string? error)
    {
        var workloadFamilies = new List<string>();
        var microbenchScenarioIds = new List<string>();

        foreach (var raw in values ?? [])
        {
            foreach (var part in raw.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (!TryNormalizeRunSingle(part, out var normalizedValue, out var selectorType, out error))
                {
                    selection = BenchmarkRunSelection.Empty;
                    return false;
                }

                if (string.Equals(normalizedValue, All, StringComparison.OrdinalIgnoreCase))
                {
                    selection = new BenchmarkRunSelection(SetupWorkloads, []);
                    error = null;
                    return true;
                }

                if (selectorType == BenchmarkSelectorType.WorkloadFamily)
                {
                    if (!workloadFamilies.Contains(normalizedValue, StringComparer.OrdinalIgnoreCase))
                    {
                        workloadFamilies.Add(normalizedValue);
                    }
                }
                else if (!microbenchScenarioIds.Contains(normalizedValue, StringComparer.OrdinalIgnoreCase))
                {
                    microbenchScenarioIds.Add(normalizedValue);
                }
            }
        }

        if (workloadFamilies.Count == 0 && microbenchScenarioIds.Count == 0)
        {
            selection = new BenchmarkRunSelection(SetupWorkloads, []);
        }
        else
        {
            selection = new BenchmarkRunSelection(workloadFamilies, microbenchScenarioIds);
        }

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
        => Contains(selectedWorkloads, Roslyn)
            || Contains(selectedWorkloads, Microbench);

    public static bool IncludesSourceTree(IReadOnlyCollection<string> selectedWorkloads)
        => Contains(selectedWorkloads, Ripgrep)
            || Contains(selectedWorkloads, Roslyn);

    private static bool TryNormalizeSetupSingle(string value, out string normalized, out string? error)
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
            case Microbench:
                normalized = Microbench;
                error = null;
                return true;
            default:
                normalized = string.Empty;
                error = $"Unknown workload '{value}'. Known workloads: {HelpText}.";
                return false;
        }
    }

    private static bool TryNormalizeRunSingle(
        string value,
        out string normalized,
        out BenchmarkSelectorType selectorType,
        out string? error)
    {
        if (TryNormalizeSetupSingle(value, out normalized, out error))
        {
            selectorType = BenchmarkSelectorType.WorkloadFamily;
            return true;
        }

        if (MicrobenchScenarioFactory.ContainsScenarioId(value))
        {
            normalized = value.Trim().ToLowerInvariant();
            selectorType = BenchmarkSelectorType.MicrobenchScenario;
            error = null;
            return true;
        }

        selectorType = BenchmarkSelectorType.Unknown;
        normalized = string.Empty;
        error = $"Unknown run selector '{value}'. Known workload families: {HelpText}. Specific microbench scenario ids are also supported.";
        return false;
    }

    private enum BenchmarkSelectorType
    {
        Unknown,
        WorkloadFamily,
        MicrobenchScenario
    }
}

public sealed class BenchmarkRunSelection
{
    public static BenchmarkRunSelection Empty { get; } = new([], []);

    public BenchmarkRunSelection(
        IReadOnlyList<string> workloadFamilies,
        IReadOnlyList<string> microbenchScenarioIds)
    {
        WorkloadFamilies = workloadFamilies;
        MicrobenchScenarioIds = microbenchScenarioIds;
    }

    public IReadOnlyList<string> WorkloadFamilies { get; }

    public IReadOnlyList<string> MicrobenchScenarioIds { get; }

    public bool IncludesWorkloadFamily(string workload)
        => WorkloadFamilies.Contains(workload, StringComparer.OrdinalIgnoreCase);

    public bool IncludesAnyMicrobenchScenario()
        => MicrobenchScenarioIds.Count > 0;
}
