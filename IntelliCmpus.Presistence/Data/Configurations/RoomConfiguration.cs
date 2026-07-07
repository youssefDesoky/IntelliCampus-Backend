using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.HasKey(r => r.RoomId);

        builder.Property(r => r.RoomName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.RoomNameAr)
            .HasMaxLength(100);

        builder.Property(r => r.Type)
            .HasMaxLength(50);

        builder.Property(r => r.Location)
            .HasMaxLength(200);

        builder.Property(r => r.LocationAr)
            .HasMaxLength(200);

        builder.Property(r => r.IsExamHall)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(r => r.FacultyId);

        builder.HasOne(r => r.Faculty)
            .WithMany(f => f.Rooms)
            .HasForeignKey(r => r.FacultyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
