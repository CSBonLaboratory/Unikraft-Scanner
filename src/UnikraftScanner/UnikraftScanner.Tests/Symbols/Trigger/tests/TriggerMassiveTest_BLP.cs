namespace UnikraftScanner.Tests;
using Xunit.Abstractions;
using System.Reflection;

[Collection(nameof(SymbolEngineTestParallel))]
public class TriggerMassive_BLP
{
    private readonly ITestOutputHelper output;
    private ParalelTriggerStageCacheHelper Helper { get; set; }
    private PrepSymbolTestEnvFixture SymbolTestEnv { get; set; }
    public TriggerMassive_BLP(PrepSymbolTestEnvFixture fix, ITestOutputHelper output)
    {
        this.output = output;
        SymbolTestEnv = fix;
        Helper = new();
    }

    [Fact]
    [Trait("Category", "TriggerStage")]
    public void TriggerCompilationBlocks_BM()
    {

        /*
        Test to see access second branch and see behavior where #ifndef and #else are theoretically the same but are ignored and some outside blocks
        */
        string defineSymbolsCmd = "-D B -DL -DP";

        int[] expected = [21, 22, 24, 26, 28, 33, 40, 41];

        string inputPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            $"../../../Symbols/Trigger/inputs/trigger_massive.c"
        );

        string overwriteResultsFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"../../../Symbols/.artifacts/trigger_results_{this.GetType().Name}.txt");
        Helper.RunTriggerTest(inputPath, defineSymbolsCmd, expected, SymbolTestEnv, overwriteResultsFile);

    }
}