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

    public DbSet<PermissionHasRole> PermissionHasRoles => Set<PermissionHasRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
