namespace UnikraftScanner.Tests;

using Xunit.Abstractions;
using UnikraftScanner.Client.Symbols;
using System.Reflection;
using UnikraftScanner.Client.Helpers;

public class CompilerCategorySearchOrderTest
{
    private readonly ITestOutputHelper output;

    public CompilerCategorySearchOrderTest(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    [Trait("Category", "ParseCompileCmd")]
    public void PlusPlusFirst()
    {
        string oDotCmdFilePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "../../../Symbols/inputs/compiler_category_search_order.o.cmd");

        NormalCommandDTO expected = new(
            includeTokens: [],
            orderedSymbolDefineTokens: [],
            otherTokens: ["-c", "/home/crypto/main.c", "-o", "/home/crypto/main.o"],
            sourceFileAbsPath: "main.c",
            compiler: "clang++-18",
            compilerCategory: NormalCompilationCommandParser.SupportedCompilers[(int)CompilerCategories.GXX],
            fullCommand: "clang++-18 -c /home/crypto/main.c -o /home/crypto/main.o"

        );

        var actualResult = new NormalCompilationCommandParser().ParseCommand(oDotCmdFilePath);
        if (!actualResult.IsSuccess)
        {
            Assert.Fail(((ErrorUnikraftScanner<string>)actualResult.Error).Data);
        }

        Assert.Equal(expected, actualResult.Value);
    }
}
