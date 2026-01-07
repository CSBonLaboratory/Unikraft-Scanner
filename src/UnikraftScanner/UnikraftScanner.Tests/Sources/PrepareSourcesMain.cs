namespace UnikraftScanner.Tests;

using System.Diagnostics;
using Xunit.Abstractions;
using System.Reflection;
using UnikraftScanner.Client.Sources;

public class PrepCompilableSourcesEnvFixture : IDisposable
{
    private string prevPWD;
    private readonly IMessageSink diagnosticMessageSink;

    // abosulte path to the trap binary
    public string TrapExePath { get; private set; }

    // latests git commit for the Unikraft App Catalog repo that the framework tests
    private const string unikraftCatalogComitHash = "5a22d73";
    private const string unikraftCatalogUrl = "https://github.com/unikraft/catalog.git";

    // absolute path of the testing framework, the binary is put in the bin/<Debug/Release>/net<version>/
    public static readonly string unikraftScannerTestsRootPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../../..");

    // absolute path of the source code project
    public static readonly string unikraftScannerClientRootPath = Path.Combine(unikraftScannerTestsRootPath, "../UnikraftScanner.Client");

    // path to directory containing source code of the compiler trap
    public static readonly string sourceTrapDirectoryPath = Path.Combine(unikraftScannerClientRootPath, "Sources/CompilerTrap");

    // path in the UnikraftScanner.Client of the future trap binary passed to Makefile
    public static readonly string originalExeTrapPath = Path.Combine(unikraftScannerClientRootPath, "bin/ClangTrap");

    // path in the UnikraftScanner.Tests where the trap and other auxiliary files will be moved and used
    public static readonly string testArtifactsDirectoryPath = Path.Combine(unikraftScannerTestsRootPath, "Sources/.artifacts");

    // path in the UnikraftScanner.Tests of the trap binary in the testing framework, adding the time of its build for easier verification
    static readonly string testExeTrapPath = Path.Combine(testArtifactsDirectoryPath, $"TestTrapCompiler_{DateTime.Now.ToString("y.M.d_H.m.s")}");

    // path in the UnikraftScanner.Tests of the file that will contain results from trap execution, that will contain all the sources used in Unikraft compilation
    public readonly string sourcesResultsFilePath = Path.Combine(testArtifactsDirectoryPath, $"trap_results_{DateTime.Now.ToString("y.M.d_H.m.s")}.txt");

    // path in the UnikraftScanner.Tests used to clone the unikraft app catalog to
    public static readonly string unikraftCatalogDeployPath = Path.Combine(unikraftScannerTestsRootPath, "Sources/.artifacts/");

    // path in the UnikraftScanner.Tests where the Unikraft App Catalog has the root repo directory
    public readonly string unikraftCatalogRootPath = Path.Combine(unikraftCatalogDeployPath, "catalog");

    // path to compiler used to build the trap
    public readonly string compilerForTrapPath = "/usr/bin/clang++-18";

    // path of the compiler used normally to build Unikraft that is proxied by the trap
    public readonly string hostCompilerPath = "/usr/bin/gcc";

    // path to a kraft binary with debug symbols used as an optimization for resource fetching
    // instead of a normal kraft binary that will also do an additional redundant compilation
    // if that binary exists the test envrinonment initializer won't rebuild kraftkit as in 
    // https://unikraft.org/docs/cli/hacking#using-a-developer-environment
    // since it is very costly
    public readonly string originalKraftkitPath = Path.Combine(unikraftScannerClientRootPath, "submodules", "kraftkit", "dist", "kraft");
    public readonly string gdbHackScriptPath = Path.Combine(unikraftScannerClientRootPath, "Sources", "ResourceFetcher", "GDBFetchHack.py");

    
    public void IsolateCompilerTrapResultsFile(CompilerTrapFinder sourceFinder, string uniqueSufix)
    {
        // how do we preserve the results files of trap-based source file finders when the trap cannot change the results file once it wa compiled ?
        // consider the results file that the trap writes to as a default file that we later rename and move to a specific folder
        // that characterizes the test + target
        // now we can easily compare results from different targets without recompiling the trap multiple times to only change the results file
        new FileInfo(Path.Combine(
            PrepCompilableSourcesEnvFixture.testArtifactsDirectoryPath, 
            uniqueSufix
            )
        ).Directory.Create();

        
        File.Move(sourceFinder.ResultsFilePath, Path.Combine(
            PrepCompilableSourcesEnvFixture.testArtifactsDirectoryPath,
            uniqueSufix
            )
        );
    }
    private void CloneUnikraftCatalog()
    {
        Directory.SetCurrentDirectory(unikraftCatalogDeployPath);

        Process initWorkdir = new Process();
        initWorkdir.StartInfo.FileName = "git";
        initWorkdir.StartInfo.Arguments = $" clone {unikraftCatalogUrl}";
        initWorkdir.StartInfo.RedirectStandardError = true;
        initWorkdir.Start();

        string stderr = initWorkdir.StandardError.ReadToEnd();

        initWorkdir.WaitForExit();

        if (initWorkdir.ExitCode != 0)
        {
            throw new InvalidOperationException($"Cloning the catalog repo exited with code {initWorkdir.ExitCode}\n{stderr}");
        }

        initWorkdir.Close();
    }

