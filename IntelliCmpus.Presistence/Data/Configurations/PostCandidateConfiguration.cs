using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class PostCandidateConfiguration : IEntityTypeConfiguration<PostCandidate>
{
    public void Configure(EntityTypeBuilder<PostCandidate> builder)
    {
        builder.HasKey(pc => pc.Id);

        builder.Property(pc => pc.Id).ValueGeneratedOnAdd();

        builder.Property(pc => pc.Score).HasColumnType("float");

        builder.HasOne(pc => pc.Post)
            .WithMany(p => p.Candidates)
            .HasForeignKey(pc => pc.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pc => pc.User)
            .WithMany()
            .HasForeignKey(pc => pc.UserId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasIndex(pc => pc.PostId);
    }
}
