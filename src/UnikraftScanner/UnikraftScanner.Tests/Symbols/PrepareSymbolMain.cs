namespace UnikraftScanner.Tests;

using UnikraftScanner.Client;
using Xunit;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Xunit.Abstractions;

public class AssertSymbolEngine : Assert
{

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
            TestCustomLists(e, a, $"Test lines of code in block with index {i}");
        }
    }
    public static void TestCustomLists<T>(List<T> expected, List<T> actual, string banner, bool failAfterFirst = false)
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
                foundError = true;
                cachedErrorMsg.Append($"Element at index {i} different:\n Expected \n{expected[i]} \n Actual \n{actual[i]}\n");
                if (failAfterFirst)
                    Assert.Fail(cachedErrorMsg.ToString());
            }
        }

        if (foundError)
            Assert.Fail(cachedErrorMsg.ToString());

    }

    public static void TestUniversalLinesOfCode(int expected, List<int> expectedUniversalLineIdxs, int actual, List<int> actualUniveralLineIdxs)
    {

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

public class PrepSymbolTestEnv : IDisposable
{
    private string prevPWD;

    private readonly IMessageSink diagnosticMessageSink;

    public PluginOptions Opts { get; init; }

    public PrepSymbolTestEnv(IMessageSink diagnosticMessageSink)
    {

        this.diagnosticMessageSink = diagnosticMessageSink;

        string unikraftTestsProjectRootPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../../..");

        string unikraftClientRootPath = Path.Combine(unikraftTestsProjectRootPath, "../UnikraftScanner.Client");

        Opts = new PluginOptions(
            compilerPath: "/usr/bin/clang-18",
            pluginPath: Path.Combine(unikraftTestsProjectRootPath, "Symbols/dependencies/TestPluginBlockFinder.so"),
            interceptionResFilePath: Path.Combine(unikraftTestsProjectRootPath, "Symbols/dependencies/discovery_results.txt")
        );

        // used for discovery stage when we need to find all blocks, this argument is passed as plugin param when executed
        Opts.RetainExcludedBlocks_PluginParam = true;

        prevPWD = Directory.GetCurrentDirectory();

        // compile the plugin from source file found inside the Unikraft Scanner client project
        Directory.SetCurrentDirectory(
            Path.Combine(
                unikraftClientRootPath,
                "Symbols")
        );

        Process prepareTestEnvironment = new Process();
        prepareTestEnvironment.StartInfo.FileName = "make";
        prepareTestEnvironment.StartInfo.RedirectStandardOutput = true;
        prepareTestEnvironment.StartInfo.RedirectStandardError = true;

        prepareTestEnvironment.Start();

        // Console.WriteLine(prepareTestEnvironment.StandardOutput.ReadToEnd());
        Console.WriteLine(prepareTestEnvironment.StandardError.ReadToEnd());

        // move the plugin library to tests project
        File.Copy(
            sourceFileName: Path.Combine(unikraftClientRootPath, "Symbols/bin/BlockFinderPlugin.so"),
            destFileName: Opts.PluginPath,
            overwrite: true
        );

    }

    public void Dispose()
    {

        //File.Delete("./dependencies/TestPluginBlockFinder.so");

        Directory.SetCurrentDirectory(prevPWD);

    }

}

public class BaseSymbolTest : IClassFixture<PrepSymbolTestEnv>{

    protected PrepSymbolTestEnv SymbolTestEnv {get; set;}

    public BaseSymbolTest(PrepSymbolTestEnv env){
        SymbolTestEnv = env;
    }

}
