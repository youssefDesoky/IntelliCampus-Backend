using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class CommunityConfiguration : IEntityTypeConfiguration<Community>
{
    public void Configure(EntityTypeBuilder<Community> builder)
    {
        builder.HasKey(c => c.CommunityId);

        builder.HasOne(c => c.Course)
            .WithOne()
            .HasForeignKey<Community>(c => c.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.CourseId).IsUnique();
    }
}
