using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.NotificationId);

        builder.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(n => n.Type)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(n => n.Title)
            .HasMaxLength(200);

        builder.Property(n => n.ClickUrl)
            .HasMaxLength(500);

        builder.Property(n => n.ImageUrl)
            .HasMaxLength(500);

        builder.HasIndex(n => n.CreatedAt);
        builder.HasIndex(n => n.Type);
    }
}
