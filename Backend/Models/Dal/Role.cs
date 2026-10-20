using Microsoft.EntityFrameworkCore;

namespace Backend.Models.Entities;

[PrimaryKey(nameof(Name))]
public class Role
{
    public string Name { get; set; } = string.Empty;
}
