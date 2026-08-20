using System.Text.Json;
using Business.Abstract;
using Business.BackgroundJobs;
using Business.Constants;
using Business.DTOs.Files;
using Business.DTOs.Reports;
using Core.DataAccess;
using Core.Utilities.Files;
using Core.Utilities.Results;
using Core.Utilities.Security;
using Core.Utilities.Time;
using DataAccess.Repositories;
using Entities;
using Entities.Dtos.Reports;
using Entities.Enums;
using Hangfire;

namespace Business.Concrete;

public sealed class ReportManager(
    IReportDal reportDal,
    IEntityRepository<ReportRequest> reportRequestRepository,
    IEntityRepository<StoredFile> storedFileRepository,
    IEntityRepository<Club> clubRepository,
    IEntityRepository<Event> eventRepository,
    IEntityRepository<AcademicTerm> academicTermRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock,
    IReportScopeResolver scopeResolver,
    IFileStorage fileStorage,
    IBackgroundJobClient backgroundJobClient) : IReportService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<IDataResult<IReadOnlyList<TermSummaryRowDto>>> GetTermSummaryAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
        {
            return DataResult<IReadOnlyList<TermSummaryRowDto>>.Forbidden(Messages.ReportScopeDenied);
        }

        var scope = await scopeResolver.ResolveAsync(userId, cancellationToken).ConfigureAwait(false);
        if (!scope.AllClubs && scope.ClubIds.Count == 0)
        {
            return DataResult<IReadOnlyList<TermSummaryRowDto>>.Forbidden(Messages.ReportScopeDenied);
        }

        var rows = await reportDal.GetTermSummaryAsync(scope.AllClubs ? null : scope.ClubIds, cancellationToken).ConfigureAwait(false);
        return DataResult<IReadOnlyList<TermSummaryRowDto>>.Success(rows);
    }

    public async Task<IResult> RequestAsync(CreateReportRequestDto request, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result.Forbidden(Messages.ReportScopeDenied);
        }

        int targetClubId;
        if (request.ReportType == ReportType.ClubMembers)
        {
            if (request.ClubId is not { } clubId)
            {
                return Result.ValidationError(Messages.InvalidReportParameters);
            }

            var club = await clubRepository.GetAsync(c => c.Id == clubId, cancellationToken).ConfigureAwait(false);
            if (club is null)
            {
                return Result.NotFound(Messages.ClubNotFound);
            }

            targetClubId = clubId;
        }
        else
        {
            if (request.EventId is not { } eventId)
            {
                return Result.ValidationError(Messages.InvalidReportParameters);
            }

            var @event = await eventRepository.GetAsync(e => e.Id == eventId, cancellationToken).ConfigureAwait(false);
            if (@event is null)
            {
                return Result.NotFound(Messages.EventNotFound);
            }

            targetClubId = @event.ClubId;
        }

        // Y-51 (1/3 an): kapsam dışıysa talep kuyruğa hiç girmez.
        var scope = await scopeResolver.ResolveAsync(userId, cancellationToken).ConfigureAwait(false);
        if (!scope.Covers(targetClubId))
        {
            return Result.Forbidden(Messages.ReportScopeDenied);
        }

        int? academicTermId = null;
        if (request.ReportType == ReportType.ClubMembers)
        {
            var term = await academicTermRepository.GetAsync(t => t.IsCurrent, cancellationToken).ConfigureAwait(false);
            if (term is null)
            {
                return Result.NotFound(Messages.NoCurrentAcademicTerm);
            }

            academicTermId = term.Id;
        }

        var parameters = new ReportParameters(request.ClubId, request.EventId, academicTermId);
        var reportRequest = new ReportRequest
        {
            RequestedByUserId = userId,
            ReportType = request.ReportType.ToString(),
            ParametersJson = JsonSerializer.Serialize(parameters),
            Status = ReportStatus.Queued,
            RequestedAtUtc = clock.UtcNow,
        };

        await reportRequestRepository.AddAsync(reportRequest, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        // Y-46: kuyruğa ekleme yalnızca commit'ten (SaveChangesAsync) sonra.
        backgroundJobClient.Enqueue<ReportGenerationJob>(job => job.GenerateAsync(reportRequest.Id));

        return Result.Success(Messages.ReportQueued);
    }

    public async Task<IDataResult<PagedResult<ReportRequestListItemDto>>> GetListPagedAsync(
        int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var clampedPageSize = ClampPageSize(pageSize);

        if (currentUser.UserId is not { } userId)
        {
            return DataResult<PagedResult<ReportRequestListItemDto>>.Success(
                new PagedResult<ReportRequestListItemDto>([], 0, pageIndex, clampedPageSize));
        }

        var paged = await reportRequestRepository
            .GetListPagedAsync(pageIndex, clampedPageSize, r => r.RequestedByUserId == userId, cancellationToken)
            .ConfigureAwait(false);

        var items = paged.Items
            .Select(r => new ReportRequestListItemDto
            {
                Id = r.Id,
                ReportType = Enum.Parse<ReportType>(r.ReportType),
                Status = r.Status,
                RequestedAtUtc = r.RequestedAtUtc,
                CompletedAtUtc = r.CompletedAtUtc,
                HasFile = r.OutputFileId is not null,
                ErrorMessage = r.ErrorMessage,
            })
            .ToList();

        var result = new PagedResult<ReportRequestListItemDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize);
        return DataResult<PagedResult<ReportRequestListItemDto>>.Success(result);
    }

    public async Task<IDataResult<FileContentDto>> DownloadAsync(int reportRequestId, CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not { } userId)
        {
            return DataResult<FileContentDto>.Forbidden(Messages.ReportNotYours);
        }

        var reportRequest = await reportRequestRepository.GetAsync(r => r.Id == reportRequestId, cancellationToken).ConfigureAwait(false);
        if (reportRequest is null)
        {
            return DataResult<FileContentDto>.NotFound(Messages.ReportNotFound);
        }

        if (reportRequest.RequestedByUserId != userId)
        {
            return DataResult<FileContentDto>.Forbidden(Messages.ReportNotYours);
        }

        if (reportRequest.Status != ReportStatus.Ready)
        {
            return DataResult<FileContentDto>.Conflict(Messages.ReportNotReady);
        }

        // Ready ama dosya yok: gecelik bakım işi 7 günden eski StoredFile'ı temizlemiş olabilir
        // (MaintenanceManager, Status'u geri Failed'a çekmez — yalnızca OutputFileId'yi null'lar).
        if (reportRequest.OutputFileId is not { } fileId)
        {
            return DataResult<FileContentDto>.NotFound(Messages.ReportFileMissing);
        }

        // Y-51 (3/3 an): indirme anında yetki YENİDEN kontrol edilir — üretim anındaki kontrolden
        // bağımsız ayrı bir kod yolu. Kuyrukta beklerken/hazır olduktan sonra yetki değişmiş olabilir.
        var scope = await scopeResolver.ResolveAsync(userId, cancellationToken).ConfigureAwait(false);
        var parameters = DeserializeParameters(reportRequest.ParametersJson);
        var targetClubId = await ResolveClubIdForScopeCheckAsync(parameters, cancellationToken).ConfigureAwait(false);
        if (targetClubId is null || !scope.Covers(targetClubId.Value))
        {
            return DataResult<FileContentDto>.Forbidden(Messages.ReportScopeLost);
        }

        var storedFile = await storedFileRepository
            .GetAsync(f => f.Id == fileId && f.Visibility == FileVisibility.Protected, cancellationToken)
            .ConfigureAwait(false);
        if (storedFile is null)
        {
            return DataResult<FileContentDto>.NotFound(Messages.ReportFileMissing);
        }

        var stream = await fileStorage.OpenReadAsync(storedFile.GeneratedFileName, cancellationToken).ConfigureAwait(false);
        if (stream is null)
        {
            return DataResult<FileContentDto>.NotFound(Messages.ReportFileMissing);
        }

        return DataResult<FileContentDto>.Success(new FileContentDto
        {
            Content = stream,
            ContentType = storedFile.ContentType,
            DownloadFileName = storedFile.OriginalFileName,
        });
    }

    private async Task<int?> ResolveClubIdForScopeCheckAsync(ReportParameters parameters, CancellationToken cancellationToken)
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

    private static ReportParameters DeserializeParameters(string? json) =>
        string.IsNullOrWhiteSpace(json)
            ? new ReportParameters(null, null, null)
            : JsonSerializer.Deserialize<ReportParameters>(json) ?? new ReportParameters(null, null, null);

    private static int ClampPageSize(int pageSize) =>
        pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
}
