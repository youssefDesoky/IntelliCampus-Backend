using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class BylawCourseConfiguration : IEntityTypeConfiguration<BylawCourse>
{
    public void Configure(EntityTypeBuilder<BylawCourse> builder)
    {
        builder.HasKey(bc => bc.BylawCourseId);

        builder.HasIndex(bc => new { bc.BylawId, bc.CourseId }).IsUnique();

        builder.Property(bc => bc.CourseType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(bc => bc.CreditHours);

        builder.Property(bc => bc.AllowedDepartmentIds)
            .HasColumnType("nvarchar(max)");

        builder.HasOne(bc => bc.Bylaw)
            .WithMany(b => b.BylawCourses)
            .HasForeignKey(bc => bc.BylawId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(bc => bc.Course)
            .WithMany()
            .HasForeignKey(bc => bc.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
