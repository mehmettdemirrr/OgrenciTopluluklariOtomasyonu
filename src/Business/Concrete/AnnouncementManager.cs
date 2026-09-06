using Business.Abstract;
using Business.Constants;
using Business.DTOs.Announcements;
using Business.RichText;
using Core.DataAccess;
using Core.Utilities.Results;
using Core.Utilities.Security;
using Core.Utilities.Time;
using DataAccess.Seed;
using Entities;
using Entities.Enums;

namespace Business.Concrete;

/// <summary>docs/PLAN-V2.md §10.4 · A-43: kulüp duyurusu + sistem duyurusu (ClubId = null).</summary>
public sealed class AnnouncementManager(
    IEntityRepository<Announcement> announcementRepository,
    IEntityRepository<Club> clubRepository,
    IEntityRepository<Student> studentRepository,
    IEntityRepository<AcademicStaff> academicStaffRepository,
    IEntityRepository<ClubMembership> clubMembershipRepository,
    IEntityRepository<AcademicTerm> academicTermRepository,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IClock clock) : IAnnouncementService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<IDataResult<PagedResult<AnnouncementListItemDto>>> GetForClubAsync(
        int clubId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var paged = await announcementRepository
            .GetListPagedAsync(
                pageIndex,
                ClampPageSize(pageSize),
                a => a.ClubId == clubId,
                a => a.PublishedAtUtc,
                descending: true,
                cancellationToken)
            .ConfigureAwait(false);

        var items = await MapWithClubNamesAsync(paged, cancellationToken).ConfigureAwait(false);
        return DataResult<PagedResult<AnnouncementListItemDto>>.Success(new PagedResult<AnnouncementListItemDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize));
    }

    public async Task<IDataResult<PagedResult<AnnouncementListItemDto>>> GetFeedAsync(
        int pageIndex, int pageSize, string? search = null, CancellationToken cancellationToken = default)
    {
        var term = SearchTerm.Normalize(search);

        // Sözleşmedeki "tarihe göre azalan" bugüne kadar yalnızca yorumdaydı — sıra artık gerçekten uygulanıyor (Y-64).
        var paged = await announcementRepository
            .GetListPagedAsync(
                pageIndex,
                ClampPageSize(pageSize),
                a => term.Length == 0 || a.Title.Contains(term),
                a => a.PublishedAtUtc,
                descending: true,
                cancellationToken)
            .ConfigureAwait(false);

        var items = await MapWithClubNamesAsync(paged, cancellationToken).ConfigureAwait(false);
        return DataResult<PagedResult<AnnouncementListItemDto>>.Success(new PagedResult<AnnouncementListItemDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize));
    }

    public async Task<IDataResult<int>> CreateAsync(int clubId, CreateAnnouncementRequestDto request, CancellationToken cancellationToken = default)
    {
        var club = await clubRepository.GetAsync(c => c.Id == clubId, cancellationToken).ConfigureAwait(false);
        if (club is null)
        {
            return DataResult<int>.NotFound(Messages.ClubNotFound);
        }

        var accessError = await EnsureClubWriteAccessAsync(club, cancellationToken).ConfigureAwait(false);
        if (accessError is not null)
        {
            return DataResult<int>.Forbidden(accessError);
        }

        if (!TryResolveContent(request.Content, request.ContentJson, out var content, out var contentError))
        {
            return DataResult<int>.ValidationError(contentError!);
        }

        var announcement = new Announcement
        {
            ClubId = clubId,
            Title = request.Title,
            Content = content,
            ContentJson = request.ContentJson,
            Visibility = request.Visibility,
            PublishedAtUtc = clock.UtcNow,
        };

        await announcementRepository.AddAsync(announcement, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return DataResult<int>.Success(announcement.Id, Messages.AnnouncementCreated);
    }

    public async Task<IResult> UpdateAsync(int announcementId, UpdateAnnouncementRequestDto request, CancellationToken cancellationToken = default)
    {
        var announcement = await announcementRepository.GetAsync(a => a.Id == announcementId, cancellationToken).ConfigureAwait(false);
        if (announcement is null)
        {
            return Result.NotFound(Messages.AnnouncementNotFound);
        }

        var accessError = await EnsureAnnouncementWriteAccessAsync(announcement, cancellationToken).ConfigureAwait(false);
        if (accessError is not null)
        {
            return Result.Forbidden(accessError);
        }

        if (!TryResolveContent(request.Content, request.ContentJson, out var content, out var contentError))
        {
            return Result.ValidationError(contentError!);
        }

        announcement.Title = request.Title;
        announcement.Content = content;
        announcement.ContentJson = request.ContentJson;
        announcement.Visibility = request.Visibility;
        announcementRepository.Update(announcement);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(Messages.AnnouncementUpdated);
    }

    public async Task<IResult> DeleteAsync(int announcementId, CancellationToken cancellationToken = default)
    {
        var announcement = await announcementRepository.GetAsync(a => a.Id == announcementId, cancellationToken).ConfigureAwait(false);
        if (announcement is null)
        {
            return Result.NotFound(Messages.AnnouncementNotFound);
        }

        var accessError = await EnsureAnnouncementWriteAccessAsync(announcement, cancellationToken).ConfigureAwait(false);
        if (accessError is not null)
        {
            return Result.Forbidden(accessError);
        }

        // Y-16: soft delete + audit interceptor'ın Delete olarak tanıması (Y-44).
        announcement.IsDeleted = true;
        announcement.DeletedAtUtc = clock.UtcNow;
        announcementRepository.Update(announcement);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(Messages.AnnouncementDeleted);
    }

    public async Task<IDataResult<AnnouncementListItemDto>> GetByIdAsync(int announcementId, CancellationToken cancellationToken = default)
    {
        var announcement = await announcementRepository.GetAsync(a => a.Id == announcementId, cancellationToken).ConfigureAwait(false);
        if (announcement is null)
        {
            return DataResult<AnnouncementListItemDto>.NotFound(Messages.AnnouncementNotFound);
        }

        var accessError = await EnsureAnnouncementWriteAccessAsync(announcement, cancellationToken).ConfigureAwait(false);
        if (accessError is not null)
        {
            return DataResult<AnnouncementListItemDto>.Forbidden(accessError);
        }

        var club = announcement.ClubId is { } clubId
            ? await clubRepository.GetAsync(c => c.Id == clubId, cancellationToken).ConfigureAwait(false)
            : null;

        return DataResult<AnnouncementListItemDto>.Success(new AnnouncementListItemDto
        {
            Id = announcement.Id,
            ClubId = announcement.ClubId,
            ClubName = club?.Name,
            Title = announcement.Title,
            Content = announcement.Content,
            ContentJson = announcement.ContentJson,
            ImageFileId = announcement.ImageFileId,
            Visibility = announcement.Visibility,
            PublishedAtUtc = announcement.PublishedAtUtc,
        });
    }

    public async Task<IDataResult<int>> CreateGlobalAsync(CreateAnnouncementRequestDto request, CancellationToken cancellationToken = default)
    {
        if (!TryResolveContent(request.Content, request.ContentJson, out var content, out var contentError))
        {
            return DataResult<int>.ValidationError(contentError!);
        }

        var announcement = new Announcement
        {
            ClubId = null,
            Title = request.Title,
            Content = content,
            ContentJson = request.ContentJson,
            Visibility = request.Visibility,
            PublishedAtUtc = clock.UtcNow,
        };

        await announcementRepository.AddAsync(announcement, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return DataResult<int>.Success(announcement.Id, Messages.AnnouncementCreated);
    }

    /// <summary>
    /// docs/MIMARI.md · Y-78: izin listesi kapalıdır; reddedilen içerik sessizce temizlenmez, hata döner.
    /// A-71: düz metin aynası JSON'dan türetilir — arama ve e-posta bu alanı okur.
    /// </summary>
    private static bool TryResolveContent(string? content, string? contentJson, out string resolvedContent, out string? error)
    {
        resolvedContent = content?.Trim() ?? string.Empty;
        error = null;

        if (string.IsNullOrWhiteSpace(contentJson))
        {
            return true;
        }

        var validation = RichTextDocumentValidator.Validate(contentJson);
        if (!validation.IsValid)
        {
            error = validation.Error ?? Messages.UnsupportedRichTextContent;
            return false;
        }

        resolvedContent = RichTextPlainTextExtractor.Extract(contentJson);
        return true;
    }

    public async Task<IResult> EnsureCanManageAsync(int announcementId, CancellationToken cancellationToken = default)
    {
        var announcement = await announcementRepository.GetAsync(a => a.Id == announcementId, cancellationToken).ConfigureAwait(false);
        if (announcement is null)
        {
            return Result.NotFound(Messages.AnnouncementNotFound);
        }

        var accessError = await EnsureAnnouncementWriteAccessAsync(announcement, cancellationToken).ConfigureAwait(false);
        return accessError is null ? Result.Success() : Result.Forbidden(accessError);
    }

    private async Task<string?> EnsureAnnouncementWriteAccessAsync(Announcement announcement, CancellationToken cancellationToken)
    {
        // Y-66: yönetici kontrolü her kapsam metodunun İLK satırıdır (A-55).
        if (currentUser.Permissions.Contains(IdentitySeedData.Permissions.ClubsManageAll))
        {
            return null;
        }

        // A-43: sistem duyurusunu yalnızca announcements.global taşıyan (Admin) düzenleyebilir/silebilir.
        if (announcement.ClubId is not { } clubId)
        {
            return currentUser.Permissions.Contains(IdentitySeedData.Permissions.AnnouncementsGlobal)
                ? null
                : Messages.NotClubAdvisorOrOfficer;
        }

        var club = await clubRepository.GetAsync(c => c.Id == clubId, cancellationToken).ConfigureAwait(false);
        if (club is null)
        {
            return Messages.ClubNotFound;
        }

        return await EnsureClubWriteAccessAsync(club, cancellationToken).ConfigureAwait(false);
    }

    // Y-23: announcements.write izni yeterli değil — EventManager.EnsureClubWriteAccessAsync ile aynı desen.
    private async Task<string?> EnsureClubWriteAccessAsync(Club club, CancellationToken cancellationToken)
    {
        // Y-66: yönetici kontrolü her kapsam metodunun İLK satırıdır (A-55).
        if (currentUser.Permissions.Contains(IdentitySeedData.Permissions.ClubsManageAll))
        {
            return null;
        }

        if (currentUser.UserId is not { } userId)
        {
            return Messages.NotClubAdvisorOrOfficer;
        }

        var advisor = await academicStaffRepository.GetAsync(s => s.Id == club.AdvisorId, cancellationToken).ConfigureAwait(false);
        if (advisor is not null && advisor.ApplicationUserId == userId)
        {
            return null;
        }

        var term = await academicTermRepository.GetAsync(t => t.IsCurrent, cancellationToken).ConfigureAwait(false);
        if (term is null)
        {
            return Messages.NotClubAdvisorOrOfficer;
        }

        var student = await studentRepository.GetAsync(s => s.ApplicationUserId == userId, cancellationToken).ConfigureAwait(false);
        if (student is null)
        {
            return Messages.NotClubAdvisorOrOfficer;
        }

        var membership = await clubMembershipRepository
            .GetAsync(m => m.ClubId == club.Id && m.StudentId == student.Id && m.AcademicTermId == term.Id, cancellationToken)
            .ConfigureAwait(false);

        // A-68: karar kapasiteden. Y-75: uçtaki [SecuredOperation] birinci kapı olarak yerinde.
        return membership is not null && membership.Capabilities.HasFlag(ClubCapability.AnnouncementsManage)
            ? null
            : Messages.NotClubAdvisorOrOfficer;
    }

    private async Task<List<AnnouncementListItemDto>> MapWithClubNamesAsync(PagedResult<Announcement> paged, CancellationToken cancellationToken)
    {
        var clubIds = paged.Items.Where(a => a.ClubId is not null).Select(a => a.ClubId!.Value).Distinct().ToList();
        var clubNames = (await clubRepository.GetListAsync(c => clubIds.Contains(c.Id), cancellationToken).ConfigureAwait(false))
            .ToDictionary(c => c.Id, c => c.Name);

        return paged.Items
            .Select(a => new AnnouncementListItemDto
            {
                Id = a.Id,
                ClubId = a.ClubId,
                ClubName = a.ClubId is { } id ? clubNames.GetValueOrDefault(id, string.Empty) : null,
                Title = a.Title,
                Content = a.Content,
                Visibility = a.Visibility,
                ContentJson = a.ContentJson,
                ImageFileId = a.ImageFileId,
                PublishedAtUtc = a.PublishedAtUtc,
            }).ToList();
    }

    private static int ClampPageSize(int pageSize) =>
        pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
}
