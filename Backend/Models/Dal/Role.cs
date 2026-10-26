using Microsoft.EntityFrameworkCore;

namespace Backend.Models.Entities;

[PrimaryKey(nameof(Name))]
public class Role
{
    public string Name { get; set; } = string.Empty;

    public ICollection<RolePermissionAssignment> PermissionAssignments { get; set; } = new List<RolePermissionAssignment>();

    public ICollection<UserRoleAssignment> UserAssignments { get; set; } = new List<UserRoleAssignment>();
}
