using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations;

/// <summary>docs/MIMARI.md · A-14: 1-1 profil, A-15: rowversion.</summary>
public sealed class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.StudentNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(s => s.StudentNumber).IsUnique();

        builder.HasOne<ApplicationUser>()
            .WithOne()
            .HasForeignKey<Student>(s => s.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.ApplicationUserId).IsUnique();

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(s => s.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(s => s.RowVersion)
            .IsRowVersion();
    }
}
