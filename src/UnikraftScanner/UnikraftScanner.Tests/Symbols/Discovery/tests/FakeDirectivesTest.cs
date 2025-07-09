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
    public void FindCompilationBlocks_Comments_MultiLine_Directives_In_Code()
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
        var actual = SymbolEngine.GetInstance().FindCompilationBlocksAndLines(
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../../../Symbols/inputs/fake_directives.c"),
            opts: SymbolTestEnv.Opts,
            includesSubCommand: "-I/usr/include"
            );

        var expected = new List<CompilationBlock>{

                new CompilationBlock(
                    type: ConditionalBlockTypes.IFDEF,
                    symbolCondition : "#ifdef B",
                    startLine: 16,
                    startLineEnd: 16,
                    fakeEndLine: 21,
                    endLine: 21,
                    blockCounter : 0,
                    parentCounter : -1,
                    lines: 3,
                    children: null),


                new CompilationBlock(
                    type: ConditionalBlockTypes.IF,
                    symbolCondition : "#if !defined(A)",
                    startLine: 75,
                    startLineEnd: 75,
                    fakeEndLine: 85,
                    endLine: 85,
                    blockCounter : 1,
                    parentCounter : -1,
                    lines: 4,
                    children: null)
            };

        AssertSymbolEngine.TestSymbolEngineBlockResults(expected, actual.Blocks);
        
        // comments should have been eliminated before parsing the source file for compilation blocks and lines of code counting
        AssertSymbolEngine.TestUniversalLinesOfCode(
            36,
            new() { 2, 3, 14, 15, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 41, 42, 43, 47, 48, 49, 50, 51, 53, 54, 55, 57, 68, 70, 71, 72, 73, 87 },
            actual.UniversalLinesOfCode,
            actual.debugUniveralLinesOfCodeIdxs
        );
    }
}
