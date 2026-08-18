using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations;

public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasOne<Faculty>()
            .WithMany()
            .HasForeignKey(d => d.FacultyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => new { d.FacultyId, d.Name }).IsUnique();
    }
}
