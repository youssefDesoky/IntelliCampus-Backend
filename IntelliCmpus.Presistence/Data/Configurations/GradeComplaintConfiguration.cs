using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class GradeComplaintConfiguration : IEntityTypeConfiguration<GradeComplaint>
{
    public void Configure(EntityTypeBuilder<GradeComplaint> builder)
    {
        builder.HasKey(c => c.ComplaintId);

        builder.Property(c => c.ComplaintType)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.Details)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.InstructorResponse)
            .HasMaxLength(2000);

        builder.HasOne(c => c.Grade)
            .WithMany()
            .HasForeignKey(c => c.GradeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.Student)
            .WithMany()
            .HasForeignKey(c => c.StudentId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
