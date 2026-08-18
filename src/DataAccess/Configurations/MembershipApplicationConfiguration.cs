using Entities;
using Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations;

/// <summary>docs/MIMARI.md · A-12/K-03: başvuru, onay/ret, soft delete.</summary>
public sealed class MembershipApplicationConfiguration : IEntityTypeConfiguration<MembershipApplication>
{
    public void Configure(EntityTypeBuilder<MembershipApplication> builder)
    {
        builder.HasKey(a => a.Id);

        builder.HasOne<Club>()
            .WithMany()
            .HasForeignKey(a => a.ClubId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Student>()
            .WithMany()
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AcademicTerm>()
            .WithMany()
            .HasForeignKey(a => a.AcademicTermId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(a => a.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Bir dönemde aynı kulübe yalnızca bir bekleyen başvuru olabilir.
        builder.HasIndex(a => new { a.ClubId, a.StudentId, a.AcademicTermId })
            .IsUnique()
            .HasFilter($"[IsDeleted] = 0 AND [Status] = {(int)ApplicationStatus.Pending}");

        // Y-16: soft delete query filter.
        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}
