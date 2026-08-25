using Business.Abstract;
using DataAccess;
using Entities;
using Entities.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebAPI.IntegrationTests;

/// <summary>
/// docs/PLAN-V5.md · Faz 28 (K-34, Y-68): demo veri üretimi.
///
/// Amaç sadece "veri oluştu mu" değil — planın çıkış koşulu <b>her durum rozetinin en az bir
/// gerçek kaydı olması</b>. Bu yüzden testler sayı değil, <i>kapsam</i> ölçer: her EventStatus,
/// her ClubRole, her AnnouncementVisibility, dolu bir kontenjan ve bir sistem duyurusu.
/// </summary>
[Collection("WebAPI Integration Tests")]
public sealed class DemoDataSeedTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private WebApplicationFactory<Program> _demoFactory = null!;

    public DemoDataSeedTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        _demoFactory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configBuilder) =>
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Seed:Demo"] = "true",
                    ["Seed:DemoPassword"] = "Demo!Test123456",
                })));

        // Host'un ayağa kalkması demo seed'i çalıştırır (Program.cs).
        using var scope = _demoFactory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
    }

    public Task DisposeAsync()
    {
        _demoFactory.Dispose();
        return Task.CompletedTask;
    }

    [Fact(DisplayName = "Demo seed kulüp, üyelik, etkinlik, katılım ve duyuru üretir")]
    public async Task Seeder_ProducesEveryScreensData()
    {
        using var scope = _demoFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.Equal(8, await CountTaggedAsync(db, DemoEntityTypes.Club));
        Assert.Equal(6, await CountTaggedAsync(db, DemoEntityTypes.AcademicStaff));
        Assert.Equal(25, await CountTaggedAsync(db, DemoEntityTypes.Student));
        Assert.Equal(20, await CountTaggedAsync(db, DemoEntityTypes.Event));
        Assert.Equal(12, await CountTaggedAsync(db, DemoEntityTypes.Announcement));
        Assert.Equal(37, await CountTaggedAsync(db, DemoEntityTypes.ClubMembership));
        Assert.Equal(10, await CountTaggedAsync(db, DemoEntityTypes.MembershipApplication));
        Assert.Equal(3, await CountTaggedAsync(db, DemoEntityTypes.ClubApplication));
        Assert.True(await CountTaggedAsync(db, DemoEntityTypes.EventParticipation) >= 40);
    }

    [Fact(DisplayName = "Her etkinlik durumunun en az bir gerçek kaydı var")]
    public async Task Seeder_CoversEveryEventStatus()
    {
        using var scope = _demoFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var statuses = await db.Events.Select(e => e.Status).Distinct().ToListAsync();

        foreach (var status in Enum.GetValues<EventStatus>())
        {
            Assert.Contains(status, statuses);
        }

        // A-49: iptal edilmiş etkinlik gerekçesiz olamaz — rozetin gerçek hâli budur.
        var cancelled = await db.Events.Where(e => e.Status == EventStatus.Cancelled).ToListAsync();
        Assert.NotEmpty(cancelled);
        Assert.All(cancelled, e => Assert.False(string.IsNullOrWhiteSpace(e.CancellationReason)));
    }

    [Fact(DisplayName = "Her topluluk rolünün kaydı var ve kulüp başına tek başkan atanmış (A-39)")]
    public async Task Seeder_CoversEveryClubRoleWithSinglePresidentPerClub()
    {
        using var scope = _demoFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var roles = await db.ClubMemberships.Select(m => m.ClubRole).Distinct().ToListAsync();
        foreach (var role in Enum.GetValues<ClubRole>())
        {
            Assert.Contains(role, roles);
        }

        var presidentsPerClub = await db.ClubMemberships
            .Where(m => m.ClubRole == ClubRole.President)
            .GroupBy(m => new { m.ClubId, m.AcademicTermId })
            .Select(g => g.Count())
            .ToListAsync();

        Assert.NotEmpty(presidentsPerClub);
        Assert.All(presidentsPerClub, count => Assert.Equal(1, count));
    }

    [Fact(DisplayName = "Kontenjanı dolan bir etkinlik ve pasif bir kulüp bulunur")]
    public async Task Seeder_ProducesFullCapacityEventAndInactiveClub()
    {
        using var scope = _demoFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var fullEvent = await db.Events
            .Where(e => e.Capacity != null && e.Status == EventStatus.Published)
            .Select(e => new { e.Id, e.Capacity, Registered = db.EventParticipations.Count(p => p.EventId == e.Id) })
            .Where(x => x.Registered >= x.Capacity)
            .FirstOrDefaultAsync();

        Assert.NotNull(fullEvent);
        Assert.Contains(await db.Clubs.ToListAsync(), c => !c.IsActive);
    }

    [Fact(DisplayName = "Hem herkese açık hem yalnızca üyelere açık duyuru ve bir sistem duyurusu var")]
    public async Task Seeder_CoversAnnouncementVisibilityAndSystemAnnouncement()
    {
        using var scope = _demoFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var visibilities = await db.Announcements.Select(a => a.Visibility).Distinct().ToListAsync();
        Assert.Contains(AnnouncementVisibility.Public, visibilities);
        Assert.Contains(AnnouncementVisibility.Members, visibilities);

        Assert.True(await db.Announcements.AnyAsync(a => a.ClubId == null));
    }

    [Fact(DisplayName = "Y-68: üretilen her kulüp, etkinlik ve duyuru künyelidir")]
    public async Task Seeder_TagsEveryRecordItCreates()
    {
        using var scope = _demoFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await AssertFullyTaggedAsync(db, DemoEntityTypes.Club, await db.Clubs.Select(c => c.Id).ToListAsync());
        await AssertFullyTaggedAsync(db, DemoEntityTypes.Event, await db.Events.Select(e => e.Id).ToListAsync());
        await AssertFullyTaggedAsync(db, DemoEntityTypes.Announcement, await db.Announcements.Select(a => a.Id).ToListAsync());
    }

    [Fact(DisplayName = "Seeder ikinci kez çalıştığında yeni satır üretmez")]
    public async Task Seeder_RunTwice_IsIdempotent()
    {
        using var scope = _demoFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var before = await db.DemoSeedRecords.CountAsync();
        Assert.True(before > 0);

        var outcome = await scope.ServiceProvider.GetRequiredService<IDemoDataSeeder>().SeedAsync();

        Assert.Equal(DemoSeedOutcome.AlreadyPresent, outcome);
        Assert.Equal(before, await db.DemoSeedRecords.CountAsync());
    }

    [Fact(DisplayName = "Demo hesapların e-posta alan adı yönlendirilemezdir")]
    public async Task Seeder_UsesNonRoutableEmailDomain()
    {
        using var scope = _demoFactory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var demoUserIds = await db.DemoSeedRecords
            .Where(r => r.EntityType == DemoEntityTypes.ApplicationUser)
            .Select(r => r.EntityId)
            .ToListAsync();

        Assert.Equal(31, demoUserIds.Count);

        var emails = await db.Users.Where(u => demoUserIds.Contains(u.Id)).Select(u => u.Email).ToListAsync();
        Assert.All(emails, email => Assert.EndsWith(".invalid", email));
    }

    private static Task<int> CountTaggedAsync(AppDbContext db, string entityType) =>
        db.DemoSeedRecords.CountAsync(r => r.EntityType == entityType);

    private static async Task AssertFullyTaggedAsync(AppDbContext db, string entityType, List<int> ids)
    {
        var tagged = await db.DemoSeedRecords
            .Where(r => r.EntityType == entityType)
            .Select(r => r.EntityId)
            .ToListAsync();

        Assert.Equal(ids.OrderBy(id => id), tagged.OrderBy(id => id));
    }
}
