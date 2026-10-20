using Microsoft.EntityFrameworkCore;

namespace Backend.Models.Entities;

// Links a permission to a role. Each row means the role has that permission.
[PrimaryKey(nameof(RoleId), nameof(PermissionId))]
public class PermissionHasRole
{
    public Guid RoleId { get; set; }

    public RoleEntity Role { get; set; } = null!;

    public Guid PermissionId { get; set; }

    public PermissionEntity Permission { get; set; } = null!;
}
