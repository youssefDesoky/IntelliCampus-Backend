using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class ElectiveBucketConfiguration : IEntityTypeConfiguration<ElectiveBucket>
{
    public void Configure(EntityTypeBuilder<ElectiveBucket> builder)
    {
        builder.HasKey(eb => eb.ElectiveBucketId);

        builder.Property(eb => eb.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(eb => eb.RequiredCreditHours)
            .HasColumnType("decimal(5,2)")
            .IsRequired();

        builder.Property(eb => eb.IsActive)
            .IsRequired();

        builder.HasOne(eb => eb.Bylaw)
            .WithMany(b => b.ElectiveBuckets)
            .HasForeignKey(eb => eb.BylawId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(eb => eb.Department)
            .WithMany(d => d.ElectiveBuckets)
            .HasForeignKey(eb => eb.DepartmentId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
