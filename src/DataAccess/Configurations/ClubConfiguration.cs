using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations;

/// <summary>docs/MIMARI.md · K-05/A-31: topluluk, danışman FK, logo FK, rowversion.</summary>
public sealed class ClubConfiguration : IEntityTypeConfiguration<Club>
{
    public void Configure(EntityTypeBuilder<Club> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(c => c.Name).IsUnique();

        builder.HasOne<AcademicStaff>()
            .WithMany()
            .HasForeignKey(c => c.AdvisorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<StoredFile>()
            .WithMany()
            .HasForeignKey(c => c.LogoFileId)
            .OnDelete(DeleteBehavior.SetNull);

        // A-60: kullanımdaki kategori silinemesin — Restrict, ReferentialIntegrityConflictException
        // üzerinden 409'a dönüşür (DeleteDepartmentAsync precedent'i).
        builder.HasOne<ClubCategory>()
            .WithMany()
            .HasForeignKey(c => c.ClubCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(c => c.RowVersion)
            .IsRowVersion();

        // K-44: iletişim alanları isteğe bağlıdır, kulübe özgü tek satırlık değerlerdir.
        builder.Property(c => c.ContactEmail).HasMaxLength(256);
        builder.Property(c => c.ContactPhone).HasMaxLength(32);
    }
}
