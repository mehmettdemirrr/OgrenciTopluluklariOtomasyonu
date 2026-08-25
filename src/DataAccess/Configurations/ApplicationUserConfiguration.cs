using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations;

/// <summary>
/// docs/MIMARI.md · A-56 (K-32): kişi adı alanları. Identity'nin kendi şeması olduğu gibi kalır;
/// yalnızca iki nullable sütun eklenir (Y-19: parola/güvenlik alanlarına dokunulmaz).
/// </summary>
public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(u => u.FirstName).HasMaxLength(100);
        builder.Property(u => u.LastName).HasMaxLength(100);

        // Kullanıcı listesi ada göre de aranıyor (A-50) — LIKE '%x%' index kullanamaz ama
        // sıralama/eşitlik sorguları için faydalı; tablo kullanıcı sayısıyla büyüyor.
        builder.HasIndex(u => new { u.LastName, u.FirstName });
    }
}
