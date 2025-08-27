namespace UnikraftScanner.Tests;

using Xunit.Abstractions;
using UnikraftScanner.Client.Symbols;
using System.Reflection;
public class DefineCaseSensitiveTest
{
    private readonly ITestOutputHelper output;

    public DefineCaseSensitiveTest(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    [Trait("Category", "CompileParse")]
    public void DiscoveryStage_CaseSensitiveFlags()
    {
        string oDotCmdFilePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "../../../Symbols/inputs/define_case_sensitive.o.cmd");


        // -D (define a macro) and -d are different features
        // same with -U (udefine a macro) and -u (undefine a symbol such as function or variable at link time)

        NormalCommandResult expected = new(
            includeTokens: [],
            orderedSymbolDefineTokens: ["-D", "BBB", "-UBBB"],
            otherTokens: ["-d", "AAA", "-uBBB", "-fake1", "-fake2", "-u", "AAA", "-fake3", "-dXXX"],
            sourceFileAbsPath: "main.c",
            compiler: "gcc",
            compilerCategory: NormalCompilationCommandParser.SupportedCompilers[(int)CompilerCategories.GCC],
            fullCommand: "gcc -d AAA -D BBB -uBBB -fake1 -fake2 -u AAA -fake3 -dXXX main.c"

        );

        Assert.Equal(expected, new NormalCompilationCommandParser().ParseCommand(oDotCmdFilePath));
    }

   
}