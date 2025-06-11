namespace UnikraftScanner.Tests;
using UnikraftScanner.Client.Symbols;
using System.IO;
using System.Reflection;
using UnikraftScanner.Client.Helpers;
using Xunit.Abstractions;

public class ExpandTest : BaseSymbolTest
{
    private readonly ITestOutputHelper output;

    public  ExpandTest(PrepSymbolTestEnv fix, ITestOutputHelper output) : base(fix){
        this.output = output;
    }

    [Fact]
    public void FindCompilationBlocks_Comments_MultiLine_Directives_In_Code()
    {
        /*
        Macro expansion must not be made in order to preserve the correct line and column location of the conditional blocks
        */

        var actual = SymbolEngine.GetInstance().FindCompilationBlocksAndLines(
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../../../Symbols/inputs/expansion.c"),
            opts: SymbolTestEnv.Opts
            );

        var expected = new List<CompilationBlock>{

                new CompilationBlock(
                    type: ConditionalBlockTypes.IFDEF,
                    symbolCondition : "#ifdef A",
                    startLine: 10,
                    startLineEnd: 10,
                    fakeEndLine: 12,
                    endLine: 12,
                    blockCounter : 0,
                    parentCounter : -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    type: ConditionalBlockTypes.ELSE,
                    symbolCondition : "#else",
                    startLine: 12,
                    startLineEnd: 12,
                    fakeEndLine: 14,
                    endLine: 14,
                    blockCounter : 1,
                    parentCounter : -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    type: ConditionalBlockTypes.IFNDEF,
                    symbolCondition : "#ifndef B",
                    startLine: 18,
                    startLineEnd: 18,
                    fakeEndLine: 21,
                    endLine: 21,
                    blockCounter : 2,
                    parentCounter : -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    type: ConditionalBlockTypes.ELIF,
                    symbolCondition : "#elif !defined(C)",
                    startLine: 21,
                    startLineEnd: 21,
                    fakeEndLine: 23,
                    endLine: 23,
                    blockCounter : 3,
                    parentCounter : -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    type: ConditionalBlockTypes.ELSE,
                    symbolCondition : "#else",
                    startLine: 23,
                    startLineEnd: 23,
                    fakeEndLine: 26,
                    endLine: 26,
                    blockCounter : 4,
                    parentCounter : -1,
                    lines: 1,
                    children: null)
            };

        AssertSymbolEngine.TestSymbolEngineBlockResults(expected, actual.Blocks);

        // even #define macros are considered lines of code
        AssertSymbolEngine.TestUniversalLinesOfCode(10, new() { 1, 2, 3, 4, 5, 6, 8, 9, 28, 32 }, actual.UniversalLinesOfCode, actual.debugUniveralLinesOfCodeIdxs);
    }
}