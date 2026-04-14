using System.Diagnostics;
using System.Net.Http.Headers;

namespace AvBench.Core.Setup;

public abstract class ToolInstaller
{
    public abstract string Name { get; }

    public abstract string? Detect();

    public abstract Task InstallAsync(CancellationToken cancellationToken);

    public async Task<string> EnsureInstalledAsync(CancellationToken cancellationToken)
    {
        var version = Detect();
        if (!string.IsNullOrWhiteSpace(version))
        {
            Console.WriteLine($"[setup] {Name} already installed: {version}");
            return version;
        }

        Console.WriteLine($"[setup] Installing {Name}...");
        await InstallAsync(cancellationToken);

        version = Detect();
        if (string.IsNullOrWhiteSpace(version))
        {
            throw new InvalidOperationException($"{Name} installation completed, but detection still fails.");
        }

        Console.WriteLine($"[setup] {Name} installed: {version}");
        return version;
    }

    protected static async Task DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

        if (File.Exists(destinationPath))
        {
            File.Delete(destinationPath);
        }

        using var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("avbench", "0.1.0"));

        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var output = File.Create(destinationPath);
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await input.CopyToAsync(output, cancellationToken);
    }

    internal static int RunProcess(string fileName, string arguments, string? workingDirectory = null, bool useShellExecute = false)
    {
        using var process = Process.Start(new ProcessStartInfo(fileName, arguments)
        {
            WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory(),
            UseShellExecute = useShellExecute,
            CreateNoWindow = true
        });

        if (process is null)
        {
            throw new InvalidOperationException($"Failed to start process: {fileName}");
        }

        process.WaitForExit();
        return process.ExitCode;
    }

    internal static string? RunAndCapture(string fileName, string arguments, string? workingDirectory = null)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo(fileName, arguments)
            {
                WorkingDirectory = workingDirectory ?? Directory.GetCurrentDirectory(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });

            if (process is null)
            {
                return null;
            }

            var stdout = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return process.ExitCode == 0 ? stdout.Trim() : null;
        }
        catch
        {
            return null;
        }
    }
}
