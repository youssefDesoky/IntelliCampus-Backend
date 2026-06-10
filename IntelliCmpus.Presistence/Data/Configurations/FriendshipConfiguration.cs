using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class FriendshipConfiguration : IEntityTypeConfiguration<Friendship>
{
    public void Configure(EntityTypeBuilder<Friendship> builder)
    {
        builder.HasKey(f => f.FriendshipId);

        builder.HasOne(f => f.User1)
            .WithMany()
            .HasForeignKey(f => f.UserId1)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(f => f.User2)
            .WithMany()
            .HasForeignKey(f => f.UserId2)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(f => new { f.UserId1, f.UserId2 }).IsUnique();
    }
}
