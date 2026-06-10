using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Entities.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class FriendRequestConfiguration : IEntityTypeConfiguration<FriendRequest>
{
    public void Configure(EntityTypeBuilder<FriendRequest> builder)
    {
        builder.HasKey(fr => fr.FriendRequestId);

        builder.Property(fr => fr.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(fr => fr.Sender)
            .WithMany()
            .HasForeignKey(fr => fr.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(fr => fr.Recipient)
            .WithMany()
            .HasForeignKey(fr => fr.RecipientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
