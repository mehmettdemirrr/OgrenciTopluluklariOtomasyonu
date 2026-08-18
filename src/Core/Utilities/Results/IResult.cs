namespace Core.Utilities.Results;

public interface IResult
{
    bool IsSuccess { get; }

    ResultStatus Status { get; }

    string? Message { get; }
}
