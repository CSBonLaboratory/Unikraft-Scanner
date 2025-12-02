namespace UnikraftScanner.Tests;
using UnikraftScanner.Client.Symbols;
using System.IO;
using System.Reflection;
using UnikraftScanner.Client.Helpers;
using Xunit.Abstractions;

[Collection(nameof(SymbolEngineTestParallel))]
public class IncludeCheckMainFileTest
{
    private PrepSymbolTestEnvFixture SymbolTestEnv { get; set; }
    private readonly ITestOutputHelper output;
    public IncludeCheckMainFileTest(PrepSymbolTestEnvFixture fix, ITestOutputHelper output)
    {
        this.output = output;
        SymbolTestEnv = fix;
    }

    [Fact]
    [Trait("Category", "DiscoveryStage")]
    public void FindCompilationBlocks_Comments_SingleLine_IncludeHeaderFileWithCompilationBlocks()
    {
        /*
        Compilation blocks, outside the source file passed inside the clang plugin command, must not be parsed
        */

        string sourceFileAbsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../../../Symbols/Discovery/inputs/include_check_mainFile.c");

        // optimization, remove bottleneck when running tests in parallel, where a single results file is used for all tests
        string overwriteResultsFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"../../../Symbols/.artifacts/{this.GetType().Name}.txt");

        PluginOptions newOpts = new PluginOptions(
            CompilerPath: SymbolTestEnv.Opts.CompilerPath,
            PluginPath: SymbolTestEnv.Opts.PluginPath,
            PluginName: SymbolTestEnv.Opts.PluginName,
            InterceptionResultsFilePath_External_PluginParam: overwriteResultsFile,
            Stage_RetainExcludedBlocks_Internal_PluginParam: SymbolTestEnv.Opts.Stage_RetainExcludedBlocks_Internal_PluginParam
        );

        var actualResult = new SymbolEngine().DiscoverCompilationBlocksAndLines(
            sourceFileAbsPath,
            opts: newOpts,
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
                    type: CompilationBlockTypes.IFNDEF,
                    symbolCondition : "#ifndef A",
                    startLine: 6,
                    startLineEnd: 6,
                    endLine: 10,
                    blockCounter : 0,
                    parentCounter : -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.ELIF,
                    symbolCondition : "#elif !defined(B)",
                    startLine: 10,
                    startLineEnd: 10,
                    endLine: 13,
                    blockCounter : 1,
                    parentCounter : -1,
                    lines: 1,
                    children: null),

                new CompilationBlock(
                    type: CompilationBlockTypes.ELIF,
                    symbolCondition : "#elif defined(B)",
                    startLine: 13,
                    startLineEnd: 13,
                    endLine: 16,
                    blockCounter : 2,
                    parentCounter : -1,
                    lines: 1,
                    children: null)
            };

        List<CompilationBlock> actualBlocks = actual.Blocks;

        List<List<int>> expectedCodeLinesInBlocks = [
            [8],
            [11],
            [14]
        ];

        List<List<int>> actualCodeLinesInBlocks = actual.debugLinesOfCodeBlocks;

        List<int> expectedUniversalLines = new() { 1, 3, 5, 18 };

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