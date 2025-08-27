namespace UnikraftScanner.Tests;

using Xunit.Abstractions;
using UnikraftScanner.Client.Symbols;
using System.Reflection;
using UnikraftScanner.Client;

public class NoCFamilySourceFileTest
{
    private readonly ITestOutputHelper output;

    public NoCFamilySourceFileTest(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    [Trait("Category", "CompileParse")]
    public void SourceFile_IsNot_C_or_CPP()
    {
        string oDotCmdFilePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "../../../Symbols/inputs/no_c_family_source_file.o.cmd");

        try
        {
            new NormalCompilationCommandParser().ParseCommand(oDotCmdFilePath);
        }
        catch (CFamilyException)
        {
            // congrats it's ok
        }
        catch (Exception oops)
        {
            Assert.Fail($"Unexpected crash:\n {oops.Message}");
        }

        Assert.Fail("Test did not raise CFamilyException!");
    }
}
