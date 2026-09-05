using System.Linq.Expressions;
using Business.Concrete;
using Core.DataAccess;
using Core.Utilities.Time;
using Entities;
using Entities.Enums;
using Moq;
using Xunit;

namespace Business.Tests;

/// <summary>
/// docs/PLAN-V2.md · Faz 14 (Y-58): anonim vitrinin filtrelerinin kodda sabit olduğunun kanıtı —
/// her testte gerçek bir predicate LINQ-to-Objects üzerinde çalıştırılır (Moq salt-geçiş değil).
/// </summary>
public class PublicContentManagerTests
{
    private static readonly DateTime FixedNow = new(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IEntityRepository<Club>> _clubRepository = new();
    private readonly Mock<IEntityRepository<Event>> _eventRepository = new();
    private readonly Mock<IEntityRepository<Announcement>> _announcementRepository = new();
    private readonly Mock<IEntityRepository<ClubCategory>> _clubCategoryRepository = new();
    private readonly Mock<IEntityRepository<Student>> _studentRepository = new();
    private readonly Mock<IEntityRepository<ClubSocialLink>> _clubSocialLinkRepository = new();
    private readonly Mock<IClock> _clock = new();
    private readonly PublicContentManager _sut;

    public PublicContentManagerTests()
    {
        _clock.Setup(c => c.UtcNow).Returns(FixedNow);
        _clubSocialLinkRepository
            .Setup(r => r.GetListAsync(It.IsAny<Expression<Func<ClubSocialLink, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        _sut = new PublicContentManager(
            _clubRepository.Object, _eventRepository.Object, _announcementRepository.Object,
            _clubCategoryRepository.Object, _studentRepository.Object, _clubSocialLinkRepository.Object, _clock.Object);
    }

    [Fact(DisplayName = "GetClubsAsync: yalnızca IsActive=true kulüpler döner, pasif kulüp listede yok")]
    public async Task GetClubsAsync_OnlyActiveClubsReturned()
    {
        var active = new Club { Id = 1, Name = "Aktif Kulüp", AdvisorId = 1, IsActive = true, CreatedAtUtc = FixedNow };
        var inactive = new Club { Id = 2, Name = "Pasif Kulüp", AdvisorId = 1, IsActive = false, CreatedAtUtc = FixedNow };
        SetupPagedFilter<Club, string>(_clubRepository, [active, inactive]);

        var result = await _sut.GetClubsAsync(0, 20);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Data!.Items);
        Assert.Equal("Aktif Kulüp", item.Name);
    }

    [Fact(DisplayName = "K-35: anonim vitrin categoryId ile filtrelenir ve kategori adını taşır")]
    public async Task GetClubsAsync_CategoryFilter_ReturnsOnlyMatchingWithName()
    {
        var inCategory = new Club { Id = 1, Name = "Kategorili", AdvisorId = 1, IsActive = true, CreatedAtUtc = FixedNow, ClubCategoryId = 4 };
        var otherCategory = new Club { Id = 2, Name = "Başka Kategori", AdvisorId = 1, IsActive = true, CreatedAtUtc = FixedNow, ClubCategoryId = 9 };
        var noCategory = new Club { Id = 3, Name = "Kategorisiz", AdvisorId = 1, IsActive = true, CreatedAtUtc = FixedNow };
        SetupPagedFilter<Club, string>(_clubRepository, [inCategory, otherCategory, noCategory]);

        _clubCategoryRepository
            .Setup(r => r.GetListAsync(It.IsAny<Expression<Func<ClubCategory, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ClubCategory { Id = 4, Name = "Bilim" }]);

        var result = await _sut.GetClubsAsync(0, 20, null, 4);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Data!.Items);
        Assert.Equal("Kategorili", item.Name);
        Assert.Equal("Bilim", item.ClubCategoryName);
    }

    [Fact(DisplayName = "K-35: categoryId verilmezse tüm aktif kulüpler döner (filtre gevşemez, sadece uygulanmaz)")]
    public async Task GetClubsAsync_NoCategoryFilter_ReturnsAllActive()
    {
        var withCategory = new Club { Id = 1, Name = "Kategorili", AdvisorId = 1, IsActive = true, CreatedAtUtc = FixedNow, ClubCategoryId = 4 };
        var withoutCategory = new Club { Id = 2, Name = "Kategorisiz", AdvisorId = 1, IsActive = true, CreatedAtUtc = FixedNow };
        var inactive = new Club { Id = 3, Name = "Pasif", AdvisorId = 1, IsActive = false, CreatedAtUtc = FixedNow, ClubCategoryId = 4 };
        SetupPagedFilter<Club, string>(_clubRepository, [withCategory, withoutCategory, inactive]);

        _clubCategoryRepository
            .Setup(r => r.GetListAsync(It.IsAny<Expression<Func<ClubCategory, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new ClubCategory { Id = 4, Name = "Bilim" }]);

        var result = await _sut.GetClubsAsync(0, 20);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.Items.Count);
        Assert.DoesNotContain(result.Data.Items, i => i.Name == "Pasif");
    }

