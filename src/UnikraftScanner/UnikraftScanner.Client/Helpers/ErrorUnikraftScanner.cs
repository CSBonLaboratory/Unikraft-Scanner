namespace UnikraftScanner.Client.Helpers;

public interface IErrorUnikraftScanner
{
    public ErrorTypes GetErrorType();
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
    virtual public ErrorTypes GetErrorType()
    {
        return ErrCode;
    }
}
