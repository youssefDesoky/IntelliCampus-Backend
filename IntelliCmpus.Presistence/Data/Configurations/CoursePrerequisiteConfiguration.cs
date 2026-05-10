using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class CoursePrerequisiteConfiguration : IEntityTypeConfiguration<CoursePrerequisite>
{
    public void Configure(EntityTypeBuilder<CoursePrerequisite> builder)
    {
        builder.HasKey(cp => new { cp.CourseId, cp.PrerequisiteCourseId });

        builder.HasOne(cp => cp.Course)
            .WithMany(c => c.Prerequisites)
            .HasForeignKey(cp => cp.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cp => cp.PrerequisiteCourse)
            .WithMany(c => c.PrerequisiteFor)
            .HasForeignKey(cp => cp.PrerequisiteCourseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
