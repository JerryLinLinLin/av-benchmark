using AvBench.Core.Internal;
using System.Runtime.Versioning;

namespace AvBench.Core.Setup;

[SupportedOSPlatform("windows")]
public sealed class VsBuildToolsInstaller(string? minimumVersion = null) : ToolInstaller
{
    private const string WingetPackageId = "Microsoft.VisualStudio.BuildTools";
    private const string RequiredWindowsSdkVersion = "10.0.26100.0";
    private static readonly string[] RequiredComponents =
    [
        "Microsoft.VisualStudio.Workload.VCTools",
        "Microsoft.VisualStudio.Workload.ManagedDesktopBuildTools",
        "Microsoft.VisualStudio.Workload.UniversalBuildTools",
        "Microsoft.VisualStudio.Component.Windows11SDK.26100"
    ];

    private static readonly string VswherePath = Path.Combine(
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86),
        @"Microsoft Visual Studio\Installer\vswhere.exe");

    public override string Name => "Visual Studio Build Tools";

    public override string? Detect()
    {
        if (!File.Exists(VswherePath))
        {
            return null;
        }

        var installedVersion = FirstNonEmptyLine(RunAndCapture(
            VswherePath,
            "-latest -products * " +
            "-requires Microsoft.Component.MSBuild " +
            "-requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 " +
            "-property installationVersion -format value"));

        if (string.IsNullOrWhiteSpace(installedVersion))
        {
            return null;
        }

        if (Version.TryParse(installedVersion, out var installed)
            && Version.TryParse(minimumVersion, out var minimum)
            && installed < minimum)
        {
            return null;
        }

        if (FindMsBuildPath() is null || !HasRequiredFiles())
        {
            return null;
        }

        return installedVersion;
    }

    public override async Task InstallAsync(CancellationToken cancellationToken)
    {
        if (WindowsRestartDetector.IsRestartPending())
        {
            throw SetupRestartRequiredException.BeforeVisualStudioInstall();
        }

        var overrideArguments = string.Join(" ",
            new[] { "--quiet", "--wait", "--norestart" }
                .Concat(RequiredComponents.Select(static component => $"--add {component}"))
                .Concat(new[] { "--includeRecommended" }));

        var arguments =
            $"install -e --id {WingetPackageId} --source winget " +
            "--accept-package-agreements --accept-source-agreements --silent --force " +
            $"--override \"{overrideArguments}\"";

        var result = await ProcessUtil.RunAsync(
            "winget",
            arguments,
            Directory.GetCurrentDirectory(),
            cancellationToken);

        if (result.ExitCode != 0 && result.ExitCode != 3010)
        {
            throw new InvalidOperationException(
                BuildInstallFailureMessage(result.ExitCode, result.Stdout, result.Stderr));
        }

        if (result.ExitCode == 3010
            || RequiresRestart(result.Stdout)
            || RequiresRestart(result.Stderr)
            || WindowsRestartDetector.IsRestartPending())
        {
            throw SetupRestartRequiredException.AfterVisualStudioInstall();
        }
    }

    public static string? FindMsBuildPath()
    {
        if (!File.Exists(VswherePath))
        {
            return null;
        }

        return FirstNonEmptyLine(RunAndCapture(
            VswherePath,
            "-latest -products * -requires Microsoft.Component.MSBuild " +
            "-find MSBuild\\**\\Bin\\MSBuild.exe"));
    }

    public static string? FindVsDevCmdPath()
    {
        if (!File.Exists(VswherePath))
        {
            return null;
        }

        return FirstNonEmptyLine(RunAndCapture(
            VswherePath,
            "-latest -products * -requires Microsoft.Component.MSBuild " +
            "-find Common7\\Tools\\VsDevCmd.bat"));
    }

    public static async Task EnsureSuccessInDeveloperShellAsync(
        string commandLine,
        string workingDirectory,
        string operationName,
        CancellationToken cancellationToken)
    {
        await ProcessUtil.EnsureSuccessAsync(
            "cmd.exe",
            BuildDeveloperShellArguments(commandLine),
            workingDirectory,
            operationName,
            cancellationToken);
    }

    public static string BuildDeveloperShellArguments(string commandLine)
    {
        var vsDevCmdPath = FindVsDevCmdPath()
            ?? throw new InvalidOperationException("VsDevCmd.bat could not be located. Visual Studio Build Tools are required.");

        return $"/d /c \"\"{vsDevCmdPath}\" -arch=amd64 -host_arch=amd64 >nul && {commandLine}\"";
    }

    private static string? FirstNonEmptyLine(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault(static line => !string.IsNullOrWhiteSpace(line));
    }

    private static bool RequiresRestart(string? output)
    {
        return !string.IsNullOrWhiteSpace(output)
            && output.Contains("restart your pc", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasRequiredFiles()
    {
        return File.Exists(FindWindowsSdkHeaderPath());
    }

    private static string FindWindowsSdkHeaderPath()
    {
        return Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFilesX86),
            @"Windows Kits\10\Include",
            RequiredWindowsSdkVersion,
            "um",
            "Windows.h");
    }

    private static string BuildInstallFailureMessage(int exitCode, string stdout, string stderr)
    {
        var message = $"Visual Studio Build Tools install exited with code {exitCode}.";
        if (!string.IsNullOrWhiteSpace(stdout))
        {
            message += $"{System.Environment.NewLine}{System.Environment.NewLine}stdout:{System.Environment.NewLine}{stdout.Trim()}";
        }

        if (!string.IsNullOrWhiteSpace(stderr))
        {
            message += $"{System.Environment.NewLine}{System.Environment.NewLine}stderr:{System.Environment.NewLine}{stderr.Trim()}";
        }

        return message;
    }
}
