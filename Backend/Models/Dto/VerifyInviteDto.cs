using System.ComponentModel.DataAnnotations;

namespace Backend.Models.DTOs;

public class VerifyInviteDto
{
    [Required]
    public string Token { get; set; } = string.Empty;
}
