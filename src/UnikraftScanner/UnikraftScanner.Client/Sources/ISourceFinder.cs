using UnikraftScanner.Client.Helpers;

namespace UnikraftScanner.Client.Sources;

public interface ISourceFinder
{
    public ResultUnikraftScanner<List<string>> FindSources();
}
