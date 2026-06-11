using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class ExamScheduleConfiguration : IEntityTypeConfiguration<ExamSchedule>
{
    public void Configure(EntityTypeBuilder<ExamSchedule> builder)
    {
        builder.ToTable("ExamSchedules");

        builder.HasKey(e => e.ExamScheduleId);

        builder.Property(e => e.CourseCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.CourseName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Day)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.Date)
            .IsRequired();

        builder.Property(e => e.StartTime)
            .IsRequired();

        builder.Property(e => e.EndTime)
            .IsRequired();

        builder.Property(e => e.Duration)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Location)
            .HasMaxLength(200);

        builder.Property(e => e.ExamType)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(e => new { e.StudentId, e.Date });
        builder.HasIndex(e => e.ExamId);

        builder.HasOne(e => e.Student)
            .WithMany()
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Exam)
            .WithMany(e => e.ExamSchedules)
            .HasForeignKey(e => e.ExamId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
