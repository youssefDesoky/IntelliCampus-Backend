using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.HasKey(d => d.DepartmentId);

        builder.Property(d => d.DepartmentName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.DepartmentNameAr)
            .HasMaxLength(100);

        builder.Property(d => d.Description)
            .HasMaxLength(500);

        builder.Property(d => d.DescriptionAr)
            .HasMaxLength(500);

        builder.HasIndex(d => d.FacultyId);

        builder.HasOne(d => d.HeadInstructor)
            .WithMany()
            .HasForeignKey(d => d.InstructorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(d => d.Faculty)
            .WithMany(f => f.Departments)
            .HasForeignKey(d => d.FacultyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.OwnsOne(d => d.RegistrationSettings, rs =>
        {
            rs.ToJson();
        });
    }
}
