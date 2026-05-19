using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class SubmissionFileConfiguration : IEntityTypeConfiguration<SubmissionFile>
{
    public void Configure(EntityTypeBuilder<SubmissionFile> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Id)
            .HasMaxLength(64);

        builder.Property(f => f.Name)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(f => f.Url)
            .IsRequired()
            .HasMaxLength(2000);

        builder.HasOne(f => f.StudentAssignment)
            .WithMany(sa => sa.Files)
            .HasForeignKey(f => f.StudentAssignmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
