using UnikraftScanner.Client;
using UnikraftScanner.Client.Helpers;
public interface IUnikraftAppResourceFetchingStrategy
{
    public ResultUnikraftScanner<bool> ApplyStrategy(BincompatContext ctx);
}