using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations;

/// <summary>docs/MIMARI.md · K-44/A-74: sosyal hesap satırı. Y-80: URL doğrulaması validator'da.</summary>
public sealed class ClubSocialLinkConfiguration : IEntityTypeConfiguration<ClubSocialLink>
{
    public void Configure(EntityTypeBuilder<ClubSocialLink> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Url)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasIndex(l => new { l.ClubId, l.DisplayOrder });

        // Y-16: yeni tablo soft delete gerektirmez; kulüp silinince satırlar birlikte gider.
        builder.HasOne<Club>()
            .WithMany()
            .HasForeignKey(l => l.ClubId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
