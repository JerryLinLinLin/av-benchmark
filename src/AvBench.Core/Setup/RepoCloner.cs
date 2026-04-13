using System.Text.Json;
using AvBench.Core.Internal;
using AvBench.Core.Models;

namespace AvBench.Core.Setup;

public static class RepoCloner
{
    private const string RipgrepRepoUrl = "https://github.com/BurntSushi/ripgrep.git";
    private const string RoslynRepoUrl = "https://github.com/dotnet/roslyn.git";
    private const string LlvmRepoUrl = "https://github.com/llvm/llvm-project.git";
    private const string FilesRepoUrl = "https://github.com/files-community/Files.git";

    public static Task<RepoEntry> CloneRipgrepAsync(string benchDirectory, string? revision, CancellationToken cancellationToken)
        => CloneRepositoryAsync("ripgrep", RipgrepRepoUrl, Path.Combine(benchDirectory, "ripgrep"), revision, cancellationToken);

    public static Task<RepoEntry> CloneRoslynAsync(string benchDirectory, CancellationToken cancellationToken)
        => CloneRepositoryAsync("roslyn", RoslynRepoUrl, Path.Combine(benchDirectory, "roslyn"), revision: null, cancellationToken);

    public static Task<RepoEntry> CloneLlvmAsync(string benchDirectory, CancellationToken cancellationToken)
        => CloneRepositoryAsync("llvm-project", LlvmRepoUrl, Path.Combine(benchDirectory, "llvm-project"), revision: null, cancellationToken);

    public static Task<RepoEntry> CloneFilesAsync(string benchDirectory, CancellationToken cancellationToken)
        => CloneRepositoryAsync("Files", FilesRepoUrl, Path.Combine(benchDirectory, "Files"), revision: null, cancellationToken);

    public static async Task CargoFetchAsync(string repoDirectory, CancellationToken cancellationToken)
    {
        Console.WriteLine($"[setup] Running cargo fetch in {repoDirectory}");
        await ProcessUtil.EnsureSuccessAsync("cargo", "fetch", repoDirectory, "cargo fetch", cancellationToken);
    }

    public static async Task HydrateRoslynAsync(string repoDirectory, CancellationToken cancellationToken)
    {
        var restorePath = Path.Combine(repoDirectory, "Restore.cmd");
        if (!File.Exists(restorePath))
        {
            throw new InvalidOperationException($"Roslyn restore script was not found at {restorePath}.");
        }

        Console.WriteLine($"[setup] Running Roslyn restore in {repoDirectory}");
        await ProcessUtil.EnsureSuccessAsync("cmd.exe", "/d /c Restore.cmd", repoDirectory, "Roslyn Restore.cmd", cancellationToken);
    }

    public static async Task HydrateLlvmAsync(string repoDirectory, string buildDirectory, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(buildDirectory);

        Console.WriteLine($"[setup] Configuring LLVM in {buildDirectory}");
        await VsBuildToolsInstaller.EnsureSuccessInDeveloperShellAsync(
            BuildLlvmConfigureCommand(repoDirectory, buildDirectory),
            repoDirectory,
            "LLVM CMake configure",
            cancellationToken);
    }

    public static async Task HydrateFilesAsync(string repoDirectory, CancellationToken cancellationToken)
    {
        var msbuildPath = VsBuildToolsInstaller.FindMsBuildPath()
            ?? throw new InvalidOperationException("MSBuild.exe could not be located. Visual Studio Build Tools are required.");

        var solutionPath = Path.Combine(repoDirectory, "Files.slnx");
        if (!File.Exists(solutionPath))
        {
            throw new InvalidOperationException($"Files solution was not found at {solutionPath}.");
        }

        Console.WriteLine($"[setup] Restoring Files solution in {repoDirectory}");
        await ProcessUtil.EnsureSuccessAsync(
            msbuildPath,
            $"\"{solutionPath}\" /t:Restore /p:Configuration=Release /p:Platform=x64 /nr:false",
            repoDirectory,
            "Files restore",
            cancellationToken);
    }

