using System.Diagnostics;
using Castle.DynamicProxy;
using Serilog;

namespace Core.Aspects.Autofac;

public sealed class PerformanceAspectHandler : IAspectHandler
{
    public async Task<TResult> HandleAsync<TResult>(IInvocation invocation, AspectAttribute attribute, Func<Task<TResult>> next)
    {
        var threshold = ((PerformanceAspectAttribute)attribute).ThresholdMilliseconds;
        var stopwatch = Stopwatch.StartNew();

        var result = await next().ConfigureAwait(false);

        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > threshold)
        {
            Log.Warning(
                "{Type}.{Method} {ElapsedMilliseconds} ms sürdü (eşik: {Threshold} ms)",
                invocation.TargetType?.Name,
                invocation.Method.Name,
                stopwatch.ElapsedMilliseconds,
                threshold);
        }

        return result;
    }
}
