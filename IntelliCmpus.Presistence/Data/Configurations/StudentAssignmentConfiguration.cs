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

        builder.Property(sa => sa.FileUrl)
            .HasMaxLength(2000);

        builder.Property(sa => sa.Notes)
            .HasMaxLength(4000);

        builder.HasOne(sa => sa.Student)
            .WithMany(s => s.StudentAssignments)
            .HasForeignKey(sa => sa.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sa => sa.Assignment)
            .WithMany(a => a.StudentAssignments)
            .HasForeignKey(sa => sa.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
