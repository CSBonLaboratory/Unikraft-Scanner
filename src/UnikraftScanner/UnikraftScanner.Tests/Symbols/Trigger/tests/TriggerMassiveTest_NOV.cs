namespace UnikraftScanner.Tests;
using Xunit.Abstractions;
using System.Reflection;

[Collection(nameof(SymbolEngineTestParallel))]
public class TriggerMassive_NOV
{
    private readonly ITestOutputHelper output;
    private ParalelTriggerStageCacheHelper Helper { get; set; }
    private PrepSymbolTestEnvFixture SymbolTestEnv { get; set; }
    public TriggerMassive_NOV(PrepSymbolTestEnvFixture fix, ITestOutputHelper output)
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
        Test to see what if the first root block is ignored besides its #else and we play with other root blocks
        where a block will always ignore its #else since the previous branch will always be true,
        where a block is ignored even when an internal block of it is always true 
        */
        string defineSymbolsCmd = "-D N -DO -DV";

        int[] expected = [35, 36, 38, 45];

        string inputPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            $"../../../Symbols/Trigger/inputs/trigger_massive.c"
        );

        string overwriteResultsFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), $"../../../Symbols/.artifacts/trigger_results_{this.GetType().Name}.txt");
        Helper.RunTriggerTest(inputPath, defineSymbolsCmd, expected, SymbolTestEnv, overwriteResultsFile);

    }
}