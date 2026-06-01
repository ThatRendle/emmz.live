using Microsoft.EntityFrameworkCore;

namespace EmmzLive.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Inbox> Inboxes => Set<Inbox>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Inbox>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Slug).IsRequired();
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.CreatedAt).IsRequired();
        });

        modelBuilder.Entity<Message>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Body).IsRequired();
            e.Property(x => x.ReceivedAt).IsRequired();
            e.HasOne(x => x.Inbox)
             .WithMany(x => x.Messages)
             .HasForeignKey(x => x.InboxId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
