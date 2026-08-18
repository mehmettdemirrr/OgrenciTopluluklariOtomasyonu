using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations;

/// <summary>docs/MIMARI.md · A-14: 1-1 profil.</summary>
public sealed class AcademicStaffConfiguration : IEntityTypeConfiguration<AcademicStaff>
{
    public void Configure(EntityTypeBuilder<AcademicStaff> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Title)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<AcademicStaff>(a => a.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => a.ApplicationUserId).IsUnique();

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(a => a.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
