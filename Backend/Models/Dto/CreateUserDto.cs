using System.ComponentModel.DataAnnotations;

namespace Backend.Models.DTOs;

public class CreateUserDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;

    // Roles are assigned through UserRoleAssignments in the database, not here.
}
