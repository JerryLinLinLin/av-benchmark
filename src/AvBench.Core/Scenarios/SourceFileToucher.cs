namespace AvBench.Core.Scenarios;

internal static class SourceFileToucher
{
    public static void Touch(string path)
    {
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Incremental touch target does not exist: {path}");
        }

        var currentWriteTimeUtc = File.GetLastWriteTimeUtc(path);
        var nextWriteTimeUtc = DateTime.UtcNow;
        if (nextWriteTimeUtc <= currentWriteTimeUtc.AddSeconds(1))
        {
            nextWriteTimeUtc = currentWriteTimeUtc.AddSeconds(2);
        }

        File.SetLastWriteTimeUtc(path, nextWriteTimeUtc);
    }
}
