namespace UnikraftScanner.Tests;
using Xunit;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using Xunit.Abstractions;
using UnikraftScanner.Client.Symbols;
using System.Collections.Concurrent;

public class PrepSymbolTestEnvFixture : IDisposable
{
    private string prevPWD;
    private readonly IMessageSink diagnosticMessageSink;
    public PluginOptions Opts { get; init; }

    // absolute path of the testing framework, the binary is put in the bin/<Debug/Release>/net<version>/
    static readonly string unikraftScannerTestsRootPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../../..");

    // absolute path of the source code project
    static readonly string unikraftScannerClientRootPath = Path.Combine(unikraftScannerTestsRootPath, "../UnikraftScanner.Client");

    // path to where the plugin build will generate the plugin binary used to build the plugin
    static readonly string originalExePluginPath = Path.Combine(unikraftScannerClientRootPath, "Symbols/Plugin/bin/BlockFinderPlugin.so");

    // compiler used together with the plugin to intercept compilation blocks and their symbols during Unikraft build step
    static readonly string unikraftCompilerPath = "/usr/bin/clang++-18";

    // compiler used to build the plugin itself
    static readonly string compilerForPlugin = "/usr/bin/clang++-18";

    // path in the UnikraftScanner.Tests where the plugin and other auxiliary files will be moved and used
    static readonly string testArtifactsDirectoryPath = Path.Combine(unikraftScannerTestsRootPath, "Symbols/.artifacts");

    // path in the UnikraftScanner.Tests of the plugin binary in the testing framework, adding the time of its build for easier verification
    static readonly string testExePluginPath = Path.Combine(testArtifactsDirectoryPath, $"TestPluginBlockFinder_{DateTime.Now.ToString("y.M.d_H.m.s")}.so");

    // path in the UnikraftScanner.Tests of the file that will contain results from plugin execution, that will be consumed by SymbolEngine
    static readonly string resultsFilePath = Path.Combine(testArtifactsDirectoryPath, $"discovery_results_{DateTime.Now.ToString("y.M.d_H.m.s")}.txt");

    // path in UnikraftScanner.Client the contains the source code for the plugin
    static readonly string sourcePluginDirectoryPath = Path.Combine(unikraftScannerClientRootPath, "Symbols/Plugin");

    // these 2 are passed to the Makefile that builds the plugin and used in the FrontEndRegistry::Add<T> Clang SDK
    static readonly string registryNameArg = "TestCompilationBlockFinder";
    static readonly string registryDescriptionArg = "lorem_ipsum_gigel";

    public void CleanupBeforeTest()
    {
        // remove moved plugin binary and the results file
        DirectoryInfo artifactDir = new DirectoryInfo(testArtifactsDirectoryPath);

        foreach (FileInfo file in artifactDir.GetFiles())
        {
            file.Delete(); 
        }
    }

    public PrepSymbolTestEnvFixture(IMessageSink diagnosticMessageSink)
    {
        this.diagnosticMessageSink = diagnosticMessageSink;

        prevPWD = Directory.GetCurrentDirectory();

        // go to location of the source for plugin
        Directory.SetCurrentDirectory(sourcePluginDirectoryPath);

        // compile the plugin itself and pass compiler used, its name and description and where to generate the binary before move
        // these args are passed to the Makefile
        Process buildPlugin = new Process();
        buildPlugin.StartInfo.FileName   = "make";
        buildPlugin.StartInfo.Arguments  = $" CXX={compilerForPlugin}";
        buildPlugin.StartInfo.Arguments += $" REGISTRY_NAME_ARG={registryNameArg}";
        buildPlugin.StartInfo.Arguments += $" REGISTRY_DESCRIPTION_ARG=\"{registryDescriptionArg}\"";
        buildPlugin.StartInfo.Arguments += $" PLUGIN_BINARY={originalExePluginPath}";
          
        buildPlugin.StartInfo.RedirectStandardOutput = true;
        buildPlugin.StartInfo.RedirectStandardError = true;

        buildPlugin.Start();

        string makeStdErr = buildPlugin.StandardError.ReadToEnd();

        buildPlugin.WaitForExit();

        if (buildPlugin.ExitCode != 0)
        {
            throw new InvalidOperationException($"Make command for building plugin failed with exit code {buildPlugin.ExitCode}:\n {makeStdErr}");
        }

        buildPlugin.Close();

        // move the plugin binary to UnikraftScanner.Tests project
        File.Copy(
            sourceFileName: originalExePluginPath,
            destFileName: testExePluginPath,
            overwrite: true
        );
        
        // prepare options params for plugin execution inside concrete tests
        Opts = new PluginOptions(
            CompilerPath: unikraftCompilerPath,
            PluginPath: testExePluginPath,
            PluginName: registryNameArg,
            InterceptionResultsFilePath_External_PluginParam: resultsFilePath,
            Stage_RetainExcludedBlocks_Internal_PluginParam: PluginStage.Discovery
        );

    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(prevPWD);
    }

}

