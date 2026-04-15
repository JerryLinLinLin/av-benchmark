using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;

namespace AvBench.Core.Environment;

[SupportedOSPlatform("windows")]
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
        try
        {
            using var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", readOnly: true);
            _ = cpuCounter.NextValue();

            var samples = new List<float>(SampleCount);
            for (var i = 0; i < SampleCount; i++)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                samples.Add(cpuCounter.NextValue());
            }

            if (samples.Count == 0)
            {
                throw new InvalidOperationException("PerformanceCounter did not return any CPU samples.");
            }

            return samples.Average();
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or PlatformNotSupportedException)
        {
            throw BuildIdleCheckFailure(
                $"PerformanceCounter CPU sampling failed: {ex.Message}");
        }
    }

    private static InvalidOperationException BuildIdleCheckFailure(string message)
    {
        return new InvalidOperationException($"Idle check could not measure CPU usage: {message}");
    }
}
