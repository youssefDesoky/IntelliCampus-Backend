using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class ScheduleConfiguration : IEntityTypeConfiguration<Schedule>
{
    public void Configure(EntityTypeBuilder<Schedule> builder)
    {
        builder.HasKey(s => s.ScheduleId);

        builder.Property(s => s.Location)
            .HasMaxLength(100);

        builder.Property(s => s.Type)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(s => s.Day)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(s => s.Student)
            .WithMany(st => st.Schedules)
            .HasForeignKey(s => s.StudentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
