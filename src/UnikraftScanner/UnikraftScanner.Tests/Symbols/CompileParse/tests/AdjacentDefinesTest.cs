namespace UnikraftScanner.Tests;

using Xunit.Abstractions;
using UnikraftScanner.Client.Symbols;
using System.Reflection;

public class AdjacentDefinesTest
{
    private readonly ITestOutputHelper output;

    public AdjacentDefinesTest(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    [Trait("Category", "CompileParse")]
    public void TriggerStage_AdjacentDefinesTest()
    {
        // we have an .o.cmd file but the command is not for compilation using a known compiler but maybe other tool
        // return null
        string oDotCmdFilePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "../../../Symbols/inputs/adjacent_defines_mixed.o.cmd");


        NormalCommandResult expected = new(
            includeTokens: [],
            orderedSymbolDefineTokens: ["-DAAAA", "-D", "YYY", "-flag2", "-U", "YYY", "-D", "XXX", "-DA", "-UQQ", "-U", "ZS"],
            otherTokens: ["-c", "main.c", "-o", "main.o", "-no-define-flag", "-no-define-flag1", "-flag3"],
            sourceFileAbsPath: "main.c",
            compiler: "gxx",
            compilerCategory: NormalCompilationCommandParser.SupportedCompilers[(int)CompilerCategories.GXX],
            fullCommand: "g++ -c main.c -o main.o -DAAAA -no-define-flag -no-define-flag1 -D YYY -flag2 -U YYY -flag3 -D XXX -DA -UQQ -U ZS"

        );


        Assert.Equal(expected, new NormalCompilationCommandParser().ParseCommand(oDotCmdFilePath));


    }
}