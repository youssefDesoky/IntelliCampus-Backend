using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class PostVoteConfiguration : IEntityTypeConfiguration<PostVote>
{
    public void Configure(EntityTypeBuilder<PostVote> builder)
    {
        builder.HasKey(v => v.PostVoteId);

        builder.HasIndex(v => new { v.PostId, v.UserId }).IsUnique();

        builder.HasOne(v => v.Post)
            .WithMany(p => p.Votes)
            .HasForeignKey(v => v.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.User)
            .WithMany(u => u.PostVotes)
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
