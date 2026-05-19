using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class StudentAssignmentConfiguration : IEntityTypeConfiguration<StudentAssignment>
{
    public void Configure(EntityTypeBuilder<StudentAssignment> builder)
    {
        builder.HasKey(sa => sa.StudentAssignmentId);

        builder.HasIndex(sa => new { sa.StudentId, sa.AssignmentId })
            .IsUnique();

        builder.Property(sa => sa.Grade)
            .HasPrecision(10, 2);

        builder.Property(sa => sa.Note)
            .HasMaxLength(4000);

        builder.Property(sa => sa.Feedback)
            .HasMaxLength(4000);

        builder.HasOne(sa => sa.Student)
            .WithMany(s => s.StudentAssignments)
            .HasForeignKey(sa => sa.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sa => sa.Assignment)
            .WithMany(a => a.StudentAssignments)
            .HasForeignKey(sa => sa.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);

        // SQL Server: avoid multiple cascade paths
        builder.HasOne(sa => sa.GradedByInstructor)
            .WithMany()
            .HasForeignKey(sa => sa.GradedByInstructorId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
