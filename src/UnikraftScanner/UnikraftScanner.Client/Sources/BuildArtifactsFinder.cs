using UnikraftScanner.Client.Helpers;

namespace UnikraftScanner.Client.Sources;

public class BuildArtifactsFinder : ISourceFinder
{
    public string AppPath { get; init; }
    public ResultUnikraftScanner<string[]> FindSources()
    {
        return null;
    }
}
