namespace UnikraftScanner.Client.Helpers;
using UnikraftScanner.Client.Defects;
public class CompilationBlock
{
    public string? SymbolCondition {get; set;}
    public List<string>? TriggeredCompilations {get; set;}
    public int StartLine {get; set;}
    public int StartLineEnd {get; set;}  // some compilation blocks may have a multi line condition
    public int EndLine {get; set;}
    public int BlockCounter {get; set;}
    public int ParentCounter {get; set;}
    public int Lines {get; set;}
    public List<int>? Children {get; set;}
    public List<BaseLineDefect>? Defects {get;set;}

    public CompilationBlock(string symbolCondition, int startLine, int startLineEnd, int endLine, int blockCounter, int parentCounter, int lines, List<int> children){
        /*
        Used when finishing parsing the entire compilation block from start to finish, this is used in SymbolEngine.cs
        Other things like defects and triggered compilations are found later
        */
        SymbolCondition = symbolCondition;
        StartLine = startLine;
        StartLineEnd = startLineEnd;
        EndLine = endLine;
        BlockCounter = blockCounter;
        ParentCounter = parentCounter;
        Lines = lines;
        Children = children;
        Defects = null;
        TriggeredCompilations = null;
    }

}
