using Entities;
using Entities.Enums;
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

        // A-49: iptal gerekçesi — serbest metin, yalnızca Cancelled durumunda dolu.
        builder.Property(e => e.CancellationReason)
            .HasMaxLength(500);

        // K-38/A-65: mevcut satırlar ve varsayılanı olmayan insert'ler Public olur — bugünkü
        // davranış korunur. Y-72: kolon nullable DEĞİL, "kitle belirsiz" diye bir durum yok.
        builder.Property(e => e.Audience)
            .HasDefaultValue(EventAudience.Public);

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

        // Faz 7: onay kuyruğu (ClubId + PendingApproval) ve yayın listesi (ClubId + Published) aynı index'i kullanır.
        builder.HasIndex(e => new { e.ClubId, e.Status });

        // Y-16: soft delete query filter.
        builder.HasQueryFilter(e => !e.IsDeleted);
    }
}
