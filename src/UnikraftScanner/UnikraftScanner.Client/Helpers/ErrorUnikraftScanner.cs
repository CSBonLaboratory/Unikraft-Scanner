namespace UnikraftScanner.Client.Helpers;

public interface IErrorUnikraftScanner
{
    public ErrorTypes GetErrorType();

    public object GetData();
}
public class ErrorUnikraftScanner<F> : IErrorUnikraftScanner
{
    public ErrorUnikraftScanner(F data, ErrorTypes errCode)
    {
        Data = data;

        ErrCode = errCode;
    }

    public ErrorTypes ErrCode;
    public F Data { get; init; }
    public ErrorTypes GetErrorType()
    {
        return ErrCode;
    }

    public object GetData() => Data;

    override public string ToString()
    {
        return $"Type: {ErrCode}\n Content: {Data}";
    }
}
