using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class AnnouncementAttachmentConfiguration : IEntityTypeConfiguration<AnnouncementAttachment>
{
    public void Configure(EntityTypeBuilder<AnnouncementAttachment> builder)
    {
        builder.HasKey(a => a.AnnouncementAttachmentId);

        builder.Property(a => a.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(a => a.FileUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.FileType)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasOne(a => a.Announcement)
            .WithMany(ann => ann.Attachments)
            .HasForeignKey(a => a.AnnouncementId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
