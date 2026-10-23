using Backend.Models.Enums;

namespace Backend.Models.DTOs;

// DTO for creating a new operational restriction
public class CreateRestrictionDto
{
    public Guid OriginatorUserId { get; set; }
    public string RestrictionDescription { get; set; } = string.Empty;
    public DateTime StartDate { get; set; } = DateTime.Now;
    public DateTime EndDate { get; set; } = DateTime.Now;
}