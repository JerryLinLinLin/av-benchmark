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
