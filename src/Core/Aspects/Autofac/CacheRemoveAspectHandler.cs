using Castle.DynamicProxy;
using Core.CrossCuttingConcerns.Caching;
using Core.Utilities.Results;

namespace Core.Aspects.Autofac;

/// <summary>docs/MIMARI.md · Y-45: yazma başarılı olduğunda desene uyan anahtarları düşürür.</summary>
public sealed class CacheRemoveAspectHandler(ICacheManager cacheManager) : IAspectHandler
{
    public async Task<TResult> HandleAsync<TResult>(IInvocation invocation, AspectAttribute attribute, Func<Task<TResult>> next)
    {
        var patterns = ((CacheRemoveAspectAttribute)attribute).Patterns;
        var result = await next().ConfigureAwait(false);

        if (result is not IResult { IsSuccess: false })
        {
            foreach (var pattern in patterns)
            {
                cacheManager.RemoveByPattern(pattern);
            }
        }

        return result;
    }
}
