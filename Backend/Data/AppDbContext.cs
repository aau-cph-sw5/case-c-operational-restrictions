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
    }

}
