using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class ReminderConfiguration : IEntityTypeConfiguration<Reminder>
{
    public void Configure(EntityTypeBuilder<Reminder> builder)
    {
        builder.HasKey(r => r.ReminderId);

        builder.Property(r => r.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(r => r.Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(r => r.Location)
            .HasMaxLength(200);

        builder.Property(r => r.Priority)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("low");

        builder.HasIndex(r => r.StudentId);

        builder.HasOne(r => r.Student)
            .WithMany(s => s.Reminders)
            .HasForeignKey(r => r.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Instructor)
            .WithMany(i => i.Reminders)
            .HasForeignKey(r => r.InstructorId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
