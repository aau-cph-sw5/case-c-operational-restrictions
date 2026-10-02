using System.Reflection;
using Backend.Data;
using Backend.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<IExampleService, ExampleService>();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Enable CORS with explicit frontend origins for security & cookie/auth header support
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173",
                "http://127.0.0.1:5173"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors();

app.MapControllers();

// A simple Hello World endpoint
app.MapGet("/api/hello", () => new
{
    message = "Hello from .NET 10 Web API!",
    timestamp = DateTime.UtcNow
});

// Seed the local database (Development only, and not while EF tools are running)
if (app.Environment.IsDevelopment() &&
    Assembly.GetEntryAssembly()?.GetName().Name != "ef")
{
    using var scope = app.Services.CreateScope();
   await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

app.Run();