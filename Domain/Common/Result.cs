namespace FinanceTracker.Domain.Common;

public abstract class AppResultBase
{
    public string? Error { get; }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    protected AppResultBase(string? error, bool isSuccess)
    {
        Error = error;
        IsSuccess = isSuccess;
    }
}

public sealed class AppResult<T> : AppResultBase
{
    public T? Value { get; }

    private AppResult(T? value, string? error, bool isSuccess)
        : base(error, isSuccess) => Value = value;

    public static AppResult<T> Ok(T value) => new(value, null, true);
    public static AppResult<T> Fail(string error) => new(default, error, false);
}

public sealed class AppResult : AppResultBase
{
    private AppResult(string? error, bool isSuccess)
        : base(error, isSuccess) { }

    public static AppResult Ok() => new(null, true);
    public static AppResult Fail(string error) => new(error, false);
}
