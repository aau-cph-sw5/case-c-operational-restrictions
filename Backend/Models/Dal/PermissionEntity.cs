using Microsoft.EntityFrameworkCore;

namespace Backend.Models.Entities;

[PrimaryKey(nameof(Name))]
public class PermissionEntity
{
    public string Name { get; set; } = string.Empty;
}
