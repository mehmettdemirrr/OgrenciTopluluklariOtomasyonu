using Business.DTOs.Traffic;
using Core.Aspects.Autofac;
using Core.DataAccess;
using Core.Utilities.Results;
using DataAccess.Seed;
using Entities.Dtos.Traffic;

namespace Business.Abstract;

/// <summary>docs/PLAN-V3.md · K-28/A-44: erişim izi — yalnızca `audit.read` (mevcut izin, yeni claim yok).</summary>
public interface ITrafficLogService
{
    /// <summary>
    /// docs/MIMARI.md · Y-43'ün mantığı: yazma iş transaction'ından bağımsız çalışır — RequestLoggingMiddleware
    /// her çağrıda ayrı bir DI scope açar. [SecuredOperation] kasıtlı olarak yok: bu metodu yalnızca
    /// middleware çağırır (bir HTTP endpoint'i değil), isteğin kimliği doğrulanmamış olsa bile
    /// (ör. başarısız giriş denemesi) kayıt tutulabilmeli.
    /// </summary>
    Task RecordAsync(RecordTrafficLogRequestDto request, CancellationToken cancellationToken = default);

    [SecuredOperation(IdentitySeedData.Permissions.AuditRead)]
    Task<IDataResult<PagedResult<TrafficLogListItemDto>>> GetPagedAsync(
        int? userId,
        string? ipAddress,
        string? httpMethod,
        string? correlationId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default);
}
