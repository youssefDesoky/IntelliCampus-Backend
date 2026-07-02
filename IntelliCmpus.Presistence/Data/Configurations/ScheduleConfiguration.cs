using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class ScheduleConfiguration : IEntityTypeConfiguration<Schedule>
{
    public void Configure(EntityTypeBuilder<Schedule> builder)
    {
        builder.ToTable("Schedules");

        builder.HasKey(s => s.ScheduleId);

        builder.Property(s => s.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.TitleAr)
            .HasMaxLength(200);

        builder.Property(s => s.Day)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(s => s.Date)
            .IsRequired();

        builder.Property(s => s.StartTime)
            .IsRequired();

        builder.Property(s => s.EndTime)
            .IsRequired();

        // Backward compatible with the existing DB schema that used column name "Type"
        builder.Property(s => s.ScheduleType)
            .HasColumnName("Type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(s => s.StudentId);
        builder.HasIndex(s => s.ScheduleType);
        builder.HasIndex(s => new { s.StudentId, s.Date });
        builder.HasIndex(s => s.CourseId);
        builder.HasIndex(s => s.ClassId);
        builder.HasIndex(s => s.RoomId);
        builder.HasIndex(s => s.InstructorId);

        builder.HasOne(s => s.Student)
            .WithMany(st => st.Schedules)
            .HasForeignKey(s => s.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(s => s.Course)
            .WithMany()
            .HasForeignKey(s => s.CourseId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.Class)
            .WithMany()
            .HasForeignKey(s => s.ClassId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.Room)
            .WithMany()
            .HasForeignKey(s => s.RoomId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.Instructor)
            .WithMany()
            .HasForeignKey(s => s.InstructorId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
