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

    public DbSet<RoleEntity> Roles => Set<RoleEntity>();

    public DbSet<PermissionEntity> Permissions => Set<PermissionEntity>();

    public DbSet<PermissionHasRole> PermissionHasRoles => Set<PermissionHasRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Roles from the class diagram (MET-C-041). Fixed ids so the seed stays the same across migrations.
        // Permissions are not seeded yet, since the permission matrix is still waiting on Metro (MET-C-022).
        modelBuilder.Entity<RoleEntity>().HasData(
            new RoleEntity { Id = new Guid("b1f23318-ff7d-4189-a8df-14f8e630593c"), Name = "Originator" },
            new RoleEntity { Id = new Guid("6bf92b9d-043f-44d7-ba08-4b56bb4b6f2b"), Name = "DutyOperationsManager" },
            new RoleEntity { Id = new Guid("63800ced-af15-4b34-bf2f-d0058e7334d5"), Name = "Operator" },
            new RoleEntity { Id = new Guid("2e81b0cf-5d64-443b-9573-062207393023"), Name = "ControlRoomSupervisor" },
            new RoleEntity { Id = new Guid("01dc6955-be5c-456f-ad3e-6ca68331f235"), Name = "Betriebsleiter" }
        );
    }
}
