using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text.Json;
using AvBench.Core.Internal;
using AvBench.Core.Models;

namespace AvBench.Core.Setup;

public static class RepoCloner
{
    private const string MetadataFileName = ".avbench-source.json";

    public static Task<RepoEntry> CloneRipgrepAsync(string benchDirectory, string? revision, CancellationToken cancellationToken)
        => PrepareRepositoryAsync(
            new GitHubRepositorySpec(
                "ripgrep",
                "BurntSushi",
                "ripgrep",
                Path.Combine(benchDirectory, "ripgrep"),
                revision,
                RepositorySourcePreference.LatestRelease),
            cancellationToken);

    public static Task<RepoEntry> CloneRoslynAsync(string benchDirectory, CancellationToken cancellationToken)
        => PrepareRepositoryAsync(
            new GitHubRepositorySpec(
                "roslyn",
                "dotnet",
                "roslyn",
                Path.Combine(benchDirectory, "roslyn"),
                null,
                RepositorySourcePreference.DefaultBranchHead),
            cancellationToken);

    public static Task<RepoEntry> CloneLlvmAsync(string benchDirectory, CancellationToken cancellationToken)
        => PrepareRepositoryAsync(
            new GitHubRepositorySpec(
                "llvm-project",
                "llvm",
                "llvm-project",
                Path.Combine(benchDirectory, "llvm-project"),
                null,
                RepositorySourcePreference.LatestRelease),
            cancellationToken);

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

    private static async Task<RepoEntry> PrepareRepositoryAsync(
        GitHubRepositorySpec spec,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(spec.TargetDirectory)!);

        var resolution = await ResolveRepositorySourceAsync(spec, cancellationToken);
        var existingCommit = TryReadExistingCommitSha(spec.TargetDirectory);

        if (string.Equals(existingCommit, resolution.CommitSha, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[setup] Reusing existing source tree: {spec.TargetDirectory} @ {resolution.CommitSha}");
        }
        else
        {
            if (Directory.Exists(spec.TargetDirectory))
            {
                Console.WriteLine($"[setup] Replacing {spec.TargetDirectory} with source archive {resolution.SourceReference} ({resolution.CommitSha})");
                Directory.Delete(spec.TargetDirectory, recursive: true);
            }
            else
            {
                Console.WriteLine($"[setup] Downloading {spec.Owner}/{spec.Repository} source archive {resolution.SourceReference} ({resolution.CommitSha})");
            }

            await DownloadAndExtractArchiveAsync(resolution.ArchiveUrl, spec.TargetDirectory, cancellationToken);
            WriteMetadata(spec.TargetDirectory, resolution);
        }

        return new RepoEntry
        {
            Name = spec.Name,
            Url = spec.RepositoryUrl,
            Sha = resolution.CommitSha,
            SourceKind = resolution.SourceKind,
            SourceReference = resolution.SourceReference,
            ArchiveUrl = resolution.ArchiveUrl,
            LocalPath = spec.TargetDirectory
        };
    }

    private static async Task<GitHubArchiveResolution> ResolveRepositorySourceAsync(
        GitHubRepositorySpec spec,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(spec.Revision))
        {
            return await ResolveRevisionAsync(spec, spec.Revision, cancellationToken);
        }

