using Castle.DynamicProxy;

namespace Core.Aspects.Autofac;

/// <summary>
/// Tek bir aspect'in davranışı. Y-30 gereği Business servis metotları her zaman
/// Task&lt;IResult&gt; ailesinden bir tip döndürdüğü için yalnızca bu şekli desteklemek yeterlidir.
/// </summary>
public interface IAspectHandler
{
    Task<TResult> HandleAsync<TResult>(IInvocation invocation, AspectAttribute attribute, Func<Task<TResult>> next);
}
