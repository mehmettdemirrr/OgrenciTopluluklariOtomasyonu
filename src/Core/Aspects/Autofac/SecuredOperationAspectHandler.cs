using Castle.DynamicProxy;
using Core.Utilities.Results;
using Core.Utilities.Security;

namespace Core.Aspects.Autofac;

/// <summary>
/// docs/MIMARI.md · A-10/Y-23: yalnızca token'daki izin claim'ini kontrol eder, kaynak
/// sahipliğine bakmaz (o Business'ın işi). Y-33: ICurrentUser constructor'dan gelir,
/// service locator yalnızca <see cref="AspectDispatchInterceptor"/>'da kalır.
/// </summary>
public sealed class SecuredOperationAspectHandler(ICurrentUser currentUser) : IAspectHandler
{
    public Task<TResult> HandleAsync<TResult>(IInvocation invocation, AspectAttribute attribute, Func<Task<TResult>> next)
    {
        var permission = ((SecuredOperationAttribute)attribute).Permission;

        if (currentUser.IsAuthenticated && currentUser.Permissions.Contains(permission))
        {
            return next();
        }

        var message = $"Bu işlem için '{permission}' izni gerekiyor.";
        return Task.FromResult(BuildForbiddenResult<TResult>(message));
    }

    // TResult, çağıran metodun Task<TResult> imzasından gelen gerçek dönüş tipi (IResult veya
    // IDataResult<T>) — Y-30 gereği Business servisleri başka bir şekil döndürmez.
    private static TResult BuildForbiddenResult<TResult>(string message)
    {
        var resultType = typeof(TResult);

        if (resultType == typeof(IResult))
        {
            return (TResult)(object)Result.Forbidden(message);
        }

        if (resultType.IsGenericType && resultType.GetGenericTypeDefinition() == typeof(IDataResult<>))
        {
            var dataType = resultType.GetGenericArguments()[0];
            var forbiddenMethod = typeof(DataResult<>).MakeGenericType(dataType).GetMethod(nameof(DataResult<object>.Forbidden))!;
            return (TResult)forbiddenMethod.Invoke(null, [message])!;
        }

        throw new InvalidOperationException(
            $"SecuredOperationAspectHandler, {resultType.Name} dönüş tipini desteklemiyor. Yalnızca IResult/IDataResult<T> desteklenir.");
    }
}
