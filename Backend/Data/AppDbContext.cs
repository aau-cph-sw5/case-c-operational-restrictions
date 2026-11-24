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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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

        // The table was created as "Signature" through OperationalRestriction.SignatureSheet;
        // keep that name now that it also has its own DbSet.
        modelBuilder.Entity<Signature>().ToTable("Signature");

        modelBuilder.Entity<RequiresSignature>(builder =>
        {
            builder.HasKey(rs => rs.Id);
            builder.HasOne(rs => rs.OperationalRestriction)
                .WithMany()
                .HasForeignKey(rs => rs.OperationalRestrictionId)
                .OnDelete(DeleteBehavior.Cascade);
            // Restrict: deleting a user must not silently erase who was required to sign.
            builder.HasOne(rs => rs.Signee)
                .WithMany()
                .HasForeignKey(rs => rs.SigneeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    public DbSet<OperationalRestriction> OperationalRestrictions => Set<OperationalRestriction>();
    public DbSet<RestrictionTransitionEvent> RestrictionTransitionEvents => Set<RestrictionTransitionEvent>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();
    public DbSet<RequiresSignature> RequiresSignatures => Set<RequiresSignature>();
    public DbSet<Signature> Signatures => Set<Signature>();


}
