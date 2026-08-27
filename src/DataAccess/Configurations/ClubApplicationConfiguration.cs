using Entities;
using Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations;

/// <summary>docs/MIMARI.md · K-29/A-45: topluluk kurma başvurusu, onay/ret, soft delete.</summary>
public sealed class ClubApplicationConfiguration : IEntityTypeConfiguration<ClubApplication>
{
    public void Configure(EntityTypeBuilder<ClubApplication> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.ProposedName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Justification)
            .IsRequired();

        builder.HasOne<Student>()
            .WithMany()
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AcademicTerm>()
            .WithMany()
            .HasForeignKey(a => a.AcademicTermId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AcademicStaff>()
            .WithMany()
            .HasForeignKey(a => a.ProposedAdvisorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(a => a.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Club>()
            .WithMany()
            .HasForeignKey(a => a.CreatedClubId)
            .OnDelete(DeleteBehavior.Restrict);

        // A-60: başvurudaki kategori de kullanım sayılır; silme 409 verir.
        builder.HasOne<ClubCategory>()
            .WithMany()
            .HasForeignKey(a => a.ProposedCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // A-39'un başkan tekilliği index'iyle aynı teknik: bir öğrenci aynı dönemde tek bekleyen
        // başvuru tutabilir (kulüp henüz yok, bu yüzden ClubId üzerinden değil StudentId üzerinden).
        builder.HasIndex(a => new { a.StudentId, a.AcademicTermId })
            .IsUnique()
            .HasFilter($"[IsDeleted] = 0 AND [Status] = {(int)ApplicationStatus.Pending}");

        // Y-16: soft delete query filter.
        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}
