namespace Backend.Models.DTOs;

// MODEL - DTO (Data Transfer Object):
// A restriction currently in force, as a column in the signature completeness matrix.
public class RestrictionInForceDto
{
    public Guid RestrictionId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Reference { get; set; }
}
