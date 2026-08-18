using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations;

public sealed class AcademicTermConfiguration : IEntityTypeConfiguration<AcademicTerm>
{
    public void Configure(EntityTypeBuilder<AcademicTerm> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(t => t.Name).IsUnique();

        // Aynı anda yalnızca bir dönem aktif olabilir — uygulama kodunda kontrol edilir,
        // DB filtered unique index ile güvence altına alınır.
        builder.HasIndex(t => t.IsCurrent)
            .IsUnique()
            .HasFilter("[IsCurrent] = 1");
    }
}
