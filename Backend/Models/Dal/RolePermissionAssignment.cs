using Microsoft.EntityFrameworkCore;

namespace Backend.Models.Entities;

// Links a permission to a role. Each row means the role has that permission.
[PrimaryKey(nameof(RoleName), nameof(PermissionName))]
public class RolePermissionAssignment
{
    public string RoleName { get; set; } = string.Empty;

    public string PermissionName { get; set; } = string.Empty;

    public Role Role { get; set; } = null!;

    public PermissionEntity Permission { get; set; } = null!;
}
