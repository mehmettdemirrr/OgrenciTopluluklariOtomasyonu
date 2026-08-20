using Business.DTOs.Files;
using Business.DTOs.Reports;
using Business.ValidationRules;
using Core.Aspects.Autofac;
using Core.DataAccess;
using Core.Utilities.Results;
using DataAccess.Seed;
using Entities.Dtos.Reports;

namespace Business.Abstract;

public interface IReportService
{
    [SecuredOperation(IdentitySeedData.Permissions.ReportsRead)]
    [PerformanceAspect(1000)]
    Task<IDataResult<IReadOnlyList<TermSummaryRowDto>>> GetTermSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// docs/MIMARI.md · Y-46/Y-06: [TransactionAspect] kasıtlı olarak kullanılmaz — commit'ten SONRA
    /// Hangfire'a kuyruğa ekleme yapılabilmesi için (bkz. ReportManager.RequestAsync).
    /// </summary>
    [SecuredOperation(IdentitySeedData.Permissions.ReportsRead)]
    [ValidationAspect(typeof(CreateReportRequestValidator))]
    Task<IResult> RequestAsync(CreateReportRequestDto request, CancellationToken cancellationToken = default);

    [SecuredOperation(IdentitySeedData.Permissions.ReportsRead)]
    Task<IDataResult<PagedResult<ReportRequestListItemDto>>> GetListPagedAsync(int pageIndex, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>docs/MIMARI.md · Y-51: sahiplik + durum + kapsamın YENİDEN kontrolü — Faz 6'nın çekirdek testi.</summary>
    [SecuredOperation(IdentitySeedData.Permissions.ReportsRead)]
    Task<IDataResult<FileContentDto>> DownloadAsync(int reportRequestId, CancellationToken cancellationToken = default);
}
