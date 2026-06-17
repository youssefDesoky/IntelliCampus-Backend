using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class ElectiveBucketCourseConfiguration : IEntityTypeConfiguration<ElectiveBucketCourse>
{
    public void Configure(EntityTypeBuilder<ElectiveBucketCourse> builder)
    {
        builder.HasKey(ebc => new { ebc.ElectiveBucketId, ebc.CourseId });

        builder.HasOne(ebc => ebc.ElectiveBucket)
            .WithMany(eb => eb.ElectiveBucketCourses)
            .HasForeignKey(ebc => ebc.ElectiveBucketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ebc => ebc.Course)
            .WithMany(c => c.ElectiveBucketCourses)
            .HasForeignKey(ebc => ebc.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
