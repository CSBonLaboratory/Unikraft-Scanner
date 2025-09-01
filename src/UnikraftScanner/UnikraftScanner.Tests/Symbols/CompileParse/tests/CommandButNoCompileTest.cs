namespace UnikraftScanner.Tests;

using Xunit.Abstractions;
using UnikraftScanner.Client.Symbols;
using System.Reflection;
using UnikraftScanner.Client;
using UnikraftScanner.Client.Helpers;

public class CommandButNoCompileTest
{
    private readonly ITestOutputHelper output;

    public CommandButNoCompileTest(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    [Trait("Category", "ParseCompileCmd")]
    public void AllStages_CommandButNoCompile()
    {
        // we have an .o.cmd file but the command is not for compilation using a known compiler but maybe other tool
        // return null
        string oDotCmdFilePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "../../../Symbols/inputs/command_but_no_compile.o.cmd");

        var actualResult = new NormalCompilationCommandParser().ParseCommand(oDotCmdFilePath);
        if (actualResult.IsSuccess)
        {
            Assert.Fail("Test must fail however it succeded");
        }
        else if (actualResult.Error.GetErrorType() != ErrorTypes.UnknownCompilerFound)
        {
            Assert.Fail(
                $"Test expecting error {ErrorTypes.UnknownCompilerFound} but failed with custom error: {actualResult.Error}"
            );
        }
    }
}