using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations;

/// <summary>docs/MIMARI.md · K-06/A-35/Y-51: rapor talebi durum makinesi.</summary>
public sealed class ReportRequestConfiguration : IEntityTypeConfiguration<ReportRequest>
{
    public void Configure(EntityTypeBuilder<ReportRequest> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReportType)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(r => r.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<StoredFile>()
            .WithMany()
            .HasForeignKey(r => r.OutputFileId)
            .OnDelete(DeleteBehavior.SetNull);

        // Kullanıcının rapor taleplerini listelemek için index.
        builder.HasIndex(r => r.RequestedByUserId);
    }
}
