using Microsoft.EntityFrameworkCore;

namespace Backend.Models.Entities;

// Links a permission to a role. Each row means the role has that permission.
[PrimaryKey(nameof(RoleName), nameof(PermissionName))]
public class PermissionHasRole
{
    public string RoleName { get; set; } = string.Empty;

    public RoleEntity Role { get; set; } = null!;

    public string PermissionName { get; set; } = string.Empty;

    public PermissionEntity Permission { get; set; } = null!;
}
