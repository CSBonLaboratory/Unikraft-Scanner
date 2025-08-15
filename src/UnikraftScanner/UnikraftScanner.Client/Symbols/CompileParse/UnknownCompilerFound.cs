namespace UnikraftScanner.Client;

public class UnknownCompilerFound : InvalidOperationException
{
    public UnknownCompilerFound()
    {
    }

    public UnknownCompilerFound(string message)
        : base(message)
    {
    }

    public UnknownCompilerFound(string message, Exception inner)
        : base(message, inner)
    {
    }
}
