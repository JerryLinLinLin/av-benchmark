namespace AvBench.Core.Collectors;

public interface IOptInCollector : IDisposable
{
    void Start(string outputDirectory);

    void Stop();
}
