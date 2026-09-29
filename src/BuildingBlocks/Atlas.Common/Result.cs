namespace Atlas.Common;

/// <summary>
/// Either a value (success) or an <see cref="Common.Error"/> (expected failure).
/// Thanks to the implicit conversions a use case can simply write
/// <c>return response;</c> or <c>return Error.NotFound();</c>.
/// </summary>
public sealed class Result<T>
{
    private Result(T value)
    {
        Value = value;
        IsSuccess = true;
    }

    private Result(Error error)
    {
        Error = error;
        IsSuccess = false;
    }

    public bool IsSuccess { get; }

    public T? Value { get; }

    public Error? Error { get; }

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);
}
