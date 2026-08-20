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
        return Task.FromResult(AspectResultFactory.Build<TResult>(nameof(Result.Forbidden), message));
    }
}
