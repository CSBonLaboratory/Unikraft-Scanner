namespace UnikraftScanner.Tests;
using Xunit.Abstractions;
using System.Reflection;

[Collection(nameof(SymbolEngineTestParallel))]
public class TriggerMassive_COEF
{
    private readonly ITestOutputHelper output;
    private ParalelTriggerStageCacheHelper Helper { get; set; }
    private PrepSymbolTestEnvFixture SymbolTestEnv { get; set; }
    public TriggerMassive_COEF(PrepSymbolTestEnvFixture fix, ITestOutputHelper output)
    {
        this.output = output;
        SymbolTestEnv = fix;
        Helper = new();
    }

    [Fact]
    [Trait("Category", "TriggerStage")]
    public void TriggerCompilationBlocks_COEF()
    {

        /*
        Test to see third branch with but some symbols (E, F) may have triggered some internal branches in first branch, unfortunatelly it is false
        Also invalidate a block that will always have an internal branch that will be true (#if 1)
        */
        string defineSymbolsCmd = "-D C -D E -D F -DO";

        int[] expected = [34];

        string inputPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            $"../../../Symbols/Trigger/inputs/trigger_massive.c"
        );

        Helper.RunTriggerTest(inputPath, defineSymbolsCmd, expected, SymbolTestEnv);

    }
}