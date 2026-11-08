using System.Diagnostics.CodeAnalysis;

namespace Backend.Models.Entities;

// MODEL - ENTITY (MVCS):
// A single action a user can be allowed to perform, identified by its name
// (e.g. "OperationalRestriction:Read"). Roles are granted permissions, so
// adding or removing a permission from a role only changes database rows.
[SuppressMessage("Design", "CA1711:Identifiers should not have incorrect suffix",
    Justification = "Permission is the domain concept this entity represents.")]
public class Permission
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public List<RolePermission> RolePermissions { get; set; } = new();
}
