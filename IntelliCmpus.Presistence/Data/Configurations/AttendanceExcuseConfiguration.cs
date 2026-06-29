using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class AttendanceExcuseConfiguration : IEntityTypeConfiguration<AttendanceExcuse>
{
    public void Configure(EntityTypeBuilder<AttendanceExcuse> builder)
    {
        builder.HasKey(e => e.ExcuseId);

        builder.HasIndex(ae => ae.StudentId);
        builder.HasIndex(ae => ae.SessionId);

        builder.HasOne(e => e.Student)
            .WithMany()
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Session)
            .WithMany(s => s.Excuses)
            .HasForeignKey(e => e.SessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(e => e.Reason).HasMaxLength(500);
        builder.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property(e => e.DocumentPath).HasMaxLength(500);
        builder.Property(e => e.DocumentOriginalName).HasMaxLength(260);
        builder.Property(e => e.DocumentContentType).HasMaxLength(100);
    }
}
