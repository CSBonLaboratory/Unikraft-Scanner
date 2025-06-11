namespace UnikraftScanner.Tests;
using UnikraftScanner.Client.Symbols;
using System.IO;
using System.Reflection;
using UnikraftScanner.Client.Helpers;
using Xunit.Abstractions;
using Xunit.Sdk;
using System.Text;

public class MultiLineTest : BaseSymbolTest
{
    public MultiLineTest(PrepSymbolTestEnv fix, ITestOutputHelper output) : base(fix){
        this.output = output;
    }
    private readonly ITestOutputHelper output;

    [Fact]
    public void FindCompilationBlocks_Multiline_Directives_Paranthesis_MultiSpace_And_Code()
    {
        /*
        Code is also multiline, every incomplete multiline is counted as a compiled line

        Multiline symbol condition with multiple \ lines and lines with symbols and \
        Symbol condition may have paranthesis on multiple lines and tokens inside the condition may have multiple whitespace characters

        We only remove multiple whitespaces and preserve only 1 between tokens and paranthesis during parsing of the conditional statement

        Redundant multiline characters after a symbol condition are counted as code lines 
        */
        SymbolEngine.EngineResult actual = SymbolEngine.GetInstance().FindCompilationBlocksAndLines(
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../../../Symbols/inputs/multi_line.c"),
            opts: SymbolTestEnv.Opts
            );


        List<CompilationBlock> expected = new List<CompilationBlock>{

                new CompilationBlock(
                    type: ConditionalBlockTypes.IF,
                    symbolCondition : "#if defined(C) && defined(D)",
                    startLine: 2,
                    startLineEnd: 7,
                    fakeEndLine: 13,
                    endLine: 13,
                    blockCounter : 0,
                    parentCounter : -1,
                    lines: 4,
                    children: null),

                // multiline operator does not produce a whitespace, so the first && is glued to the paranthesis some for third || operator
                new CompilationBlock(
                    type: ConditionalBlockTypes.ELIF,
                    symbolCondition : "#elif (defined(X) || defined(B) )&& (defined(X)) && ( defined(Y) || defined(A))|| ( defined(Y) )",
                    startLine: 13,
                    startLineEnd: 18,
                    fakeEndLine: 21,
                    endLine: 21,
                    blockCounter : 1,
                    parentCounter : -1,
                    lines: 2,
                    children: null),

                // even redundant multiline characters after a symbol condition are counted as code lines
                // start end line is not where the last redundant multiline character is (we respect compiler decision here, no need to refactor)
                new CompilationBlock(
                    type: ConditionalBlockTypes.ELIF,
                    symbolCondition : "#elif defined(A)",
                    startLine: 21,
                    startLineEnd: 21,
                    fakeEndLine: 27,
                    endLine: 27,
                    blockCounter : 2,
                    parentCounter : -1,
                    lines: 4,
                    children: null),

                // the source code has multiple whitespaces at the end of the condition that must be removed
                new CompilationBlock(
                    type: ConditionalBlockTypes.IFDEF,
                    symbolCondition : "#ifdef C",
                    startLine: 33,
                    startLineEnd: 33,
                    fakeEndLine: 35,
                    endLine: 35,
                    blockCounter : 3,
                    parentCounter : -1,
                    lines: 1,
                    children: null),

                // #if token is split on multi lines
                new CompilationBlock(
                    type: ConditionalBlockTypes.IF,
                    symbolCondition : "#if defined(A)",
                    startLine: 37,
                    startLineEnd: 41,
                    fakeEndLine: 46,
                    endLine: 46,
                    blockCounter : 4,
                    parentCounter : -1,
                    lines: 2,
                    children: null),


                // #elif token is split on multi lines
                // a PREPROCESS SYMBOL is also split on multi lines
                // a PREPROCESS operator (||) is also split on multi lines
                new CompilationBlock(
                    type: ConditionalBlockTypes.ELIF,
                    symbolCondition : "#elif defined(SAHUR) || defined(ABCDE)",
                    startLine: 46,
                    startLineEnd: 49,
                    fakeEndLine: 51,
                    endLine: 51,
                    blockCounter : 5,
                    parentCounter : -1,
                    lines: 1,
                    children: null),


                // #else token is split on multi lines 
                // #else also has a line with \ and then whitespaces that must be eliminated
                // #endif is split on multi lines, so fakeEndLine is important here !
                // immediately after endif we have an ending }
                new CompilationBlock(
                    type: ConditionalBlockTypes.ELSE,
                    symbolCondition : "#else",
                    startLine: 51,
                    startLineEnd: 53,
                    fakeEndLine: 55,
                    endLine: 58,
                    blockCounter : 6,
                    parentCounter : -1,
                    lines: 1,
                    children: null)
        };


        AssertSymbolEngine.TestSymbolEngineBlockResults(expected, actual.Blocks);

        AssertSymbolEngine.TestUniversalLinesOfCode(4, new(){ 29, 30, 31, 59}, actual.UniversalLinesOfCode, actual.debugUniveralLinesOfCodeIdxs);
    }

}
