namespace UnikraftScanner.Tests;
using UnikraftScanner.Client.Symbols;
using System.IO;
using System.Reflection;
using UnikraftScanner.Client.Helpers;
using Xunit.Abstractions;

public class IncludeCheckMainFileTest : BaseSymbolTest
{
    public IncludeCheckMainFileTest(PrepSymbolTestEnv fix, ITestOutputHelper output) : base(fix){
        this.output = output;
    }
    private readonly ITestOutputHelper output;

    [Fact]
    public void FindCompilationBlocks_Comments_MultiLine_Directives_In_Code()
    {
        /*
        Conditional blocks, outside the source file passed inside the clang plugin command, must not be parsed
        */
        var actual = SymbolEngine.GetInstance().FindCompilationBlocksAndLines(
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../../../Symbols/inputs/include_check_mainFile.c"),
            opts: SymbolTestEnv.Opts,
            includesSubCommand: "-I/usr/include"
            );

        var expected = new List<CompilationBlock>{

                new CompilationBlock(
                    type: ConditionalBlockTypes.IFNDEF,
                    symbolCondition : "#ifndef A",
                    startLine: 6,
                    startLineEnd: 6,
                    fakeEndLine: 10,
                    endLine: 10,
                    blockCounter : 0,
                    parentCounter : -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    type: ConditionalBlockTypes.ELIF,
                    symbolCondition : "#elif !defined(B)",
                    startLine: 10,
                    startLineEnd: 10,
                    fakeEndLine: 13,
                    endLine: 13,
                    blockCounter : 1,
                    parentCounter : -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    type: ConditionalBlockTypes.ELIF,
                    symbolCondition : "#elif defined(B)",
                    startLine: 13,
                    startLineEnd: 13,
                    fakeEndLine: 16,
                    endLine: 16,
                    blockCounter : 2,
                    parentCounter : -1,
                    lines: 1,
                    children: null)
            };

        AssertSymbolEngine.TestSymbolEngineBlockResults(expected, actual.Blocks);

        AssertSymbolEngine.TestUniversalLinesOfCode(
            4,
            new() { 1, 3, 5, 18 },
            actual.UniversalLinesOfCode,
            actual.debugUniveralLinesOfCodeIdxs
        );
    }
}