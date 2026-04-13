using System.Diagnostics;
using System.Text;

namespace AvBench.Core.Internal;

internal static class ProcessUtil
{
    public static async Task<ProcessExecutionResult> RunAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        CancellationToken cancellationToken)
    {
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

        var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        return new ProcessExecutionResult
        {
            ExitCode = process.ExitCode,
            Stdout = await stdoutTask,
            Stderr = await stderrTask
        };
    }

    public static async Task EnsureSuccessAsync(
        string fileName,
        string arguments,
        string workingDirectory,
        string operationName,
        CancellationToken cancellationToken)
    {
        var result = await RunAsync(fileName, arguments, workingDirectory, cancellationToken);
        if (result.ExitCode == 0)
        {
            return;
        }

        var builder = new StringBuilder();
        builder.Append(operationName);
        builder.Append(" failed with exit code ");
        builder.Append(result.ExitCode);
        builder.Append('.');

        if (!string.IsNullOrWhiteSpace(result.Stdout))
        {
            builder.AppendLine();
            builder.AppendLine();
            builder.AppendLine("stdout:");
            builder.Append(result.Stdout.Trim());
        }

        if (!string.IsNullOrWhiteSpace(result.Stderr))
        {
            builder.AppendLine();
            builder.AppendLine();
            builder.AppendLine("stderr:");
            builder.Append(result.Stderr.Trim());
        }

        throw new InvalidOperationException(builder.ToString());
    }
}

internal sealed class ProcessExecutionResult
{
    public required int ExitCode { get; init; }

    public required string Stdout { get; init; }

    public required string Stderr { get; init; }
}
