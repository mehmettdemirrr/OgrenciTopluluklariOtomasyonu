using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations;

/// <summary>docs/MIMARI.md · A-25/A-15/K-05: etkinlik, rowversion, soft delete.</summary>
public sealed class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.HasOne<Club>()
            .WithMany()
            .HasForeignKey(e => e.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<StoredFile>()
            .WithMany()
            .HasForeignKey(e => e.PosterFileId)
            .OnDelete(DeleteBehavior.SetNull);

        // A-15: kontenjan aşımı için eşzamanlılık kontrolü.
        builder.Property(e => e.RowVersion)
            .IsRowVersion();

        // Y-16: soft delete query filter.
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
