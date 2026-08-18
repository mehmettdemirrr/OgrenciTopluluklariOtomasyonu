using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations;

/// <summary>docs/MIMARI.md: topluluk duyurusu, Y-16: soft delete.</summary>
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

        builder.HasOne<Club>()
            .WithMany()
            .HasForeignKey(a => a.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        // Y-16: soft delete query filter.
        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}
