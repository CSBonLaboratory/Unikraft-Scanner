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
    [Trait("Category", "DiscoveryStage")]
    public void FindCompilationBlocks_AllTypes_NoComments_NoNested_OneLiners_NoPadding_NoElse()
    {
        /*
        Simple test
        We only remove multiple whitespaces and preserve only 1 between tokens and paranthesis
        */
        string sourceFileAbsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../../../Symbols/Discovery/inputs/simple.c");
        var actualResult = new SymbolEngine().DiscoverCompilationBlocksAndLines(
            sourceFileAbsPath,
            SymbolTestEnv.Opts,
            targetCompilationCommand: $"{SymbolTestEnv.Opts.CompilerPath} -I/usr/include -c {sourceFileAbsPath}"
            );

        if (!actualResult.IsSuccess)
        {
            Assert.Fail(((ErrorUnikraftScanner<string>)actualResult.Error).Data);
        }

        SymbolEngine.EngineDTO actual = actualResult.Value;

        var expected = new List<CompilationBlock>{
                new CompilationBlock(
                    type: CompilationBlockTypes.IF,
                    symbolCondition : "#if 1",
                    startLine: 8,
                    startLineEnd: 8,
                    endLine: 11,
                    blockCounter : 0,
                    parentCounter : -1,
                    lines: 2,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.IF,
                    symbolCondition : "#if !0",
                    startLine: 12,
                    startLineEnd: 12,
                    endLine: 18,
                    blockCounter: 1,
                    parentCounter: -1,
                    lines: 2,
                    children: null),

                // after constant we have some trailing whitespaces that must be removed (apply rules 2,3,6 from GetCondition)
                // ending line of the document is also the ending line of the last compilation block
                new CompilationBlock(
                    type: CompilationBlockTypes.IF,
                    symbolCondition: "#if 1",
                    startLine: 25,
                    startLineEnd: 25,
                    endLine: 47,
                    blockCounter: 2,
                    parentCounter: -1,
                    lines: 17,
                    children: null)
            };

        List<CompilationBlock> actualBlocks = actual.Blocks;

        List<List<int>> expectedCodeLinesInBlocks = [
            [9, 10],
            [13, 16],
            [27, 29, 30, 32, 33, 34, 35, 36, 37, 38, 40, 41, 42, 43, 44, 45, 46]
        ];

        List<List<int>> actualCodeLinesInBlocks = actual.debugLinesOfCodeBlocks;

        AssertSymbolEngine.TestCustomLists<CompilationBlock>(
            expected,
            actualBlocks,
            "Check compilation blocks:",
            expectedCodeLinesInBlocks,
            actualCodeLinesInBlocks
        );

        List<int> expectedUniversalLines = new() { 1, 2, 3, 4, 6, 20, 21, 23 };
        AssertSymbolEngine.TestUniversalLinesOfCode(
            expectedUniversalLines,
            actual.debugUniveralLinesOfCodeIdxs
        );

        AssertSymbolEngine.TestLinesOfCodeInBlocks(expectedCodeLinesInBlocks, actualCodeLinesInBlocks);

    }

}
