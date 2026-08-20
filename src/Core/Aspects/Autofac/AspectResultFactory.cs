using Core.Utilities.Results;

namespace Core.Aspects.Autofac;

/// <summary>
/// Aspect handler'ların, intercept ettikleri metodun gerçek dönüş tipine (IResult veya
/// IDataResult&lt;T&gt;) göre doğru başarısızlık sonucunu reflection ile üretmesi için ortak yardımcı.
/// Y-30 gereği Business servisleri her zaman bu iki şekilden birini döner.
/// </summary>
internal static class AspectResultFactory
{
    public static TResult Build<TResult>(string factoryMethodName, string message)
    {
        var resultType = typeof(TResult);

        if (resultType == typeof(IResult))
        {
            var method = typeof(Result).GetMethod(factoryMethodName, [typeof(string)])
                ?? throw new InvalidOperationException($"Result.{factoryMethodName}(string) bulunamadı.");
            return (TResult)method.Invoke(null, [message])!;
        }

        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(IDataResult<>))
        {
            var dataType = resultType.GetGenericArguments()[0];
            var method = typeof(DataResult<>).MakeGenericType(dataType).GetMethod(factoryMethodName, [typeof(string)])
                ?? throw new InvalidOperationException($"DataResult<{dataType.Name}>.{factoryMethodName}(string) bulunamadı.");
            return (TResult)method.Invoke(null, [message])!;
        }

        throw new InvalidOperationException(
            $"Aspect sonucu {resultType.Name} dönüş tipini desteklemiyor. Yalnızca IResult/IDataResult<T> desteklenir.");
    }
}
