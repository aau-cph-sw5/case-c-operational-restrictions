using Backend.Models.Entities;

using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

// DATA LAYER (EF Core):
// Represents the database session context. Configures PostgreSQL tables, relations, and LINQ queries
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<OperationalRestriction> OperationalRestrictions => Set<OperationalRestriction>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();
    public DbSet<NotificationAuditEntry> NotificationAuditEntries => Set<NotificationAuditEntry>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(n => n.Id);

            // Recipient must exist in Users.
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(n => n.RecipientUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // A notification can belong to an OR.
            entity.HasOne(n => n.OperationalRestriction)
                .WithMany()
                .HasForeignKey(n => n.OperationalRestrictionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes for notification filters.
            entity.HasIndex(n => new
            {
                n.RecipientUserId,
                n.CreatedAt
            });

            entity.HasIndex(n => new
            {
                n.RecipientUserId,
                n.ReadAt
            });

            entity.HasIndex(n => new
            {
                n.RecipientUserId,
                n.RequiresAction,
                n.ResolvedAt
            });
        });

        modelBuilder.Entity<NotificationDelivery>(entity =>
        {
            entity.HasKey(d => d.Id);

            entity.HasOne(d => d.Notification)
                .WithMany(n => n.Deliveries)
                .HasForeignKey(d => d.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Helps the retry worker find pending deliveries.
            entity.HasIndex(d => new
            {
                d.Status,
                d.NextAttemptAt
            });
        });

        modelBuilder.Entity<NotificationAuditEntry>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.HasOne<Notification>()
                .WithMany()
                .HasForeignKey(a => a.NotificationId)
                .OnDelete(DeleteBehavior.Restrict);

            // Lets the history of a restriction be read in time order.
            entity.HasIndex(a => new
            {
                a.OperationalRestrictionId,
                a.OccurredAt
            });
        });
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        RejectNotificationAuditChanges();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        RejectNotificationAuditChanges();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    // The notification audit trail is append-only (C-021): entries can be added but never changed or deleted.
    private void RejectNotificationAuditChanges()
    {
        var hasChangedEntries = ChangeTracker.Entries<NotificationAuditEntry>()
            .Any(e => e.State is EntityState.Modified or EntityState.Deleted);

        if (hasChangedEntries)
            throw new InvalidOperationException("Notification audit entries are append-only and cannot be changed or deleted.");
    }

}
