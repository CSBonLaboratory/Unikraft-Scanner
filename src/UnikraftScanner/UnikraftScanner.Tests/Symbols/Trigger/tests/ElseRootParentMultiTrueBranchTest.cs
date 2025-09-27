namespace UnikraftScanner.Tests;
using Xunit.Abstractions;
using System.Reflection;

[Collection(nameof(SymbolEngineTestParallel))]
public class ElseRootParentMultiTrueBranch
{
    private readonly ITestOutputHelper output;
    private ParalelTriggerStageCacheHelper Helper { get; set; }
    private PrepSymbolTestEnvFixture SymbolTestEnv { get; set; }
    public ElseRootParentMultiTrueBranch(PrepSymbolTestEnvFixture fix, ITestOutputHelper output)
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
        Test to see multiple branches that are true, some also have #else some do not
        */
        string defineSymbolsCmd = "-DA -DB -DC -DX -DY -DZ -DI -DJ -DT -DY -DU";

        int[] expected = [0, 4, 8, 11];

        string inputPath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            $"../../../Symbols/Trigger/inputs/else_root_parent.c"
        );

        Helper.RunTriggerTest(inputPath, defineSymbolsCmd, expected, SymbolTestEnv, $"trigger_results_{this.GetType().Name}.txt");

    }
}