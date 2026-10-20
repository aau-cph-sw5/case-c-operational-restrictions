using Microsoft.EntityFrameworkCore;

namespace Backend.Models.Entities;

// Named RoleEntity because the Role enum in User.cs still exists. Rename to Role once the enum is removed (MET-C-22-B).
[PrimaryKey(nameof(Name))]
public class RoleEntity
{
    public string Name { get; set; } = string.Empty;
}
