namespace UnikraftScanner.Client.Helpers;
using UnikraftScanner.Client.Defects;
using UnikraftScanner.Client.Symbols;

public class CompilationBlock : IEquatable<CompilationBlock>
{
    public string SymbolCondition {get; set;}
    
    public int StartLine {get; set;}
    public int StartLineEnd {get; set;}  // some compilation blocks may have a multi line condition
    public int EndLine {get; set;} 
    public int BlockCounter { get; set; }
    public int ParentCounter {get; set;}

    public CompilationBlockTypes CBType { get; set; }

    // lines of code that will be compiled
    public int Lines {get; set;}
    public List<int>? Children {get; set;}
    public List<BaseLineDefect>? Defects {get;set;}
    public List<string>? TriggeredCompilations {get; set;}

    public CompilationBlock(string condition, int startLine, int blockCounter){
        
        SymbolCondition = condition;
        StartLine = startLine;
        StartLineEnd = -1;
        EndLine = -1;
        BlockCounter = blockCounter;
        ParentCounter = -1;
        Lines = 0;
        Children = null;
        Defects = null;
        TriggeredCompilations = null;
    }

    public CompilationBlock(
        CompilationBlockTypes type,
        string condition,
        int startLine,
        int blockCounter,
        int startLineEnd,
        int endLine,
        int parentCounter) : this(condition, startLine, blockCounter){

        CBType = type;
        StartLineEnd = startLineEnd;
        
        EndLine = endLine;
        ParentCounter = parentCounter;
    } 

    public CompilationBlock(
        CompilationBlockTypes type,
        string symbolCondition,
        int startLine,
        int startLineEnd,
        int endLine,
        int blockCounter,
        int parentCounter,
        int lines,
        List<int>? children) : this(symbolCondition, startLine, blockCounter){
        
        CBType = type;
        StartLineEnd = startLineEnd;
        EndLine = endLine;
        BlockCounter = blockCounter;
        ParentCounter = parentCounter;
        Lines = lines;
        Children = children;

    }

    public override string ToString(){
        string res = @$"CompilationBlock: 
            TYPE: {CBType},
            Condition: {SymbolCondition}, 
            StartLine: {StartLine},
            StartLineEnd: {StartLineEnd},
            EndLine: {EndLine},
            BlockCounter: {BlockCounter},
            ParentCounter: {ParentCounter},
            LoC: {Lines},
            Children: ";

        if(Children != null)
            foreach(int c in Children){
                res += c.ToString();
                res += " ";
            }
        return res;
    }

    // very important when comparing generic lists like in PrepareSymbolMain.cs TestCustomLists<CompilationBlocks>
    // https://stackoverflow.com/questions/36235271/c-sharp-using-equals-method-in-a-generic-list-fails
    public override bool Equals(object? obj) => Equals(obj as CompilationBlock);

    public bool Equals(CompilationBlock? b)
    {
        if (
            this.StartLine == b.StartLine &&
            this.SymbolCondition.Equals(b.SymbolCondition) &&
            this.StartLineEnd == b.StartLineEnd &&
            this.EndLine == b.EndLine &&
            this.BlockCounter == b.BlockCounter &&
            this.Lines == b.Lines
        )
        {

            if (this.Children == null && b.Children == null)
                return true;

            else if (this.Children != null && b.Children != null)
                return Enumerable.SequenceEqual(this.Children, b.Children);

            return false;

        }

        return false;
    }

}
