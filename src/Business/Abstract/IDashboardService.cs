using Business.DTOs.Dashboard;
using Core.Utilities.Results;

namespace Business.Abstract;

/// <summary>
/// docs/PLAN-V2.md · Faz 12 (K-25): [SecuredOperation] kasıtlı olarak yok — herhangi bir kimliği
/// doğrulanmış kullanıcı kendi özetini görebilir (Y-21 fallback policy zaten kimlik doğrulaması
/// şart koşuyor); kapsam farkı izin claim'i ile değil, IReportScopeResolver ile içeride çözülür.
/// </summary>
public interface IDashboardService
{
    Task<IDataResult<DashboardSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken = default);
}
