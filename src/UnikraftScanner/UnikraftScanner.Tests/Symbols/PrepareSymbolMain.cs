namespace UnikraftScanner.Tests;

using UnikraftScanner.Client;
using Xunit;
using System.IO;
using System.Diagnostics;
using System.Reflection;
using UnikraftScanner.Client.Helpers;
using System.Text;

public class AssertSymbolEngine : Assert
{
    public static void TestSymbolEngineBlockResults(List<CompilationBlock> expected, List<CompilationBlock> actual, bool failAfterFirst = false)
    {
        StringBuilder cachedErrorMsg = new();

        if (expected.Count != actual.Count)
        {
            cachedErrorMsg.Append($"Lists have different lengths: expected {expected.Count} VS actual {actual.Count}\n");

            cachedErrorMsg.Append("Expected: ");
            foreach (CompilationBlock eb in expected)
            {
                cachedErrorMsg.Append($"{eb}\n");
            }


            cachedErrorMsg.Append("Actual: ");

            foreach (CompilationBlock ab in actual)
            {
                cachedErrorMsg.Append($"{ab}\n");
            }
            Assert.Fail(cachedErrorMsg.ToString());
        }

        bool foundError = false;
        for (int i = 0; i < expected.Count; i++)
        {
            if (!expected[i].Equals(actual[i]))
            {
                foundError = true;
                cachedErrorMsg.Append($"Compilation block at index {i} different:\n Expected \n{expected[i]} \n Actual \n{actual[i]}\n");
                if (failAfterFirst)
                    Assert.Fail(cachedErrorMsg.ToString());
            }
        }

        if (foundError)
            Assert.Fail(cachedErrorMsg.ToString());

    }

    public static void TestUniversalLinesOfCode(int expected, List<int> expectedUniversalLineIdxs,  int actual, List<int> actualUniveralLineIdxs)
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

    public InterceptorOptions Opts { get; init; }

    public PrepSymbolTestEnv()
    {


        // plugin path is hardcoded here and in ./dependencies/prepare.sh
        Opts = new InterceptorOptions(
            compilerPath: "/usr/bin/clang++-18",
            pluginPath: "./dependencies/TestPluginBlockFinder.so",
            interceptionResFilePath: "./dependencies/results.txt"
        );

        prevPWD = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "../../../Symbols/"));

        Process prepareTestEnvironment = new Process();
        prepareTestEnvironment.StartInfo.FileName = "./dependencies/prepare.sh";
        prepareTestEnvironment.StartInfo.RedirectStandardOutput = true;
        prepareTestEnvironment.StartInfo.RedirectStandardError = true;

        prepareTestEnvironment.Start();

        // Console.WriteLine(prepareTestEnvironment.StandardOutput.ReadToEnd());
        Console.WriteLine(prepareTestEnvironment.StandardError.ReadToEnd());

        //prepareTestEnvironment.WaitForExit();

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
