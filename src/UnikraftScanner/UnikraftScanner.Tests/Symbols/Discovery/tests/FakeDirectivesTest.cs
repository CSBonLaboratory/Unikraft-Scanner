namespace UnikraftScanner.Tests;
using UnikraftScanner.Client.Symbols;
using System.IO;
using System.Reflection;
using UnikraftScanner.Client.Helpers;
using Xunit.Abstractions;

public class FakeDirectivesTest : BaseSymbolTest
{
    public FakeDirectivesTest(PrepSymbolTestEnv fix, ITestOutputHelper output) : base(fix){
        this.output = output;
    }
    private readonly ITestOutputHelper output;

    [Fact]
    [Trait("Category", "DiscoveryStage")]
    public void FindCompilationBlocks_Comments_CodeMultiLine_DirectivesAsStringsAndVariables()
    {
        /*
        Code is also multiline, every incomplete multiline is counted a compiled line.

        Code includes directive tokens as strings, which must not be considered preprocessor directives .

        Comments containing names resembling preprocessor directives are also here to play tricks. Should be ignored.

        Code also contains variables with names identical to preprocessor directives. Should be ignored.

        Multiline symbol condition with multiple \ lines and lines with symbols and \
        Symbol condition may have paranthesis on multiple lines and tokens inside the condition may have multiple whitespace characters

        We only remove multiple whitespaces and preserve only 1 between tokens and paranthesis.

        Second compilation block used to force parsing of source lines that contain a struct with a special token name.
        It also tests how lines of code are counted. It contains multi code line with a line that only contains the backslash char
        */

        string sourceFileAbsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../../../Symbols/Discovery/inputs/fake_directives.c");

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
                    symbolCondition : "#ifdef B",
                    startLine: 16,
                    startLineEnd: 16,
                    endLine: 21,
                    blockCounter : 0,
                    parentCounter : -1,
                    lines: 3,
                    children: null),


                new CompilationBlock(
                    type: CompilationBlockTypes.IF,
                    symbolCondition : "#if !defined(A)",
                    startLine: 75,
                    startLineEnd: 75,
                    endLine: 85,
                    blockCounter : 1,
                    parentCounter : -1,
                    lines: 4,
                    children: null)
        ];

        List<CompilationBlock> actualBlocks = actual.Blocks;

        List<List<int>> expectedCodeLinesInBlocks = [
            [17, 18, 19],
            [81, 82, 83, 84]
        ];

        List<List<int>> actualCodeLinesInBlocks = actual.debugLinesOfCodeBlocks;

        // comments should have been eliminated before parsing the source file for compilation blocks and lines of code counting
        List<int> expectedUniversalLines = new() { 2, 3, 14, 15, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 41, 42, 43, 47, 48, 49, 50, 51, 53, 54, 55, 57, 68, 70, 71, 72, 73, 87 };

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
