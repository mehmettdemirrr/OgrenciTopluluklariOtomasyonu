using System.Text.Json;
using Business.Abstract;
using Business.Constants;
using Business.DTOs.Reports;
using Core.DataAccess;
using Core.Utilities.Files;
using Core.Utilities.Results;
using Core.Utilities.Time;
using DataAccess.Repositories;
using Entities;
using Entities.Enums;

namespace Business.Concrete;

/// <summary>
/// docs/MIMARI.md · Y-47: yalnızca id alır, idempotent (Queued dışı bir durumda hemen çıkar).
/// [TransactionAspect] kasıtlı olarak kullanılmaz (§0.5) — Processing→(Ready|Failed) durum geçişleri
/// ayrı ayrı yazılır; tek transaction olsaydı hata yolunda yazılan Failed rollback ile silinirdi.
/// </summary>
public sealed class ReportGenerationManager(
    IEntityRepository<ReportRequest> reportRequestRepository,
    IEntityRepository<StoredFile> storedFileRepository,
    IEntityRepository<Club> clubRepository,
    IEntityRepository<Event> eventRepository,
    IEntityRepository<AcademicTerm> academicTermRepository,
    IUnitOfWork unitOfWork,
    IClock clock,
    IReportDal reportDal,
    IReportScopeResolver scopeResolver,
    IExcelReportBuilder excelReportBuilder,
    IFileStorage fileStorage) : IReportGenerationService
{
    public async Task<IResult> GenerateAsync(int reportRequestId, CancellationToken cancellationToken = default)
    {
        var reportRequest = await reportRequestRepository.GetAsync(r => r.Id == reportRequestId, cancellationToken).ConfigureAwait(false);
        if (reportRequest is null)
        {
            return Result.NotFound(Messages.ReportNotFound);
        }

        if (reportRequest.Status != ReportStatus.Queued)
        {
            return Result.Success();
        }

        reportRequest.Status = ReportStatus.Processing;
        reportRequestRepository.Update(reportRequest);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var parameters = DeserializeParameters(reportRequest.ParametersJson);
        var reportType = Enum.Parse<ReportType>(reportRequest.ReportType);

        // Y-51 (2/3 an): üretim anında TALEP SAHİBİNİN o anki kapsamı kontrol edilir — arka plan
        // işinde HTTP kullanıcısı yok, ICurrentUser'a değil ReportRequest.RequestedByUserId'e bakılır.
        var scope = await scopeResolver.ResolveAsync(reportRequest.RequestedByUserId, cancellationToken).ConfigureAwait(false);

        if (reportType == ReportType.TermSummary)
        {
            // TermSummary tek bir kulübe değil TÜM kapsama bağlıdır — targetClubId kavramı uygun değil.
            if (!scope.AllClubs && scope.ClubIds.Count == 0)
            {
                await MarkFailedAsync(reportRequest, Messages.ReportScopeLost, cancellationToken).ConfigureAwait(false);
                return Result.Success();
            }
        }
        else
        {
            var targetClubId = await ResolveClubIdAsync(parameters, cancellationToken).ConfigureAwait(false);
            if (targetClubId is null || !scope.Covers(targetClubId.Value))
            {
                await MarkFailedAsync(reportRequest, Messages.ReportScopeLost, cancellationToken).ConfigureAwait(false);
                return Result.Success();
            }
        }

        try
        {
            byte[] workbookBytes;

            if (reportType == ReportType.ClubMembers)
            {
                if (parameters.ClubId is not { } clubId || parameters.AcademicTermId is not { } termId)
                {
                    await MarkFailedAsync(reportRequest, Messages.InvalidReportParameters, cancellationToken).ConfigureAwait(false);
                    return Result.Success();
                }

                var club = await clubRepository.GetAsync(c => c.Id == clubId, cancellationToken).ConfigureAwait(false);
                var term = await academicTermRepository.GetAsync(t => t.Id == termId, cancellationToken).ConfigureAwait(false);
                var rows = await reportDal.GetClubMemberRowsAsync(clubId, termId, cancellationToken).ConfigureAwait(false);
                workbookBytes = excelReportBuilder.BuildClubMemberWorkbook(rows, club?.Name ?? string.Empty, term?.Name ?? string.Empty);
            }
            else if (reportType == ReportType.EventParticipants)
            {
                if (parameters.EventId is not { } eventId)
                {
                    await MarkFailedAsync(reportRequest, Messages.InvalidReportParameters, cancellationToken).ConfigureAwait(false);
                    return Result.Success();
                }

                var @event = await eventRepository.GetAsync(e => e.Id == eventId, cancellationToken).ConfigureAwait(false);
                var rows = await reportDal.GetEventParticipationRowsAsync(eventId, cancellationToken).ConfigureAwait(false);
                workbookBytes = excelReportBuilder.BuildEventParticipationWorkbook(rows, @event?.Title ?? string.Empty);
            }
            else
            {
                var rows = await reportDal.GetTermSummaryAsync(scope.AllClubs ? null : scope.ClubIds, cancellationToken).ConfigureAwait(false);
                workbookBytes = excelReportBuilder.BuildTermSummaryWorkbook(rows);
            }

            // Fiziksel dosya önce yazılır, DB satırları sonra — yarıda kalan çalışma en fazla
            // sahipsiz bir disk dosyası bırakır (bilinçli V1 sınırı).
            var generatedFileName = $"{Guid.NewGuid():N}.xlsx";
            await using (var stream = new MemoryStream(workbookBytes))
            {
                await fileStorage.SaveAsync(generatedFileName, stream, cancellationToken).ConfigureAwait(false);
            }

            var storedFile = new StoredFile
            {
                GeneratedFileName = generatedFileName,
                OriginalFileName = $"{reportRequest.ReportType}-{reportRequest.Id}.xlsx",
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileSizeBytes = workbookBytes.LongLength,
                Visibility = FileVisibility.Protected,
                UploadedByUserId = reportRequest.RequestedByUserId,
                UploadedAtUtc = clock.UtcNow,
            };

            await storedFileRepository.AddAsync(storedFile, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            reportRequest.Status = ReportStatus.Ready;
            reportRequest.OutputFileId = storedFile.Id;
            reportRequest.CompletedAtUtc = clock.UtcNow;
            reportRequestRepository.Update(reportRequest);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return Result.Success();
        }
        catch (Exception ex)
        {
            await MarkFailedAsync(reportRequest, ex.Message, cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
    }

    private async Task<int?> ResolveClubIdAsync(ReportParameters parameters, CancellationToken cancellationToken)
    {
        if (parameters.ClubId is { } clubId)
        {
            return clubId;
        }

        if (parameters.EventId is { } eventId)
        {
            var @event = await eventRepository.GetAsync(e => e.Id == eventId, cancellationToken).ConfigureAwait(false);
            return @event?.ClubId;
        }

        return null;
    }

    private async Task MarkFailedAsync(ReportRequest reportRequest, string errorMessage, CancellationToken cancellationToken)
    {
        reportRequest.Status = ReportStatus.Failed;
        reportRequest.ErrorMessage = errorMessage;
        reportRequest.CompletedAtUtc = clock.UtcNow;
        reportRequestRepository.Update(reportRequest);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static ReportParameters DeserializeParameters(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? new ReportParameters(null, null, null)
            : JsonSerializer.Deserialize<ReportParameters>(json) ?? new ReportParameters(null, null, null);
}
