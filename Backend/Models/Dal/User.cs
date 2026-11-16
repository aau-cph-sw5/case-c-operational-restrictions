using Backend.Models.Enums;

using Microsoft.EntityFrameworkCore;

namespace Backend.Models.Entities;

[Index(nameof(Email), IsUnique = true)]
public class User
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    // Roles are not stored on the user: they come from UserRoleAssignments,
    // so a user can hold several roles and role changes are data-only.

    public UserWorkingStatus WorkingStatus { get; set; }

    public bool InCharge { get; set; }
}
