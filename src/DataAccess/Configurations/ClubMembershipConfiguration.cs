using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations;

/// <summary>
/// docs/MIMARI.md · A-13/A-15/Y-16/Y-18: çift üyelik DB seviyesinde engellenir.
/// Unique index: (ClubId, StudentId, AcademicTermId) — soft-deleted kayıtlar hariç.
/// </summary>
public sealed class ClubMembershipConfiguration : IEntityTypeConfiguration<ClubMembership>
{
    public void Configure(EntityTypeBuilder<ClubMembership> builder)
    {
        builder.HasKey(m => m.Id);

        builder.HasOne<Club>()
            .WithMany()
            .HasForeignKey(m => m.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Student>()
            .WithMany()
            .HasForeignKey(m => m.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AcademicTerm>()
            .WithMany()
            .HasForeignKey(m => m.AcademicTermId)
            .OnDelete(DeleteBehavior.Restrict);

        // A-15/Y-18: aynı öğrenci, aynı kulüp, aynı dönemde yalnızca bir aktif üyelik.
        builder.HasIndex(m => new { m.ClubId, m.StudentId, m.AcademicTermId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Y-16: soft delete query filter.
        builder.HasQueryFilter(m => !m.IsDeleted);
    }
}
