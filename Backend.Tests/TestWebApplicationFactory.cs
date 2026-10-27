using Backend.Data;
using Backend.Models.Entities;
using Backend.Services.Auth;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Backend.Tests;

// Runs the real API in memory for integration tests.
// The "Testing" environment skips the migrations and seeding in Program.cs,
// and PostgreSQL is replaced by a seeded in-memory database.
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Jwt:SigningKey", "integration-test-signing-key-at-least-32-characters");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }

    public async Task SeedAsync()
    {
        using var scope = Services.CreateScope();
        await DbSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    // Mints a real JWT for the given user id, the same way AuthService does on login.
    public string CreateTokenFor(Guid userId)
    {
        using var scope = Services.CreateScope();
        var jwtService = scope.ServiceProvider.GetRequiredService<JwtService>();

        return jwtService.Mint(new User { Id = userId, Name = "Test User", Email = $"{userId}@example.com" }).AccessToken;
    }
}
