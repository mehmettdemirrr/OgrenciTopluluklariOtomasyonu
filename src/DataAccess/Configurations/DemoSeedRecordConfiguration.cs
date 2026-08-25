using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations;

/// <summary>docs/MIMARI.md · Y-68: demo veri künyesi.</summary>
public sealed class DemoSeedRecordConfiguration : IEntityTypeConfiguration<DemoSeedRecord>
{
    public void Configure(EntityTypeBuilder<DemoSeedRecord> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.EntityType)
            .IsRequired()
            .HasMaxLength(64);

        // Y-18: aynı satır iki kez işaretlenemez — seeder'ın idempotanlığını veritabanı garanti eder,
        // "önce kontrol ettim" varsayımı değil.
        builder.HasIndex(r => new { r.EntityType, r.EntityId }).IsUnique();
    }
}
