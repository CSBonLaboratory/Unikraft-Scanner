namespace UnikraftScanner.Tests;
using UnikraftScanner.Client.Symbols;
using System.IO;
using System.Reflection;
using UnikraftScanner.Client.Helpers;
using Xunit.Abstractions;
public class PaddingTest : BaseSymbolTest
{
    public PaddingTest(PrepSymbolTestEnv fix, ITestOutputHelper output) : base(fix){
        this.output = output;
    }
    private readonly ITestOutputHelper output;

    [Fact]
    public void FindCompilationBlocks_AllTypes_NoComments_NoNested_MultiLiners_MixPadding()
    {

        /*
            Preprocessor directives and code statements have mixed paddings and multiline mixed paddings
        */
        string sourceFileAbsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../../../Symbols/Discovery/inputs/padding_allignment.c");
        var actual = SymbolEngine.GetInstance().FindCompilationBlocksAndLines(
            sourceFileAbsPath,
            opts: SymbolTestEnv.Opts,
            targetCompilationCommand: $"{SymbolTestEnv.Opts.CompilerPath} -I/usr/include -c {sourceFileAbsPath} {DiscoveryStageCommandParser.additionalFlags}"
            );


        var expected = new List<CompilationBlock>{
            
                // block starts with a padding even though is not nested 
            new CompilationBlock(
                    type: CompilationBlockTypes.IFDEF,
                    symbolCondition: "#if defined(A)",
                    startLine: 5,
                    startLineEnd: 5,
                    endLine: 6,
                    blockCounter: 0,
                    parentCounter: -1,
                    lines: 0,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.IFDEF,
                    symbolCondition: "#ifdef CONFIG_LIB1",
                    startLine: 7,
                    startLineEnd: 7,
                    endLine: 13,
                    blockCounter: 1,
                    parentCounter: -1,
                    lines: 2,
                    children: null),

                    // has ending padding which must be considered
                    new CompilationBlock(
                    type: CompilationBlockTypes.ELIF,
                    symbolCondition: "#elif defined(CONFIG_LIB2)",
                    startLine: 13,
                    startLineEnd: 13,
                    endLine: 16,
                    blockCounter: 2,
                    parentCounter: -1,
                    lines: 1,
                    children: null),

                    new CompilationBlock(
                    type: CompilationBlockTypes.ELSE,
                    symbolCondition: "#else",
                    startLine: 16,
                    startLineEnd: 16,
                    endLine: 19,
                    blockCounter: 3,
                    parentCounter: -1,
                    lines: 1,
                    children: null)
        };

        List<CompilationBlock> actualBlocks = actual.Blocks;
        AssertSymbolEngine.TestCustomLists<CompilationBlock>(expected, actualBlocks, "Check compilation blocks:");

        List<int> expectedUniversalLines = new() { 2, 4, 20 };
        AssertSymbolEngine.TestUniversalLinesOfCode(
            expectedUniversalLines.Count,
            expectedUniversalLines,
            actual.UniversalLinesOfCode,
            actual.debugUniveralLinesOfCodeIdxs
        );

        List<List<int>> expectedCodeLinesInBlocks = [
            [],
            [9, 11],
            [14],
            [17]
        ];

        List<List<int>> actualCodeLinesInBlocks = actual.debugLinesOfCodeBlocks;

        AssertSymbolEngine.TestLinesOfCodeInBlocks(expectedCodeLinesInBlocks, actualCodeLinesInBlocks);
    }
}
