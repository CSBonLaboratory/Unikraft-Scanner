namespace UnikraftScanner.Tests;

using Xunit.Abstractions;
using UnikraftScanner.Client.Symbols;
using System.Reflection;
using UnikraftScanner.Client;

public class CommandButNoCompileTest
{
    private readonly ITestOutputHelper output;

    public CommandButNoCompileTest(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    [Trait("Category", "CompileParse")]
    public void AllStages_CommandButNoCompile()
    {
        // we have an .o.cmd file but the command is not for compilation using a known compiler but maybe other tool
        // return null
        string oDotCmdFilePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "../../../Symbols/inputs/command_but_no_compile.o.cmd");

        try
        {
            new NormalCompilationCommandParser().ParseCommand(oDotCmdFilePath);
        }
        catch (UnknownCompilerFound)
        {
            // congrats it's the right exception
        }
        catch (Exception oops)
        {
            Assert.Fail($"Unexpected crash or wrong exception:\n {oops.Message}");
        }

        Assert.Fail("Test with wrong contents did not raise an UnknownCompilerFound exception !");
    }
}