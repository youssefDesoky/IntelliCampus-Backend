using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class BroadcastAnnouncementConfiguration : IEntityTypeConfiguration<BroadcastAnnouncement>
{
    public void Configure(EntityTypeBuilder<BroadcastAnnouncement> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Title)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasOne(b => b.Sender)
            .WithMany()
            .HasForeignKey(b => b.SenderId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(b => b.Faculty)
            .WithMany()
            .HasForeignKey(b => b.FacultyId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.Property(b => b.Audience).HasConversion<int>().IsRequired();
        builder.Property(b => b.TargetStudentType).HasConversion<int>();

        builder.HasIndex(b => new { b.FacultyId, b.Audience });
    }
}
