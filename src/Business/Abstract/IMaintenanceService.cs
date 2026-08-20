using Core.Aspects.Autofac;
using Core.Utilities.Results;

namespace Business.Abstract;

/// <summary>docs/MIMARI.md · K-08/§7: gecelik tek yinelenen iş — süresi geçmiş refresh token'lar + eskimiş rapor dosyaları.</summary>
public interface IMaintenanceService
{
    [PerformanceAspect(5000)]
    Task<IResult> RunNightlyMaintenanceAsync(CancellationToken cancellationToken = default);
}
