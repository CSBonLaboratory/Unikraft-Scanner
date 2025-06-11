namespace UnikraftScanner.Tests;
using UnikraftScanner.Client.Symbols;
using System.IO;
using System.Reflection;
using UnikraftScanner.Client.Helpers;
using Xunit.Abstractions;


public class SimpleTest : BaseSymbolTest
{

    public SimpleTest(PrepSymbolTestEnv fix, ITestOutputHelper output) : base(fix){
        this.output = output;
    }
    private readonly ITestOutputHelper output;
    [Fact]
    public void FindCompilationBlocks_AllTypes_NoComments_NoNested_OneLiners_NoPadding_NoElse()
    {
        /*
        Simple test
        We only remove multiple whitespaces and preserve only 1 between tokens and paranthesis
        */
        var actual = SymbolEngine.GetInstance().FindCompilationBlocksAndLines(
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../../../Symbols/inputs/simple.c"),
            SymbolTestEnv.Opts,
            includesSubCommand: "-I/usr/include"
            );


        var expected = new List<CompilationBlock>{
                new CompilationBlock(
                    type: ConditionalBlockTypes.IF,
                    symbolCondition : "#if 1",
                    startLine: 8,
                    startLineEnd: 8,
                    fakeEndLine: 11,
                    endLine: 11,
                    blockCounter : 0,
                    parentCounter : -1,
                    lines: 2,
                    children: null),

                new CompilationBlock(
                    type: ConditionalBlockTypes.IF,
                    symbolCondition : "#if !0",
                    startLine: 12,
                    startLineEnd: 12,
                    fakeEndLine: 18,
                    endLine: 18,
                    blockCounter: 1,
                    parentCounter: -1,
                    lines: 2,
                    children: null),

                // after constant we have some trailing whitespaces that must be removed (apply rules 2,3,6 from GetCondition)
                // ending line of the document is also the ending line of the last compilation block
                new CompilationBlock(
                    type: ConditionalBlockTypes.IF,
                    symbolCondition: "#if 1",
                    startLine: 25,
                    startLineEnd: 25,
                    fakeEndLine: 47,
                    endLine: 47,
                    blockCounter: 2,
                    parentCounter: -1,
                    lines: 17,
                    children: null)
            };

        AssertSymbolEngine.TestSymbolEngineBlockResults(expected, actual.Blocks);

        AssertSymbolEngine.TestUniversalLinesOfCode(
            8,
            new() { 1, 2, 3, 4, 6, 20, 21, 23 },
            actual.UniversalLinesOfCode,
            actual.debugUniveralLinesOfCodeIdxs
        );
    }

}
