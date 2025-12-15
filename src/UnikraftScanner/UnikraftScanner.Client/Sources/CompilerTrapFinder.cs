using UnikraftScanner.Client.Helpers;

namespace UnikraftScanner.Client.Sources;

public class CompilerTrapFinder : ISourceFinder
{
    public string AppPath { get; set; }
    public string TrapCompilerPath { get; set; }
    public string ResultsFilePath { get; set; }
    public CompilerTrapFinder(string trapCompilerPath, string appPath, string resultsFilePath)
    {
        TrapCompilerPath = trapCompilerPath;
        AppPath = appPath;
        ResultsFilePath = resultsFilePath;
    }

    public virtual ResultUnikraftScanner<List<string>> FindSources()
    {

        return null;
    }
}
