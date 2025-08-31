namespace UnikraftScanner.Tests;
using UnikraftScanner.Client.Symbols;
using System.IO;
using System.Reflection;
using UnikraftScanner.Client.Helpers;
using Xunit.Abstractions;

public class MultiLineTest : BaseSymbolTest
{
    public MultiLineTest(PrepSymbolTestEnv fix, ITestOutputHelper output) : base(fix){
        this.output = output;
    }
    private readonly ITestOutputHelper output;

    [Fact]
    [Trait("Category", "DiscoveryStage")]
    public void FindCompilationBlocks_MultilineDirectives_Paranthesis_MultiSpace_And_Code()
    {
        /*
        Code is also multiline, every incomplete multiline is counted as a compiled line

        Multiline symbol condition with multiple \ lines and lines with symbols and \
        Symbol condition may have paranthesis on multiple lines and tokens inside the condition may have multiple whitespace characters

        We only remove multiple whitespaces and preserve only 1 between tokens and paranthesis during parsing of the Compilation statement

        Redundant multiline characters after a symbol condition are counted as code lines 
        */

        string sourceFileAbsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../../../Symbols/Discovery/inputs/multi_line.c");
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


        List<CompilationBlock> expected = new List<CompilationBlock>{

            new CompilationBlock(
                type: CompilationBlockTypes.IF,
                symbolCondition : "#if defined(C) && defined(D)",
                startLine: 2,
                startLineEnd: 7,
                endLine: 13,
                blockCounter : 0,
                parentCounter : -1,
                lines: 4,
                children: null),

            // multiline operator does not produce a whitespace, so the first && is glued to the paranthesis same for third || operator
            new CompilationBlock(
                type: CompilationBlockTypes.ELIF,
                symbolCondition : "#elif (defined(X) || defined(B) )&& (defined(X)) && ( defined(Y) || defined(A))|| ( defined(Y) )",
                startLine: 13,
                startLineEnd: 18,
                endLine: 21,
                blockCounter : 1,
                parentCounter : -1,
                lines: 2,
                children: null),

            // even redundant multiline characters after a symbol condition are counted as code lines
            // start end line is not where the last redundant multiline character is (we respect compiler decision here, no need to refactor)
            new CompilationBlock(
                type: CompilationBlockTypes.ELIF,
                symbolCondition : "#elif defined(A)",
                startLine: 21,
                startLineEnd: 21,
                endLine: 27,
                blockCounter : 2,
                parentCounter : -1,
                lines: 4,
                children: null),

            // the source code has multiple whitespaces at the end of the condition that must be removed
            new CompilationBlock(
                type: CompilationBlockTypes.IFDEF,
                symbolCondition : "#ifdef C",
                startLine: 36,
                startLineEnd: 36,
                endLine: 38,
                blockCounter : 3,
                parentCounter : -1,
                lines: 1,
                children: null),

            // #ifdef split in the symbol with a macro in the code line with same name in the second half of the symbol
            // code line is only 1, we dont count macro expansion final result
            new CompilationBlock(
                type: CompilationBlockTypes.IFDEF,
                symbolCondition : "#ifdef XYZTW",
                startLine: 40,
                startLineEnd: 41,
                endLine: 43,
                blockCounter : 4,
                parentCounter : -1,
                lines: 1,
                children: null),
                    
            // same as previous one
            new CompilationBlock(
                type: CompilationBlockTypes.IFNDEF,
                symbolCondition : "#ifndef XYZTW",
                startLine: 45,
                startLineEnd: 46,
                endLine: 48,
                blockCounter : 5,
                parentCounter : -1,
                lines: 1,
                children: null),


            // https://github.com/CSBonLaboratory/Unikraft-Scanner/issues/14#issuecomment-3092380988
            // STUV msut be ignored since it is split but there is a space between it and OPR so this will be a second symbol
            // and #ifdef/#ifndef do not accept multiple symbols with or without operators such as && and ||
            new CompilationBlock(
                type: CompilationBlockTypes.IFNDEF,
                symbolCondition : "#ifndef OPR",
                startLine: 50,
                startLineEnd: 51,
                endLine: 54,
                blockCounter : 6,
                parentCounter : -1,
                lines: 2,
                children: null),

            // multiline #if with split in the symbol on 2+ lines and some lines are empty backslash lines
            new CompilationBlock(
                type: CompilationBlockTypes.IF,
                symbolCondition : "#if defined(ABCDEF )",
                startLine: 55,
                startLineEnd: 61,
                endLine: 67,
                blockCounter : 7,
                parentCounter : -1,
                lines: 2,
                children: null),


            new CompilationBlock(
                type: CompilationBlockTypes.ELIF,
                symbolCondition : "#elif defined(SAHUR) || defined(ABC DE )",
                startLine: 67,
                startLineEnd: 72,
                endLine: 78,
                blockCounter : 8,
                parentCounter : -1,
                lines: 3,
                children: null),
            
            // multiline #else
            new CompilationBlock(
                type: CompilationBlockTypes.ELSE,
                symbolCondition : "#else",
                startLine: 78,
                startLineEnd: 78,
                endLine: 82,
                blockCounter : 9,
                parentCounter : -1,
                lines: 2,
                children: null),
        };


        List<CompilationBlock> actualBlocks = actual.Blocks;

        List<List<int>> expectedCodeLinesInBlocks = [
            [9, 10, 11, 12],
            [19, 20],
            [22, 23, 24, 26],
            [37],
            [42],
            [47],
            [52, 53],
            [64, 65],
            [73, 74, 76],
            [79, 81]
        ];

        List<List<int>> actualCodeLinesInBlocks = actual.debugLinesOfCodeBlocks;

        List<int> expectedUniversalLines = new() { 29, 30, 31, 32, 33, 34, 83 };

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
