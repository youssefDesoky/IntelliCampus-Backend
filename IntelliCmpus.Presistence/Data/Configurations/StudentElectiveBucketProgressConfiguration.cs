using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class StudentElectiveBucketProgressConfiguration : IEntityTypeConfiguration<StudentElectiveBucketProgress>
{
    public void Configure(EntityTypeBuilder<StudentElectiveBucketProgress> builder)
    {
        builder.HasKey(sebp => new { sebp.StudentId, sebp.ElectiveBucketId });

        builder.Property(sebp => sebp.CompletedCreditHours)
            .HasColumnType("decimal(5,2)")
            .IsRequired();

        builder.Property(sebp => sebp.IsLocked)
            .IsRequired();

        builder.HasOne(sebp => sebp.Student)
            .WithMany(s => s.ElectiveBucketProgresses)
            .HasForeignKey(sebp => sebp.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(sebp => sebp.ElectiveBucket)
            .WithMany(eb => eb.StudentProgresses)
            .HasForeignKey(sebp => sebp.ElectiveBucketId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
