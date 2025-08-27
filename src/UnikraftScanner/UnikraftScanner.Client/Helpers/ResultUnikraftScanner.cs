namespace UnikraftScanner.Client.Helpers;

public record ResultUnikraftScanner<T>
{
    public bool IsSuccess { get; }
    public IErrorUnikraftScanner Error { get; }
    public T? Value { get; }

    protected ResultUnikraftScanner(bool isSuccess, IErrorUnikraftScanner error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }
    public static ResultUnikraftScanner<T> Success() => new(isSuccess: true, null);
    public static ResultUnikraftScanner<T> PartialSuccess(IErrorUnikraftScanner error) => new(isSuccess: true, error);
    public static ResultUnikraftScanner<T> Failure(IErrorUnikraftScanner error) => new(isSuccess: false, error ?? throw new ArgumentNullException(nameof(error)));
    private ResultUnikraftScanner(T value) : this(true, null) => Value = value;
    private ResultUnikraftScanner(IErrorUnikraftScanner error) => Failure(error);

    public static implicit operator ResultUnikraftScanner<T>(T value) => new(value);
}
