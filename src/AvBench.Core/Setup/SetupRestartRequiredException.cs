namespace AvBench.Core.Setup;

public sealed class SetupRestartRequiredException(string message) : Exception(message)
{
    public static SetupRestartRequiredException BeforeVisualStudioInstall() =>
        new("A Windows restart is already pending. Restart the PC, reopen an elevated terminal, and rerun `avbench setup` before installing Visual Studio prerequisites.");

    public static SetupRestartRequiredException AfterVisualStudioInstall() =>
        new("Visual Studio installation finished, but Windows must be restarted before benchmark setup can continue. Restart the PC, reopen an elevated terminal, and rerun `avbench setup`.");

    public static SetupRestartRequiredException PendingVisualStudioFinalize() =>
        new("Visual Studio still has pending restart work. Restart the PC, reopen an elevated terminal, and rerun `avbench setup` before hydrating benchmark repos.");
}
