using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class CourseWorkWeightConfiguration : IEntityTypeConfiguration<CourseWorkWeight>
{
    public void Configure(EntityTypeBuilder<CourseWorkWeight> builder)
    {
        builder.HasKey(w => w.CourseWorkWeightId);

        builder.Property(w => w.QuizWeight)
            .HasPrecision(5, 2);

        builder.Property(w => w.AssignmentWeight)
            .HasPrecision(5, 2);

        builder.Property(w => w.MidtermWeight)
            .HasPrecision(5, 2);

        builder.HasOne(w => w.Course)
            .WithOne()
            .HasForeignKey<CourseWorkWeight>(w => w.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(w => w.CourseId).IsUnique();
    }
}
