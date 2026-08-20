using Business.Abstract;
using Core.DataAccess;
using Core.Utilities.Files;
using Core.Utilities.Results;
using Core.Utilities.Time;
using Entities;
using Entities.Enums;

namespace Business.Concrete;

/// <summary>docs/MIMARI.md · §7 sessiz onay: tam olarak iki görev, tek yinelenen işte.</summary>
public sealed class MaintenanceManager(
    IEntityRepository<RefreshToken> refreshTokenRepository,
    IEntityRepository<StoredFile> storedFileRepository,
    IEntityRepository<ReportRequest> reportRequestRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    IFileStorage fileStorage) : IMaintenanceService
{
    private static readonly TimeSpan StaleReportFileAge = TimeSpan.FromDays(7);

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

        return Result.Success();
    }
}
