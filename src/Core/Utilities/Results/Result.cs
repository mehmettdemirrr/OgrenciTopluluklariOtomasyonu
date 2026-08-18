namespace Core.Utilities.Results;

public sealed class Result : IResult
{
    private Result(bool isSuccess, ResultStatus status, string? message)
    {
        IsSuccess = isSuccess;
        Status = status;
        Message = message;
    }

    public bool IsSuccess { get; }

    public ResultStatus Status { get; }

    public string? Message { get; }

    public static Result Success(string? message = null) => new(true, ResultStatus.Success, message);

    public static Result ValidationError(string message) => new(false, ResultStatus.ValidationError, message);

    public static Result Forbidden(string message) => new(false, ResultStatus.Forbidden, message);

    public static Result NotFound(string message) => new(false, ResultStatus.NotFound, message);

    public static Result Conflict(string message) => new(false, ResultStatus.Conflict, message);
}
