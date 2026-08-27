using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations;

/// <summary>docs/MIMARI.md · A-60: FacultyConfiguration ile birebir aynı biçim.</summary>
public sealed class ClubCategoryConfiguration : IEntityTypeConfiguration<ClubCategory>
{
    public void Configure(EntityTypeBuilder<ClubCategory> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(c => c.Name).IsUnique();
    }
}