    public static string ResolveRipgrepTouchPath(string repoDirectory)
        => ResolveFirstExistingPath(
            new[]
            {
                Path.Combine(repoDirectory, "crates", "core", "main.rs"),
                Path.Combine(repoDirectory, "crates", "core", "lib.rs"),
                Path.Combine(repoDirectory, "src", "main.rs")
            },
            () => Directory.EnumerateFiles(repoDirectory, "*.rs", SearchOption.AllDirectories)
                .Where(static path => !path.Contains($"{Path.DirectorySeparatorChar}target{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .OrderBy(static path => path.Length)
                .FirstOrDefault(),
            "Unable to locate a Rust source file for the ripgrep incremental scenario.");

    public static string ResolveRoslynTouchPath(string repoDirectory)
        => ResolveFirstExistingPath(
            new[]
            {
                Path.Combine(repoDirectory, "src", "Compilers", "Core", "Portable", "CommandLineParser.cs"),
                Path.Combine(repoDirectory, "src", "Compilers", "CSharp", "Portable", "CSharpCommandLineParser.cs"),
                Path.Combine(repoDirectory, "src", "Compilers", "VisualBasic", "Portable", "VisualBasicCommandLineParser.vb")
            },
            () => Directory.EnumerateFiles(Path.Combine(repoDirectory, "src"), "*.cs", SearchOption.AllDirectories)
                .Where(static path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .OrderBy(static path => path.Length)
                .FirstOrDefault(),
            "Unable to locate a C# source file for the Roslyn incremental scenario.");

    public static string ResolveLlvmTouchPath(string repoDirectory)
        => ResolveFirstExistingPath(
            new[]
            {
                Path.Combine(repoDirectory, "llvm", "lib", "Support", "APInt.cpp"),
                Path.Combine(repoDirectory, "llvm", "lib", "Support", "CommandLine.cpp"),
                Path.Combine(repoDirectory, "llvm", "lib", "Support", "WithColor.cpp")
            },
            () => Directory.EnumerateFiles(Path.Combine(repoDirectory, "llvm"), "*.cpp", SearchOption.AllDirectories)
                .Where(static path => !path.Contains($"{Path.DirectorySeparatorChar}build{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .OrderBy(static path => path.Length)
                .FirstOrDefault(),
            "Unable to locate a C++ source file for the LLVM incremental scenario.");

    public static string ResolveFilesTouchPath(string repoDirectory)
        => ResolveFirstExistingPath(
            new[]
            {
                Path.Combine(repoDirectory, "src", "Files.App", "App.xaml.cs"),
                Path.Combine(repoDirectory, "src", "Files.App", "MainWindow.xaml.cs")
            },
            () => Directory.EnumerateFiles(Path.Combine(repoDirectory, "src", "Files.App"), "*.cs", SearchOption.AllDirectories)
                .Where(static path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .OrderBy(static path => path.Length)
                .FirstOrDefault(),
            "Unable to locate a Files C# source file for the incremental scenario.");

    public static string ResolveDotNetSdkVersion(string repoDirectory)
    {
        using var document = LoadGlobalJson(repoDirectory);
        if (document.RootElement.TryGetProperty("sdk", out var sdkNode)
            && sdkNode.TryGetProperty("version", out var versionNode)
            && versionNode.GetString() is { Length: > 0 } version)
        {
            return version;
        }

        var globalJsonPath = Path.Combine(repoDirectory, "global.json");
        throw new InvalidOperationException($"global.json at {globalJsonPath} does not declare sdk.version.");
    }

    public static string? ResolveVisualStudioVersion(string repoDirectory)
    {
        using var document = LoadGlobalJson(repoDirectory);
        if (document.RootElement.TryGetProperty("tools", out var toolsNode)
            && toolsNode.TryGetProperty("vs", out var vsNode)
            && vsNode.TryGetProperty("version", out var versionNode)
            && versionNode.GetString() is { Length: > 0 } version)
        {
            return version;
        }

        return null;
    }

    public static string BuildLlvmConfigureCommand(string repoDirectory, string buildDirectory)
    {
        var sourceDirectory = Path.Combine(repoDirectory, "llvm");
        return string.Join(" ",
            "cmake",
            $"-S \"{sourceDirectory}\"",
            $"-B \"{buildDirectory}\"",
            "-G Ninja",
            "-DLLVM_ENABLE_PROJECTS=clang",
            "-DLLVM_TARGETS_TO_BUILD=X86",
            "-DCMAKE_BUILD_TYPE=Release");
    }

    private static async Task<RepoEntry> CloneRepositoryAsync(
        string name,
        string repoUrl,
        string targetDirectory,
        string? revision,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(targetDirectory)!);

        if (!Directory.Exists(targetDirectory))
        {
            Console.WriteLine($"[setup] Cloning {repoUrl} into {targetDirectory}");

            var cloneArguments = string.IsNullOrWhiteSpace(revision)
                ? $"clone --depth 1 --filter=blob:none --config core.autocrlf=false {repoUrl} \"{targetDirectory}\""
                : $"clone --config core.autocrlf=false {repoUrl} \"{targetDirectory}\"";

            await RunGitAsync(cloneArguments, Path.GetDirectoryName(targetDirectory)!, cancellationToken);
        }
        else
        {
            Console.WriteLine($"[setup] Reusing existing repo: {targetDirectory}");
        }

        await RunGitAsync($"-C \"{targetDirectory}\" fetch --all --tags --prune", Directory.GetCurrentDirectory(), cancellationToken);

        if (!string.IsNullOrWhiteSpace(revision))
        {
            await RunGitAsync($"-C \"{targetDirectory}\" checkout {revision}", Directory.GetCurrentDirectory(), cancellationToken);
        }

        var sha = ToolInstaller.RunAndCapture("git", $"-C \"{targetDirectory}\" rev-parse HEAD")
            ?? throw new InvalidOperationException($"Unable to resolve {name} HEAD SHA.");

        return new RepoEntry
        {
            Name = name,
            Url = repoUrl,
            Sha = sha,
            LocalPath = targetDirectory
        };
    }

    private static string ResolveFirstExistingPath(
        IEnumerable<string> preferredCandidates,
        Func<string?> fallbackResolver,
        string errorMessage)
    {
        foreach (var candidate in preferredCandidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        var fallback = fallbackResolver();
        return !string.IsNullOrWhiteSpace(fallback)
            ? fallback
            : throw new InvalidOperationException(errorMessage);
    }

    private static async Task RunGitAsync(string arguments, string workingDirectory, CancellationToken cancellationToken)
    {
        await ProcessUtil.EnsureSuccessAsync("git", arguments, workingDirectory, $"git {arguments}", cancellationToken);
    }

    private static JsonDocument LoadGlobalJson(string repoDirectory)
    {
        var globalJsonPath = Path.Combine(repoDirectory, "global.json");
        if (!File.Exists(globalJsonPath))
        {
            throw new InvalidOperationException($"global.json was not found at {globalJsonPath}.");
        }

        return JsonDocument.Parse(File.ReadAllText(globalJsonPath));
    }
}
