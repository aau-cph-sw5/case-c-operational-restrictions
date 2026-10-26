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

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<PermissionEntity> Permissions => Set<PermissionEntity>();

    public DbSet<RolePermissionAssignment> PermissionHasRoles => Set<RolePermissionAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Composite primary keys on the join tables are scalar FK columns; the navigation
        // properties cannot be part of a key, since EF can only map scalar values.
        modelBuilder.Entity<RolePermissionAssignment>()
            .HasOne(a => a.Role)
            .WithMany(r => r.PermissionAssignments)
            .HasForeignKey(a => a.RoleName);

        modelBuilder.Entity<RolePermissionAssignment>()
            .HasOne(a => a.Permission)
            .WithMany(p => p.RoleAssignments)
            .HasForeignKey(a => a.PermissionName);

        modelBuilder.Entity<UserRoleAssignment>()
            .HasOne(a => a.User)
            .WithMany(u => u.RoleAssignments)
            .HasForeignKey(a => a.UserId);

        modelBuilder.Entity<UserRoleAssignment>()
            .HasOne(a => a.Role)
            .WithMany(r => r.UserAssignments)
            .HasForeignKey(a => a.RoleName);

        // Roles from the class diagram (MET-C-041).
        // Permissions are not seeded yet, since the permission matrix is still waiting on Metro (MET-C-022).
        modelBuilder.Entity<Role>().HasData(
            new Role { Name = "Originator" },
            new Role { Name = "DutyOperationsManager" },
            new Role { Name = "Operator" },
            new Role { Name = "ControlRoomSupervisor" },
            new Role { Name = "Betriebsleiter" }
        );
    }
}
