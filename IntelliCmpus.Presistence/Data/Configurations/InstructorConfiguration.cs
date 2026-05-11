using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class InstructorConfiguration : IEntityTypeConfiguration<Instructor>
{
    public void Configure(EntityTypeBuilder<Instructor> builder)
    {
        builder.ToTable("Instructors");

        builder.Property(i => i.InstructorId)
            .ValueGeneratedOnAddOrUpdate();

        builder.Property(i => i.InstructorCode)
            .HasMaxLength(50);

        builder.Property(i => i.InstructorRole)
            .HasMaxLength(50);

        builder.Property(i => i.Specialization)
            .HasMaxLength(100);

        builder.HasOne(i => i.Department)
            .WithMany(d => d.Instructors)
            .HasForeignKey(i => i.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
