namespace UnikraftScanner.Tests;
using UnikraftScanner.Client.Symbols;
using System.IO;
using System.Reflection;
using UnikraftScanner.Client.Helpers;
using Xunit.Abstractions;

[Collection(nameof(SymbolEngineTestParallel))]
public class PaddingTest
{
    private PrepSymbolTestEnvFixture SymbolTestEnv { get; set; }
    private readonly ITestOutputHelper output;
    public PaddingTest(PrepSymbolTestEnvFixture fix, ITestOutputHelper output)
    {
        this.output = output;
        SymbolTestEnv = fix;
    }

    [Fact]
    [Trait("Category", "DiscoveryStage")]
    public void FindCompilationBlocks_AllTypes_NoComments_NoNested_MultiLiners_MixPadding()
    {

        /*
            Preprocessor directives and code statements have mixed paddings and multiline mixed paddings
        */
        string sourceFileAbsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../../../Symbols/Discovery/inputs/padding_allignment.c");

        // optimization, remove bottleneck when running tests in parallel, where a single results file is used for all tests
        string overwriteResultsFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"../../../Symbols/.artifacts/{this.GetType().Name}.txt");

        PluginOptions newOpts = new PluginOptions(
            CompilerPath: SymbolTestEnv.Opts.CompilerPath,
            PluginPath: SymbolTestEnv.Opts.PluginPath,
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

        List<List<int>> expectedCodeLinesInBlocks = [
            [],
            [9, 11],
            [14],
            [17]
        ];

        List<List<int>> actualCodeLinesInBlocks = actual.debugLinesOfCodeBlocks;

        List<int> expectedUniversalLines = new() { 2, 4, 20 };

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