        return spec.Preference switch
        {
            RepositorySourcePreference.LatestRelease => await TryResolveLatestReleaseAsync(spec, cancellationToken)
                ?? await ResolveDefaultBranchAsync(spec, cancellationToken),
            RepositorySourcePreference.DefaultBranchHead => await ResolveDefaultBranchAsync(spec, cancellationToken),
            _ => throw new InvalidOperationException($"Unknown repository source preference: {spec.Preference}")
        };
    }

    private static async Task<GitHubArchiveResolution?> TryResolveLatestReleaseAsync(
        GitHubRepositorySpec spec,
        CancellationToken cancellationToken)
    {
        using var releaseDocument = await TryGetGitHubJsonAsync(
            $"repos/{spec.Owner}/{spec.Repository}/releases/latest",
            cancellationToken);

        if (releaseDocument is null)
        {
            return null;
        }

        if (!releaseDocument.RootElement.TryGetProperty("tag_name", out var tagNode)
            || string.IsNullOrWhiteSpace(tagNode.GetString()))
        {
            throw new InvalidOperationException($"GitHub latest release metadata for {spec.Owner}/{spec.Repository} did not include tag_name.");
        }

        var tag = tagNode.GetString()!;
        var commitSha = await ResolveTagCommitShaAsync(spec, tag, cancellationToken);
        return new GitHubArchiveResolution(
            "github-latest-release-archive",
            tag,
            commitSha,
            BuildArchiveUrl(spec, commitSha));
    }

    private static async Task<GitHubArchiveResolution> ResolveDefaultBranchAsync(
        GitHubRepositorySpec spec,
        CancellationToken cancellationToken)
    {
        using var repositoryDocument = await GetGitHubJsonAsync(
            $"repos/{spec.Owner}/{spec.Repository}",
            cancellationToken);

        if (!repositoryDocument.RootElement.TryGetProperty("default_branch", out var branchNode)
            || string.IsNullOrWhiteSpace(branchNode.GetString()))
        {
            throw new InvalidOperationException($"GitHub repository metadata for {spec.Owner}/{spec.Repository} did not include default_branch.");
        }

        var branchName = branchNode.GetString()!;

        using var branchDocument = await GetGitHubJsonAsync(
            $"repos/{spec.Owner}/{spec.Repository}/branches/{Uri.EscapeDataString(branchName)}",
            cancellationToken);

        var commitSha = branchDocument.RootElement
            .GetProperty("commit")
            .GetProperty("sha")
            .GetString();

        if (string.IsNullOrWhiteSpace(commitSha))
        {
            throw new InvalidOperationException($"GitHub branch metadata for {spec.Owner}/{spec.Repository}:{branchName} did not include commit.sha.");
        }

        return new GitHubArchiveResolution(
            "github-default-branch-archive",
            branchName,
            commitSha,
            BuildArchiveUrl(spec, commitSha));
    }

    private static async Task<GitHubArchiveResolution> ResolveRevisionAsync(
        GitHubRepositorySpec spec,
        string revision,
        CancellationToken cancellationToken)
    {
        using var commitDocument = await GetGitHubJsonAsync(
            $"repos/{spec.Owner}/{spec.Repository}/commits/{Uri.EscapeDataString(revision)}",
            cancellationToken);

        var commitSha = commitDocument.RootElement.GetProperty("sha").GetString();
        if (string.IsNullOrWhiteSpace(commitSha))
        {
            throw new InvalidOperationException($"GitHub commit metadata for {spec.Owner}/{spec.Repository}:{revision} did not include sha.");
        }

        return new GitHubArchiveResolution(
            "github-ref-archive",
            revision,
            commitSha,
            BuildArchiveUrl(spec, commitSha));
    }

    private static async Task<string> ResolveTagCommitShaAsync(
        GitHubRepositorySpec spec,
        string tag,
        CancellationToken cancellationToken)
    {
        using var refDocument = await GetGitHubJsonAsync(
            $"repos/{spec.Owner}/{spec.Repository}/git/ref/tags/{Uri.EscapeDataString(tag)}",
            cancellationToken);

        var target = refDocument.RootElement.GetProperty("object");
        var objectType = target.GetProperty("type").GetString();
        var objectSha = target.GetProperty("sha").GetString();

        if (string.IsNullOrWhiteSpace(objectType) || string.IsNullOrWhiteSpace(objectSha))
        {
            throw new InvalidOperationException($"Git tag metadata for {spec.Owner}/{spec.Repository}:{tag} was incomplete.");
        }

        if (string.Equals(objectType, "commit", StringComparison.OrdinalIgnoreCase))
        {
            return objectSha;
        }

        if (!string.Equals(objectType, "tag", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Unsupported Git tag object type '{objectType}' for {spec.Owner}/{spec.Repository}:{tag}.");
        }

        using var tagDocument = await GetGitHubJsonAsync(
            $"repos/{spec.Owner}/{spec.Repository}/git/tags/{objectSha}",
            cancellationToken);

        var commitNode = tagDocument.RootElement.GetProperty("object");
        var commitType = commitNode.GetProperty("type").GetString();
        var commitSha = commitNode.GetProperty("sha").GetString();

        if (!string.Equals(commitType, "commit", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(commitSha))
        {
            throw new InvalidOperationException($"Annotated tag {spec.Owner}/{spec.Repository}:{tag} did not resolve to a commit.");
        }

        return commitSha;
    }

    private static async Task DownloadAndExtractArchiveAsync(
        string archiveUrl,
        string targetDirectory,
        CancellationToken cancellationToken)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "avbench", "sources");
        Directory.CreateDirectory(tempRoot);

        var archivePath = Path.Combine(tempRoot, $"{Guid.NewGuid():N}.zip");
        var extractDirectory = Path.Combine(tempRoot, Guid.NewGuid().ToString("N"));

        try
        {
            await DownloadArchiveAsync(archiveUrl, archivePath, cancellationToken);
            Directory.CreateDirectory(extractDirectory);
            ZipFile.ExtractToDirectory(archivePath, extractDirectory);

            var extractedRoot = Directory.EnumerateDirectories(extractDirectory).SingleOrDefault();
            if (extractedRoot is null)
            {
                throw new InvalidOperationException($"Archive {archiveUrl} did not contain a single top-level directory.");
            }

            Directory.Move(extractedRoot, targetDirectory);
        }
        finally
        {
            if (File.Exists(archivePath))
            {
                File.Delete(archivePath);
            }

            if (Directory.Exists(extractDirectory))
            {
                Directory.Delete(extractDirectory, recursive: true);
            }
        }
    }

    private static async Task DownloadArchiveAsync(string archiveUrl, string destinationPath, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

        using var client = CreateGitHubClient();
        using var response = await client.GetAsync(archiveUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var output = File.Create(destinationPath);
        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        await input.CopyToAsync(output, cancellationToken);
    }

    private static string? TryReadExistingCommitSha(string targetDirectory)
    {
        if (!Directory.Exists(targetDirectory))
        {
            return null;
        }

        var metadataPath = Path.Combine(targetDirectory, MetadataFileName);
        if (File.Exists(metadataPath))
        {
            try
            {
                var metadata = JsonSerializer.Deserialize<RepositoryWorkspaceMetadata>(File.ReadAllText(metadataPath));
                if (!string.IsNullOrWhiteSpace(metadata?.CommitSha))
                {
                    return metadata.CommitSha;
                }
            }
            catch (JsonException)
            {
            }
        }

        var gitDirectory = Path.Combine(targetDirectory, ".git");
        return Directory.Exists(gitDirectory)
            ? ToolInstaller.RunAndCapture("git", $"-C \"{targetDirectory}\" rev-parse HEAD")
            : null;
    }

    private static void WriteMetadata(string targetDirectory, GitHubArchiveResolution resolution)
    {
        var metadataPath = Path.Combine(targetDirectory, MetadataFileName);
        var metadata = new RepositoryWorkspaceMetadata
        {
            SourceKind = resolution.SourceKind,
            SourceReference = resolution.SourceReference,
            CommitSha = resolution.CommitSha,
            ArchiveUrl = resolution.ArchiveUrl
        };

        File.WriteAllText(metadataPath, JsonSerializer.Serialize(metadata, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }

    private static string BuildArchiveUrl(GitHubRepositorySpec spec, string commitSha)
        => $"https://api.github.com/repos/{spec.Owner}/{spec.Repository}/zipball/{commitSha}";

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

    private static JsonDocument LoadGlobalJson(string repoDirectory)
    {
        var globalJsonPath = Path.Combine(repoDirectory, "global.json");
        if (!File.Exists(globalJsonPath))
        {
            throw new InvalidOperationException($"global.json was not found at {globalJsonPath}.");
        }

        return JsonDocument.Parse(File.ReadAllText(globalJsonPath));
    }

    private static async Task<JsonDocument> GetGitHubJsonAsync(string relativeUrl, CancellationToken cancellationToken)
    {
        using var client = CreateGitHubClient();
        using var response = await client.GetAsync(relativeUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    private static async Task<JsonDocument?> TryGetGitHubJsonAsync(string relativeUrl, CancellationToken cancellationToken)
    {
        using var client = CreateGitHubClient();
        using var response = await client.GetAsync(relativeUrl, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    private static HttpClient CreateGitHubClient()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri("https://api.github.com/")
        };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("avbench", "0.2.0"));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        return client;
    }

    private sealed record GitHubRepositorySpec(
        string Name,
        string Owner,
        string Repository,
        string TargetDirectory,
        string? Revision,
        RepositorySourcePreference Preference)
    {
        public string RepositoryUrl => $"https://github.com/{Owner}/{Repository}";
    }

    private sealed record GitHubArchiveResolution(
        string SourceKind,
        string SourceReference,
        string CommitSha,
        string ArchiveUrl);

    private sealed class RepositoryWorkspaceMetadata
    {
        public string? SourceKind { get; set; }

        public string? SourceReference { get; set; }

        public string? CommitSha { get; set; }

        public string? ArchiveUrl { get; set; }
    }

    private enum RepositorySourcePreference
    {
        LatestRelease,
        DefaultBranchHead
    }
}
