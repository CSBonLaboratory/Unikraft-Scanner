namespace UnikraftScanner.Tests;
using UnikraftScanner.Client.Symbols;
using System.IO;
using System.Reflection;
using UnikraftScanner.Client.Helpers;
using Xunit.Abstractions;

public class SimpleElse : BaseSymbolTest
{
    public SimpleElse(PrepSymbolTestEnv fix, ITestOutputHelper output) : base(fix){
        this.output = output;
    }
    private readonly ITestOutputHelper output;

    [Fact]
    [Trait("Category", "DiscoveryStage")]
    public void FindCompilationBlocks_AllTypes_NoComments_NoNested_OneLinerCondition_MultiLineCode_NoPadding_WithElse()
    {
        /*
            #else logic

            multi line code logic => multiple lines are added as only 1 compiled line in a CompilationBlock 

            "else" keyword may also appear as code keyword and not as preprocessor directive

            if code block can be in different compilation blocks

            Do not count white space lines

            #else can be after and #ifdef, #ifndef or after an #elif
        */
        string sourceFileAbsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../../../Symbols/Discovery/inputs/simple_else.c");
        var actualResult = new SymbolEngine()
        .DiscoverCompilationBlocksAndLines(
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
                    type: CompilationBlockTypes.IFNDEF,
                    symbolCondition: "#ifdef CONFIG_1",
                    startLine: 5,
                    startLineEnd: 5,
                    endLine: 12,
                    blockCounter: 0,
                    parentCounter: -1,
                    lines: 6,
                    children: null
                ),
                new CompilationBlock(
                    type: CompilationBlockTypes.ELIF,
                    symbolCondition: "#elif CONFIG_1 || CONFIG_2",
                    startLine: 12,
                    startLineEnd: 12,
                    endLine: 17,
                    blockCounter: 1,
                    parentCounter: -1,
                    lines: 4,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.ELIF,
                    symbolCondition: "#elif !CONFIG_2",
                    startLine: 17,
                    startLineEnd: 17,
                    endLine: 19,
                    blockCounter: 2,
                    parentCounter: -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.ELSE,
                    symbolCondition: "#else",
                    startLine: 19,
                    startLineEnd: 19,
                    endLine: 25,
                    blockCounter: 3,
                    parentCounter: -1,
                    lines: 3,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.IFDEF,
                    symbolCondition: "#ifdef CONFIG_KVMPLAT",
                    startLine: 27,
                    startLineEnd: 27,
                    endLine: 29,
                    blockCounter: 4,
                    parentCounter: -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.ELIF,
                    symbolCondition: "#elif !CONFIG_RUST",
                    startLine: 29,
                    startLineEnd: 29,
                    endLine: 31,
                    blockCounter: 5,
                    parentCounter: -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.ELSE,
                    symbolCondition: "#else",
                    startLine: 31,
                    startLineEnd: 31,
                    endLine: 33,
                    blockCounter: 6,
                    parentCounter: -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.IF,
                    symbolCondition: "#if defined(CONFIG_UK)",
                    startLine: 35,
                    startLineEnd: 35,
                    endLine: 38,
                    blockCounter: 7,
                    parentCounter: -1,
                    lines: 2,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.ELIF,
                    symbolCondition: "#elif !defined(CONFIG_UK) || defined(CONFIG_RUST)",
                    startLine: 38,
                    startLineEnd: 38,
                    endLine: 40,
                    blockCounter: 8,
                    parentCounter: -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.ELSE,
                    symbolCondition: "#else",
                    startLine: 40,
                    startLineEnd: 40,
                    endLine: 44,
                    blockCounter: 9,
                    parentCounter: -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.IF,
                    symbolCondition: "#if !defined(CONFIG_LIB)",
                    startLine: 46,
                    startLineEnd: 46,
                    endLine: 51,
                    blockCounter: 10,
                    parentCounter: -1,
                    lines: 3,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.ELSE,
                    symbolCondition: "#else",
                    startLine: 51,
                    startLineEnd: 51,
                    endLine: 53,
                    blockCounter: 11,
                    parentCounter: -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.IFDEF,
                    symbolCondition: "#ifdef CONFIG_LIB",
                    startLine: 55,
                    startLineEnd: 55,
                    endLine: 59,
                    blockCounter: 12,
                    parentCounter: -1,
                    lines: 1,
                    children: null)
            };

        List<CompilationBlock> actualBlocks = actual.Blocks;

        List<List<int>> expectedCodeLinesInBlocks = [

            [6,7,8,9,10,11],
            [13,14,15,16],
            [18],
            [20,21,23],
            [28],
            [30],
            [32],
            [36,37],
            [39],
            [43],
            [47,49,50],
            [52],
            [57]

        ];

        List<List<int>> actualCodeLinesInBlocks = actual.debugLinesOfCodeBlocks;

        List<int> expectedUniversalLines = new() { 1, 2, 3, 26, 45, 61 };

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
