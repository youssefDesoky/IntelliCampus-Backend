using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.HasKey(s => s.SessionId);

        builder.Property(s => s.Topic)
            .HasMaxLength(200);

        builder.Property(s => s.SessionType)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasIndex(s => s.ClassId);

        builder.HasOne(s => s.Class)
            .WithMany(c => c.Sessions)
            .HasForeignKey(s => s.ClassId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
