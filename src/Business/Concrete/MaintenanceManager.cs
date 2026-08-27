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
    IEntityRepository<ClubApplication> clubApplicationRepository,
    IEntityRepository<ClubApplicationDocument> clubApplicationDocumentRepository,
    ITrafficLogDal trafficLogDal,
    IUnitOfWork unitOfWork,
    IClock clock,
    IFileStorage fileStorage) : IMaintenanceService
{
    private static readonly TimeSpan StaleReportFileAge = TimeSpan.FromDays(7);

    /// <summary>A-44: erişim izi yalnızca 30 gün saklanır.</summary>
    private static readonly TimeSpan TrafficLogRetention = TimeSpan.FromDays(30);

    /// <summary>docs/MIMARI.md · A-63: reddedilen başvurunun evrakları 90 gün sonra silinir.</summary>
    private static readonly TimeSpan RejectedApplicationDocumentRetention = TimeSpan.FromDays(90);

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
        var protectedCandidates = await storedFileRepository
            .GetListAsync(f => f.Visibility == FileVisibility.Protected && f.UploadedAtUtc < cutoff, cancellationToken)
            .ConfigureAwait(false);

        // K-37: Faz 33'ten beri Protected yalnızca "rapor çıktısı" demek DEĞİL — kuruluş evrakları da
        // Protected. Bu adım 7 günlük rapor temizliğidir; evrakların saklama süresi 90 gündür (A-63) ve
        // aşağıda ayrıca ele alınır. Bu ayıklama olmadan evraklar bir haftada silinir, silinemezse de
        // FK Restrict gecelik bakımı komple düşürürdü.
        var staleFiles = await ExcludeApplicationDocumentsAsync(protectedCandidates, cancellationToken).ConfigureAwait(false);

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

        // A-63: reddedilen başvuruların evrakları. Onaylananınki kulübün kuruluş dosyası olarak kalır;
        // başvuru kaydının kendisi hiçbir hâlde silinmez (Y-16 — o bir olay kaydıdır).
        var documentCutoff = now - RejectedApplicationDocumentRetention;
        var staleApplications = await clubApplicationRepository
            .GetListAsync(
                a => a.Status == ApplicationStatus.Rejected && a.ReviewedAtUtc != null && a.ReviewedAtUtc < documentCutoff,
                cancellationToken)
            .ConfigureAwait(false);

        if (staleApplications.Count > 0)
        {
            var staleApplicationIds = staleApplications.Select(a => a.Id).ToList();
            var staleLinks = await clubApplicationDocumentRepository
                .GetListAsync(d => staleApplicationIds.Contains(d.ClubApplicationId), cancellationToken)
                .ConfigureAwait(false);

            if (staleLinks.Count > 0)
            {
                var staleDocumentFileIds = staleLinks.Select(d => d.StoredFileId).Distinct().ToList();
                var staleDocumentFiles = await storedFileRepository
                    .GetListAsync(f => staleDocumentFileIds.Contains(f.Id), cancellationToken)
                    .ConfigureAwait(false);

                // Sıra zorunlu: FK Restrict önce bağ satırını, sonra StoredFile'ı ister (A-62 konfigürasyonu).
                foreach (var link in staleLinks)
                {
                    clubApplicationDocumentRepository.Delete(link);
                }

                foreach (var file in staleDocumentFiles)
                {
                    storedFileRepository.Delete(file);
                }

                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                foreach (var file in staleDocumentFiles)
                {
                    // Y-47: idempotent — zaten silinmişse false döner, akış bozulmaz.
                    await fileStorage.DeleteAsync(file.GeneratedFileName, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        // A-44: erişim izi tablosu kendi DAL'ıyla temizlenir — TrafficLog IEntity implemente etmediği
        // için generic repository üzerinden erişilemez (bkz. TrafficLog.cs).
        await trafficLogDal.DeleteOlderThanAsync(now - TrafficLogRetention, cancellationToken).ConfigureAwait(false);

        // K-37 Tuzak 3: başvuru transaction'ı geri alınırsa StoredFile satırı gider ama disk dosyası
        // kalır. Sahipsiz dosya asla erişilemez (her okuma StoredFile'dan geçer) ama kişisel veri
        // olduğu için diskte bırakılamaz. Bu tarama son adımdır: yukarıdaki silmeler zaten commit edildi.
        var diskFileNames = await fileStorage.ListAsync(cancellationToken).ConfigureAwait(false);
        if (diskFileNames.Count > 0)
        {
            var knownFileNames = (await storedFileRepository.GetListAsync(f => true, cancellationToken).ConfigureAwait(false))
                .Select(f => f.GeneratedFileName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var name in diskFileNames.Where(n => !knownFileNames.Contains(n)))
            {
                await fileStorage.DeleteAsync(name, cancellationToken).ConfigureAwait(false);
            }
        }

        return Result.Success();
    }

    /// <summary>
    /// K-37: bir başvuruya bağlı evrak dosyalarını rapor temizliğinin dışında tutar.
    /// Tek toplu sorgu (Y-10) — dosya başına sorgu N+1 üretirdi.
    /// </summary>
    private async Task<List<StoredFile>> ExcludeApplicationDocumentsAsync(
        List<StoredFile> candidates, CancellationToken cancellationToken)
    {
        if (candidates.Count == 0)
        {
            return candidates;
        }

        var candidateIds = candidates.Select(f => f.Id).ToList();
        var documentFileIds = (await clubApplicationDocumentRepository
                .GetListAsync(d => candidateIds.Contains(d.StoredFileId), cancellationToken)
                .ConfigureAwait(false))
            .Select(d => d.StoredFileId)
            .ToHashSet();

        return documentFileIds.Count == 0
            ? candidates
            : candidates.Where(f => !documentFileIds.Contains(f.Id)).ToList();
    }
}
