using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class SecurityAuditLogConfiguration : IEntityTypeConfiguration<SecurityAuditLog>
{
    public void Configure(EntityTypeBuilder<SecurityAuditLog> builder)
    {
        builder.ToTable("SecurityAuditLogs");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Purpose).IsRequired().HasMaxLength(50);
        builder.Property(l => l.NationalIdMasked).HasMaxLength(20);
        builder.Property(l => l.IpAddress).HasMaxLength(45);
        builder.Property(l => l.UserAgent).HasMaxLength(500);
        builder.Property(l => l.FailureReason).HasMaxLength(500);

        builder.HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
