using System.Globalization;
using AvBench.Core.Internal;

namespace AvBench.Core.Environment;

public static class IdleChecker
{
    private const double CpuThresholdPercent = 20.0;
    private const int SampleCount = 3;

    public static async Task VerifyAsync(CancellationToken cancellationToken)
    {
        var averageCpu = await MeasureAverageCpuPercentAsync(cancellationToken);
        if (averageCpu > CpuThresholdPercent)
        {
            throw new InvalidOperationException(
                $"Idle check failed: average CPU usage was {averageCpu:F1}% over {SampleCount} seconds, above the {CpuThresholdPercent:F1}% threshold.");
        }

        Console.WriteLine(
            $"[run] Idle check: average CPU {averageCpu:F1}% over {SampleCount} seconds (threshold {CpuThresholdPercent:F1}%).");
    }

    private static async Task<double> MeasureAverageCpuPercentAsync(CancellationToken cancellationToken)
    {
        var result = await ProcessUtil.RunAsync(
            "typeperf",
            $"\"\\Processor(_Total)\\% Processor Time\" -si 1 -sc {SampleCount}",
            Directory.GetCurrentDirectory(),
            cancellationToken);

        if (result.ExitCode != 0)
        {
            throw BuildIdleCheckFailure(
                $"typeperf exited with code {result.ExitCode}.",
                result.Stdout,
                result.Stderr);
        }

        var samples = new List<double>(SampleCount);
        foreach (var rawLine in result.Stdout.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            if (!rawLine.StartsWith('"'))
            {
                continue;
            }

            var columns = rawLine.Split("\",\"", StringSplitOptions.None);
            if (columns.Length < 2)
            {
                continue;
            }

            var rawValue = columns[^1].Trim().Trim('"');
            if (double.TryParse(rawValue, NumberStyles.Float, CultureInfo.InvariantCulture, out var invariantValue)
                || double.TryParse(rawValue, NumberStyles.Float, CultureInfo.CurrentCulture, out invariantValue))
            {
                samples.Add(invariantValue);
            }
        }

        if (samples.Count == 0)
        {
            throw BuildIdleCheckFailure(
                "typeperf did not return any CPU samples.",
                result.Stdout,
                result.Stderr);
        }

        return samples.Average();
    }

    private static InvalidOperationException BuildIdleCheckFailure(string message, string stdout, string stderr)
    {
        var details = new List<string> { $"Idle check could not measure CPU usage: {message}" };
        if (!string.IsNullOrWhiteSpace(stdout))
        {
            details.Add($"stdout:{System.Environment.NewLine}{stdout.Trim()}");
        }

        if (!string.IsNullOrWhiteSpace(stderr))
        {
            details.Add($"stderr:{System.Environment.NewLine}{stderr.Trim()}");
        }

        return new InvalidOperationException(string.Join(System.Environment.NewLine + System.Environment.NewLine, details));
    }
}
