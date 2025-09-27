namespace UnikraftScanner.Tests;

using Xunit.Abstractions;
using System.Reflection;

[Collection(nameof(SymbolEngineTestParallel))]
public class TriggerMassive_AEFGHIJK
{
    private readonly ITestOutputHelper output;
    private ParalelTriggerStageCacheHelper Helper { get; set; }
    private PrepSymbolTestEnvFixture SymbolTestEnv { get; set; }
    public TriggerMassive_AEFGHIJK(PrepSymbolTestEnvFixture fix, ITestOutputHelper output)
    {
        this.output = output;
        SymbolTestEnv = fix;
        Helper = new();
    }

    [Fact]
    [Trait("Category", "TriggerStage")]
    public void TriggerCompilationBlocks_AEFGHIJK()
    {

        /*
        Test that goes half deep in a branch. H, I, J, K are useless defined symbols and some outside blocks
        */
        string defineSymbolsCmd = "-DA -DE -DF -DG -DH -DI -DJ -DK";

        int[] expected = [0, 1, 2, 20, 40, 41];

        string inputPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            $"../../../Symbols/Trigger/inputs/trigger_massive.c"
        );

        Helper.RunTriggerTest(inputPath, defineSymbolsCmd, expected, SymbolTestEnv, $"trigger_results_{this.GetType().Name}.txt");

    }
}