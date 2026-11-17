using System.Diagnostics.CodeAnalysis;

namespace Backend.Models.Entities;

// MODEL - ENTITY (MVCS):
// Join row connecting a role to one of its permissions.
[SuppressMessage("Design", "CA1711:Identifiers should not have incorrect suffix",
    Justification = "Permission is the domain concept this join entity represents.")]
public class RolePermission
{
    public Guid RoleId { get; set; }

    public Role Role { get; set; } = null!;

    public Guid PermissionId { get; set; }

    public Permission Permission { get; set; } = null!;
}
