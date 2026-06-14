using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class ExamHallConfiguration : IEntityTypeConfiguration<ExamHall>
{
    public void Configure(EntityTypeBuilder<ExamHall> builder)
    {
        builder.HasKey(e => e.ExamHallId);

        builder.Property(e => e.HallName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.HallNameAr)
            .HasMaxLength(100);

        builder.Property(e => e.Capacity)
            .IsRequired();
    }
}
