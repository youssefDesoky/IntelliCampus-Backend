using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class StudentCourseConfiguration : IEntityTypeConfiguration<StudentCourse>
{
    public void Configure(EntityTypeBuilder<StudentCourse> builder)
    {
        builder.HasKey(sc => new { sc.StudentId, sc.CourseId });

        builder.Property(sc => sc.Level);

        builder.Property(sc => sc.Semester)
            .HasMaxLength(20);

        builder.HasIndex(sc => sc.CourseId);
        builder.HasIndex(sc => sc.ClassId);
        builder.HasIndex(sc => sc.Semester);
        builder.HasIndex(sc => sc.StudentId);
        builder.HasIndex(sc => sc.Status);

        builder.HasOne(sc => sc.Student)
            .WithMany(s => s.StudentCourses)
            .HasForeignKey(sc => sc.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sc => sc.Course)
            .WithMany(c => c.StudentCourses)
            .HasForeignKey(sc => sc.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sc => sc.Class)
            .WithMany(c => c.StudentCourses)
            .HasForeignKey(sc => sc.ClassId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
