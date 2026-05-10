using IntelliCampus.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelliCampus.Presistence.Data.Configurations;

public class NoteSummaryConfiguration : IEntityTypeConfiguration<NoteSummary>
{
    public void Configure(EntityTypeBuilder<NoteSummary> builder)
    {
        builder.HasKey(ns => ns.SummaryId);

        builder.Property(ns => ns.GeneratedText)
            .IsRequired();

        builder.HasOne(ns => ns.Note)
            .WithOne(n => n.NoteSummary)
            .HasForeignKey<NoteSummary>(ns => ns.NoteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
