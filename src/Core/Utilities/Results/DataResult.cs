namespace Core.Utilities.Results;

public sealed class DataResult<T> : IDataResult<T>
{
    private DataResult(T data, bool isSuccess, ResultStatus status, string? message)
    {
        Data = data;
        IsSuccess = isSuccess;
        Status = status;
        Message = message;
    }

    public T Data { get; }

    public bool IsSuccess { get; }

    public ResultStatus Status { get; }

    public string? Message { get; }

    public static DataResult<T> Success(T data, string? message = null) => new(data, true, ResultStatus.Success, message);

    // Y-31 istisnası: IsSuccess=false olduğunda Data'nın anlamsız olması Result deseninin
    // kendi sözleşmesidir (çağıran taraf her zaman önce IsSuccess'e bakar). Burada susturulan
    // gerçek bir hata değil, C#'ın "T ama sadece bazen null" durumunu ifade edememesi.
    public static DataResult<T> ValidationError(string message) => new(default!, false, ResultStatus.ValidationError, message);

    public static DataResult<T> Forbidden(string message) => new(default!, false, ResultStatus.Forbidden, message);

    public static DataResult<T> NotFound(string message) => new(default!, false, ResultStatus.NotFound, message);

    public static DataResult<T> Conflict(string message) => new(default!, false, ResultStatus.Conflict, message);

    public static DataResult<T> Unauthorized(string message) => new(default!, false, ResultStatus.Unauthorized, message);
}
