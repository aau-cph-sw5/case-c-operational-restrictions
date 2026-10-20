using Microsoft.EntityFrameworkCore;

namespace Backend.Models.Entities;

// Named RoleEntity because the Role enum in User.cs still exists. Rename to Role once the enum is removed (MET-C-22-B).
[Index(nameof(Name), IsUnique = true)]
public class RoleEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
