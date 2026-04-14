namespace AvBench.Core.Internal;

internal static class FileSystemUtil
{
    public static void DeletePathIfExists(string path)
    {
        if (Directory.Exists(path))
        {
            PrepareDirectoryForDeletion(path);
            Directory.Delete(path, recursive: true);
            return;
        }

        if (File.Exists(path))
        {
            File.SetAttributes(path, FileAttributes.Normal);
            File.Delete(path);
        }
    }

    private static void PrepareDirectoryForDeletion(string directoryPath)
    {
        foreach (var childDirectory in Directory.EnumerateDirectories(directoryPath, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(childDirectory, FileAttributes.Normal);
        }

        foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(filePath, FileAttributes.Normal);
        }

        File.SetAttributes(directoryPath, FileAttributes.Normal);
    }
}
