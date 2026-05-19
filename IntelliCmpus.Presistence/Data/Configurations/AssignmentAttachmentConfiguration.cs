using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class AssignmentAttachmentConfiguration : IEntityTypeConfiguration<AssignmentAttachment>
{
    public void Configure(EntityTypeBuilder<AssignmentAttachment> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasMaxLength(64);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(a => a.Url)
            .IsRequired()
            .HasMaxLength(2000);

        builder.HasOne(a => a.Assignment)
            .WithMany(x => x.Attachments)
            .HasForeignKey(a => a.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
