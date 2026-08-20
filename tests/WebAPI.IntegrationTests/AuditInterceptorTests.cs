using DataAccess;
using Entities;
using Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace WebAPI.IntegrationTests;

/// <summary>
/// docs/MIMARI.md · K-12/A-33/Y-44/Y-26: AuditSaveChangesInterceptor'ın Insert/Update/soft-delete
/// davranışını ve hassas alan (parola/hash) hariç tutmasını uçtan uca doğrular.
/// </summary>
[Collection("WebAPI Integration Tests")]
public sealed class AuditInterceptorTests : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;

    public AuditInterceptorTests(CustomWebApplicationFactory factory) => _factory = factory;

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact(DisplayName = "Audit: yeni kayıt eklendiğinde Insert satırı yazılır, OldValues null olur")]
    public async Task Insert_WritesAuditLogWithInsertAction()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var faculty = new Faculty { Name = $"Audit Fakültesi {Guid.NewGuid():N}" };
        db.Faculties.Add(faculty);
        await db.SaveChangesAsync();

        var log = await db.AuditLogs
            .SingleAsync(a => a.EntityType == nameof(Faculty) && a.EntityId == faculty.Id.ToString());

        Assert.Equal(AuditAction.Insert, log.Action);
        Assert.Null(log.OldValues);
        Assert.Contains(faculty.Name, log.NewValues);
    }

    [Fact(DisplayName = "Audit: güncelleme yapıldığında Update satırı eski/yeni değerlerle yazılır")]
    public async Task Update_WritesAuditLogWithOldAndNewValues()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var originalName = $"Eski Ad {Guid.NewGuid():N}";
        var faculty = new Faculty { Name = originalName };
        db.Faculties.Add(faculty);
        await db.SaveChangesAsync();

        faculty.Name = "Yeni Ad";
        await db.SaveChangesAsync();

        var log = await db.AuditLogs
            .Where(a => a.EntityType == nameof(Faculty) && a.EntityId == faculty.Id.ToString() && a.Action == AuditAction.Update)
            .SingleAsync();

        Assert.Contains(originalName, log.OldValues);
        Assert.Contains("Yeni Ad", log.NewValues);
    }

    [Fact(DisplayName = "Audit: soft delete (IsDeleted=true) Update değil Delete olarak kaydedilir (Y-44)")]
    public async Task SoftDelete_IsRecordedAsDeleteAction()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var faculty = new Faculty { Name = $"Audit Fakültesi {Guid.NewGuid():N}" };
        db.Faculties.Add(faculty);
        await db.SaveChangesAsync();

        var department = new Department { Name = $"Audit Bölümü {Guid.NewGuid():N}", FacultyId = faculty.Id };
        db.Departments.Add(department);
        await db.SaveChangesAsync();

        var term = new AcademicTerm
        {
            Name = $"Audit Dönemi {Guid.NewGuid():N}",
            StartDateUtc = DateTime.UtcNow,
            EndDateUtc = DateTime.UtcNow.AddMonths(4),
            IsCurrent = false,
        };
        db.AcademicTerms.Add(term);

        var advisorEmail = $"advisor-{Guid.NewGuid():N}@test.local";
        var studentEmail = $"student-{Guid.NewGuid():N}@test.local";
        var advisorUser = new ApplicationUser { UserName = advisorEmail, Email = advisorEmail };
        var studentUser = new ApplicationUser { UserName = studentEmail, Email = studentEmail };
        db.Users.AddRange(advisorUser, studentUser);
        await db.SaveChangesAsync();

        var advisor = new AcademicStaff { ApplicationUserId = advisorUser.Id, Title = "Dr.", DepartmentId = department.Id };
        db.AcademicStaff.Add(advisor);
        await db.SaveChangesAsync();

        var student = new Student
        {
            ApplicationUserId = studentUser.Id,
            StudentNumber = $"S{Guid.NewGuid():N}"[..12],
            DepartmentId = department.Id,
            EnrollmentYear = 2026,
        };
        db.Students.Add(student);
        await db.SaveChangesAsync();

        var club = new Club { Name = $"Audit Kulübü {Guid.NewGuid():N}", AdvisorId = advisor.Id, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        db.Clubs.Add(club);
        await db.SaveChangesAsync();

        var membership = new ClubMembership
        {
            ClubId = club.Id,
            StudentId = student.Id,
            AcademicTermId = term.Id,
            ClubRole = ClubRole.Member,
            JoinedAtUtc = DateTime.UtcNow,
        };
        db.ClubMemberships.Add(membership);
        await db.SaveChangesAsync();

        membership.IsDeleted = true;
        membership.DeletedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var log = await db.AuditLogs
            .Where(a => a.EntityType == nameof(ClubMembership) && a.EntityId == membership.Id.ToString())
            .OrderByDescending(a => a.Id)
            .FirstAsync();

        Assert.Equal(AuditAction.Delete, log.Action);
    }

    [Fact(DisplayName = "Audit: PasswordHash gibi hassas alanlar audit kaydına yazılmaz (Y-26)")]
    public async Task SensitiveFields_AreExcludedFromAuditPayload()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var email = $"secret-{Guid.NewGuid():N}@test.local";
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            PasswordHash = "super-secret-hash-value-should-not-leak",
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        var log = await db.AuditLogs
            .SingleAsync(a => a.EntityType == nameof(ApplicationUser) && a.EntityId == user.Id.ToString());

        Assert.DoesNotContain("super-secret-hash-value-should-not-leak", log.NewValues);
        Assert.DoesNotContain("PasswordHash", log.NewValues);
    }
}
