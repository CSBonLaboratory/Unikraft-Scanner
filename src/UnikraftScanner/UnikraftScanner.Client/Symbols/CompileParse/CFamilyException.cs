namespace UnikraftScanner.Client;

public class CFamilyException : InvalidOperationException
{
    public CFamilyException()
    {
    }

    public CFamilyException(string message)
        : base(message)
    {
    }

    public CFamilyException(string message, Exception inner)
        : base(message, inner)
    {
    }
}