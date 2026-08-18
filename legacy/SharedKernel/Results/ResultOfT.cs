namespace SharedKernel.Results;

public sealed class Result<TValue> : Result
{
    private readonly TValue? _value;

    internal Result(TValue? value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Başarısız bir sonucun değerine erişilemez.");

    public static implicit operator Result<TValue>(TValue value) => Success(value);
}