using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class SpecializationPrerequisiteConfiguration : IEntityTypeConfiguration<SpecializationPrerequisite>
{
    public void Configure(EntityTypeBuilder<SpecializationPrerequisite> builder)
    {
        builder.HasKey(sp => new { sp.SpecializationId, sp.CourseId });

        builder.Property(sp => sp.MinGrade)
            .HasColumnType("decimal(5,2)");

        builder.HasOne(sp => sp.Specialization)
            .WithMany(s => s.Prerequisites)
            .HasForeignKey(sp => sp.SpecializationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sp => sp.Course)
            .WithMany()
            .HasForeignKey(sp => sp.CourseId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
