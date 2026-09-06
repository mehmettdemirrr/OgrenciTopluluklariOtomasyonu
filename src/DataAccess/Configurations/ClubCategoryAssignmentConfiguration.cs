using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations;

/// <summary>docs/MIMARI.md · A-80: kulüp silinince bağ gider (Cascade); kullanımdaki kategori silinemez (Restrict, A-12).</summary>
public sealed class ClubCategoryAssignmentConfiguration : IEntityTypeConfiguration<ClubCategoryAssignment>
{
    public void Configure(EntityTypeBuilder<ClubCategoryAssignment> builder)
    {
        builder.HasKey(a => a.Id);

        // Aynı kulübe aynı kategori iki kez bağlanamaz.
        builder.HasIndex(a => new { a.ClubId, a.ClubCategoryId }).IsUnique();

        builder.HasOne<Club>()
            .WithMany()
            .HasForeignKey(a => a.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ClubCategory>()
            .WithMany()
            .HasForeignKey(a => a.ClubCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
