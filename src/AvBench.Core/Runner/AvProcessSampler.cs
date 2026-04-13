using System.Diagnostics;
using AvBench.Core.Models;

namespace AvBench.Core.Runner;

public sealed class AvProcessSampler : IDisposable
{
    private readonly Dictionary<string, string> _displayNames;
    private readonly TimeSpan _interval;
    private readonly Dictionary<string, List<SampleSnapshot>> _samples = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, PreviousProcessState> _previousByPid = [];
    private readonly CancellationTokenSource _cancellation = new();
    private Task? _samplingTask;

    public AvProcessSampler(IEnumerable<string> processNames, TimeSpan? interval = null)
    {
        _displayNames = processNames
            .Select(name => name.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                name => Path.GetFileNameWithoutExtension(name),
                name => name,
                StringComparer.OrdinalIgnoreCase);

        _interval = interval ?? TimeSpan.FromSeconds(1);
    }

    public void Start()
    {
        if (_displayNames.Count == 0 || _samplingTask is not null)
        {
            return;
        }

        _samplingTask = Task.Run(SampleLoopAsync);
    }

    public async Task<List<AvSample>> StopAsync()
    {
        if (_samplingTask is null)
        {
            return [];
        }

        _cancellation.Cancel();
        await _samplingTask;

        return _samples
            .OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .Select(entry => new AvSample
            {
                Process = entry.Key,
                MeanCpuPct = Math.Round(entry.Value.Average(sample => sample.CpuPct), 1),
                PeakWsMb = (long)Math.Round(entry.Value.Max(sample => sample.WorkingSetMb))
            })
            .ToList();
    }

    private async Task SampleLoopAsync()
    {
        while (!_cancellation.IsCancellationRequested)
        {
            CaptureSample();

            try
            {
                await Task.Delay(_interval, _cancellation.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void CaptureSample()
    {
        var now = DateTime.UtcNow;
        var seenPids = new HashSet<int>();

        foreach (var (processName, displayName) in _displayNames)
        {
            double combinedCpuPct = 0;
            double combinedWorkingSetMb = 0;

            foreach (var process in Process.GetProcessesByName(processName))
            {
                using (process)
                {
                    seenPids.Add(process.Id);

                    try
                    {
                        var totalCpu = process.TotalProcessorTime;
                        var workingSetMb = process.WorkingSet64 / (1024d * 1024d);
                        combinedWorkingSetMb += workingSetMb;

                        if (_previousByPid.TryGetValue(process.Id, out var previous))
                        {
                            var deltaWallMs = (now - previous.TimestampUtc).TotalMilliseconds;
                            var deltaCpuMs = (totalCpu - previous.TotalCpu).TotalMilliseconds;
                            if (deltaWallMs > 0)
                            {
                                combinedCpuPct += deltaCpuMs / (deltaWallMs * System.Environment.ProcessorCount) * 100d;
                            }
                        }

                        _previousByPid[process.Id] = new PreviousProcessState(totalCpu, now);
                    }
                    catch
                    {
                        // Access to some AV processes can fail mid-sample. Best effort only.
                    }
                }
            }

            if (combinedCpuPct > 0 || combinedWorkingSetMb > 0)
            {
                if (!_samples.TryGetValue(displayName, out var entries))
                {
                    entries = [];
                    _samples[displayName] = entries;
                }

                entries.Add(new SampleSnapshot(combinedCpuPct, combinedWorkingSetMb));
            }
        }

        foreach (var stalePid in _previousByPid.Keys.Except(seenPids).ToList())
        {
            _previousByPid.Remove(stalePid);
        }
    }

    public void Dispose()
    {
        _cancellation.Cancel();
        _cancellation.Dispose();
    }

    private readonly record struct SampleSnapshot(double CpuPct, double WorkingSetMb);

    private readonly record struct PreviousProcessState(TimeSpan TotalCpu, DateTime TimestampUtc);
}
