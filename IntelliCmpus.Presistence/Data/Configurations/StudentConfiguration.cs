using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class StudentConfiguration : IEntityTypeConfiguration<Student>
{
    public void Configure(EntityTypeBuilder<Student> builder)
    {
        builder.ToTable("Students");

        builder.Property(s => s.StudentId)
            .ValueGeneratedOnAdd()
            .UseIdentityColumn(1, 1);

        builder.Property(s => s.StudentCode)
            .HasMaxLength(50);

        builder.Property(s => s.Program)
            .HasConversion<int>();

        builder.Property(s => s.Gpa)
            .HasDefaultValue(0.0);

        builder.HasOne(s => s.Department)
            .WithMany()
            .HasForeignKey(s => s.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.Bylaw)
            .WithMany(b => b.Students)
            .HasForeignKey(s => s.BylawId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
