using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations;

/// <summary>
/// docs/MIMARI.md · K-12/A-33: denetim izi tablosu. Navigation property yok.
/// Audit kaydı generic repository'den erişilmez, interceptor tarafından yazılır.
/// </summary>
public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.EntityType)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.EntityId)
            .IsRequired()
            .HasMaxLength(50);

        // K-28: aynı isteğin TrafficLog satırıyla eşleştirilmesi için — AuditSaveChangesInterceptor yazar.
        builder.Property(a => a.CorrelationId)
            .HasMaxLength(100);

        // Sorgu performansı: entity bazında audit geçmişi çekilirken kullanılır.
        builder.HasIndex(a => new { a.EntityType, a.EntityId });

        builder.HasIndex(a => a.TimestampUtc);

        builder.HasIndex(a => a.CorrelationId);
    }
}
