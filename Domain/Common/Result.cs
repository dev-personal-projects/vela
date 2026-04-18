namespace FinanceTracker.Domain.Common;

public abstract class ResultBase
{
    public string? Error { get; }
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    protected ResultBase(string? error, bool isSuccess)
    {
        Error = error;
        IsSuccess = isSuccess;
    }
}

public sealed class Result<T> : ResultBase
{
    public T? Value { get; }

    private Result(T? value, string? error, bool isSuccess)
        : base(error, isSuccess) => Value = value;

    public static Result<T> Ok(T value) => new(value, null, true);
    public static Result<T> Fail(string error) => new(default, error, false);
}

public sealed class Result : ResultBase
{
    private Result(string? error, bool isSuccess)
        : base(error, isSuccess) { }

    public static Result Ok() => new(null, true);
    public static Result Fail(string error) => new(error, false);
}
