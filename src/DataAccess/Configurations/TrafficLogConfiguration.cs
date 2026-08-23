using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations;

/// <summary>
/// docs/MIMARI.md · K-28/A-44: trafik/erişim izi tablosu. Navigation property yok.
/// Generic repository'den erişilmez, yalnızca RequestLoggingMiddleware tarafından yazılır.
/// </summary>
public sealed class TrafficLogConfiguration : IEntityTypeConfiguration<TrafficLog>
{
    public void Configure(EntityTypeBuilder<TrafficLog> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.CorrelationId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(t => t.IpAddress)
            .IsRequired()
            .HasMaxLength(45);

        builder.Property(t => t.UserAgent)
            .HasMaxLength(512);

        builder.Property(t => t.HttpMethod)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(t => t.Path)
            .IsRequired()
            .HasMaxLength(512);

        // Y-59: yalnızca redakte edilmiş query string — ham gövde/header asla yazılmaz.
        builder.Property(t => t.RedactedQueryString)
            .HasMaxLength(1024);

        builder.HasIndex(t => t.CorrelationId);
        builder.HasIndex(t => t.TimestampUtc);
        builder.HasIndex(t => t.UserId);
    }
}
