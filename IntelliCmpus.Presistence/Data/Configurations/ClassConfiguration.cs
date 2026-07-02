using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class ClassConfiguration : IEntityTypeConfiguration<Class>
{
    public void Configure(EntityTypeBuilder<Class> builder)
    {
        builder.HasKey(c => c.ClassId);

        builder.Property(c => c.GroupCode)
            .HasMaxLength(20);

        builder.Property(c => c.GroupCodeAr)
            .HasMaxLength(20);

        builder.Property(c => c.ClassType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(c => c.Day)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(c => c.Room)
            .WithMany()
            .HasForeignKey(c => c.RoomId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(c => c.Capacity);

        builder.HasOne(c => c.Course)
            .WithMany(co => co.Classes)
            .HasForeignKey(c => c.CourseId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Instructor)
            .WithMany(i => i.Classes)
            .HasForeignKey(c => c.InstructorId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(c => c.CourseId);
        builder.HasIndex(c => c.InstructorId);
        builder.HasIndex(c => c.ClassType);
    }
}
