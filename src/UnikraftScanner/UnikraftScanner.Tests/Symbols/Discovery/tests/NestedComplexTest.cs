namespace UnikraftScanner.Tests;
using UnikraftScanner.Client.Symbols;
using System.IO;
using System.Reflection;
using UnikraftScanner.Client.Helpers;
using Xunit.Abstractions;
public class NestedComplexTest : BaseSymbolTest
{

    public NestedComplexTest(PrepSymbolTestEnv fix, ITestOutputHelper output) : base(fix){
        this.output = output;
    }
    private readonly ITestOutputHelper output;
    [Fact]
    public void FindCompilationBlocks_Nested_Complex_SpaceBetweenHashtagAndDirective()
    {

        /*
        Simple nested compilation blocks with multiple nested blocks where their parent is the root 

        We only remove multiple whitespaces and preserve only 1 between tokens and paranthesis
        */

        string sourceFileAbsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../../../Symbols/Discovery/inputs/nested_complex.c");
        var actual = SymbolEngine.GetInstance().FindCompilationBlocksAndLines(
            sourceFileAbsPath,
            opts: SymbolTestEnv.Opts,
            targetCompilationCommand: $"{SymbolTestEnv.Opts.CompilerPath} -I/usr/include -c {sourceFileAbsPath} {DiscoveryStageCommandParser.additionalFlags}"
            );


        var expected = new List<CompilationBlock>{
                new CompilationBlock(
                    type: CompilationBlockTypes.IF,
                    symbolCondition : "#if defined(A)",
                    startLine: 4,
                    startLineEnd: 4,
                    endLine: 5,
                    blockCounter : 0,
                    parentCounter : -1,
                    lines: 0,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.ELIF,
                    symbolCondition : "#elif defined(B)",
                    startLine: 5,
                    startLineEnd: 5,
                    endLine: 20,
                    blockCounter : 1,
                    parentCounter : -1,
                    lines: 0,
                    children: [2, 8, 9]),

                new CompilationBlock(
                    type: CompilationBlockTypes.IFNDEF,
                    symbolCondition : "#ifndef E",
                    startLine: 6,
                    startLineEnd: 6,
                    endLine: 14,
                    blockCounter : 2,
                    parentCounter : 1,
                    lines: 0,
                    children: [3, 4, 5]),

                new CompilationBlock(
                    type: CompilationBlockTypes.IF,
                    symbolCondition : "#if defined(G)",
                    startLine: 7,
                    startLineEnd: 7,
                    endLine: 8,
                    blockCounter : 3,
                    parentCounter : 2,
                    lines: 0,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.ELIF,
                    symbolCondition : "#elif defined(H)",
                    startLine: 8,
                    startLineEnd: 8,
                    endLine: 9,
                    blockCounter : 4,
                    parentCounter : 2,
                    lines: 0,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.ELSE,
                    symbolCondition : "#else",
                    startLine: 9,
                    startLineEnd: 9,
                    endLine: 13,
                    blockCounter : 5,
                    parentCounter : 2,
                    lines: 0,
                    children: [6, 7]),

                new CompilationBlock(
                    type: CompilationBlockTypes.IFNDEF,
                    symbolCondition : "#ifndef I",
                    startLine: 10,
                    startLineEnd: 10,
                    endLine: 11,
                    blockCounter : 6,
                    parentCounter : 5,
                    lines: 0,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.ELIF,
                    symbolCondition : "#elif !defined(J)",
                    startLine: 11,
                    startLineEnd: 11,
                    endLine: 12,
                    blockCounter : 7,
                    parentCounter : 5,
                    lines: 0,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.IFDEF,
                    symbolCondition : "#ifdef A",
                    startLine: 15,
                    startLineEnd: 15,
                    endLine: 16,
                    blockCounter : 8,
                    parentCounter : 1,
                    lines: 0,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.IFNDEF,
                    symbolCondition : "#ifndef B",
                    startLine: 18,
                    startLineEnd: 18,
                    endLine: 19,
                    blockCounter : 9,
                    parentCounter : 1,
                    lines: 0,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.ELIF,
                    symbolCondition : "#elif defined(C)",
                    startLine: 20,
                    startLineEnd: 20,
                    endLine: 22,
                    blockCounter : 10,
                    parentCounter : -1,
                    lines: 0,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.ELIF,
                    symbolCondition : "#elif !defined(D)",
                    startLine: 22,
                    startLineEnd: 22,
                    endLine: 29,
                    blockCounter : 11,
                    parentCounter : -1,
                    lines: 0,
                    children: [12, 13]),

                new CompilationBlock(
                    type: CompilationBlockTypes.IF,
                    symbolCondition : "#if defined(K)",
                    startLine: 24,
                    startLineEnd: 24,
                    endLine: 25,
                    blockCounter : 12,
                    parentCounter : 11,
                    lines: 0,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.IFDEF,
                    symbolCondition : "#ifdef L",
                    startLine: 27,
                    startLineEnd: 27,
                    endLine: 28,
                    blockCounter : 13,
                    parentCounter : 11,
                    lines: 0,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.IF,
                    symbolCondition : "#if defined(Z)",
                    startLine: 32,
                    startLineEnd: 32,
                    endLine: 39,
                    blockCounter : 14,
                    parentCounter : -1,
                    lines: 0,
                    children: [15]),

                new CompilationBlock(
                    type: CompilationBlockTypes.IFDEF,
                    symbolCondition : "#ifdef V",
                    startLine: 33,
                    startLineEnd: 33,
                    endLine: 38,
                    blockCounter : 15,
                    parentCounter : 14,
                    lines: 0,
                    children: [16]),

                new CompilationBlock(
                    type: CompilationBlockTypes.IFNDEF,
                    symbolCondition : "#ifndef M",
                    startLine: 34,
                    startLineEnd: 34,
                    endLine: 37,
                    blockCounter : 16,
                    parentCounter : 15,
                    lines: 0,
                    children: new List<int>{17}),

                new CompilationBlock(
                    type: CompilationBlockTypes.IF,
                    symbolCondition : "#if !defined(N)",
                    startLine: 35,
                    startLineEnd: 35,
                    endLine: 36,
                    blockCounter : 17,
                    parentCounter : 16,
                    lines: 0,
                    children: null)
            };

        List<CompilationBlock> actualBlocks = actual.Blocks;
        AssertSymbolEngine.TestCustomLists<CompilationBlock>(expected, actualBlocks, "Check compilation blocks:");

        List<int> expectedUniversalLines = new() { 1, 40 };
        AssertSymbolEngine.TestUniversalLinesOfCode(expectedUniversalLines.Count, expectedUniversalLines, actual.UniversalLinesOfCode, actual.debugUniveralLinesOfCodeIdxs);

        List<List<int>> expectedCodeLinesInBlocks = Enumerable.Repeat(new List<int>(), expected.Count).ToList();

        List<List<int>> actualCodeLinesInBlocks = actual.debugLinesOfCodeBlocks;

        AssertSymbolEngine.TestLinesOfCodeInBlocks(expectedCodeLinesInBlocks, actualCodeLinesInBlocks);
    }
}
