namespace UnikraftScanner.Tests.Symbols;
using UnikraftScanner.Client.Symbols;
using System.IO;
using System.Reflection;
using UnikraftScanner.Client.Helpers;
using Xunit.Abstractions;
public class ShawarmaTest : BaseSymbolTest
{
    public ShawarmaTest(PrepSymbolTestEnv fix, ITestOutputHelper output) : base(fix){
        this.output = output;
    }
    private readonly ITestOutputHelper output;
    [Fact]
    public void FindCompilationBlocks_AllTypes_WithComments_Nested_Multiline_MixPadding()
    {

        /*
            All tests combined. With everything like a shawarma :)

            Any non-whitespace line is considered to be a code line and increment the compiled lines number (even #include or #define directives or macro calls etc.)

            Comments on the same line as code statements
        */
        var actual = SymbolEngine
        .GetInstance()
        .FindCompilationBlocksAndLines(
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../../../Symbols/inputs/shawarma.c"),
            SymbolTestEnv.Opts
            );


        var expected = new List<CompilationBlock>{
                new CompilationBlock(
                    type: ConditionalBlockTypes.IF,
                    symbolCondition: "#if defined(CONFIG_LIB1)",
                    startLine: 9,
                    startLineEnd: 9,
                    fakeEndLine: 17,
                    endLine: 17,
                    blockCounter: 0,
                    parentCounter: -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    type: ConditionalBlockTypes.ELIF,
                    symbolCondition: "#elif defined(CONFIG_LIB2)",
                    startLine: 17,
                    startLineEnd: 17,
                    fakeEndLine: 44,
                    endLine: 44,
                    blockCounter: 1,
                    parentCounter: -1,
                    lines: 1,
                    children: new List<int>{2, 3, 6}),

                new CompilationBlock(
                    type: ConditionalBlockTypes.IFNDEF,
                    symbolCondition: "#ifndef CONFIG_LIB3",
                    startLine: 20,
                    startLineEnd: 20,
                    fakeEndLine: 24,
                    endLine: 24,
                    blockCounter: 2,
                    parentCounter: 1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    type: ConditionalBlockTypes.ELIF,
                    symbolCondition: "#elif defined(CONFIG_LIB4)",
                    startLine: 24,
                    startLineEnd: 24,
                    fakeEndLine: 41,
                    endLine: 41,
                    blockCounter: 3,
                    parentCounter: 1,
                    lines: 3,
                    children: new List<int>{4}),

                new CompilationBlock(
                    type: ConditionalBlockTypes.IFDEF,
                    symbolCondition: "#ifdef CONFIG_LIB4",
                    startLine: 27,
                    startLineEnd: 29,
                    fakeEndLine: 35,
                    endLine: 35,
                    blockCounter: 4,
                    parentCounter: 3,
                    lines: 1,
                    children: new List<int>{5}),

                 new CompilationBlock(
                    type: ConditionalBlockTypes.IFNDEF,
                    symbolCondition: "#ifndef CONFIG_LIB5",
                    startLine: 32,
                    startLineEnd: 32,
                    fakeEndLine: 34,
                    endLine: 34,
                    blockCounter: 5,
                    parentCounter: 4,
                    lines: 1,
                    children: new List<int>{5}),

                new CompilationBlock(
                    type: ConditionalBlockTypes.ELSE,
                    symbolCondition: "#else",
                    startLine: 41,
                    startLineEnd: 41,
                    fakeEndLine: 46,
                    endLine: 46,
                    blockCounter: 6,
                    parentCounter: 1,
                    lines: 1,
                    children: new List<int>{7}),

                new CompilationBlock(
                    type: ConditionalBlockTypes.IFDEF,
                    symbolCondition: "#ifdef CONFIG_LIB6",
                    startLine: 42,
                    startLineEnd: 42,
                    fakeEndLine: 44,
                    endLine: 44,
                    blockCounter: 7,
                    parentCounter: 6,
                    lines: 0,
                    children: null),

                new CompilationBlock(
                    type: ConditionalBlockTypes.ELIF,
                    symbolCondition: "#elif defined(CONFIG_LIB7)",
                    startLine: 47,
                    startLineEnd: 47,
                    fakeEndLine: 58,
                    endLine: 58,
                    blockCounter: 8,
                    parentCounter: -1,
                    lines: 0,
                    children: new List<int>{9}),

                new CompilationBlock(
                    type: ConditionalBlockTypes.IFDEF,
                    symbolCondition: "#ifdef defined(CONFIG_LIB8)",
                    startLine: 48,
                    startLineEnd: 48,
                    fakeEndLine: 57,
                    endLine: 57,
                    blockCounter: 9,
                    parentCounter: 8,
                    lines: 1,
                    children: null),

            };
            
        AssertSymbolEngine.TestSymbolEngineBlockResults(expected, actual.Blocks);
    }
}
