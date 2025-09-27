namespace UnikraftScanner.Tests;
using Xunit;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Xunit.Abstractions;
using UnikraftScanner.Client.Symbols;
using System.Collections.Concurrent;

public class AssertSymbolEngine : Assert
{

    public static void TestTriggeredIndexes(int[] actual, int[] expected)
    {
        StringBuilder cachedErrorMsg = new();
        if (actual.Length != expected.Length)
        {
            cachedErrorMsg.Append($"List different sizes: Actual ({actual.Length}) VS Expected ({expected.Length})\n");
            cachedErrorMsg.Append("Actual:   ");
            foreach (int a in actual)
            {
                cachedErrorMsg.Append($" {a}");
            }

            cachedErrorMsg.Append("\n");
            cachedErrorMsg.Append("Expected: ");
            foreach (int e in expected)
            {
                cachedErrorMsg.Append($" {e}");
            }

            Assert.Fail(cachedErrorMsg.ToString());
        }

        for (int i = 0; i < expected.Length; i++)
        {
            if (actual[i] != expected[i])
            {
                cachedErrorMsg.Append("Actual: ");
                foreach (int a in actual)
                {
                    cachedErrorMsg.Append($" {a}");
                }

                cachedErrorMsg.Append("\n");
                cachedErrorMsg.Append("Expected: ");
                foreach (int e in expected)
                {
                    cachedErrorMsg.Append($" {e}");
                }

                Assert.Fail(cachedErrorMsg.ToString());
            }
        }
    }
    public static void TestLinesOfCodeInBlocks(List<List<int>> expected, List<List<int>> actual, bool failAfterFirst = false)
    {
        StringBuilder cachedErrorMsg = new();

        if (expected.Count != actual.Count)
        {
            cachedErrorMsg.Append($"Comparing lists of lists of code line idxs have different lengths: expected {expected.Count} VS actual {actual.Count}\n");

            Assert.Fail(cachedErrorMsg.ToString());
        }

        for (int i = 0; i < expected.Count; i++)
        {
            List<int> e = expected[i];
            List<int> a = actual[i];
            TestCustomLists(e, a, $"Test lines of code in block with index {i}", null, null);
        }
    }
    public static void TestCustomLists<T>(
        List<T> expected,
        List<T> actual,
        string banner,
        List<List<int>>? debugExpectedLoC, // used when comparing compilation blocks
        List<List<int>>? debugActualLoC, // used when comparing compilation blocks
        bool failAfterFirst = false
        )
    {
        StringBuilder cachedErrorMsg = new();

        cachedErrorMsg.Append($"{banner}\n");

        if (expected.Count != actual.Count)
        {
            cachedErrorMsg.Append($"Lists have different lengths: expected {expected.Count} VS actual {actual.Count}\n");

            cachedErrorMsg.Append("Expected: ");
            foreach (T eb in expected)
            {
                cachedErrorMsg.Append($"{eb} ");
            }

            cachedErrorMsg.Append("\n");
            cachedErrorMsg.Append("Actual: ");

            foreach (T ab in actual)
            {
                cachedErrorMsg.Append($"{ab} ");
            }
            Assert.Fail(cachedErrorMsg.ToString());
        }

        bool foundError = false;
        for (int i = 0; i < expected.Count; i++)
        {
            if (!expected[i].Equals(actual[i]))
            {

                cachedErrorMsg.Append($"Expected code line indexes for block index {i}: ");
                foreach (int eLoC in debugExpectedLoC[i])
                {
                    cachedErrorMsg.Append($"{eLoC} ");
                }

                cachedErrorMsg.Append($"\nActual code line indexes for block index {i}: ");
                foreach (int eLoC in debugActualLoC[i])
                {
                    cachedErrorMsg.Append($"{eLoC} ");
                }
                
                
                foundError = true;
                cachedErrorMsg.Append($"\nElement at index {i} different:\n Expected \n{expected[i]} \n Actual \n{actual[i]}\n");
                if (failAfterFirst)
                    Assert.Fail(cachedErrorMsg.ToString());
            
            }
        }

        if (foundError)
            Assert.Fail(cachedErrorMsg.ToString());

    }

