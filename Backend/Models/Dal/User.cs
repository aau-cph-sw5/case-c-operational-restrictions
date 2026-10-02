namespace Backend.Models.Entities;

public enum Role { Admin, Clinician, Scheduler }

// MODEL - ENTITY (MVCS):
// Represents database tables managed and mapped by EF Core to PostgreSQL
public class User
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public Role Role { get; set; }
}