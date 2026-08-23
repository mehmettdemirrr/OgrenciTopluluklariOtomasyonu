using Business.Abstract;
using Core.DataAccess;
using Core.Utilities.Files;
using Core.Utilities.Results;
using Core.Utilities.Time;
using DataAccess.Repositories;
using Entities;
using Entities.Enums;

namespace Business.Concrete;

/// <summary>docs/MIMARI.md · §7 sessiz onay: gecelik bakım işi; K-28/A-44 ile trafik logu temizliği üçüncü adım olarak eklendi.</summary>
public sealed class MaintenanceManager(
    IEntityRepository<RefreshToken> refreshTokenRepository,
    IEntityRepository<StoredFile> storedFileRepository,
    IEntityRepository<ReportRequest> reportRequestRepository,
    ITrafficLogDal trafficLogDal,
    IUnitOfWork unitOfWork,
    IClock clock,
    IFileStorage fileStorage) : IMaintenanceService
{
    private static readonly TimeSpan StaleReportFileAge = TimeSpan.FromDays(7);

    /// <summary>A-44: erişim izi yalnızca 30 gün saklanır.</summary>
    private static readonly TimeSpan TrafficLogRetention = TimeSpan.FromDays(30);

    public async Task<IResult> RunNightlyMaintenanceAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;

        var expiredTokens = await refreshTokenRepository.GetListAsync(t => t.ExpiresAtUtc < now, cancellationToken).ConfigureAwait(false);
        foreach (var token in expiredTokens)
        {
            refreshTokenRepository.Delete(token);
        }

        if (expiredTokens.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var cutoff = now - StaleReportFileAge;
        var staleFiles = await storedFileRepository
            .GetListAsync(f => f.Visibility == FileVisibility.Protected && f.UploadedAtUtc < cutoff, cancellationToken)
            .ConfigureAwait(false);

        if (staleFiles.Count > 0)
        {
            var staleFileIds = staleFiles.Select(f => f.Id).ToList();
            var relatedReportRequests = await reportRequestRepository
                .GetListAsync(r => r.OutputFileId != null && staleFileIds.Contains(r.OutputFileId!.Value), cancellationToken)
                .ConfigureAwait(false);

            foreach (var reportRequest in relatedReportRequests)
            {
                reportRequest.OutputFileId = null;
                reportRequestRepository.Update(reportRequest);
            }

            foreach (var file in staleFiles)
            {
                storedFileRepository.Delete(file);
            }

            // DB yazımı fiziksel silmeden ÖNCE commit edilir — yarıda kalırsa en fazla sahipsiz
            // bir disk dosyası kalır (StoredFile kaydı olmayan bir dosya asla erişilemez).
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            foreach (var file in staleFiles)
            {
                // Zaten silinmişse false döner, akış bozulmaz (idempotent).
                await fileStorage.DeleteAsync(file.GeneratedFileName, cancellationToken).ConfigureAwait(false);
            }
        }

        // A-44: erişim izi tablosu kendi DAL'ıyla temizlenir — TrafficLog IEntity implemente etmediği
        // için generic repository üzerinden erişilemez (bkz. TrafficLog.cs).
        await trafficLogDal.DeleteOlderThanAsync(now - TrafficLogRetention, cancellationToken).ConfigureAwait(false);

        return Result.Success();
    }
}
