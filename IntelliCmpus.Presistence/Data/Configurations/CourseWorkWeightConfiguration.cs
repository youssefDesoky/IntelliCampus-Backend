using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class CourseWorkWeightConfiguration : IEntityTypeConfiguration<CourseWorkWeight>
{
    public void Configure(EntityTypeBuilder<CourseWorkWeight> builder)
    {
        builder.HasKey(w => w.CourseId);

        builder.Property(w => w.QuizWeight)
            .HasPrecision(10, 2)
            .HasDefaultValue(0);

        builder.Property(w => w.AssignmentWeight)
            .HasPrecision(10, 2)
            .HasDefaultValue(0);

        builder.Property(w => w.MidtermWeight)
            .HasPrecision(10, 2)
            .HasDefaultValue(0);

        builder.HasOne(w => w.Course)
            .WithOne(c => c.CourseWorkWeight)
            .HasForeignKey<CourseWorkWeight>(w => w.CourseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
