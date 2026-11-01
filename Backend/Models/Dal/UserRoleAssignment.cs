using Microsoft.EntityFrameworkCore;

namespace Backend.Models.Entities;

// Links a user to a role. Each row means the user has that role.
[PrimaryKey(nameof(UserId), nameof(RoleName))]
public class UserRoleAssignment
{
    public Guid UserId { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public User User { get; set; } = null!;

    public Role Role { get; set; } = null!;
}
