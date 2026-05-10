using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class StudentDepartmentConfiguration : IEntityTypeConfiguration<StudentDepartment>
{
    public void Configure(EntityTypeBuilder<StudentDepartment> builder)
    {
        builder.HasKey(sd => new { sd.DepartmentId, sd.StudentId });

        builder.HasOne(sd => sd.Department)
            .WithMany(d => d.StudentDepartments)
            .HasForeignKey(sd => sd.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sd => sd.Student)
            .WithMany(s => s.StudentDepartments)
            .HasForeignKey(sd => sd.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