    private void FreezeUnikraftCatalogVersion()
    {
        Directory.SetCurrentDirectory(unikraftCatalogRootPath);

        Process freezeVersion = new Process();
        freezeVersion.StartInfo.FileName = "git";
        freezeVersion.StartInfo.Arguments = $" checkout {unikraftCatalogComitHash}";
        freezeVersion.StartInfo.RedirectStandardError = true;
        freezeVersion.Start();

        string stderr = freezeVersion.StandardError.ReadToEnd();
        freezeVersion.WaitForExit();

        if (freezeVersion.ExitCode != 0)
        {
            throw new InvalidOperationException($"Checking out to commit {unikraftCatalogComitHash} exited with code {freezeVersion.ExitCode}\n{stderr}");
        }

        freezeVersion.Close();
    }

    private void BuildCompilerTrap()
    {
        Directory.SetCurrentDirectory(sourceTrapDirectoryPath);

        Process trapMaker = new Process();
        trapMaker.StartInfo.FileName = "make";
        trapMaker.StartInfo.Arguments =  $" CXX={compilerForTrapPath}";
        trapMaker.StartInfo.Arguments += $" COMPILER_TRAP_BINARY={originalExeTrapPath}";
        trapMaker.StartInfo.Arguments += $" HOST_COMPILER_ARG={hostCompilerPath}";
        trapMaker.StartInfo.Arguments += $" RESULTS_FILE_PATH_ARG={sourcesResultsFilePath}";
        trapMaker.StartInfo.RedirectStandardError = true;

        trapMaker.Start();

        string stderr = trapMaker.StandardError.ReadToEnd();

        trapMaker.WaitForExit();

        if (trapMaker.ExitCode != 0)
        {
            throw new InvalidOperationException(
                @$"Building compiler trap exited with code {trapMaker.ExitCode}\n{stderr}");
        }

        trapMaker.Close();

    }
    public PrepCompilableSourcesEnvFixture(IMessageSink diagnosticMessageSink)
    {

        this.prevPWD = Directory.GetCurrentDirectory();

        CloneUnikraftCatalog();

        FreezeUnikraftCatalogVersion();

        BuildCompilerTrap();

        // move the trap binary to UnikraftScanner.Tests project
        File.Move(
            sourceFileName: originalExeTrapPath,
            destFileName: testExeTrapPath,
            overwrite: true
        );

        this.TrapExePath = testExeTrapPath;
    }

    public void KraftBuildUnikraftAppOrLib(
        string relPathInCatalog,
        string kraftBuildCmd = "build -g --no-cache --no-update --plat qemu --plat qemu --arch x86_64")
    {
        Directory.SetCurrentDirectory(Path.Combine(unikraftCatalogRootPath, relPathInCatalog));

        Process kraftBuilder = new Process();
        kraftBuilder.StartInfo.FileName = "kraft";
        kraftBuilder.StartInfo.Arguments = $" {kraftBuildCmd}";
        kraftBuilder.StartInfo.RedirectStandardError = true;

        kraftBuilder.Start();

        string stderr = kraftBuilder.StandardError.ReadToEnd();

        kraftBuilder.WaitForExit();

        if (kraftBuilder.ExitCode != 0)
        {
            throw new InvalidOperationException(
                @$"Building Unikraft app at {Directory.GetCurrentDirectory()} using command \
                `{kraftBuildCmd}` exited with code {kraftBuilder.ExitCode}\n{stderr}");
        }

        kraftBuilder.Close();

    }
    public void Dispose()
    {
        Directory.SetCurrentDirectory(prevPWD);
    }
}

[CollectionDefinition(nameof(SourcesFinderTestParallel))]
public class SourcesFinderTestParallel : ICollectionFixture<PrepCompilableSourcesEnvFixture>{}
