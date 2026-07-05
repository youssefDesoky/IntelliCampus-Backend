using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class ExamSeatAssignmentConfiguration : IEntityTypeConfiguration<ExamSeatAssignment>
{
    public void Configure(EntityTypeBuilder<ExamSeatAssignment> builder)
    {
        builder.HasKey(e => e.ExamSeatAssignmentId);

        builder.Property(e => e.SeatNumber)
            .IsRequired();

        builder.HasOne(e => e.Exam)
            .WithMany(e => e.ExamSeatAssignments)
            .HasForeignKey(e => e.ExamId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Student)
            .WithMany(s => s.ExamSeatAssignments)
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Room)
            .WithMany()
            .HasForeignKey(e => e.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.ExamId, e.StudentId }).IsUnique();
    }
}
