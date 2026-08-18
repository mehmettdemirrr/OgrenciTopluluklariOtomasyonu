using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataAccess.Configurations;

/// <summary>docs/MIMARI.md · A-15: (EventId, StudentId) unique index, Y-16: soft delete.</summary>
public sealed class EventParticipationConfiguration : IEntityTypeConfiguration<EventParticipation>
{
    public void Configure(EntityTypeBuilder<EventParticipation> builder)
    {
        builder.HasKey(p => p.Id);

        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(p => p.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Student>()
            .WithMany()
            .HasForeignKey(p => p.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        // A-15/Y-18: aynı öğrenci aynı etkinliğe iki kez kaydolamaz.
        builder.HasIndex(p => new { p.EventId, p.StudentId })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        // Y-16: soft delete query filter.
        builder.HasQueryFilter(p => !p.IsDeleted);
    }
}
