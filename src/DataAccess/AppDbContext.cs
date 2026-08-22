using Core.DataAccess;
using DataAccess.Configurations;
using DataAccess.Seed;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

/// <summary>
/// docs/MIMARI.md · A-05: IUnitOfWork'ün somut implementasyonu. Faz 3'te Identity + RefreshToken,
/// Faz 4'te domain entity'leri (Club, Event, ...) ve audit tablosu eklendi.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, int>(options), IUnitOfWork
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<Faculty> Faculties => Set<Faculty>();

    public DbSet<Department> Departments => Set<Department>();

    public DbSet<AcademicTerm> AcademicTerms => Set<AcademicTerm>();

    public DbSet<Student> Students => Set<Student>();

    public DbSet<AcademicStaff> AcademicStaff => Set<AcademicStaff>();

    public DbSet<Club> Clubs => Set<Club>();

    public DbSet<ClubMembership> ClubMemberships => Set<ClubMembership>();

    public DbSet<MembershipApplication> MembershipApplications => Set<MembershipApplication>();

    public DbSet<Event> Events => Set<Event>();

    public DbSet<EventParticipation> EventParticipations => Set<EventParticipation>();

    public DbSet<Announcement> Announcements => Set<Announcement>();

    public DbSet<StoredFile> StoredFiles => Set<StoredFile>();

    public DbSet<ReportRequest> ReportRequests => Set<ReportRequest>();

    /// <summary>K-12/Y-44: audit kaydı yalnızca AuditSaveChangesInterceptor tarafından yazılır.</summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        new EfTransaction(await Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>docs/PLAN-V2.md · A-38/Y-53: kontenjan RowVersion çakışması Business'a EF tipi sızdırmadan taşınır.</summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(ex.Message, ex);
        }
        catch (DbUpdateException ex) when (IsForeignKeyConstraintViolation(ex))
        {
            // docs/PLAN-V2.md · Faz 13 (A-12): kullanımdaki referans veri (FK Restrict) silinmeye
            // çalışıldığında burada yakalanır — Business'a EF tipi sızmaz. Yalnızca SQL Server'ın
            // FK ihlali (547) hedeflenir — unique index ihlalleri (A-15 testlerinin doğrudan
            // DbUpdateException bekleyen sözleşmesi) bilinçli olarak buradan GEÇMEZ.
            throw new ReferentialIntegrityConflictException(ex.Message, ex);
        }
    }

    private static bool IsForeignKeyConstraintViolation(DbUpdateException ex) =>
        ex.InnerException is SqlException { Number: 547 };

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfiguration(new RefreshTokenConfiguration());

        builder.ApplyConfiguration(new FacultyConfiguration());
        builder.ApplyConfiguration(new DepartmentConfiguration());
        builder.ApplyConfiguration(new AcademicTermConfiguration());
        builder.ApplyConfiguration(new StudentConfiguration());
        builder.ApplyConfiguration(new AcademicStaffConfiguration());
        builder.ApplyConfiguration(new ClubConfiguration());
        builder.ApplyConfiguration(new ClubMembershipConfiguration());
        builder.ApplyConfiguration(new MembershipApplicationConfiguration());
        builder.ApplyConfiguration(new EventConfiguration());
        builder.ApplyConfiguration(new EventParticipationConfiguration());
        builder.ApplyConfiguration(new AnnouncementConfiguration());
        builder.ApplyConfiguration(new StoredFileConfiguration());
        builder.ApplyConfiguration(new ReportRequestConfiguration());
        builder.ApplyConfiguration(new AuditLogConfiguration());

        // A-27: izin/rol seed'i HasData ile — tamamen statik, parola hash'i içermez.
        builder.Entity<ApplicationRole>().HasData(IdentitySeedData.Roles());
        builder.Entity<IdentityRoleClaim<int>>().HasData(IdentitySeedData.RoleClaims());

        // A-27: Faz 5'in dikey diliminin denenebilmesi için asgari referans verisi.
        builder.Entity<Faculty>().HasData(DomainSeedData.Faculty());
        builder.Entity<Department>().HasData(DomainSeedData.Department());
        builder.Entity<AcademicTerm>().HasData(DomainSeedData.AcademicTerm());
    }
}
