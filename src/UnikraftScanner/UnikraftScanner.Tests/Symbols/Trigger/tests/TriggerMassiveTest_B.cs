namespace UnikraftScanner.Tests;
using Xunit.Abstractions;
using System.Reflection;

[Collection(nameof(SymbolEngineTestParallel))]
public class TriggerMassive_B
{
    private readonly ITestOutputHelper output;
    private ParalelTriggerStageCacheHelper Helper { get; set; }
    private PrepSymbolTestEnvFixture SymbolTestEnv { get; set; }
    public TriggerMassive_B(PrepSymbolTestEnvFixture fix, ITestOutputHelper output)
    {
        this.output = output;
        SymbolTestEnv = fix;
        Helper = new();
    }

    [Fact]
    [Trait("Category", "TriggerStage")]
    public void TriggerCompilationBlocks_B()
    {

        /*
        Test to see shy entrance on second branch and some outside blocks
        */
        string defineSymbolsCmd = "-D B";

        int[] expected = [21, 24, 25, 33, 40, 41];

        string inputPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            $"../../../Symbols/Trigger/inputs/trigger_massive.c"
        );

        string overwriteResultsFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"../../../Symbols/.artifacts/trigger_results_{this.GetType().Name}.txt");
        Helper.RunTriggerTest(inputPath, defineSymbolsCmd, expected, SymbolTestEnv, overwriteResultsFile);

    }
}