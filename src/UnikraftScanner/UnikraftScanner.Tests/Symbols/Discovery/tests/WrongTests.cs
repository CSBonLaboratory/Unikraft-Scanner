namespace UnikraftScanner.Tests;
using UnikraftScanner.Client.Symbols;
using System.IO;
using System.Reflection;
using UnikraftScanner.Client.Helpers;
using Xunit.Abstractions;
using UnikraftScanner.Client;

public class CompilationErrorTest : BaseSymbolTest
{
    private readonly ITestOutputHelper output;

    public CompilationErrorTest(PrepSymbolTestEnv fix, ITestOutputHelper output) : base(fix)
    {
        this.output = output;
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


        var actual = new SymbolEngine().DiscoverCompilationBlocksAndLines(
            sourceFileAbsPath,
            opts: SymbolTestEnv.Opts,
            targetCompilationCommand: $"{SymbolTestEnv.Opts.CompilerPath} -I/usr/include -c {sourceFileAbsPath}"
        );

        if (actual.IsSuccess)
        {
            Assert.Fail("Test must have expected failed result but actual value has succeeded");
        }
        else if (actual.Error.GetErrorType() != ErrorTypes.WrongPreprocessorDirective)
        {
            Assert.Fail(
                @$"Test failed but error is not {ErrorTypes.WrongPreprocessorDirective} 
                but {actual.Error.GetErrorType()} and contents:\n{actual.Error.GetData()}");
        }

    }

    [Trait("Category", "DiscoveryStage")]
    [Fact]
    public void FindCompilatioBlocks_CompileError()
    {
        string sourceFileAbsPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"../../../Symbols/Discovery/inputs/compilation_error");

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
                $"Test failed with custom error: {actual.Error}"
            );
        }
        
    }
}
