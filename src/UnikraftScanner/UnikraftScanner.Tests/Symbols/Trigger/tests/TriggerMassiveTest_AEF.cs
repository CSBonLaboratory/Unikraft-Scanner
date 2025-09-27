namespace UnikraftScanner.Tests;
using Xunit.Abstractions;
using System.Reflection;

[Collection(nameof(SymbolEngineTestParallel))]
public class TriggerMassive_AEF
{
    private PrepSymbolTestEnvFixture SymbolTestEnv { get; set; }
    private ParalelTriggerStageCacheHelper Helper { get; set; }
    private readonly ITestOutputHelper output;
    public TriggerMassive_AEF(PrepSymbolTestEnvFixture fix, ITestOutputHelper output)
    {
        this.output = output;
        SymbolTestEnv = fix;
        Helper = new();
    }

    [Fact]
    [Trait("Category", "TriggerStage")]
    public void TriggerCompilationBlocks_AEF()
    {

        /*
        Test that goes as deep as possible in the first branch where else are true but also contain siblings which are true that take priority and some outside blocks
        */
        string defineSymbolsCmd = "-DA -DE -DF";

        int[] expected = [0, 1, 2, 3, 5, 9, 11, 12, 20, 40, 41];

        // used as optimization, multithread safe

        string inputPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            $"../../../Symbols/Trigger/inputs/trigger_massive.c"
        );

        Helper.RunTriggerTest(inputPath, defineSymbolsCmd, expected, SymbolTestEnv, $"trigger_results_{this.GetType().Name}.txt");

    }
}