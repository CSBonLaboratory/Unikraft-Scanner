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
    [Trait("Category", "DiscoveryStage")]
    public void FindCompilationBlocks_MacroExpansion_SingleLineCondition_UniversalLinesGaps()
    {
        /*
        Macro expansion must not be made in order to preserve the correct line and column location of the Compilation blocks
        */

        string sourceFileAbsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../../../Symbols/Discovery/inputs/expansion.c");

        var actualResult = new SymbolEngine().DiscoverCompilationBlocksAndLines(
            sourceFileAbsPath,
            opts: SymbolTestEnv.Opts,
            targetCompilationCommand: $"{SymbolTestEnv.Opts.CompilerPath} -I/usr/include -c {sourceFileAbsPath}"
        );

        if (!actualResult.IsSuccess)
        {
            Assert.Fail(
                $"Test failed with custom error: {actualResult.Error}"
            );
        }

        SymbolEngine.EngineDTO actual = actualResult.Value;

        List<CompilationBlock> expected = [

                new CompilationBlock(
                    type: CompilationBlockTypes.IFDEF,
                    symbolCondition : "#ifdef A",
                    startLine: 10,
                    startLineEnd: 10,
                    endLine: 12,
                    blockCounter : 0,
                    parentCounter : -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.ELSE,
                    symbolCondition : "#else",
                    startLine: 12,
                    startLineEnd: 12,
                    endLine: 14,
                    blockCounter : 1,
                    parentCounter : -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.IFNDEF,
                    symbolCondition : "#ifndef B",
                    startLine: 18,
                    startLineEnd: 18,
                    endLine: 21,
                    blockCounter : 2,
                    parentCounter : -1,
                    lines: 2,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.ELIF,
                    symbolCondition : "#elif !defined(C)",
                    startLine: 21,
                    startLineEnd: 21,
                    endLine: 23,
                    blockCounter : 3,
                    parentCounter : -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.ELSE,
                    symbolCondition : "#else",
                    startLine: 23,
                    startLineEnd: 23,
                    endLine: 26,
                    blockCounter : 4,
                    parentCounter : -1,
                    lines: 1,
                    children: null)
        ];

        List<CompilationBlock> actualBlocks = actual.Blocks;

        List<List<int>> expectedCodeLinesInBlocks = [

            [11],
            [13],
            [19, 20],
            [22],
            [25],
        ];

        List<List<int>> actualCodeLinesInBlocks = actual.debugLinesOfCodeBlocks;

        // even #define macros (other directives besides #ifdef, #if, #ifndef, #else, #elif, #endif) and their contents are considered lines of code
        List<int> expectedUniversalLines = new() { 1, 2, 3, 4, 5, 6, 8, 9, 15, 16, 28, 32 };

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