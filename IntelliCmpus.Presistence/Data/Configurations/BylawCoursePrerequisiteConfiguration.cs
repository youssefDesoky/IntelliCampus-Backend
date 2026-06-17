using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class BylawCoursePrerequisiteConfiguration : IEntityTypeConfiguration<BylawCoursePrerequisite>
{
    public void Configure(EntityTypeBuilder<BylawCoursePrerequisite> builder)
    {
        builder.HasKey(bcp => new { bcp.BylawCourseId, bcp.PrerequisiteBylawCourseId });

        builder.HasOne(bcp => bcp.BylawCourse)
            .WithMany(bc => bc.Prerequisites)
            .HasForeignKey(bcp => bcp.BylawCourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(bcp => bcp.PrerequisiteCourse)
            .WithMany(bc => bc.PrerequisiteFor)
            .HasForeignKey(bcp => bcp.PrerequisiteBylawCourseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
