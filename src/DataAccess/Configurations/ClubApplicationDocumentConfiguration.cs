using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations;

/// <summary>docs/MIMARI.md · K-37/A-62/A-63: başvuru–evrak bağı.</summary>
public sealed class ClubApplicationDocumentConfiguration : IEntityTypeConfiguration<ClubApplicationDocument>
{
    public void Configure(EntityTypeBuilder<ClubApplicationDocument> builder)
    {
        builder.HasKey(d => d.Id);

        // Y-18: aynı başvuruya aynı evrak tipi iki kez yüklenemez.
        builder.HasIndex(d => new { d.ClubApplicationId, d.ClubDocumentTypeId }).IsUnique();

        // Başvuru fiziksel olarak silinirse (Y-16 gereği normalde silinmez) evrak bağı da gider.
        builder.HasOne<ClubApplication>()
            .WithMany()
            .HasForeignKey(d => d.ClubApplicationId)
            .OnDelete(DeleteBehavior.Cascade);

        // A-62: kullanımdaki evrak tipi silinemesin.
        builder.HasOne<ClubDocumentType>()
            .WithMany()
            .HasForeignKey(d => d.ClubDocumentTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        // A-63: saklama temizliği önce bu satırı, sonra StoredFile'ı siler — Restrict sırayı zorunlu kılar.
        builder.HasOne<StoredFile>()
            .WithMany()
            .HasForeignKey(d => d.StoredFileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
