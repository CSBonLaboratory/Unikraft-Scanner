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
    [Trait("Category", "DiscoveryStage")]
    public void FindCompilationBlocks_Comments_SingleLine_IncludeHeaderFileWithCompilationBlocks()
    {
        /*
        Compilation blocks, outside the source file passed inside the clang plugin command, must not be parsed
        */

        string sourceFileAbsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../../../Symbols/Discovery/inputs/include_check_mainFile.c");

        var actualResult = new SymbolEngine().DiscoverCompilationBlocksAndLines(
            sourceFileAbsPath,
            opts: SymbolTestEnv.Opts,
            targetCompilationCommand: $"{SymbolTestEnv.Opts.CompilerPath} -I/usr/include -c {sourceFileAbsPath}"
        );

        if (!actualResult.IsSuccess)
        {
            Assert.Fail(((ErrorUnikraftScanner<string>)actualResult.Error).Data);
        }

        SymbolEngine.EngineDTO actual = actualResult.Value;

        var expected = new List<CompilationBlock>{

                new CompilationBlock(
                    type: CompilationBlockTypes.IFNDEF,
                    symbolCondition : "#ifndef A",
                    startLine: 6,
                    startLineEnd: 6,
                    endLine: 10,
                    blockCounter : 0,
                    parentCounter : -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.ELIF,
                    symbolCondition : "#elif !defined(B)",
                    startLine: 10,
                    startLineEnd: 10,
                    endLine: 13,
                    blockCounter : 1,
                    parentCounter : -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.ELIF,
                    symbolCondition : "#elif defined(B)",
                    startLine: 13,
                    startLineEnd: 13,
                    endLine: 16,
                    blockCounter : 2,
                    parentCounter : -1,
                    lines: 1,
                    children: null)
            };

        List<CompilationBlock> actualBlocks = actual.Blocks;

        List<List<int>> expectedCodeLinesInBlocks = [
            [8],
            [11],
            [14]
        ];

        List<List<int>> actualCodeLinesInBlocks = actual.debugLinesOfCodeBlocks;

        List<int> expectedUniversalLines = new() { 1, 3, 5, 18 };

        AssertSymbolEngine.TestCustomLists<CompilationBlock>(
            expected,
            actualBlocks,
            "Check compilation blocks:",
            expectedCodeLinesInBlocks,
            actualCodeLinesInBlocks
        );

        AssertSymbolEngine.TestUniversalLinesOfCode(
            expectedUniversalLines,
            actual.debugUniveralLinesOfCodeIdxs
        );

        AssertSymbolEngine.TestLinesOfCodeInBlocks(expectedCodeLinesInBlocks, actualCodeLinesInBlocks);

    }
}