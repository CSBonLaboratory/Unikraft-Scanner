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

        var actual = SymbolEngine.GetInstance().FindCompilationBlocksAndLines(
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../../../Symbols/inputs/padding_allignment.c"),
            opts: SymbolTestEnv.Opts
            );


        var expected = new List<CompilationBlock>{
            
                // block starts with a padding even though is not nested 
            new CompilationBlock(
                    type: ConditionalBlockTypes.IFDEF,
                    symbolCondition: "#if defined(A)",
                    startLine: 5,
                    startLineEnd: 5,
                    fakeEndLine: 6,
                    endLine: 6,
                    blockCounter: 0,
                    parentCounter: -1,
                    lines: 0,
                    children: null),

                new CompilationBlock(
                    type: ConditionalBlockTypes.IFDEF,
                    symbolCondition: "#ifdef CONFIG_LIB1",
                    startLine: 7,
                    startLineEnd: 7,
                    fakeEndLine: 13,
                    endLine: 13,
                    blockCounter: 1,
                    parentCounter: -1,
                    lines: 2,
                    children: null),

                    // has ending padding which must be considered
                    new CompilationBlock(
                    type: ConditionalBlockTypes.ELIF,
                    symbolCondition: "#elif defined(CONFIG_LIB2)",
                    startLine: 13,
                    startLineEnd: 13,
                    fakeEndLine: 16,
                    endLine: 16,
                    blockCounter: 2,
                    parentCounter: -1,
                    lines: 1,
                    children: null),

                    new CompilationBlock(
                    type: ConditionalBlockTypes.ELSE,
                    symbolCondition: "#else",
                    startLine: 16,
                    startLineEnd: 16,
                    fakeEndLine: 19,
                    endLine: 19,
                    blockCounter: 3,
                    parentCounter: -1,
                    lines: 1,
                    children: null)
        };

        AssertSymbolEngine.TestSymbolEngineBlockResults(expected, actual.Blocks);

        AssertSymbolEngine.TestUniversalLinesOfCode(
            3,
            new() { 2, 4, 20},
            actual.UniversalLinesOfCode,
            actual.debugUniveralLinesOfCodeIdxs
        );
    }
}
