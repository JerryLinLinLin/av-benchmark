using AvBench.Core.Internal;

namespace AvBench.Core.Setup;

public sealed class VsBuildToolsInstaller(string? minimumVersion = null) : ToolInstaller
{
    private const string WingetPackageId = "Microsoft.VisualStudio.BuildTools";

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

        return installedVersion;
    }

    public override async Task InstallAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();

        var overrideArguments = string.Join(" ",
            "--quiet",
            "--wait",
            "--norestart",
            "--add Microsoft.VisualStudio.Workload.VCTools",
            "--add Microsoft.VisualStudio.Workload.ManagedDesktopBuildTools",
            "--add Microsoft.VisualStudio.Workload.UniversalBuildTools",
            "--add Microsoft.VisualStudio.Component.Windows11SDK.26100",
            "--add Microsoft.VisualStudio.ComponentGroup.WindowsAppSDK.Cs",
            "--includeRecommended");

        var arguments =
            $"install -e --id {WingetPackageId} --source winget " +
            "--accept-package-agreements --accept-source-agreements --silent " +
            $"--override \"{overrideArguments}\"";

        var exitCode = RunProcess("winget", arguments);
        if (exitCode != 0 && exitCode != 3010)
        {
            throw new InvalidOperationException($"Visual Studio Build Tools install exited with code {exitCode}.");
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
}