    public static void TestUniversalLinesOfCode(List<int> expectedUniversalLineIdxs, List<int> actualUniveralLineIdxs)
    {
        int expected = expectedUniversalLineIdxs.Count;
        int actual = actualUniveralLineIdxs.Count;

        if (expected != actual || !expectedUniversalLineIdxs.SequenceEqual(actualUniveralLineIdxs))
        {
            StringBuilder cachedErrMsg = new($"Universal lines of code test failed: Expected {expected} VS Actual {actual}\n");
            cachedErrMsg.Append("Actual universal lines of code indexes:   ");
            foreach (int idx in actualUniveralLineIdxs)
                cachedErrMsg.Append($"{idx} ");

            cachedErrMsg.Append("\n");
            cachedErrMsg.Append("Expected universal lines of code indexes: ");

            foreach (int idx in expectedUniversalLineIdxs)
                cachedErrMsg.Append($"{idx} ");

            Assert.Fail(cachedErrMsg.ToString());
        }
        
    }
}

public class PrepSymbolTestEnvFixture : IDisposable
{
    private string prevPWD;

    private string unikraftTestsProjectRootPath;

    private readonly IMessageSink diagnosticMessageSink;

    public PluginOptions Opts { get; init; }

    public PrepSymbolTestEnvFixture(IMessageSink diagnosticMessageSink)
    {
        this.diagnosticMessageSink = diagnosticMessageSink;

        this.unikraftTestsProjectRootPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../../..");

        string unikraftClientRootPath = Path.Combine(unikraftTestsProjectRootPath, "../UnikraftScanner.Client");

        Opts = new PluginOptions(
            CompilerPath: "/usr/bin/clang-18",
            PluginPath: Path.Combine(unikraftTestsProjectRootPath, "Symbols/.artifacts/TestPluginBlockFinder.so"),
            InterceptionResultsFilePath_External_PluginParam: Path.Combine(unikraftTestsProjectRootPath, "Symbols/.artifacts/discovery_results.txt"),
            Stage_RetainExcludedBlocks_Internal_PluginParam: PluginStage.Discovery
        );

        prevPWD = Directory.GetCurrentDirectory();

        // compile the plugin from source file found inside the Unikraft Scanner client project
        Directory.SetCurrentDirectory(
            Path.Combine(
                unikraftClientRootPath,
                "Symbols/Plugin")
        );

        Process prepareTestEnvironment = new Process();
        prepareTestEnvironment.StartInfo.FileName = "make";
        prepareTestEnvironment.StartInfo.RedirectStandardOutput = true;
        prepareTestEnvironment.StartInfo.RedirectStandardError = true;

        prepareTestEnvironment.Start();

        prepareTestEnvironment.StandardError.ReadToEnd();

        prepareTestEnvironment.WaitForExit();

        if (prepareTestEnvironment.ExitCode != 0)
        {
            throw new InvalidOperationException($"Make command for building plugin failed with exit code {prepareTestEnvironment.ExitCode}");
        }
            
        // move the plugin library to tests project
        File.Copy(
            sourceFileName: Path.Combine(unikraftClientRootPath, "Symbols/Plugin/bin/BlockFinderPlugin.so"),
            destFileName: Opts.PluginPath,
            overwrite: true
        );

    }

    public void Dispose()
    {

        DirectoryInfo artifactDir = new DirectoryInfo(Path.Combine(unikraftTestsProjectRootPath, "Symbols/.artifacts"));

        foreach (FileInfo file in artifactDir.GetFiles())
        {
            file.Delete(); 
        }

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
            
            // if we use the same file from the discovery stage we will have a bottleneck since all parallel files will try to write to it
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
