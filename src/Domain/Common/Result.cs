namespace Aerarium.Domain.Common;

public sealed record Result<T>
{
    private Result(T value) { Value = value; IsSuccess = true; Error = null; }
    private Result(string error) { Value = default; IsSuccess = false; Error = error; }

    public T? Value { get; }
    public bool IsSuccess { get; }
    public string? Error { get; }
    public bool IsFailure => !IsSuccess;

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);
}
