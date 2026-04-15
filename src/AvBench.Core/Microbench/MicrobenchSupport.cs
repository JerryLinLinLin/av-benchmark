using System.IO.Compression;
using AvBench.Core.Internal;

namespace AvBench.Core.Microbench;

public static class MicrobenchSupport
{
    public static Task PrepareWorkingDirectoryAsync(string runRoot, string scenarioId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var workingDirectory = Path.Combine(runRoot, scenarioId);
        FileSystemUtil.DeletePathIfExists(workingDirectory);
        Directory.CreateDirectory(workingDirectory);
        return Task.CompletedTask;
    }

    public static async Task EnsureArchiveZipAsync(string supportRoot, string archiveZipPath, CancellationToken cancellationToken)
    {
        if (File.Exists(archiveZipPath))
        {
            return;
        }

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

    public static async Task EnsureUnsignedNoopExeAsync(string supportRoot, string unsignedExePath, CancellationToken cancellationToken)
    {
        if (File.Exists(unsignedExePath))
        {
            return;
        }

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
