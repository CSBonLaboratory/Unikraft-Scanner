namespace UnikraftScanner.Tests;
using Xunit.Abstractions;
using System.Reflection;

[Collection(nameof(SymbolEngineTestParallel))]
public class ElseRootParentOnlyElse
{
    private readonly ITestOutputHelper output;
    private ParalelTriggerStageCacheHelper Helper { get; set; }
    private PrepSymbolTestEnvFixture SymbolTestEnv { get; set; }
    public ElseRootParentOnlyElse(PrepSymbolTestEnvFixture fix, ITestOutputHelper output)
    {
        this.output = output;
        SymbolTestEnv = fix;
        Helper = new();
    }

    [Fact]
    [Trait("Category", "TriggerStage")]
    public void TriggerCompilationBlocks_NoDefines()
    {

        /*
        Test to see triggered blocks when we have only root blocks ending in else branches
        */
        string defineSymbolsCmd = "";

        int[] expected = [3, 7, 13];

        string inputPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            $"../../../Symbols/Trigger/inputs/else_root_parent.c"
        );

        string overwriteResultsFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"../../../Symbols/.artifacts/trigger_results_{this.GetType().Name}.txt");
        Helper.RunTriggerTest(inputPath, defineSymbolsCmd, expected, SymbolTestEnv, overwriteResultsFile);

    }
}