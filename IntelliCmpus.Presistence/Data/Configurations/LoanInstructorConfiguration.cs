using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class LoanInstructorConfiguration : IEntityTypeConfiguration<LoanInstructor>
{
    public void Configure(EntityTypeBuilder<LoanInstructor> builder)
    {
        builder.ToTable("LoanInstructors");

        builder.HasKey(li => li.UserId);

        builder.HasOne(li => li.Instructor)
            .WithMany()
            .HasForeignKey(li => li.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(li => li.LoanProfessorId)
            .HasMaxLength(50);

        builder.HasOne(li => li.LoanFromDepartment)
            .WithMany()
            .HasForeignKey(li => li.LoanFromDepartmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
