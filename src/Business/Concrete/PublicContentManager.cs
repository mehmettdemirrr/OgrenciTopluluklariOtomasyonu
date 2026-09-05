using Business.Abstract;
using Business.Constants;
using Business.DTOs.Public;
using Core.DataAccess;
using Core.Utilities.Results;
using Core.Utilities.Time;
using Entities;
using Entities.Enums;

namespace Business.Concrete;

/// <summary>docs/PLAN-V2.md · Faz 14 (A-42/Y-58): anonim vitrin — filtreler kodda sabit, dışarıdan parametrelenmez.</summary>
public sealed class PublicContentManager(
    IEntityRepository<Club> clubRepository,
    IEntityRepository<Event> eventRepository,
    IEntityRepository<Announcement> announcementRepository,
    IEntityRepository<ClubCategory> clubCategoryRepository,
    IEntityRepository<Student> studentRepository,
    IClock clock) : IPublicContentService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<IDataResult<PagedResult<PublicClubListItemDto>>> GetClubsAsync(
        int pageIndex, int pageSize, string? search = null, int? categoryId = null, string? letter = null, CancellationToken cancellationToken = default)
    {
        var term = SearchTerm.Normalize(search);
        var initial = NameInitial.Normalize(letter);
        var initialLower = NameInitial.ToSearchLower(initial);

        // A-50: arama, kategori ve harf filtresi SQL'de. IsActive filtresi kodda sabit kalır (Y-58) —
        // search / categoryId / letter onu gevşetemez. Büyük/küçük harf iki sabit önekle taranır
        // (EF ToLower+culture SQL'e çevrilemez; ToLower burada, ifade ağacının dışında alınır).
        var paged = await clubRepository
            .GetListPagedAsync(
                pageIndex,
                ClampPageSize(pageSize),
                c => c.IsActive
                    && (categoryId == null || c.ClubCategoryId == categoryId)
                    && (term.Length == 0 || c.Name.Contains(term))
                    && (initial.Length == 0
                        || c.Name.StartsWith(initial)
                        || c.Name.StartsWith(initialLower)),
                c => c.Name,
                descending: false,
                cancellationToken)
            .ConfigureAwait(false);

        var categoryNames = await GetCategoryNamesAsync(
            paged.Items.Where(c => c.ClubCategoryId is not null).Select(c => c.ClubCategoryId!.Value),
            cancellationToken).ConfigureAwait(false);

        var items = paged.Items
            .Select(c => new PublicClubListItemDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                LogoFileId = c.LogoFileId,
                ClubCategoryName = c.ClubCategoryId is { } id ? categoryNames.GetValueOrDefault(id) : null,
            })
            .ToList();

        return DataResult<PagedResult<PublicClubListItemDto>>.Success(
            new PagedResult<PublicClubListItemDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize));
    }

    public async Task<IDataResult<PublicClubDetailDto>> GetClubByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var club = await clubRepository.GetAsync(c => c.Id == id && c.IsActive, cancellationToken).ConfigureAwait(false);
        if (club is null)
        {
            return DataResult<PublicClubDetailDto>.NotFound(Messages.ClubNotFound);
        }

        var categoryNames = await GetCategoryNamesAsync(
            club.ClubCategoryId is { } cid ? [cid] : [],
            cancellationToken).ConfigureAwait(false);

        return DataResult<PublicClubDetailDto>.Success(new PublicClubDetailDto
        {
            Id = club.Id,
            Name = club.Name,
            Description = club.Description,
            LogoFileId = club.LogoFileId,
            ClubCategoryName = club.ClubCategoryId is { } detailCategoryId ? categoryNames.GetValueOrDefault(detailCategoryId) : null,
        });
    }

    /// <summary>Y-10: tek toplu sorgu — satır başına sorgu N+1 üretirdi.</summary>
    private async Task<Dictionary<int, string>> GetCategoryNamesAsync(IEnumerable<int> categoryIds, CancellationToken cancellationToken)
    {
        var ids = categoryIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        return (await clubCategoryRepository.GetListAsync(c => ids.Contains(c.Id), cancellationToken).ConfigureAwait(false))
            .ToDictionary(c => c.Id, c => c.Name);
    }

    public async Task<IDataResult<PagedResult<PublicEventListItemDto>>> GetEventsAsync(
        int? clubId, int pageIndex, int pageSize, string? search = null, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var term = SearchTerm.Normalize(search);

        // Yaklaşan etkinlik listesi tarihe göre artan sıralanır — vitrinde en yakın etkinlik başta (Y-64).
        // Y-72: Audience filtresi de Status gibi KODDA SABİT — search/clubId parametreleri onu gevşetemez.
        var paged = await eventRepository
            .GetListPagedAsync(
                pageIndex,
                ClampPageSize(pageSize),
                e => e.Status == EventStatus.Published && e.Audience == EventAudience.Public
                    && e.StartDateUtc >= now && (clubId == null || e.ClubId == clubId)
                    && (term.Length == 0 || e.Title.Contains(term)),
                e => e.StartDateUtc,
                descending: false,
                cancellationToken)
            .ConfigureAwait(false);

        var clubIds = paged.Items.Select(e => e.ClubId).Distinct().ToList();
        var clubNames = (await clubRepository.GetListAsync(c => clubIds.Contains(c.Id), cancellationToken).ConfigureAwait(false))
            .ToDictionary(c => c.Id, c => c.Name);

        var items = paged.Items
            .Select(e => new PublicEventListItemDto
            {
                Id = e.Id,
                ClubId = e.ClubId,
                ClubName = clubNames.GetValueOrDefault(e.ClubId, string.Empty),
                Title = e.Title,
                Description = e.Description,
                Location = e.Location,
                StartDateUtc = e.StartDateUtc,
                EndDateUtc = e.EndDateUtc,
                Capacity = e.Capacity,
                PosterFileId = e.PosterFileId,
            })
            .ToList();

        return DataResult<PagedResult<PublicEventListItemDto>>.Success(
            new PagedResult<PublicEventListItemDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize));
    }

    public async Task<IDataResult<PagedResult<PublicAnnouncementListItemDto>>> GetAnnouncementsAsync(
        int? clubId, int pageIndex, int pageSize, string? search = null, CancellationToken cancellationToken = default)
    {
        var term = SearchTerm.Normalize(search);

        // Duyuru akışı en yeniden eskiye (Y-64).
        var paged = await announcementRepository
            .GetListPagedAsync(
                pageIndex,
                ClampPageSize(pageSize),
                a => a.Visibility == AnnouncementVisibility.Public && (clubId == null || a.ClubId == clubId)
                    && (term.Length == 0 || a.Title.Contains(term)),
                a => a.PublishedAtUtc,
                descending: true,
                cancellationToken)
            .ConfigureAwait(false);

        var clubIds = paged.Items.Where(a => a.ClubId is not null).Select(a => a.ClubId!.Value).Distinct().ToList();
        var clubNames = (await clubRepository.GetListAsync(c => clubIds.Contains(c.Id), cancellationToken).ConfigureAwait(false))
            .ToDictionary(c => c.Id, c => c.Name);

        var items = paged.Items
            .Select(a => new PublicAnnouncementListItemDto
            {
                Id = a.Id,
                ClubId = a.ClubId,
                ClubName = a.ClubId is { } id ? clubNames.GetValueOrDefault(id, string.Empty) : null,
                Title = a.Title,
                Content = a.Content,
                ContentJson = a.ContentJson,
                ImageFileId = a.ImageFileId,
                PublishedAtUtc = a.PublishedAtUtc,
            })
            .ToList();

        return DataResult<PagedResult<PublicAnnouncementListItemDto>>.Success(
            new PagedResult<PublicAnnouncementListItemDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize));
    }

    public async Task<IDataResult<PublicStatsDto>> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;

        // Y-42: sayım GetListPagedAsync TotalCount üzerinden SQL'de yapılır; sayfa boyutu 1
        // yalnızca mevcut sözleşmeyi kullanmak içindir, satırlar bellekte toplanmaz.
        var clubsTask = clubRepository.GetListPagedAsync(0, 1, null, cancellationToken);
        var activeClubsTask = clubRepository.GetListPagedAsync(0, 1, c => c.IsActive, cancellationToken);
        var studentsTask = studentRepository.GetListPagedAsync(0, 1, null, cancellationToken);
        var eventsTask = eventRepository.GetListPagedAsync(
            0,
            1,
            e => e.Status == EventStatus.Published && e.Audience == EventAudience.Public && e.StartDateUtc >= now,
            cancellationToken);

        await Task.WhenAll(clubsTask, activeClubsTask, studentsTask, eventsTask).ConfigureAwait(false);

        return DataResult<PublicStatsDto>.Success(new PublicStatsDto
        {
            ClubCount = clubsTask.Result.TotalCount,
            ActiveClubCount = activeClubsTask.Result.TotalCount,
            StudentCount = studentsTask.Result.TotalCount,
            UpcomingEventCount = eventsTask.Result.TotalCount,
        });
    }

    public async Task<IDataResult<IReadOnlyList<PublicClubCategoryDto>>> GetClubCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = await clubCategoryRepository.GetListAsync(null, cancellationToken).ConfigureAwait(false);
        var items = categories
            .OrderBy(c => c.Name)
            .ThenBy(c => c.Id)
            .Select(c => new PublicClubCategoryDto { Id = c.Id, Name = c.Name })
            .ToList();

        return DataResult<IReadOnlyList<PublicClubCategoryDto>>.Success(items);
    }

    private static int ClampPageSize(int pageSize) =>
        pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
}
