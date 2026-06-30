using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.UserId);

        builder.Property(u => u.NationalId)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(u => u.NationalId)
            .IsUnique();

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.FullNameAr)
            .HasMaxLength(100)
            .IsUnicode();

        builder.Property(u => u.PhoneNumber)
            .HasMaxLength(20);

        builder.Property(u => u.Nationality)
            .HasMaxLength(100);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.Address)
            .HasMaxLength(250);

        builder.Property(u => u.Password)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.ProfileImage);

        builder.Property(u => u.MustChangePassword)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(u => u.RecoveryEmail)
            .HasMaxLength(100);

        builder.Property(u => u.RecoveryEmailVerified)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.FacultyId);
        builder.HasOne(u => u.Faculty)
            .WithMany(f => f.Users)
            .HasForeignKey(u => u.FacultyId)
            .OnDelete(DeleteBehavior.SetNull);

        // Filter out soft-deleted or shared base queries — TPT handles mapping
    }
}
