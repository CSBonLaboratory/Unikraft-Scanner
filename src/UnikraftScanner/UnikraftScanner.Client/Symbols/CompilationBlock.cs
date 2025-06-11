namespace UnikraftScanner.Client.Helpers;

using System.Text;
using UnikraftScanner.Client.Defects;
using UnikraftScanner.Client.Symbols;

public class CompilationBlock : IEquatable<CompilationBlock>
{
    public string SymbolCondition {get; set;}
    
    public int StartLine {get; set;}
    public int StartLineEnd {get; set;}  // some compilation blocks may have a multi line condition
    public int EndLine {get; set;}

    // used for compilation blocks that have #endif directive multiline (split using \ on multiple lines)
    /* 
        1:#en\
        2:d\
        3:i\
        4:f\
    */
    // this means that FakeEndLine will be on line 1 and EndLine will be on 4
    public int FakeEndLine { get; set; } 
    public int BlockCounter { get; set; }
    public int ParentCounter {get; set;}

    public ConditionalBlockTypes CBType { get; set; }

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
        ConditionalBlockTypes type,
        string condition,
        int startLine,
        int blockCounter,
        int startLineEnd,
        int fakeEndLine,
        int endLine,
        int parentCounter) : this(condition, startLine, blockCounter){

        CBType = type;
        StartLineEnd = startLineEnd;
        FakeEndLine = fakeEndLine;
        EndLine = endLine;
        ParentCounter = parentCounter;
    } 

    public CompilationBlock(
        ConditionalBlockTypes type,
        string symbolCondition,
        int startLine,
        int startLineEnd,
        int fakeEndLine,
        int endLine,
        int blockCounter,
        int parentCounter,
        int lines,
        List<int>? children) : this(symbolCondition, startLine, blockCounter){
        
        CBType = type;
        StartLineEnd = startLineEnd;
        FakeEndLine = fakeEndLine;
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
            FakeEndLine: {FakeEndLine},
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

    public bool Equals(CompilationBlock? b)
    {

        if (
            this.StartLine == b.StartLine &&
            this.SymbolCondition.Equals(b.SymbolCondition) &&
            this.StartLineEnd == b.StartLineEnd &&
            this.FakeEndLine == b.FakeEndLine &&
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
