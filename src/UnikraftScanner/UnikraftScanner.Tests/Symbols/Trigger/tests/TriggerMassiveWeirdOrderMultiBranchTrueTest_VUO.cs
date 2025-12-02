namespace UnikraftScanner.Tests;
using Xunit.Abstractions;
using System.Reflection;

[Collection(nameof(SymbolEngineTestParallel))]
public class TriggerMassiveWeirdOrderMultiBranchTrueTest_VUO
{
    private readonly ITestOutputHelper output;
    private ParalelTriggerStageCacheHelper Helper { get; set; }
    private PrepSymbolTestEnvFixture SymbolTestEnv { get; set; }
    public TriggerMassiveWeirdOrderMultiBranchTrueTest_VUO(PrepSymbolTestEnvFixture fix, ITestOutputHelper output)
    {
        this.output = output;
        SymbolTestEnv = fix;
        Helper = new();
    }

    [Fact]
    [Trait("Category", "TriggerStage")]
    public void TriggerCompilationBlocks_NUV()
    {

        /*
        Test to see how it bheaves with only else from first block and last root block bu the order of defines symbols is not natural
        */
        string defineSymbolsCmd = "-DV -DU -DO";

        int[] expected = [35, 44];

        string inputPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            $"../../../Symbols/Trigger/inputs/trigger_massive.c"
        );

        string overwriteResultsFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"../../../Symbols/.artifacts/trigger_results_{this.GetType().Name}.txt");
        Helper.RunTriggerTest(inputPath, defineSymbolsCmd, expected, SymbolTestEnv, overwriteResultsFile);

    }
}