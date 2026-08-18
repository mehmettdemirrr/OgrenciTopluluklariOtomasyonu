using System.Reflection;
using Autofac;
using Castle.DynamicProxy;

namespace Core.Aspects.Autofac;

/// <summary>
/// Bir arayüz metodunun üstündeki tüm <see cref="AspectAttribute"/>'ları sırayla uygulayan
/// tek Castle interceptor'ı. AsyncInterceptorBase, Castle'ın IInterceptor'ını doğrudan
/// uygulamadığı (yalnızca IAsyncInterceptor) için bu sınıf onu kompozisyonla köprüler —
/// docs/MIMARI.md · Y-33: service locator (ILifetimeScope.Resolve) yalnızca aspect içinde.
/// </summary>
public sealed class AspectDispatchInterceptor : IInterceptor
{
    private readonly IInterceptor _inner;

    public AspectDispatchInterceptor(ILifetimeScope lifetimeScope)
    {
        _inner = new Dispatcher(lifetimeScope).ToInterceptor();
    }

    public void Intercept(IInvocation invocation) => _inner.Intercept(invocation);

    private sealed class Dispatcher(ILifetimeScope lifetimeScope) : AsyncInterceptorBase
    {
        protected override Task<TResult> InterceptAsync<TResult>(
            IInvocation invocation,
            IInvocationProceedInfo proceedInfo,
            Func<IInvocation, IInvocationProceedInfo, Task<TResult>> proceed)
        {
            // Dikkat: IEnumerable<AspectAttribute>.Reverse() burada bilinçli olarak dizi'ye
            // çevrilmeden önce çağrılıyor — aksi halde derleyici System.MemoryExtensions'ın
            // yerinde (in-place, void) Span<T> aşırı yüklemesini seçebilir.
            var attributes = invocation.Method.GetCustomAttributes<AspectAttribute>().Reverse();

            Func<Task<TResult>> pipeline = () => proceed(invocation, proceedInfo);

            // Attribute'lar tersten sarmalanır ki uygulanma sırası deklare edildiği sırayla eşleşsin.
            foreach (var attribute in attributes)
            {
                var handler = (IAspectHandler)lifetimeScope.Resolve(attribute.HandlerType);
                var next = pipeline;
                pipeline = () => handler.HandleAsync(invocation, attribute, next);
            }

            return pipeline();
        }

        protected override Task InterceptAsync(
            IInvocation invocation,
            IInvocationProceedInfo proceedInfo,
            Func<IInvocation, IInvocationProceedInfo, Task> proceed) =>
            // Y-30 gereği Business servisleri her zaman Task<TResult> döner; bu overload yalnızca
            // AsyncInterceptorBase'in soyut üyesini karşılamak için var, gövdesi devreye girmez.
            proceed(invocation, proceedInfo);
    }
}
