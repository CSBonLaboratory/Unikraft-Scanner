namespace UnikraftScanner.Tests;
using UnikraftScanner.Client.Symbols;
using System.IO;
using System.Reflection;
using Xunit.Abstractions;
using UnikraftScanner.Client;

[Collection(nameof(SymbolEngineTestParallel))]
public class CompilationErrorTest
{
    private readonly ITestOutputHelper output;
    private PrepSymbolTestEnvFixture SymbolTestEnv { get; set; }
    public CompilationErrorTest(PrepSymbolTestEnvFixture fix, ITestOutputHelper output)
    {
        this.output = output;
        SymbolTestEnv = fix;
    }

    [Theory]
    [Trait("Category", "DiscoveryStage")]
    [InlineData("wrong_multiline_backslash_split_elif.c")]
    [InlineData("wrong_multiline_backslash_split_else.c")]
    [InlineData("wrong_multiline_backslash_split_if.c")]
    [InlineData("wrong_multiline_backslash_split_ifdef.c")]
    [InlineData("wrong_multiline_backslash_split_ifndef.c")]
    [InlineData("wrong_multiline_elif.c")]
    [InlineData("wrong_multiline_else.c")]
    [InlineData("wrong_multiline_split_elif.c")]
    [InlineData("wrong_multiline_split_if.c")]
    [InlineData("wrong_multiline_split_ifdef.c")]
    [InlineData("wrong_multiline_split_ifndef.c")]
    [InlineData("wrong_multiple_directives.c")]
    [InlineData("wrong_nested_multiline_if.c")]
    [InlineData("wrong_nested_multiline_ifdef.c")]
    [InlineData("wrong_nested_multiline_ifndef.c")]
    public void FindCompilationBlocks_WrongTests(string testNameFile)
    {
        /*
        Macro expansion must not be made in order to preserve the correct line and column location of the Compilation blocks
        */

        string sourceFileAbsPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"../../../Symbols/Discovery/inputs/{testNameFile}");

        // optimization, remove bottleneck when running tests in parallel, where a single results file is used for all tests
        string overwriteResultsFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"../../../Symbols/.artifacts/{this.GetType().Name}_{testNameFile}.txt");

        PluginOptions newOpts = new PluginOptions(
            CompilerPath: SymbolTestEnv.Opts.CompilerPath,
            PluginPath: SymbolTestEnv.Opts.PluginPath,
            PluginName: SymbolTestEnv.Opts.PluginName,
            InterceptionResultsFilePath_External_PluginParam: overwriteResultsFile,
            Stage_RetainExcludedBlocks_Internal_PluginParam: SymbolTestEnv.Opts.Stage_RetainExcludedBlocks_Internal_PluginParam
        );
        var actual = new SymbolEngine().DiscoverCompilationBlocksAndLines(
            sourceFileAbsPath,
            opts: newOpts,
            targetCompilationCommand: $"{SymbolTestEnv.Opts.CompilerPath} -I/usr/include -c {sourceFileAbsPath}"
        );

        if (actual.IsSuccess)
        {
            Assert.Fail("Test must have expected failed result but actual value has succeeded");
        }
        else if (actual.Error.GetErrorType() != ErrorTypes.WrongPreprocessorDirective)
        {
           Assert.Fail(
                $"Test expecting error {ErrorTypes.WrongPreprocessorDirective} but failed with custom error: {actual.Error}"
            );
        }

    }

    [Trait("Category", "DiscoveryStage")]
    [Fact]
    public void FindCompilatioBlocks_CompileError()
    {
        string sourceFileAbsPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"../../../Symbols/Discovery/inputs/compilation_error.c");

        var actual = new SymbolEngine().DiscoverCompilationBlocksAndLines(
            sourceFileAbsPath,
            opts: SymbolTestEnv.Opts,
            targetCompilationCommand: $"{SymbolTestEnv.Opts.CompilerPath} -I/usr/include -c {sourceFileAbsPath}"
        );

        if (actual.IsSuccess)
        {
            Assert.Fail("Test must have expected failed result but actual value has succeeded");
        }
        else if (actual.Error.GetErrorType() != ErrorTypes.CompilationInPluginFailure)
        {
            Assert.Fail(
                $"Test expecting error {ErrorTypes.CompilationInPluginFailure} but failed with custom error: {actual.Error}"
            );
        }
        
    }
}
