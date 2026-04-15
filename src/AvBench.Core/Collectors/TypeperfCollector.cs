using System.Diagnostics;

namespace AvBench.Core.Collectors;

public sealed class TypeperfCollector : IDisposable
{
    private static readonly string[] Counters =
    [
        @"\Processor(_Total)\% Processor Time",
        @"\PhysicalDisk(_Total)\Disk Bytes/sec",
        @"\PhysicalDisk(_Total)\Disk Read Bytes/sec",
        @"\PhysicalDisk(_Total)\Disk Write Bytes/sec",
        @"\Memory\Available MBytes",
        @"\Memory\Pages/sec"
    ];

    private Process? _process;
    private string _outputPath = string.Empty;

    public void Start(string outputDirectory)
    {
        _outputPath = Path.Combine(outputDirectory, "counters.csv");
        var counterArgs = string.Join(" ", Counters.Select(static counter => $"\"{counter}\""));

        var processStartInfo = new ProcessStartInfo(
            "typeperf",
            $"{counterArgs} -si 1 -f CSV -o \"{_outputPath}\"")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        try
        {
            _process = Process.Start(processStartInfo);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[counters] WARNING: failed to start typeperf: {ex.Message}");
        }
    }

    public void Stop()
    {
        if (_process is null || _process.HasExited)
        {
            return;
        }

        try
        {
            _process.StandardInput.Close();
            if (!_process.WaitForExit(5_000))
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[counters] WARNING: failed to stop typeperf cleanly: {ex.Message}");
        }
    }

    public void Dispose()
    {
        Stop();
        _process?.Dispose();
    }
}
