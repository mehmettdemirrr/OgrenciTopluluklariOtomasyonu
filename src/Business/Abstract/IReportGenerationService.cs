using Core.Aspects.Autofac;
using Core.Utilities.Results;

namespace Business.Abstract;

/// <summary>
/// docs/MIMARI.md · arka plan işi, HTTP bağlamı yok — Task&lt;IResult&gt; dönmesi kasıtlı (Task
/// dönseydi AspectDispatchInterceptor'ın generic olmayan overload'u aspect'leri hiç uygulamazdı).
/// </summary>
public interface IReportGenerationService
{
    [PerformanceAspect(3000)]
    Task<IResult> GenerateAsync(int reportRequestId, CancellationToken cancellationToken = default);
}
