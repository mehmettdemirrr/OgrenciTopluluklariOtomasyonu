using Core.Aspects.Autofac;
using Core.Utilities.Results;

namespace Business.Abstract;

/// <summary>
/// Faz 2'nin "boş bir servis metodu aspect zincirinden geçip doğru HTTP kodunu üretiyor"
/// kabul kriterini kanıtlayan asgari servis (docs/MIMARI.md · Bölüm 5, Faz 2).
/// </summary>
public interface IDiagnosticsService
{
    [PerformanceAspect(thresholdMilliseconds: 1)]
    Task<IDataResult<string>> PingAsync();
}
