using System.ComponentModel.DataAnnotations;

namespace Backend.Models.DTOs;

public class AcceptInviteDto
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(128)]
    public string Password { get; set; } = string.Empty;
}
