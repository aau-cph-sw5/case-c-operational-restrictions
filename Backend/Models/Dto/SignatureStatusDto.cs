
namespace Backend.Models.DTOs;

public class SignatureStatusDto
{
    public Guid RequiresSignatureId { get; set; }
    public Guid SigneeId { get; set; }
    public string SigneeName { get; set; } = string.Empty;
    public string SigneeRole { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
