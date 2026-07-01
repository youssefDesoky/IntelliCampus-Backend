using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class UserNotificationSettingsConfiguration : IEntityTypeConfiguration<UserNotificationSettings>
{
    public void Configure(EntityTypeBuilder<UserNotificationSettings> builder)
    {
        builder.HasKey(s => s.UserNotificationSettingsId);

        builder.HasIndex(s => s.UserId)
            .IsUnique();

        builder.Property(s => s.InAppNotificationsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.PushNotificationsEnabled)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasOne(s => s.User)
            .WithOne(u => u.NotificationSettings)
            .HasForeignKey<UserNotificationSettings>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
