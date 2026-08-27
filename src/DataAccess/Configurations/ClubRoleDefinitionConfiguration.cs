using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations;

/// <summary>docs/MIMARI.md · K-36/A-61/O-20: unvan kataloğu kulübe özeldir.</summary>
public sealed class ClubRoleDefinitionConfiguration : IEntityTypeConfiguration<ClubRoleDefinition>
{
    public void Configure(EntityTypeBuilder<ClubRoleDefinition> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(100);

        // Y-18/O-20: aynı kulüpte aynı unvan iki kez tanımlanamaz; farklı kulüpler serbest.
        builder.HasIndex(d => new { d.ClubId, d.Name }).IsUnique();

        builder.HasOne<Club>()
            .WithMany()
            .HasForeignKey(d => d.ClubId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
