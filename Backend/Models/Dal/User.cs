using Backend.Models.Enums;
using Microsoft.EntityFrameworkCore;
namespace Backend.Models.Entities;

public enum Role { Originator, Dom, Operator }

[Index(nameof(Email), IsUnique = true)]
public class User
{
    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.Empty;

    public DateTime StartDate { get; set; } = DateTime.Now;

    public DateTime? EndDate { get; set; } = DateTime.Now;

    public UserStatus UserStatus { get; set; } = UserStatus.Empty;

    public WorkingStatus WorkingStatus { get; set; } = WorkingStatus.Empty;

    public Guid ReportsTo { get; set; } = Guid.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; } = DateTime.Now;

}
