using Core.DataAccess;
using DataAccess.Configurations;
using DataAccess.Seed;
using Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DataAccess;

/// <summary>
/// docs/MIMARI.md · A-05: IUnitOfWork'ün somut implementasyonu (Core/DataAccess/IUnitOfWork.cs'teki
/// "Faz 4" notuna kısmi bir ön adım — bu sınıf Faz 3'te yalnızca Identity + RefreshToken taşır,
/// Faz 4 domain entity'lerini (Club, Event, ...) aynı sınıfa DbSet olarak ekleyecek.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, int>(options), IUnitOfWork
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfiguration(new RefreshTokenConfiguration());

        // A-27: izin/rol seed'i HasData ile — tamamen statik, parola hash'i içermez.
        builder.Entity<ApplicationRole>().HasData(IdentitySeedData.Roles());
        builder.Entity<IdentityRoleClaim<int>>().HasData(IdentitySeedData.RoleClaims());
    }
}
