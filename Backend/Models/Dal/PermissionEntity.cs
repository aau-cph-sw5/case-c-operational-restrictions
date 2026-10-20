using Microsoft.EntityFrameworkCore;

namespace Backend.Models.Entities;

[Index(nameof(Name), IsUnique = true)]
public class PermissionEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
