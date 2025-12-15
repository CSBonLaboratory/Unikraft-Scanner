namespace UnikraftScanner.Tests;

using Xunit.Abstractions;
using UnikraftScanner.Client.Sources;
using UnikraftScanner.Client;

public class  SameConfigsDifferentAppsDifferentFindersTest : IClassFixture<PrepCompilableSourcesEnvFixture>
{
    private readonly ITestOutputHelper output;
    private PrepCompilableSourcesEnvFixture SourcesTestEnv { get; set; }
    public SameConfigsDifferentAppsDifferentFindersTest(PrepCompilableSourcesEnvFixture fix, ITestOutputHelper output)
    {
        this.output = output;
        SourcesTestEnv = fix;
    }

    [Fact]
    [Trait("Category", "Sources")]
    public void FindAllCompilableSources_TrapCompiler_MakefileDryRun_MultipleApps()
    {
        string nginxPath = Path.Combine(SourcesTestEnv.unikraftCatalogRootPath, "library/nginx/1.25");

        string nodePath  = Path.Combine(SourcesTestEnv.unikraftCatalogRootPath, "library/node/18");

        // SourcesTestEnv.KraftBuildUnikraftAppOrLib("library/nginx/1.25");

        // SourcesTestEnv.KraftBuildUnikraftAppOrLib("library/node/18");


        // -------------------------------------------- NGINX ---------------------------------------------------------------------

        BincompatHelper nginxAppRuntime = new(
            kraftfilePath: Path.Combine(nginxPath, "Kraftfile"),
            kraftTarget: "--plat qemu --arch x86_64",
            appPath: nginxPath,
            buildScriptFileName: CompilerTrapBincompatFinder.ElfBuildScriptFileName,
            kconfigFilePath: Path.Combine(nginxPath, ".config.nginx_qemu-x86_64")
        );

        
        CompilerTrapBincompatFinder nginxCT = new(
            trapCompilerPath: SourcesTestEnv.TrapExePath,
            appPath: nginxPath,
            resultsFilePath: SourcesTestEnv.sourcesResultsFilePath,
            kraftfilePath: Path.Combine(nginxPath, "Kraftfile"),
            kraftTarget: "--plat qemu --arch x86_64",
            hostCompilerPath: SourcesTestEnv.hostCompilerPath,
            hostLinkerPath: SourcesTestEnv.hostCompilerPath,
            targetAppRuntime: nginxAppRuntime
        );

        MakefileDryRunFinder nginxDryrun = new(
            appPath: nginxPath,
            kraftfilePath: Path.Combine(nginxPath, "Kraftfile"),
            kraftTarget: "--plat qemu --arch x86_64",
            targetAppRuntime: nginxAppRuntime
        );

        // -------------------------------------------- NODE 18 ---------------------------------------------------------------------

        BincompatHelper nodeAppRuntime = new(
            kraftfilePath: Path.Combine(nodePath, "Kraftfile"),
            kraftTarget: "--plat qemu --arch x86_64",
            appPath: nodePath,
            buildScriptFileName: CompilerTrapBincompatFinder.ElfBuildScriptFileName,
            kconfigFilePath: Path.Combine(nodePath, ".config.nginx_qemu-x86_64")
        );

        CompilerTrapBincompatFinder nodeCT = new(
            trapCompilerPath: SourcesTestEnv.TrapExePath,
            appPath: nodePath,
            resultsFilePath: SourcesTestEnv.sourcesResultsFilePath,
            kraftfilePath: Path.Combine(nodePath, "Kraftfile"),
            kraftTarget: "--plat qemu --arch x86_64",
            hostCompilerPath: SourcesTestEnv.hostCompilerPath,
            hostLinkerPath: SourcesTestEnv.hostCompilerPath,
            targetAppRuntime: nodeAppRuntime
        );

        MakefileDryRunFinder nodeDryrun = new(
            appPath: nginxPath,
            kraftfilePath: Path.Combine(nginxPath, "Kraftfile"),
            kraftTarget: "--plat qemu --arch x86_64",
            targetAppRuntime: nodeAppRuntime
        );

        var nginxCTRes = nginxCT.FindSources();
        if (!nginxCTRes.IsSuccess)
        {
            Assert.Fail($"Compiler trap bincompat failed for {nginxCT.AppPath} with status {nginxCTRes.Error}");
        }
        SourcesTestEnv.IsolateCompilerTrapResultsFile(nginxCT, Path.Combine(this.GetType().ToString(), "nginx"));

        var nodeCTRes = nodeCT.FindSources();
        if (!nodeCTRes.IsSuccess)
        {
            Assert.Fail($"Compiler trap bincompat failed for {nodeCT.AppPath} with status {nodeCTRes.Error}");
        }
        SourcesTestEnv.IsolateCompilerTrapResultsFile(nodeCT, Path.Combine(this.GetType().ToString(), "node"));

        var nodeDryrunRes = nodeDryrun.FindSources();
        if (!nodeDryrunRes.IsSuccess)
        {
            Assert.Fail($"Makefile dry run bincompat failed for {nodeDryrun.AppPath} with status {nodeDryrunRes.Error}");
        }

        var nginxDryrunRes = nginxDryrun.FindSources();
        if (!nginxDryrunRes.IsSuccess)
        {
            Assert.Fail($"Makefile dry run bincompat failed for {nginxDryrun.AppPath} with status {nginxDryrunRes.Error}");
        }
        
        CustomAssertsSources.TestMultipleSourceFinders(
            nginxCTRes.Value,
            "Nginx compiler trap", 
            nginxDryrunRes.Value, 
            "Nginx makefile dry run"
        );

        // CustomAssertsSources.TestMultipleSourceFinders(
        //     nodeCTRes.Value,
        //     "Node compiler trap", 
        //     nodeDryrunRes.Value, 
        //     "Node makefile dry run"
        // );

        // CustomAssertsSources.TestMultipleSourceFinders(
        //     nginxCTRes.Value,
        //     "Nginx compiler trap", 
        //     nodeCTRes.Value, 
        //     "Node compiler trap"
        // );

    }
}
