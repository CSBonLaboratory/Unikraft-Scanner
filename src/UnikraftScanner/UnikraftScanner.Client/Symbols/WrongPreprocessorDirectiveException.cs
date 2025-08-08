namespace UnikraftScanner.Client;

using System;

public class WrongPreprocessorDirectiveException : Exception
{
    public WrongPreprocessorDirectiveException()
    {
    }

    public WrongPreprocessorDirectiveException(string message)
        : base(message)
    {
    }

    public WrongPreprocessorDirectiveException(string message, Exception inner)
        : base(message, inner)
    {
    }
}