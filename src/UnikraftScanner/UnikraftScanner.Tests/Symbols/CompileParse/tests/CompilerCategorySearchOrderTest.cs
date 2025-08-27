namespace UnikraftScanner.Tests;

using Xunit.Abstractions;
using UnikraftScanner.Client.Symbols;
using System.Reflection;
public class CompilerCategorySearchOrderTest
{
    private readonly ITestOutputHelper output;

    public CompilerCategorySearchOrderTest(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    [Trait("Category", "CompileParse")]
    public void PlusPlusFirst()
    {
        string oDotCmdFilePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "../../../Symbols/inputs/compiler_category_search_order.o.cmd");

        NormalCommandResult expected = new(
            includeTokens: [],
            orderedSymbolDefineTokens: [],
            otherTokens: ["-c", "/home/crypto/main.c", "-o", "/home/crypto/main.o"],
            sourceFileAbsPath: "main.c",
            compiler: "clang++-18",
            compilerCategory: NormalCompilationCommandParser.SupportedCompilers[(int)CompilerCategories.GXX],
            fullCommand: "clang++-18 -c /home/crypto/main.c -o /home/crypto/main.o"

        );

        Assert.Equal(expected, new NormalCompilationCommandParser().ParseCommand(oDotCmdFilePath));


    }
}
