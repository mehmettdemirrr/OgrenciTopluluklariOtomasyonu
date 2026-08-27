using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations;

/// <summary>
/// docs/MIMARI.md · A-58/A-62: sekiz gerçek MTÜ formu HasData ile — gerçek kurumsal referans
/// verisi kalıcı, belirleyici ve üretime gider (demo verisi DEĞİL, Y-68 kapsamı dışında).
/// </summary>
public sealed class ClubDocumentTypeConfiguration : IEntityTypeConfiguration<ClubDocumentType>
{
    public void Configure(EntityTypeBuilder<ClubDocumentType> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(300);

        builder.HasIndex(t => t.Code).IsUnique();

        builder.HasData(
            new ClubDocumentType { Id = 1, Code = "FR-0230", Name = "Topluluk Akademik Danışman Dilekçesi", IsRequired = true, IsActive = true, DisplayOrder = 1 },
            new ClubDocumentType { Id = 2, Code = "FR-0240", Name = "Topluluk Asıl Üyeler (Yönetim Kurulu)", IsRequired = true, IsActive = true, DisplayOrder = 2 },
            new ClubDocumentType { Id = 3, Code = "FR-0241", Name = "Topluluk Faaliyet Planı", IsRequired = true, IsActive = true, DisplayOrder = 3 },
            new ClubDocumentType { Id = 4, Code = "FR-0242", Name = "Topluluk Kapak Sayfası", IsRequired = true, IsActive = true, DisplayOrder = 4 },
            new ClubDocumentType { Id = 5, Code = "FR-0243", Name = "Topluluk Kurucu Üye Dilekçesi", IsRequired = true, IsActive = true, DisplayOrder = 5 },
            new ClubDocumentType { Id = 6, Code = "FR-0244", Name = "Topluluk Kuruluş Dilekçesi", IsRequired = true, IsActive = true, DisplayOrder = 6 },
            new ClubDocumentType { Id = 7, Code = "FR-0245", Name = "Topluluk Üye Listesi", IsRequired = true, IsActive = true, DisplayOrder = 7 },
            new ClubDocumentType { Id = 8, Code = "FR-0272", Name = "Topluluk Örnek Tüzük", IsRequired = true, IsActive = true, DisplayOrder = 8 });
    }
}
