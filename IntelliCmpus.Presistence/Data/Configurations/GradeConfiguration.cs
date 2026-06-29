using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class GradeConfiguration : IEntityTypeConfiguration<Grade>
{
    public void Configure(EntityTypeBuilder<Grade> builder)
    {
        builder.HasKey(g => g.GradeId);

        builder.Property(g => g.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(g => g.Score)
            .HasPrecision(10, 2);

        builder.Property(g => g.MaxScore)
            .HasPrecision(10, 2);

        builder.Property(g => g.Weight)
            .HasPrecision(10, 2);

        builder.Property(g => g.GradeType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(g => g.Status)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(g => g.Notes)
            .HasMaxLength(2000);

        builder.HasIndex(g => g.StudentId);
        builder.HasIndex(g => g.CourseId);

        builder.HasOne(g => g.Course)
            .WithMany(c => c.Grades)
            .HasForeignKey(g => g.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(g => g.Student)
            .WithMany(s => s.Grades)
            .HasForeignKey(g => g.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
