using Backend.Models.Entities;

using Microsoft.EntityFrameworkCore;
using Backend.Models.Entities;

namespace Backend.Data;

// DATA LAYER (EF Core):
// Represents the database session context. Configures PostgreSQL tables, relations, and LINQ queries
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    public DbSet<OperationalRestriction> OperationalRestrictions => Set<OperationalRestriction>();
}
