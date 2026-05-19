using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class AnnouncementCommentConfiguration : IEntityTypeConfiguration<AnnouncementComment>
{
    public void Configure(EntityTypeBuilder<AnnouncementComment> builder)
    {
        builder.HasKey(c => c.AnnouncementCommentId);

        builder.Property(c => c.Content)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        builder.HasOne(c => c.Announcement)
            .WithMany(a => a.Comments)
            .HasForeignKey(c => c.AnnouncementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
