using Business.DTOs.Reports;

namespace Business.Abstract;

/// <summary>
/// docs/MIMARI.md · Y-51: bir kullanıcının rapor kapsamını token'dan değil DB'den çözer — talep,
/// üretim ve indirme anlarının üçünde de ayrı ayrı çağrılır. İç bileşen, aspect taşımaz.
/// </summary>
public interface IReportScopeResolver
{
    Task<ReportScope> ResolveAsync(int userId, CancellationToken cancellationToken = default);
}
