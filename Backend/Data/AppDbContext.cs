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
    }

    public DbSet<OperationalRestriction> OperationalRestrictions { get; set; }
    public DbSet<User> Users => Set<User>();


}
