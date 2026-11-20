
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

    public DbSet<OperationalRestriction> OperationalRestrictions => Set<OperationalRestriction>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();
    public DbSet<RestrictionTransitionEvent> RestrictionTransitionEvents => Set<RestrictionTransitionEvent>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<OperationalRestriction>()
            .HasKey(r => r.RestrictionId);

        modelBuilder.Entity<Permission>(builder =>
        {
            builder.HasKey(p => p.Id);
            builder.HasIndex(p => p.Name).IsUnique();
        });

        modelBuilder.Entity<Role>(builder =>
        {
            builder.HasKey(r => r.Id);
            builder.HasIndex(r => r.Name).IsUnique();
        });

        modelBuilder.Entity<RolePermission>(builder =>
        {
            builder.HasKey(rp => new { rp.RoleId, rp.PermissionId });

            builder.HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserRoleAssignment>(builder =>
        {
            builder.HasKey(ura => new { ura.UserId, ura.RoleId });

            builder.HasOne(ura => ura.User)
                .WithMany()
                .HasForeignKey(ura => ura.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(ura => ura.Role)
                .WithMany(r => r.UserRoleAssignments)
                .HasForeignKey(ura => ura.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(n => n.Id);

            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(n => n.RecipientUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(n => n.OperationalRestriction)
                .WithMany()
                .HasForeignKey(n => n.OperationalRestrictionId)
                .OnDelete(DeleteBehavior.Restrict);

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

            entity.HasIndex(d => new
            {
                d.Status,
                d.NextAttemptAt
            });
        });
    }
}
