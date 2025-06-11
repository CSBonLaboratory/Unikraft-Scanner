namespace UnikraftScanner.Client.Defects;
using UnikraftScanner.Client.Helpers;
public class BaseLineDefect
{
    public int LineNumber {get; set;}
    public string AbsSourcePath {get; set;}
    public string CompilationTag {get; set;}
    public AnalysisProvider Provider {get; set;}
}
