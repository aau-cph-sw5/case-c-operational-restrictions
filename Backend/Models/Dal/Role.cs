namespace Backend.Models.Entities;

// MODEL - ENTITY (MVCS):
// A named role (e.g. "DutyOperationsManager"). Users are assigned roles and
// roles are granted permissions, both through database rows, so role changes
// never require code changes.
public class Role
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public List<RolePermission> RolePermissions { get; set; } = new();

    public List<UserRoleAssignment> UserRoleAssignments { get; set; } = new();
}
