using System.Text;

namespace UnikraftScanner.Tests;

public class CustomAssertsSources : Assert
{

    public static void TestMultipleSourceFinders(List<string> finder1, string banner1, List<string> finder2, string banner2)
    {
        HashSet<string> ans1 = new(finder1);
        HashSet<string> ans2 = new(finder2);
        
        if(ans1.SetEquals(ans2))
            return;

        IEnumerable<string> diff1 = ans1.Except(ans2);

        StringBuilder msg = new();
        msg.Append($"Finder 1 ({banner1}) has different result than finder 2 ({banner2})");
        foreach(string s1 in diff1)
        {
            msg.Append(s1);
            msg.Append("\n");
        }

        IEnumerable<string> diff2 = ans2.Except(ans1);
        msg.Append($"Finder 2 ({banner2}) has different result than finder 1 ({banner1})");
        foreach(string s2 in diff2)
        {
            msg.Append(s2);
            msg.Append("\n");
        }

        Assert.Fail(msg.ToString());
    }
}
