using System.CommandLine;
using System.Runtime.Versioning;
using System.Security.Principal;
using AvBench.Cli.Commands;
using AvBench.Core.Setup;

if (!OperatingSystem.IsWindows())
{
    Console.Error.WriteLine("ERROR: avbench only supports Windows.");
    return 1;
}

if (!IsAdministrator())
{
    Console.Error.WriteLine("ERROR: avbench must run from an elevated Administrator terminal.");
    return 1;
}

KnownToolPaths.EnsureCommonToolPaths();

var rootCommand = new RootCommand("AV benchmark suite for Windows build and API activity measurement.");
rootCommand.Subcommands.Add(SetupCommand.Create());
rootCommand.Subcommands.Add(RunCommand.Create());

return rootCommand.Parse(args).Invoke();

[SupportedOSPlatform("windows")]
static bool IsAdministrator()
{
    using var identity = WindowsIdentity.GetCurrent();
    var principal = new WindowsPrincipal(identity);
    return principal.IsInRole(WindowsBuiltInRole.Administrator);
}
