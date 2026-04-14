using Microsoft.Win32;
using System.Runtime.Versioning;

namespace AvBench.Core.Setup;

[SupportedOSPlatform("windows")]
internal static class WindowsRestartDetector
{
    private const string ComponentBasedServicingRebootPending = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending";
    private const string WindowsUpdateRebootRequired = @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired";
    private const string SessionManager = @"SYSTEM\CurrentControlSet\Control\Session Manager";

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
        return sessionManagerKey?.GetValue("PendingFileRenameOperations") is string[] pendingOperations
            && pendingOperations.Length > 0;
    }
}
