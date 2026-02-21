namespace ConsignadoHub.BuildingBlocks.Results;

public sealed class Result<T>
{
    public T? Value { get; }
    public Error Error { get; }
    public bool IsSuccess => Error.IsNone;
    public bool IsFailure => !IsSuccess;

    private Result(T value)
    {
        Value = value;
        Error = Error.None;
    }

    private Result(Error error)
    {
        Value = default;
        Error = error;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Error error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure)
        => IsSuccess ? onSuccess(Value!) : onFailure(Error);
}

public sealed class Result
{
    public Error Error { get; }
    public bool IsSuccess => Error.IsNone;
    public bool IsFailure => !IsSuccess;

    private Result(Error error)
    {
        Error = error;
    }

    private static readonly Result _success = new(Error.None);

    public static Result Success() => _success;
    public static Result Failure(Error error) => new(error);

    public static implicit operator Result(Error error) => Failure(error);
}
