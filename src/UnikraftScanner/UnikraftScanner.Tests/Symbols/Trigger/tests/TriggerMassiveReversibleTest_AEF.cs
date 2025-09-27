namespace UnikraftScanner.Tests;
using Xunit.Abstractions;
using System.Reflection;

[Collection(nameof(SymbolEngineTestParallel))]
public class TriggerMassiveReversible_AEF
{
    private readonly ITestOutputHelper output;
    private ParalelTriggerStageCacheHelper Helper { get; set; }
    private PrepSymbolTestEnvFixture SymbolTestEnv { get; set; }
    public TriggerMassiveReversible_AEF(PrepSymbolTestEnvFixture fix, ITestOutputHelper output)
    {
        this.output = output;
        SymbolTestEnv = fix;
        Helper = new();
    }
    
    [Fact]
    [Trait("Category", "TriggerStage")]
    public void TriggerCompilationBlocks_Reversible_AEF()
    {
        /*
        Test that goes as deep as possible in the first branch but also adds some undefines that are neutered by same defines and some outside blocks
        */
        string defineSymbolsCmd = "-DA -DE -DF -U E -UF -U A -D F -D A -D E";

        int[] expected = [0, 1, 2, 3, 5, 9, 11, 12, 20, 40, 41];

        // used as optimization, multithread safe

        string inputPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            $"../../../Symbols/Trigger/inputs/trigger_massive.c"
        );

        Helper.RunTriggerTest(inputPath, defineSymbolsCmd, expected, SymbolTestEnv, $"trigger_results_{this.GetType().Name}.txt");

    }
}