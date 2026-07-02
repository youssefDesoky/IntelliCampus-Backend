using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class MeetingConfiguration : IEntityTypeConfiguration<Meeting>
{
    public void Configure(EntityTypeBuilder<Meeting> builder)
    {
        builder.HasKey(m => m.MeetingId);

        builder.Property(m => m.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.DateTime)
            .IsRequired();

        builder.Property(m => m.RoomName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(m => m.IsActive)
            .HasDefaultValue(true);

        builder.HasIndex(m => m.CourseId);

        builder.HasOne(m => m.Course)
            .WithMany()
            .HasForeignKey(m => m.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Instructor)
            .WithMany()
            .HasForeignKey(m => m.InstructorId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
