using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class InternalMessageConfiguration : IEntityTypeConfiguration<InternalMessage>
{
    public void Configure(EntityTypeBuilder<InternalMessage> builder)
    {
        builder.HasKey(e => e.MessageId);

        builder.Property(e => e.Subject)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.Body)
            .IsRequired();

        builder.Property(e => e.IsRead)
            .HasDefaultValue(false);

        builder.Property(e => e.IsDeletedBySender)
            .HasDefaultValue(false);

        builder.Property(e => e.IsDeletedByRecipient)
            .HasDefaultValue(false);

        builder.HasIndex(im => im.SenderId);
        builder.HasIndex(im => im.RecipientId);
        builder.HasIndex(im => im.ParentMessageId);
    }
}
