using IntelliCampus.Web.Modules.Inbox.Models;
using Microsoft.EntityFrameworkCore;

namespace IntelliCampus.Web.Modules.Inbox.Data;

public class InboxDbContext(DbContextOptions<InboxDbContext> options) : DbContext(options)
{
    public DbSet<InternalMessage> InternalMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InternalMessage>(entity =>
        {
            entity.HasKey(e => e.MessageId);

            entity.Property(e => e.Subject)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.Body)
                .IsRequired();

            entity.Property(e => e.IsRead)
                .HasDefaultValue(false);

            entity.Property(e => e.IsDeletedBySender)
                .HasDefaultValue(false);

            entity.Property(e => e.IsDeletedByRecipient)
                .HasDefaultValue(false);
        });
    }
}
