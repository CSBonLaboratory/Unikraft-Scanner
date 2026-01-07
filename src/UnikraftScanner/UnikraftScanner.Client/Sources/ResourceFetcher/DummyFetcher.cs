using UnikraftScanner.Client;
using UnikraftScanner.Client.Helpers;

public class DummyFetcher : IUnikraftAppResourceFetchingStrategy
{
    public ResultUnikraftScanner<bool> ApplyStrategy(BincompatContext ctx)
    {
        return (ResultUnikraftScanner<bool>)true;
    }
}