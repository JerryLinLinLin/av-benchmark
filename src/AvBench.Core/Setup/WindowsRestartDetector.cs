using Microsoft.Win32;
using System.Runtime.Versioning;

namespace AvBench.Core.Setup;

[SupportedOSPlatform("windows")]
internal static class WindowsRestartDetector
{
    private const string ComponentBasedServicingRebootPending = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending";
    private const string WindowsUpdateRebootRequired = @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired";
    private const string SessionManager = @"SYSTEM\CurrentControlSet\Control\Session Manager";
    private const string PendingFileRenameOperationsValueName = "PendingFileRenameOperations";
    private const string NtPathPrefix = @"\??\";
    private static readonly string VisualStudioBootstrapperDirectory = Path.GetFullPath(
        @"C:\ProgramData\Microsoft\VisualStudio\Packages\_bootstrapper");

    public static bool IsRestartPending()
    {
        return HasSubKey(RegistryHive.LocalMachine, ComponentBasedServicingRebootPending)
            || HasSubKey(RegistryHive.LocalMachine, WindowsUpdateRebootRequired)
            || HasPendingFileRenameOperations();
    }

    private static bool HasSubKey(RegistryHive hive, string subKeyPath)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using var subKey = baseKey.OpenSubKey(subKeyPath);
        return subKey is not null;
    }

    private static bool HasPendingFileRenameOperations()
    {
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);
        using var sessionManagerKey = baseKey.OpenSubKey(SessionManager);
        if (sessionManagerKey?.GetValue(PendingFileRenameOperationsValueName) is not string[] pendingOperations)
        {
            return false;
        }

        foreach (var operation in pendingOperations)
        {
            if (string.IsNullOrWhiteSpace(operation))
            {
                continue;
            }

            if (!IsIgnorablePendingRename(operation))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsIgnorablePendingRename(string operation)
    {
        var normalized = NormalizePendingRenamePath(operation);
        if (normalized is null)
        {
            return false;
        }

        // Visual Studio Build Tools 2026 can leave a bootstrapper cleanup delete queued
        // even when vswhere reports the instance is complete and no reboot is required.
        return normalized.StartsWith(VisualStudioBootstrapperDirectory, StringComparison.OrdinalIgnoreCase)
            && Path.GetFileName(normalized).StartsWith("vs_setup_bootstrapper_", StringComparison.OrdinalIgnoreCase)
            && string.Equals(Path.GetExtension(normalized), ".json", StringComparison.OrdinalIgnoreCase);
    }

    private static string? NormalizePendingRenamePath(string operation)
    {
        var trimmed = operation.Trim();
        if (trimmed.Length == 0)
        {
            return null;
        }

        if (trimmed.StartsWith(NtPathPrefix, StringComparison.Ordinal))
        {
            trimmed = trimmed[NtPathPrefix.Length..];
        }

        try
        {
            return Path.GetFullPath(trimmed);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }
}
