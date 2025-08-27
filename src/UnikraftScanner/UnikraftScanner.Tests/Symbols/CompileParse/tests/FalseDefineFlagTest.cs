namespace UnikraftScanner.Tests;

using Xunit.Abstractions;
using UnikraftScanner.Client.Symbols;
using System.Reflection;
public class FalseDefineFlagTest
{
    private readonly ITestOutputHelper output;

    public FalseDefineFlagTest(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    [Trait("Category", "CompileParse")]
    public void DiscoveryStage_FalseFlagsTest()
    {
        string oDotCmdFilePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "../../../Symbols/inputs/false_define_flags.o.cmd");

        // flag that contains -D<symbol name> sequence is not a define flag if it has prefix such as "-fake-flag"
        // --U does not exist as undefine flag

        NormalCommandResult expected = new(
            includeTokens: [],
            orderedSymbolDefineTokens: [],
            otherTokens: ["-fake-flag-DSYMBOL", "--D", "SYMBOL1", "-c", "/home/haproxy/main.c", "-fake-flag-USYMBOL", "-o", "./main.o", "--U", "SYMBOL1"],
            sourceFileAbsPath: "/home/haproxy/main.c",
            compiler: "gcc",
            compilerCategory: NormalCompilationCommandParser.SupportedCompilers[(int)CompilerCategories.GCC],
            fullCommand: "gcc -fake-flag-DSYMBOL --D SYMBOL1 -c /home/haproxy/main.c -fake-flag-USYMBOL -o ./main.o --U SYMBOL1"
        );

        Assert.Equal(expected, new NormalCompilationCommandParser().ParseCommand(oDotCmdFilePath));
    }
}
