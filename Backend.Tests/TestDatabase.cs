using Backend.Data;

using Microsoft.EntityFrameworkCore;

namespace Backend.Tests;

// Creates an in-memory database seeded with the same data as the local development database.
// Every call gets its own database name, so tests do not share data.
public static class TestDatabase
{
    public static AppDbContext CreateSeededContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var appDbContext = new AppDbContext(options);
        DbSeeder.SeedAsync(appDbContext).GetAwaiter().GetResult();

        return appDbContext;
    }
}
