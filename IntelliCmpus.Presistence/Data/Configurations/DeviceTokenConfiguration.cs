using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class DeviceTokenConfiguration : IEntityTypeConfiguration<DeviceToken>
{
    public void Configure(EntityTypeBuilder<DeviceToken> builder)
    {
        builder.HasKey(dt => dt.DeviceTokenId);

        builder.HasIndex(dt => dt.Token)
            .IsUnique();

        builder.HasIndex(dt => dt.UserId);

        builder.Property(dt => dt.Token)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(dt => dt.Platform)
            .HasMaxLength(50);

        builder.HasOne(dt => dt.User)
            .WithMany()
            .HasForeignKey(dt => dt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
