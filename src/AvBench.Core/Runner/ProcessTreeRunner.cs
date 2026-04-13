using System.Diagnostics;

namespace AvBench.Core.Runner;

public static class ProcessTreeRunner
{
    public static async Task<ProcessTreeRunResult> RunAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        string stdoutLogPath,
        string stderrLogPath,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(stdoutLogPath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(stderrLogPath)!);

        using var stdoutStream = new FileStream(stdoutLogPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var stderrStream = new FileStream(stderrLogPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var job = new JobObject();
        using var process = Process.Start(new ProcessStartInfo(fileName, arguments)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        });

        if (process is null)
        {
            throw new InvalidOperationException($"Failed to start process: {fileName}");
        }

        job.AssignProcess(process.Handle);

        using var timeoutCts = new CancellationTokenSource(timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        var stopwatch = Stopwatch.StartNew();
        var stdoutCopy = process.StandardOutput.BaseStream.CopyToAsync(stdoutStream, linkedCts.Token);
        var stderrCopy = process.StandardError.BaseStream.CopyToAsync(stderrStream, linkedCts.Token);

        try
        {
            await process.WaitForExitAsync(linkedCts.Token);
            await Task.WhenAll(stdoutCopy, stderrCopy);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
        {
            TryTerminate(process);
            throw new TimeoutException($"Process exceeded timeout of {timeout}.");
        }
        catch
        {
            TryTerminate(process);
            throw;
        }
        finally
        {
            stopwatch.Stop();
        }

        var accounting = job.QueryAccounting();
        return new ProcessTreeRunResult
        {
            ExitCode = process.ExitCode,
            WallMs = stopwatch.ElapsedMilliseconds,
            Accounting = accounting
        };
    }

    private static void TryTerminate(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Best effort cleanup only.
        }
    }
}