[CollectionDefinition(nameof(SymbolEngineTestParallel))]
public class SymbolEngineTestParallel : ICollectionFixture<PrepSymbolTestEnvFixture>{}

public class ParalelTriggerStageCacheHelper
{
    // class used for trigger stage tests that use same source file as input
    // it does the discovery stage only once and caches the result for other tests that are run in paralel
    private static ConcurrentDictionary<string, Lazy<KeyValuePair<SymbolEngine, SymbolEngine.DiscoveryResDTO>>> cachedDiscoveryInputs = new();
    
    // prepare discovery stage for tests that use same input file
    private KeyValuePair<SymbolEngine, SymbolEngine.DiscoveryResDTO> PrepareDiscoveryOnlyOnce(string inputPath, PrepSymbolTestEnvFixture symbolTestEnv)
    {
        // https://andrewlock.net/making-getoradd-on-concurrentdictionary-thread-safe-using-lazy/
        var discoveryResult = ParalelTriggerStageCacheHelper.cachedDiscoveryInputs.GetOrAdd(inputPath,
            x => new Lazy<KeyValuePair<SymbolEngine, SymbolEngine.DiscoveryResDTO>>(
                () =>
                {

                    SymbolEngine engine = new SymbolEngine();
                    var discoveryResult = engine.DiscoverCompilationBlocksAndLines(
                        inputPath,
                        opts: symbolTestEnv.Opts,
                        targetCompilationCommand: $"{symbolTestEnv.Opts.CompilerPath} -I/usr/include -c {inputPath}"
                    );

                    if (!discoveryResult.IsSuccess)
                    {
                        Assert.Fail(
                        $"Failed with custom error: {discoveryResult.Error}"
                        );
                    }

                    return new KeyValuePair<SymbolEngine, SymbolEngine.DiscoveryResDTO>(engine, discoveryResult.Value);
                }));

        return discoveryResult.Value;
    }

    public void RunTriggerTest(string inputPath, string defineSymbolsCmd, int[] expectedBlockIdxs, PrepSymbolTestEnvFixture symbolTestEnv, string overWriteResultsFilename)
    {
        KeyValuePair<SymbolEngine, SymbolEngine.DiscoveryResDTO> discoveredEnv = PrepareDiscoveryOnlyOnce(inputPath, symbolTestEnv);

        // prepare for Trigger Stage
        PluginOptions newOpts = new PluginOptions(
            CompilerPath: symbolTestEnv.Opts.CompilerPath,
            PluginPath: symbolTestEnv.Opts.PluginPath,
            PluginName: symbolTestEnv.Opts.PluginName,

            // if we use the same file from the discovery stage we will have a bottleneck since all parallel tests that use the same source code as input
            //  will try to write to it
            // this is why every Trigger stage test uses a results file that has the name of the test class

            InterceptionResultsFilePath_External_PluginParam: overWriteResultsFilename,
            Stage_RetainExcludedBlocks_Internal_PluginParam: PluginStage.Trigger
        );


        var actualResult = discoveredEnv.Key.TriggerCompilationBlocks(
            discoveredEnv.Value.Blocks,
            newOpts,
            $"{symbolTestEnv.Opts.CompilerPath} {defineSymbolsCmd} -I/usr/include -c {inputPath}"
        );


        if (!actualResult.IsSuccess)
        {
            Assert.Fail(
                $"Failed with custom error: {actualResult.Error}"
            );
        }

        AssertSymbolEngine.TestTriggeredIndexes(actualResult.Value, expectedBlockIdxs);
    }
}
