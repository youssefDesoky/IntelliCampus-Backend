using IntelliCampus.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.DAL.Data.Configurations;

public class AdminConfiguration : IEntityTypeConfiguration<Admin>
{
    public void Configure(EntityTypeBuilder<Admin> builder)
    {
        builder.ToTable("Admins");

        builder.Property(a => a.AdminId)
            .ValueGeneratedOnAdd();

        builder.Property(a => a.AdminCode)
            .HasMaxLength(50);
    }
}