    [Fact(DisplayName = "Alfabetik filtre: letter ile adın ilk harfi SQL'de eşleşir, Ç C'ye düşmez")]
    public async Task GetClubsAsync_LetterFilter_ReturnsNamesStartingWithLetter()
    {
        var bilim = new Club { Id = 1, Name = "Bilim Topluluğu", AdvisorId = 1, IsActive = true, CreatedAtUtc = FixedNow };
        var kucukB = new Club { Id = 5, Name = "bisiklet Topluluğu", AdvisorId = 1, IsActive = true, CreatedAtUtc = FixedNow };
        var cevre = new Club { Id = 2, Name = "Çevre Topluluğu", AdvisorId = 1, IsActive = true, CreatedAtUtc = FixedNow };
        var sanat = new Club { Id = 3, Name = "Sanat Topluluğu", AdvisorId = 1, IsActive = true, CreatedAtUtc = FixedNow };
        var inactive = new Club { Id = 4, Name = "Bilinmeyen Pasif", AdvisorId = 1, IsActive = false, CreatedAtUtc = FixedNow };
        SetupPagedFilter<Club, string>(_clubRepository, [bilim, kucukB, cevre, sanat, inactive]);

        var bResult = await _sut.GetClubsAsync(0, 20, letter: "b");
        Assert.Equal(2, bResult.Data!.Items.Count);
        Assert.Contains(bResult.Data.Items, i => i.Name == "Bilim Topluluğu");
        Assert.Contains(bResult.Data.Items, i => i.Name == "bisiklet Topluluğu");

        var cResult = await _sut.GetClubsAsync(0, 20, letter: "C");
        Assert.Empty(cResult.Data!.Items);

        var ceResult = await _sut.GetClubsAsync(0, 20, letter: "ç");
        Assert.Equal("Çevre Topluluğu", Assert.Single(ceResult.Data!.Items).Name);
    }

    [Fact(DisplayName = "GetClubCategoriesAsync: kategoriler ada göre sıralı döner, yalnızca id ve ad taşır")]
    public async Task GetClubCategoriesAsync_ReturnsSortedNames()
    {
        _clubCategoryRepository
            .Setup(r => r.GetListAsync(It.IsAny<Expression<Func<ClubCategory, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new ClubCategory { Id = 2, Name = "Spor" },
                new ClubCategory { Id = 1, Name = "Bilim" },
                new ClubCategory { Id = 3, Name = "Sanat" },
            ]);

        var result = await _sut.GetClubCategoriesAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(["Bilim", "Sanat", "Spor"], result.Data!.Select(c => c.Name).ToArray());
        Assert.Equal(1, result.Data[0].Id);
    }

