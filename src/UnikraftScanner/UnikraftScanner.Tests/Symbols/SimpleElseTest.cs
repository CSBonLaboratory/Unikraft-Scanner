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
        var actual = SymbolEngine
        .GetInstance()
        .FindCompilationBlocksAndLines(
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../../../Symbols/inputs/simple_else.c"),
            SymbolTestEnv.Opts,
            includesSubCommand: "-I/usr/include"
            );

        var expected = new List<CompilationBlock>{
                new CompilationBlock(
                    type: ConditionalBlockTypes.IFNDEF,
                    symbolCondition: "#ifdef CONFIG_1",
                    startLine: 5,
                    startLineEnd: 5,
                    fakeEndLine: 12,
                    endLine: 12,
                    blockCounter: 0,
                    parentCounter: -1,
                    lines: 6,
                    children: null
                ),
                new CompilationBlock(
                    type: ConditionalBlockTypes.ELIF,
                    symbolCondition: "#elif CONFIG_1 || CONFIG_2",
                    startLine: 12,
                    startLineEnd: 12,
                    fakeEndLine: 17,
                    endLine: 17,
                    blockCounter: 1,
                    parentCounter: -1,
                    lines: 4,
                    children: null),

                new CompilationBlock(
                    type: ConditionalBlockTypes.ELIF,
                    symbolCondition: "#elif !CONFIG_2",
                    startLine: 17,
                    startLineEnd: 17,
                    fakeEndLine: 19,
                    endLine: 19,
                    blockCounter: 2,
                    parentCounter: -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    type: ConditionalBlockTypes.ELSE,
                    symbolCondition: "#else",
                    startLine: 19,
                    startLineEnd: 19,
                    fakeEndLine: 25,
                    endLine: 25,
                    blockCounter: 3,
                    parentCounter: -1,
                    lines: 3,
                    children: null),

                new CompilationBlock(
                    type: ConditionalBlockTypes.IFDEF,
                    symbolCondition: "#ifdef CONFIG_KVMPLAT",
                    startLine: 27,
                    startLineEnd: 27,
                    fakeEndLine: 29,
                    endLine: 29,
                    blockCounter: 4,
                    parentCounter: -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    type: ConditionalBlockTypes.ELIF,
                    symbolCondition: "#elif !CONFIG_RUST",
                    startLine: 29,
                    startLineEnd: 29,
                    fakeEndLine: 31,
                    endLine: 31,
                    blockCounter: 5,
                    parentCounter: -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    type: ConditionalBlockTypes.ELSE,
                    symbolCondition: "#else",
                    startLine: 31,
                    startLineEnd: 31,
                    fakeEndLine: 33,
                    endLine: 33,
                    blockCounter: 6,
                    parentCounter: -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    type: ConditionalBlockTypes.IF,
                    symbolCondition: "#if defined(CONFIG_UK)",
                    startLine: 35,
                    startLineEnd: 35,
                    fakeEndLine: 38,
                    endLine: 38,
                    blockCounter: 7,
                    parentCounter: -1,
                    lines: 2,
                    children: null),

                new CompilationBlock(
                    type: ConditionalBlockTypes.ELIF,
                    symbolCondition: "#elif !defined(CONFIG_UK) || defined(CONFIG_RUST)",
                    startLine: 38,
                    startLineEnd: 38,
                    fakeEndLine: 40,
                    endLine: 40,
                    blockCounter: 8,
                    parentCounter: -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    type: ConditionalBlockTypes.ELSE,
                    symbolCondition: "#else",
                    startLine: 40,
                    startLineEnd: 40,
                    fakeEndLine: 44,
                    endLine: 44,
                    blockCounter: 9,
                    parentCounter: -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    type: ConditionalBlockTypes.IF,
                    symbolCondition: "#if !defined(CONFIG_LIB)",
                    startLine: 46,
                    startLineEnd: 46,
                    fakeEndLine: 51,
                    endLine: 51,
                    blockCounter: 10,
                    parentCounter: -1,
                    lines: 3,
                    children: null),

                new CompilationBlock(
                    type: ConditionalBlockTypes.ELSE,
                    symbolCondition: "#else",
                    startLine: 51,
                    startLineEnd: 51,
                    fakeEndLine: 53,
                    endLine: 53,
                    blockCounter: 11,
                    parentCounter: -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    type: ConditionalBlockTypes.IFDEF,
                    symbolCondition: "#ifdef CONFIG_LIB",
                    startLine: 55,
                    startLineEnd: 55,
                    fakeEndLine: 59,
                    endLine: 59,
                    blockCounter: 12,
                    parentCounter: -1,
                    lines: 1,
                    children: null)
            };

        AssertSymbolEngine.TestSymbolEngineBlockResults(expected, actual.Blocks);

        AssertSymbolEngine.TestUniversalLinesOfCode(
            6,
            new() { 1, 2, 3, 26, 45, 61 },
            actual.UniversalLinesOfCode,
            actual.debugUniveralLinesOfCodeIdxs
        );
    }
}
