using Microsoft.EntityFrameworkCore;

namespace Backend.Models.Entities;

// Not named Permission, since analyzer rule CA1711 reserves that suffix for .NET security permission types.
[PrimaryKey(nameof(Name))]
public class PermissionEntity
{
    public string Name { get; set; } = string.Empty;
}
