using Castle.DynamicProxy;
using Core.CrossCuttingConcerns.Caching;
using Core.Utilities.Results;

namespace Core.Aspects.Autofac;

/// <summary>
/// docs/MIMARI.md · A-17: yalnızca primitive argümanlı okuma metotları için — anahtar
/// stratejisi kasıtlı olarak basit tutulur, genel bir serileştirme icat edilmez.
/// Başarısız iş sonuçları asla cache'lenmez.
/// </summary>
public sealed class CacheAspectHandler(ICacheManager cacheManager) : IAspectHandler
{
    public async Task<TResult> HandleAsync<TResult>(IInvocation invocation, AspectAttribute attribute, Func<Task<TResult>> next)
    {
        var durationMinutes = ((CacheAspectAttribute)attribute).DurationMinutes;
        var key = BuildKey(invocation);

        if (cacheManager.TryGet<TResult>(key, out var cached) && cached is not null)
        {
            return cached;
        }

        var result = await next().ConfigureAwait(false);

        if (result is not IResult { IsSuccess: false })
        {
            cacheManager.Add(key, result!, durationMinutes);
        }

        return result;
    }

    private static string BuildKey(IInvocation invocation)
    {
        var args = invocation.Arguments
            .Where(a => a is not CancellationToken)
            .Select(a => a?.ToString() ?? "null");

        return $"{invocation.TargetType?.Name}.{invocation.Method.Name}({string.Join(",", args)})";
    }
}
