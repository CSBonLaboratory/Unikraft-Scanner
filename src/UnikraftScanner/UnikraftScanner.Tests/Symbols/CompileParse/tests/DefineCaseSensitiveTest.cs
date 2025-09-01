namespace UnikraftScanner.Tests;

using Xunit.Abstractions;
using UnikraftScanner.Client.Symbols;
using System.Reflection;
using UnikraftScanner.Client.Helpers;
public class DefineCaseSensitiveTest
{
    private readonly ITestOutputHelper output;

    public DefineCaseSensitiveTest(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    [Trait("Category", "ParseCompileCmd")]
    public void DiscoveryStage_CaseSensitiveFlags()
    {
        string oDotCmdFilePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "../../../Symbols/inputs/define_case_sensitive.o.cmd");


        // -D (define a macro) and -d are different features
        // same with -U (udefine a macro) and -u (undefine a symbol such as function or variable at link time)

        NormalCommandDTO expected = new(
            includeTokens: [],
            orderedSymbolDefineTokens: ["-D", "BBB", "-UBBB"],
            otherTokens: ["-d", "AAA", "-uBBB", "-fake1", "-fake2", "-u", "AAA", "-fake3", "-dXXX"],
            sourceFileAbsPath: "main.c",
            compiler: "gcc",
            compilerCategory: NormalCompilationCommandParser.SupportedCompilers[(int)CompilerCategories.GCC],
            fullCommand: "gcc -d AAA -D BBB -uBBB -fake1 -fake2 -u AAA -fake3 -dXXX main.c"

        );

        var actualResult = new NormalCompilationCommandParser().ParseCommand(oDotCmdFilePath);
        if (!actualResult.IsSuccess)
        {
            Assert.Fail(((ErrorUnikraftScanner<string>)actualResult.Error).Data);
        }

        Assert.Equal(expected, actualResult.Value);
    }

   
}