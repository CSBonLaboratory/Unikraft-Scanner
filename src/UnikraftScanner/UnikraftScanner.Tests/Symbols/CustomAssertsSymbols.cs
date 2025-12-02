namespace UnikraftScanner.Tests;
using System.Text;
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
