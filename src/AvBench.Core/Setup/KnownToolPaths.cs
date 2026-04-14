namespace AvBench.Core.Setup;

public static class KnownToolPaths
{
    public const string DotNetInstallDirectory = @"C:\Program Files\dotnet";
    public const string NinjaInstallDirectory = @"C:\Tools\ninja";

    public static void EnsureCommonToolPaths()
    {
        EnsureGitOnPath();
        EnsureCargoOnPath();
        EnsureDotNetOnPath();
        EnsureNinjaOnPath();
        EnsureCmakeOnPath();
        EnsurePythonOnPath();
    }

    public static void EnsureGitOnPath()
    {
        AddToPathIfExists(@"C:\Program Files\Git\cmd");
    }

    public static void EnsureCargoOnPath()
    {
        var cargoPath = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile),
            ".cargo",
            "bin");

        AddToPathIfExists(cargoPath);
    }

    public static void EnsureDotNetOnPath()
    {
        AddToPathIfExists(DotNetInstallDirectory);
    }

    public static void EnsureNinjaOnPath()
    {
        AddToPathIfExists(NinjaInstallDirectory);
    }

    public static void EnsureCmakeOnPath()
    {
        AddToPathIfExists(@"C:\Program Files\CMake\bin");
    }

    public static void EnsurePythonOnPath()
    {
        var localPrograms = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
            "Programs",
            "Python");

        if (Directory.Exists(localPrograms))
        {
            foreach (var pythonHome in Directory.EnumerateDirectories(localPrograms, "Python*").OrderByDescending(static path => path, StringComparer.OrdinalIgnoreCase))
            {
                AddToPathIfExists(pythonHome);
                AddToPathIfExists(Path.Combine(pythonHome, "Scripts"));
            }
        }

        var allUsersPrograms = Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.ProgramFiles),
            "Python");

        if (Directory.Exists(allUsersPrograms))
        {
            foreach (var pythonHome in Directory.EnumerateDirectories(allUsersPrograms, "Python*").OrderByDescending(static path => path, StringComparer.OrdinalIgnoreCase))
            {
                AddToPathIfExists(pythonHome);
                AddToPathIfExists(Path.Combine(pythonHome, "Scripts"));
            }
        }
    }

    private static void AddToPathIfExists(string path)
    {
        if (!Directory.Exists(path))
        {
            return;
        }

        var currentPath = System.Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var segments = currentPath.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Contains(path, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        System.Environment.SetEnvironmentVariable("PATH", $"{path};{currentPath}");
    }
}
