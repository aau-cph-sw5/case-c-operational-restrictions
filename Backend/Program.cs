using Backend.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<IExampleService, ExampleService>();

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

app.Run();
