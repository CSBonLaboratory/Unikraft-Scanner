namespace UnikraftScanner.Tests.Symbols;
using UnikraftScanner.Client.Symbols;
using System.IO;
using System.Reflection;
using UnikraftScanner.Client.Helpers;
using Xunit.Abstractions;

[Collection(nameof(SymbolEngineTestParallel))]
public class ShawarmaTest
{
    private PrepSymbolTestEnvFixture SymbolTestEnv { get; set; }
    private readonly ITestOutputHelper output;
    public ShawarmaTest(PrepSymbolTestEnvFixture fix, ITestOutputHelper output)
    {
        this.output = output;
        SymbolTestEnv = fix;
    }

    [Fact]
    [Trait("Category", "DiscoveryStage")]
    public void FindCompilationBlocks_Final_AllTypes_WithComments_Nested_Multiline_MixPadding()
    {
        /*
            All tests combined. With everything like a shawarma :)

            Includes wrong non-existent header files.

            Any non-whitespace line is considered to be a code line and increment the compiled lines number (even #include or #define directives or macro calls etc.)

            Comments on the same line as code statements
        */
        string sourceFileAbsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../../../Symbols/Discovery/inputs/shawarma.c");
        var actualResult = new SymbolEngine()
        .DiscoverCompilationBlocksAndLines(
            sourceFileAbsPath,
            SymbolTestEnv.Opts,
            targetCompilationCommand: $"{SymbolTestEnv.Opts.CompilerPath} -I/usr/include -c {sourceFileAbsPath}"
            );

        if (!actualResult.IsSuccess)
        {
            Assert.Fail(
                $"Test failed with custom error: {actualResult.Error}"
            );
        }

        SymbolEngine.DiscoveryResDTO actual = actualResult.Value;

        var expected = new List<CompilationBlock>{
                new CompilationBlock(
                    type: CompilationBlockTypes.IF,
                    symbolCondition: "#if defined(A)",
                    startLine: 16,
                    startLineEnd: 16,
                    endLine: 73,
                    blockCounter: 0,
                    parentCounter: -1,
                    lines: 1,
                    children: [1, 2]),

                new CompilationBlock(
                    type: CompilationBlockTypes.IFNDEF,
                    symbolCondition: "#ifndef ABCDEFGHIJK",
                    startLine: 18,
                    startLineEnd: 19,
                    endLine: 26,
                    blockCounter: 1,
                    parentCounter: 0,
                    lines: 4,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.ELSE,
                    symbolCondition: "#else",
                    startLine: 26,
                    startLineEnd: 26,
                    endLine: 70,
                    blockCounter: 2,
                    parentCounter: 0,
                    lines: 0,
                    children: [3, 4, 9]),

                new CompilationBlock(
                    type: CompilationBlockTypes.IF,
                    symbolCondition: "#if defined(A) && defined(B) && (defined(C) && defined(D))",
                    startLine: 27,
                    startLineEnd: 31,
                    endLine: 39,
                    blockCounter: 3,
                    parentCounter: 2,
                    lines: 3,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.ELIF,
                    symbolCondition: "#elif defined(E)",
                    startLine: 39,
                    startLineEnd: 39,
                    endLine: 68,
                    blockCounter: 4,
                    parentCounter: 2,
                    lines: 6,
                    children: [5, 6, 7, 8]),

                new CompilationBlock(
                    type: CompilationBlockTypes.IF,
                    symbolCondition: "#if defined(X) || defined(Y)",
                    startLine: 43,
                    startLineEnd: 43,
                    endLine: 46,
                    blockCounter: 5,
                    parentCounter: 4,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.IF,
                    symbolCondition: "#if defined(X)",
                    startLine: 50,
                    startLineEnd: 50,
                    endLine: 52,
                    blockCounter: 6,
                    parentCounter: 4,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.IF,
                    symbolCondition: "#elif defined(Y)",
                    startLine: 52,
                    startLineEnd: 52,
                    endLine: 57,
                    blockCounter: 7,
                    parentCounter: 4,
                    lines: 3,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.IFNDEF,
                    symbolCondition: "#ifndef X",
                    startLine: 61,
                    startLineEnd: 61,
                    endLine: 64,
                    blockCounter: 8,
                    parentCounter: 4,
                    lines: 2,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.ELIF,
                    symbolCondition: "#elif defined(F)",
                    startLine: 68,
                    startLineEnd: 68,
                    endLine: 69,
                    blockCounter: 9,
                    parentCounter: 3,
                    lines: 0,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.ELSE,
                    symbolCondition: "#else",
                    startLine: 73,
                    startLineEnd: 73,
                    endLine: 101,
                    blockCounter: 10,
                    parentCounter: -1,
                    lines: 1,
                    children: [11]),

                new CompilationBlock(
                    type: CompilationBlockTypes.IFNDEF,
                    symbolCondition: "#ifndef ABCDEFGHIJK",
                    startLine: 92,
                    startLineEnd: 96,
                    endLine: 100,
                    blockCounter: 11,
                    parentCounter: 10,
                    lines: 1,
                    children: null),


            };

        List<List<int>> expectedCodeLinesInBlocks = [
            [72], // #if defined(A)
            [20, 22, 23, 25], // #ifndef ABCDEFGHIJK
            [], // #else
            [33, 35, 37], // #if defined(A) && defined(B) && (defined(C) && defined(D))
            [42, 47, 48, 49, 59, 66], // #elif definded(E)
            [45],  // #if defined(X) || defined(Y)
            [51], // #if defined(X)
            [53, 54, 55], // #elif defined(Y)
            [62, 63], // #ifndef X
            [], // #elif defined(F)
            [77], // #else
            [99] // #ifndef ABCDEFGHIJK
        ];

        List<int> expectedUniversalLines = new() { 1, 2, 11, 12, 13, 104 };

        List<List<int>> actualCodeLinesInBlocks = actual.debugLinesOfCodeBlocks;
        List<CompilationBlock> actualBlocks = actual.Blocks;

        AssertSymbolEngine.TestCustomLists<CompilationBlock>(
            expected,
            actualBlocks,
            "Check compilation blocks",
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
