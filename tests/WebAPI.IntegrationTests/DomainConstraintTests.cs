using Core.DataAccess;
using DataAccess;
using Entities;
using Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebAPI.IntegrationTests;

/// <summary>
/// docs/MIMARI.md · Faz 4 "bitti sayılır": çift üyelik ve kontenjan aşımı veritabanı seviyesinde
/// reddediliyor (A-15). Y-34: her test kendi verisini kurar; fixture başına benzersiz LocalDB kullanılır.
/// </summary>
[Collection("WebAPI Integration Tests")]
public sealed class DomainConstraintTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public DomainConstraintTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "A-15: aynı öğrenci aynı kulübe aynı dönemde ikinci kez üye yapılamaz (unique index)")]
    public async Task ClubMembership_DuplicateInSameTerm_IsRejectedByDatabase()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var (club, student, term) = await SeedClubStudentTermAsync(db, "dup");

        db.ClubMemberships.Add(new ClubMembership
        {
            ClubId = club.Id,
            StudentId = student.Id,
            AcademicTermId = term.Id,
            ClubRole = ClubRole.Member,
            JoinedAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        db.ClubMemberships.Add(new ClubMembership
        {
            ClubId = club.Id,
            StudentId = student.Id,
            AcademicTermId = term.Id,
            ClubRole = ClubRole.Member,
            JoinedAtUtc = DateTime.UtcNow,
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact(DisplayName = "A-15: aynı öğrenci aynı etkinliğe ikinci kez kaydolamaz (unique index)")]
    public async Task EventParticipation_DuplicateRegistration_IsRejectedByDatabase()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var (club, student, _) = await SeedClubStudentTermAsync(db, "evtdup");
        var @event = await SeedEventAsync(db, club.Id, capacity: 10);

        db.EventParticipations.Add(new EventParticipation
        {
            EventId = @event.Id,
            StudentId = student.Id,
            RegisteredAtUtc = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        db.EventParticipations.Add(new EventParticipation
        {
            EventId = @event.Id,
            StudentId = student.Id,
            RegisteredAtUtc = DateTime.UtcNow,
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact(DisplayName = "A-15: eşzamanlı iki güncelleme aynı Event satırında RowVersion çakışmasıyla reddedilir (kontenjan aşımını önleyen mekanizma)")]
    public async Task Event_ConcurrentCapacityUpdates_SecondWriteRejectedByRowVersion()
    {
        using var setupScope = _factory.Services.CreateScope();
        var setupDb = setupScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var (club, _, _) = await SeedClubStudentTermAsync(setupDb, "cap");
        var @event = await SeedEventAsync(setupDb, club.Id, capacity: 1);

        using var scopeA = _factory.Services.CreateScope();
        using var scopeB = _factory.Services.CreateScope();
        var dbA = scopeA.ServiceProvider.GetRequiredService<AppDbContext>();
        var dbB = scopeB.ServiceProvider.GetRequiredService<AppDbContext>();

        // İki "eşzamanlı" isteğin aynı satırı okuyup değiştirmeye çalıştığı senaryo: ikincisi eski
        // RowVersion ile geldiği için reddedilmeli — Faz 10'un EventParticipationManager.RegisterAsync'in
        // dayandığı mekanizma budur. AppDbContext.SaveChangesAsync EF'in DbUpdateConcurrencyException'ını
        // Y-08 gereği ConcurrencyConflictException'a çevirir (bkz. AppDbContext.cs).
        var eventA = await dbA.Events.SingleAsync(e => e.Id == @event.Id);
        var eventB = await dbB.Events.SingleAsync(e => e.Id == @event.Id);

        eventA.Capacity = 0;
        await dbA.SaveChangesAsync();

        eventB.Capacity = 0;
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => dbB.SaveChangesAsync());
    }

    private static async Task<Event> SeedEventAsync(AppDbContext db, int clubId, int capacity)
    {
        var @event = new Event
        {
            ClubId = clubId,
            Title = "Test Etkinliği",
            StartDateUtc = DateTime.UtcNow.AddDays(1),
            EndDateUtc = DateTime.UtcNow.AddDays(1).AddHours(2),
            Capacity = capacity,
            Status = EventStatus.Published,
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.Events.Add(@event);
        await db.SaveChangesAsync();

        return @event;
    }

    private static async Task<(Club Club, Student Student, AcademicTerm Term)> SeedClubStudentTermAsync(AppDbContext db, string suffix)
    {
        var faculty = new Faculty { Name = $"Fen Fakültesi-{suffix}" };
        db.Faculties.Add(faculty);
        await db.SaveChangesAsync();

        var department = new Department { Name = $"Bilgisayar Mühendisliği-{suffix}", FacultyId = faculty.Id };
        db.Departments.Add(department);
        await db.SaveChangesAsync();

        var term = new AcademicTerm
        {
            Name = $"2026-Güz-{suffix}",
            StartDateUtc = DateTime.UtcNow,
            EndDateUtc = DateTime.UtcNow.AddMonths(4),
            IsCurrent = false,
        };
        db.AcademicTerms.Add(term);
        await db.SaveChangesAsync();

        var advisorUser = new ApplicationUser { UserName = $"advisor-{suffix}@test.local", Email = $"advisor-{suffix}@test.local" };
        var studentUser = new ApplicationUser { UserName = $"student-{suffix}@test.local", Email = $"student-{suffix}@test.local" };
        db.Users.AddRange(advisorUser, studentUser);
        await db.SaveChangesAsync();

        var advisor = new AcademicStaff { ApplicationUserId = advisorUser.Id, Title = "Dr. Öğr. Üyesi", DepartmentId = department.Id };
        db.AcademicStaff.Add(advisor);
        await db.SaveChangesAsync();

        var student = new Student
        {
            ApplicationUserId = studentUser.Id,
            StudentNumber = $"2026{suffix}001",
            DepartmentId = department.Id,
            EnrollmentYear = 2026,
        };
        db.Students.Add(student);
        await db.SaveChangesAsync();

        var club = new Club
        {
            Name = $"Yazılım Kulübü-{suffix}",
            AdvisorId = advisor.Id,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
        };
        db.Clubs.Add(club);
        await db.SaveChangesAsync();

        return (club, student, term);
    }

    [Fact(DisplayName = "K-38: Event.Audience varsayılan olarak Public kaydedilir (mevcut davranış korunur)")]
    public async Task Event_AudienceNotSet_DefaultsToPublic()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var (club, _, _) = await SeedClubStudentTermAsync(db, "aud1");

        var @event = new Event
        {
            ClubId = club.Id,
            Title = "Varsayılan Kitle",
            StartDateUtc = new DateTime(2026, 10, 1, 10, 0, 0, DateTimeKind.Utc),
            EndDateUtc = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc),
            Status = EventStatus.Draft,
            CreatedAtUtc = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
        };
        db.Events.Add(@event);
        await db.SaveChangesAsync();

        var reloaded = await db.Events.AsNoTracking().SingleAsync(e => e.Id == @event.Id);
        Assert.Equal(EventAudience.Public, reloaded.Audience);
    }

    [Fact(DisplayName = "K-38: Event.Audience = ClubMembers kaydedilip aynı değerle okunur")]
    public async Task Event_AudienceClubMembers_RoundTrips()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var (club, _, _) = await SeedClubStudentTermAsync(db, "aud2");

        var @event = new Event
        {
            ClubId = club.Id,
            Title = "Üyelere Özel",
            StartDateUtc = new DateTime(2026, 10, 2, 10, 0, 0, DateTimeKind.Utc),
            EndDateUtc = new DateTime(2026, 10, 2, 12, 0, 0, DateTimeKind.Utc),
            Status = EventStatus.Draft,
            Audience = EventAudience.ClubMembers,
            CreatedAtUtc = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
        };
        db.Events.Add(@event);
        await db.SaveChangesAsync();

        var reloaded = await db.Events.AsNoTracking().SingleAsync(e => e.Id == @event.Id);
        Assert.Equal(EventAudience.ClubMembers, reloaded.Audience);
    }

    [Fact(DisplayName = "K-39: AcademicTerm başvuru penceresini taşır — tarihler nullable, override varsayılanı FollowSchedule")]
    public async Task AcademicTerm_ClubApplicationWindow_RoundTrips()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var term = new AcademicTerm
        {
            Name = $"Pencere Dönemi {Guid.NewGuid():N}"[..30],
            StartDateUtc = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDateUtc = new DateTime(2027, 1, 31, 0, 0, 0, DateTimeKind.Utc),
            IsCurrent = false,
        };
        db.AcademicTerms.Add(term);
        await db.SaveChangesAsync();

        // A-66: pencere tanımsız başlar ve varsayılan FollowSchedule'dır — yani KAPALI (fail-closed).
        var fresh = await db.AcademicTerms.AsNoTracking().SingleAsync(t => t.Id == term.Id);
        Assert.Null(fresh.ClubApplicationStartUtc);
        Assert.Null(fresh.ClubApplicationEndUtc);
        Assert.Equal(ClubApplicationWindowOverride.FollowSchedule, fresh.ClubApplicationOverride);

        term.ClubApplicationStartUtc = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        term.ClubApplicationEndUtc = new DateTime(2026, 10, 15, 23, 59, 59, DateTimeKind.Utc);
        term.ClubApplicationOverride = ClubApplicationWindowOverride.ForceClosed;
        await db.SaveChangesAsync();

        var updated = await db.AcademicTerms.AsNoTracking().SingleAsync(t => t.Id == term.Id);
        Assert.Equal(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), updated.ClubApplicationStartUtc);
        Assert.Equal(ClubApplicationWindowOverride.ForceClosed, updated.ClubApplicationOverride);
    }

    [Fact(DisplayName = "A-66: migration mevcut GÜNCEL dönemi ForceOpen işaretler — dağıtımda kesinti olmaz")]
    public async Task Migration_MarksCurrentTermForceOpen()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var current = await db.AcademicTerms.AsNoTracking().SingleOrDefaultAsync(t => t.IsCurrent);
        Assert.NotNull(current);

        // Bu satır olmadan fail-closed varsayılan, bugüne kadar hep açık olan akışı sessizce durdururdu.
        Assert.Equal(ClubApplicationWindowOverride.ForceOpen, current!.ClubApplicationOverride);
    }

    [Fact(DisplayName = "A-60: aynı adla ikinci kategori DB seviyesinde reddedilir (unique index)")]
    public async Task ClubCategory_DuplicateName_IsRejectedByDatabase()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var name = $"Kategori {Guid.NewGuid():N}"[..20];
        db.ClubCategories.Add(new ClubCategory { Name = name });
        await db.SaveChangesAsync();

        db.ClubCategories.Add(new ClubCategory { Name = name });

        await Assert.ThrowsAnyAsync<Exception>(() => db.SaveChangesAsync());
    }

    [Fact(DisplayName = "A-60: kullanımdaki kategori silinemez (FK Restrict)")]
    public async Task ClubCategory_InUse_CannotBeDeleted()
    {
        int categoryId;

        using (var seedScope = _factory.Services.CreateScope())
        {
            var seedDb = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var (club, _, _) = await SeedClubStudentTermAsync(seedDb, "kat1");

            var category = new ClubCategory { Name = $"Kullanımda {Guid.NewGuid():N}"[..20] };
            seedDb.ClubCategories.Add(category);
            await seedDb.SaveChangesAsync();
            categoryId = category.Id;

            club.ClubCategoryId = categoryId;
            await seedDb.SaveChangesAsync();
        }

        // Silme AYRI bir context'te: kulüp izlenmiyor, tıpkı üretimdeki
        // ReferenceDataManager.DeleteClubCategoryAsync gibi (o da yalnızca kategoriyi yükler).
        // Aynı context'te silinseydi EF, DELETE'i göndermeden önce izlediği kulübün FK'sını
        // null'a çeker ve kısıt hiç sınanmazdı — test kuralı değil, EF'in fixup'ını ölçerdi.
        using var deleteScope = _factory.Services.CreateScope();
        var db = deleteScope.ServiceProvider.GetRequiredService<AppDbContext>();

        db.ClubCategories.Remove(await db.ClubCategories.SingleAsync(c => c.Id == categoryId));

        await Assert.ThrowsAnyAsync<Exception>(() => db.SaveChangesAsync());
    }

    [Fact(DisplayName = "A-60: kategori nullable — kategorisiz kulüp kaydedilebilir")]
    public async Task Club_WithoutCategory_IsAccepted()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var (club, _, _) = await SeedClubStudentTermAsync(db, "kat2");

        var reloaded = await db.Clubs.AsNoTracking().SingleAsync(c => c.Id == club.Id);
        Assert.Null(reloaded.ClubCategoryId);
    }
}