    [Fact(DisplayName = "GetClubByIdAsync: aktif kulüp için detay döner (AdvisorId gibi iç alan taşımaz)")]
    public async Task GetClubByIdAsync_ActiveClub_ReturnsDetail()
    {
        var club = new Club { Id = 1, Name = "Aktif Kulüp", Description = "Açıklama", AdvisorId = 7, LogoFileId = 3, IsActive = true, CreatedAtUtc = FixedNow };
        _clubRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Club, bool>> filter, CancellationToken _) => new[] { club }.AsQueryable().Where(filter).FirstOrDefault());

        var result = await _sut.GetClubByIdAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Equal("Aktif Kulüp", result.Data!.Name);
        Assert.Equal(3, result.Data.LogoFileId);
    }

    [Fact(DisplayName = "GetClubByIdAsync: pasif kulüp NotFound döner (aktif/pasif ayrımı gizlenmez ama içerik sızmaz)")]
    public async Task GetClubByIdAsync_InactiveClub_ReturnsNotFound()
    {
        var club = new Club { Id = 1, Name = "Pasif Kulüp", AdvisorId = 7, IsActive = false, CreatedAtUtc = FixedNow };
        _clubRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Club, bool>> filter, CancellationToken _) => new[] { club }.AsQueryable().Where(filter).FirstOrDefault());

        var result = await _sut.GetClubByIdAsync(1);

        Assert.False(result.IsSuccess);
    }

    [Fact(DisplayName = "GetEventsAsync: Draft/PendingApproval/Rejected ve geçmiş etkinlikler elenir, yalnızca yayındaki gelecek etkinlik döner")]
    public async Task GetEventsAsync_FiltersOutNonPublishedAndPast_OnlyPublishedFutureReturned()
    {
        var club = new Club { Id = 1, Name = "Kulüp", AdvisorId = 1, IsActive = true, CreatedAtUtc = FixedNow };
        var published = new Event { Id = 1, ClubId = 1, Title = "Yayında", StartDateUtc = FixedNow.AddDays(1), EndDateUtc = FixedNow.AddDays(1).AddHours(2), Status = EventStatus.Published, CreatedAtUtc = FixedNow };
        var draft = new Event { Id = 2, ClubId = 1, Title = "Taslak", StartDateUtc = FixedNow.AddDays(1), EndDateUtc = FixedNow.AddDays(1).AddHours(2), Status = EventStatus.Draft, CreatedAtUtc = FixedNow };
        var pending = new Event { Id = 3, ClubId = 1, Title = "Onay Bekliyor", StartDateUtc = FixedNow.AddDays(1), EndDateUtc = FixedNow.AddDays(1).AddHours(2), Status = EventStatus.PendingApproval, CreatedAtUtc = FixedNow };
        var rejected = new Event { Id = 4, ClubId = 1, Title = "Reddedildi", StartDateUtc = FixedNow.AddDays(1), EndDateUtc = FixedNow.AddDays(1).AddHours(2), Status = EventStatus.Rejected, CreatedAtUtc = FixedNow };
        var past = new Event { Id = 5, ClubId = 1, Title = "Geçmiş", StartDateUtc = FixedNow.AddDays(-1), EndDateUtc = FixedNow.AddDays(-1).AddHours(2), Status = EventStatus.Published, CreatedAtUtc = FixedNow };
        SetupPagedFilter<Event, DateTime>(_eventRepository, [published, draft, pending, rejected, past]);
        _clubRepository.Setup(r => r.GetListAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([club]);

        var result = await _sut.GetEventsAsync(null, 0, 20);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Data!.Items);
        Assert.Equal("Yayında", item.Title);
        Assert.Equal("Kulüp", item.ClubName);
    }

    [Fact(DisplayName = "GetEventsAsync: clubId verilirse yalnızca o kulübün etkinlikleri döner")]
    public async Task GetEventsAsync_ClubIdFilter_ScopesToSingleClub()
    {
        var clubA = new Club { Id = 1, Name = "A Kulübü", AdvisorId = 1, IsActive = true, CreatedAtUtc = FixedNow };
        var clubB = new Club { Id = 2, Name = "B Kulübü", AdvisorId = 1, IsActive = true, CreatedAtUtc = FixedNow };
        var eventA = new Event { Id = 1, ClubId = 1, Title = "A Etkinliği", StartDateUtc = FixedNow.AddDays(1), EndDateUtc = FixedNow.AddDays(1).AddHours(2), Status = EventStatus.Published, CreatedAtUtc = FixedNow };
        var eventB = new Event { Id = 2, ClubId = 2, Title = "B Etkinliği", StartDateUtc = FixedNow.AddDays(1), EndDateUtc = FixedNow.AddDays(1).AddHours(2), Status = EventStatus.Published, CreatedAtUtc = FixedNow };
        SetupPagedFilter<Event, DateTime>(_eventRepository, [eventA, eventB]);
        _clubRepository.Setup(r => r.GetListAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([clubA, clubB]);

        var result = await _sut.GetEventsAsync(1, 0, 20);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Data!.Items);
        Assert.Equal("A Etkinliği", item.Title);
    }

    [Fact(DisplayName = "Y-72: ClubMembers kitleli etkinlik anonim vitrinde görünmez, Public görünür")]
    public async Task GetEventsAsync_MembersOnlyAudience_IsExcluded()
    {
        var publicEvent = new Event
        {
            Id = 1, ClubId = 1, Title = "Herkese Açık",
            StartDateUtc = FixedNow.AddDays(1), EndDateUtc = FixedNow.AddDays(1).AddHours(2),
            Status = EventStatus.Published, Audience = EventAudience.Public, CreatedAtUtc = FixedNow,
        };
        var membersOnly = new Event
        {
            Id = 2, ClubId = 1, Title = "Üyelere Özel",
            StartDateUtc = FixedNow.AddDays(1), EndDateUtc = FixedNow.AddDays(1).AddHours(2),
            Status = EventStatus.Published, Audience = EventAudience.ClubMembers, CreatedAtUtc = FixedNow,
        };
        SetupPagedFilter<Event, DateTime>(_eventRepository, [publicEvent, membersOnly]);
        _clubRepository.Setup(r => r.GetListAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await _sut.GetEventsAsync(null, 0, 20);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Data!.Items);
        Assert.Equal("Herkese Açık", item.Title);
    }

    [Fact(DisplayName = "GetAnnouncementsAsync: yalnızca Visibility=Public duyurular döner, Members görünürlüklü sızmaz")]
    public async Task GetAnnouncementsAsync_FiltersOutMembersOnly_OnlyPublicReturned()
    {
        var club = new Club { Id = 1, Name = "Kulüp", AdvisorId = 1, IsActive = true, CreatedAtUtc = FixedNow };
        var pub = new Announcement { Id = 1, ClubId = 1, Title = "Herkese Açık", Content = "İçerik", Visibility = AnnouncementVisibility.Public, PublishedAtUtc = FixedNow };
        var membersOnly = new Announcement { Id = 2, ClubId = 1, Title = "Yalnızca Üyeler", Content = "Gizli İçerik", Visibility = AnnouncementVisibility.Members, PublishedAtUtc = FixedNow };
        SetupPagedFilter<Announcement, DateTime>(_announcementRepository, [pub, membersOnly]);
        _clubRepository.Setup(r => r.GetListAsync(It.IsAny<Expression<Func<Club, bool>>>(), It.IsAny<CancellationToken>())).ReturnsAsync([club]);

        var result = await _sut.GetAnnouncementsAsync(null, 0, 20);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Data!.Items);
        Assert.Equal("Herkese Açık", item.Title);
    }

    [Fact(DisplayName = "GetStatsAsync: aktif/pasif kulüp, öğrenci ve yaklaşan herkese açık etkinlik sayılır")]
    public async Task GetStatsAsync_CountsActiveInactiveClubsStudentsAndUpcomingPublicEvents()
    {
        var active = new Club { Id = 1, Name = "Aktif", AdvisorId = 1, IsActive = true, CreatedAtUtc = FixedNow };
        var inactive = new Club { Id = 2, Name = "Pasif", AdvisorId = 1, IsActive = false, CreatedAtUtc = FixedNow };
        SetupCountFilter(_clubRepository, [active, inactive]);

        SetupCountFilter(_studentRepository, [
            new Student { Id = 1, ApplicationUserId = 10, StudentNumber = "1", DepartmentId = 1, EnrollmentYear = 2024 },
            new Student { Id = 2, ApplicationUserId = 11, StudentNumber = "2", DepartmentId = 1, EnrollmentYear = 2025 },
            new Student { Id = 3, ApplicationUserId = 12, StudentNumber = "3", DepartmentId = 1, EnrollmentYear = 2026 },
        ]);

        var upcomingPublic = new Event
        {
            Id = 1, ClubId = 1, Title = "Yaklaşan", StartDateUtc = FixedNow.AddDays(2), EndDateUtc = FixedNow.AddDays(2).AddHours(2),
            Status = EventStatus.Published, Audience = EventAudience.Public, CreatedAtUtc = FixedNow,
        };
        var pastPublic = new Event
        {
            Id = 2, ClubId = 1, Title = "Geçmiş", StartDateUtc = FixedNow.AddDays(-2), EndDateUtc = FixedNow.AddDays(-2).AddHours(2),
            Status = EventStatus.Published, Audience = EventAudience.Public, CreatedAtUtc = FixedNow,
        };
        var membersOnly = new Event
        {
            Id = 3, ClubId = 1, Title = "Üye", StartDateUtc = FixedNow.AddDays(2), EndDateUtc = FixedNow.AddDays(2).AddHours(2),
            Status = EventStatus.Published, Audience = EventAudience.ClubMembers, CreatedAtUtc = FixedNow,
        };
        SetupCountFilter(_eventRepository, [upcomingPublic, pastPublic, membersOnly]);

        var result = await _sut.GetStatsAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Data!.ClubCount);
        Assert.Equal(1, result.Data.ActiveClubCount);
        Assert.Equal(3, result.Data.StudentCount);
        Assert.Equal(1, result.Data.UpcomingEventCount);
    }

    /// <summary>
    /// Y-64: sıralı aşırı yükleme taklit edilir ve sıralama <b>gerçekten uygulanır</b> — böylece test
    /// hem filtreyi hem sırayı LINQ-to-Objects üzerinde doğrular, Moq salt-geçiş olmaz.
    /// </summary>
    private static void SetupPagedFilter<T, TKey>(Mock<IEntityRepository<T>> repository, T[] all) where T : class, Core.Entities.IEntity
    {
        repository
            .Setup(r => r.GetListPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<T, bool>>>(),
                It.IsAny<Expression<Func<T, TKey>>>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((int pageIndex, int pageSize, Expression<Func<T, bool>> filter, Expression<Func<T, TKey>> orderBy, bool descending, CancellationToken _) =>
            {
                var filtered = all.AsQueryable().Where(filter);
                var ordered = descending
                    ? filtered.OrderByDescending(orderBy).ThenBy(e => e.Id)
                    : filtered.OrderBy(orderBy).ThenBy(e => e.Id);

                var matched = ordered.ToList();
                return new PagedResult<T>(matched, matched.Count, pageIndex, pageSize);
            });
    }

    private static void SetupCountFilter<T>(Mock<IEntityRepository<T>> repository, T[] all) where T : class, Core.Entities.IEntity
    {
        repository
            .Setup(r => r.GetListPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<T, bool>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((int pageIndex, int pageSize, Expression<Func<T, bool>>? filter, CancellationToken _) =>
            {
                var matched = filter is null ? all.ToList() : all.AsQueryable().Where(filter).ToList();
                return new PagedResult<T>(matched.Take(Math.Max(pageSize, 0)).ToList(), matched.Count, pageIndex, pageSize);
            });
    }
}
