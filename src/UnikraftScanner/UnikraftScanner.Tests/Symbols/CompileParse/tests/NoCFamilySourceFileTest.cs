namespace UnikraftScanner.Tests;

using Xunit.Abstractions;
using UnikraftScanner.Client.Symbols;
using System.Reflection;
using UnikraftScanner.Client;
using UnikraftScanner.Client.Helpers;

public class NoCFamilySourceFileTest
{
    private readonly ITestOutputHelper output;

    public NoCFamilySourceFileTest(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    [Trait("Category", "ParseCompileCmd")]
    public void SourceFile_IsNot_C_or_CPP()
    {
        string oDotCmdFilePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "../../../Symbols/inputs/no_c_family_source_file.o.cmd");

        var actualResult = new NormalCompilationCommandParser().ParseCommand(oDotCmdFilePath);
        if (actualResult.IsSuccess)
        {
            Assert.Fail("Test must fail however it succeded");
        }
        else if (actualResult.Error.GetErrorType() != ErrorTypes.CFamillyCompilerNotFound)
        {
            Assert.Fail(
                $"Test expecting error {ErrorTypes.CFamillyCompilerNotFound} but failed with custom error: {actualResult.Error}"
            );
        }
    }
}
