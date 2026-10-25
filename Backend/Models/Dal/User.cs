using Backend.Models.Enum;
using Microsoft.EntityFrameworkCore;

namespace Backend.Models.Entities;

[Index(nameof(Email), IsUnique = true)]
public class User
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public UserWorkingStatus  WorkingStatus { get; set; }

    public bool InCharge { get; set; }
}
