using System.IO.Compression;
using AvBench.Core.Internal;
using AvBench.Core.Models;

namespace AvBench.Core.Microbench;

public static class MicrobenchSupport
{
    public const string SupportVersion = "m3-support-v2";
    public const string RequiredDotNetSdkVersion = "8.0.303";

    private const string VersionMarkerFileName = "support-version.txt";

    public static async Task<MicrobenchSupportEntry> PrepareAsync(string benchDirectory, CancellationToken cancellationToken)
    {
        var supportRoot = Path.Combine(benchDirectory, "microbench-support");
        var runRoot = Path.Combine(benchDirectory, "microbench");
        var archiveZipPath = Path.Combine(supportRoot, "archive", "bench_archive.zip");
        var unsignedExePath = Path.Combine(supportRoot, "procbench", "out", "noop.exe");

        if (NeedsRefresh(supportRoot, archiveZipPath, unsignedExePath))
        {
            FileSystemUtil.DeletePathIfExists(supportRoot);
            Directory.CreateDirectory(supportRoot);

            await CreateArchiveZipAsync(archiveZipPath, cancellationToken);
            await BuildUnsignedNoopExeAsync(supportRoot, unsignedExePath, cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(supportRoot, VersionMarkerFileName), SupportVersion, cancellationToken);
        }

        Directory.CreateDirectory(runRoot);

        return new MicrobenchSupportEntry
        {
            Version = SupportVersion,
            SupportRoot = supportRoot,
            RunRoot = runRoot,
            ArchiveZipPath = archiveZipPath,
            UnsignedExePath = unsignedExePath
        };
    }

    public static Task PrepareWorkingDirectoryAsync(string runRoot, string scenarioId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var workingDirectory = Path.Combine(runRoot, scenarioId);
        FileSystemUtil.DeletePathIfExists(workingDirectory);
        Directory.CreateDirectory(workingDirectory);
        return Task.CompletedTask;
    }

    public static void ValidateManifestEntry(MicrobenchSupportEntry support)
    {
        if (!string.Equals(support.Version, SupportVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Microbench support version '{support.Version}' does not match runner support version '{SupportVersion}'. Run `avbench setup --workload microbench` again.");
        }

        if (!File.Exists(support.ArchiveZipPath) || !File.Exists(support.UnsignedExePath))
        {
            throw new InvalidOperationException(
                "Microbench support assets are missing from disk. Run `avbench setup --workload microbench` again.");
        }
    }

    private static bool NeedsRefresh(string supportRoot, string archiveZipPath, string unsignedExePath)
    {
        if (!Directory.Exists(supportRoot))
        {
            return true;
        }

        var markerPath = Path.Combine(supportRoot, VersionMarkerFileName);
        if (!File.Exists(markerPath))
        {
            return true;
        }

        var version = File.ReadAllText(markerPath).Trim();
        if (!string.Equals(version, SupportVersion, StringComparison.Ordinal))
        {
            return true;
        }

        return !File.Exists(archiveZipPath) || !File.Exists(unsignedExePath);
    }

    private static async Task CreateArchiveZipAsync(string archiveZipPath, CancellationToken cancellationToken)
    {
        var archiveDirectory = Path.GetDirectoryName(archiveZipPath)
            ?? throw new InvalidOperationException("Archive zip path does not have a parent directory.");
        Directory.CreateDirectory(archiveDirectory);

        var stageDirectory = Path.Combine(archiveDirectory, "stage");
        FileSystemUtil.DeletePathIfExists(stageDirectory);
        Directory.CreateDirectory(stageDirectory);

        try
        {
            var rng = new Random(42);
            string[] extensions = [".cs", ".js", ".json", ".xml", ".dll", ".exe", ".txt", ".md"];
            int[] sizes = [64, 256, 1024, 4096, 16384, 65536];

            for (var index = 0; index < 2_000; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var extension = extensions[index % extensions.Length];
                var size = sizes[index % sizes.Length];
                var subDirectory = Path.Combine(stageDirectory, $"pkg_{index / 100:D2}");
                Directory.CreateDirectory(subDirectory);

                var content = new byte[size];
                rng.NextBytes(content);
                if ((extension == ".dll" || extension == ".exe") && size >= 2)
                {
                    content[0] = 0x4D;
                    content[1] = 0x5A;
                }

                await File.WriteAllBytesAsync(Path.Combine(subDirectory, $"file_{index:D4}{extension}"), content, cancellationToken);
            }

            ZipFile.CreateFromDirectory(stageDirectory, archiveZipPath);
        }
        finally
        {
            FileSystemUtil.DeletePathIfExists(stageDirectory);
        }
    }

    private static async Task BuildUnsignedNoopExeAsync(string supportRoot, string unsignedExePath, CancellationToken cancellationToken)
    {
        var projectDirectory = Path.Combine(supportRoot, "procbench");
        var outputDirectory = Path.Combine(projectDirectory, "out");
        Directory.CreateDirectory(projectDirectory);
        Directory.CreateDirectory(outputDirectory);

        var programPath = Path.Combine(projectDirectory, "Program.cs");
        var projectPath = Path.Combine(projectDirectory, "noop.csproj");

        await File.WriteAllTextAsync(programPath, "return 0;" + System.Environment.NewLine, cancellationToken);
        await File.WriteAllTextAsync(
            projectPath,
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net8.0</TargetFramework>
              </PropertyGroup>
            </Project>
            """,
            cancellationToken);

        await ProcessUtil.EnsureSuccessAsync(
            "dotnet",
            $"build \"{projectPath}\" -c Release -o \"{outputDirectory}\"",
            projectDirectory,
            "Build unsigned noop.exe",
            cancellationToken);

        if (!File.Exists(unsignedExePath))
        {
            throw new InvalidOperationException($"Unsigned noop.exe was not produced at {unsignedExePath}.");
        }
    }
}
