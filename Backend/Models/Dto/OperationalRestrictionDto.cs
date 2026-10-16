using Backend.Models.Enums;
namespace Backend.Models.DTOs;


// MODEL - DTO (Data Transfer Object):
// Defines the JSON structure sent over the network for API requests and responses
// Prevents exposing internal database entities directly to the client
public class OperationalRestrictionDto
{
    public Guid RestrictionId { get; set; }

    public RestrictionState State { get; set; } = RestrictionState.Empty;

    public String RestrionDescription { get; set; } = string.Empty;
}
