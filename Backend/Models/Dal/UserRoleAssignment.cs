namespace Backend.Models.Entities;

// MODEL - ENTITY (MVCS):
// Join row connecting a user to one of their roles. A user can have several
// rows, and the rows can be changed in the database without code changes.
public class UserRoleAssignment
{
    public Guid UserId { get; set; }

    public User User { get; set; } = null!;

    public Guid RoleId { get; set; }

    public Role Role { get; set; } = null!;
}
