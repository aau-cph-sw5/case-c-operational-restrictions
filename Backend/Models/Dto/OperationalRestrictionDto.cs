using Backend.Models.Enums;
namespace Backend.Models.DTOs;


// MODEL - DTO (Data Transfer Object):
// Defines the JSON structure sent over the network for API requests and responses
// Prevents exposing internal database entities directly to the client
public class OperationalRestrictionDto
{
    public Guid RestrictionId { get; set; }

    public RestrictionState State { get; set; } = RestrictionState.Empty;

    public String RestrictionDescription { get; set; } = string.Empty;

    public Guid OriginatorUserId { get; set; }
  
    public DateTime StartDate { get; set; } = DateTime.Now;
    
    public DateTime? EndDate { get; set; } = DateTime.Now;
}
