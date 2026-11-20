namespace Backend.Models.DTOs;

// MODEL - DTO (Data Transfer Object):
// Defines the JSON structure sent over the network for API requests and responses
// Prevents exposing internal database entities directly to the client
// Read model for the signature completeness view (MET-C-013): every operator on
// shift against every restriction in force, including the time it was computed.
public class SignatureCompletenessReportDto
{
    public DateTime ComputedAt { get; set; }

    public int OutstandingSignatureCount { get; set; }

    public List<RestrictionInForceDto> Restrictions { get; set; } = new();

    public List<OperatorSignatureStatusDto> Operators { get; set; } = new();
}
