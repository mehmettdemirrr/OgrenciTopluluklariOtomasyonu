using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations;

/// <summary>docs/MIMARI.md: topluluk duyurusu, Y-16: soft delete. docs/PLAN-V2.md · A-43: nullable ClubId, Visibility.</summary>
public sealed class AnnouncementConfiguration : IEntityTypeConfiguration<Announcement>
{
    public void Configure(EntityTypeBuilder<Announcement> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(a => a.Content)
            .IsRequired();

        // A-43: sistem duyurusu (ClubId = null) kulüp silinse bile kalır — Cascade değil SetNull.
        builder.HasOne<Club>()
            .WithMany()
            .HasForeignKey(a => a.ClubId)
            .OnDelete(DeleteBehavior.SetNull);

        // Y-57: anonim vitrin ucu ve akış filtresi bu index'i kullanır.
        builder.HasIndex(a => new { a.Visibility, a.PublishedAtUtc });

        // Y-16: soft delete query filter.
        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}
