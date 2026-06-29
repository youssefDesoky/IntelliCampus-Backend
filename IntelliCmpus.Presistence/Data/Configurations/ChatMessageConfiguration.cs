using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class ChatMessageConfiguration : IEntityTypeConfiguration<ChatMessage>
{
    public void Configure(EntityTypeBuilder<ChatMessage> builder)
    {
        builder.HasKey(message => message.MessageId);

        builder.Property(m => m.Content)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(m => m.SenderId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(m => m.RecipientId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(m => m.IsEdited)
            .HasDefaultValue(false);

        builder.Property(m => m.IsPinned)
            .HasDefaultValue(false);

        builder.HasIndex(m => m.SenderId);
        builder.HasIndex(m => m.RecipientId);
        builder.HasIndex(m => m.GroupName);

        builder.Ignore(m => m.Sender);
        builder.Ignore(m => m.Recipient);
    }
}