using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.HasKey(c => c.CourseId);

        builder.Property(c => c.CourseCode)
            .HasMaxLength(50);

        builder.Property(c => c.CourseCodeAr)
            .HasMaxLength(50);

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        builder.Property(c => c.DescriptionAr)
            .HasMaxLength(500)
            .IsUnicode();

        builder.Property(c => c.CourseName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.CourseNameAr)
            .HasMaxLength(100)
            .IsUnicode();

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(c => c.Department)
            .WithMany(d => d.Courses)
            .HasForeignKey(c => c.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => c.DepartmentId);
        builder.HasIndex(c => c.CourseCode);
        builder.HasIndex(c => c.Status);
    }
}
