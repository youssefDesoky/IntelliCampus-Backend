using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class AdminConfiguration : IEntityTypeConfiguration<Admin>
{
    public void Configure(EntityTypeBuilder<Admin> builder)
    {
        builder.ToTable("Admins");

        builder.Property(a => a.AdminId)
            .ValueGeneratedOnAddOrUpdate();

        builder.Property(a => a.AdminCode)
            .HasMaxLength(50);
    }
}
