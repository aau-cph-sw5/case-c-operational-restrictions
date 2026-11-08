using Backend.Models.Entities;
using Backend.Models.Enums;
namespace Backend.Models.DTOs;


// MODEL - DTO (Data Transfer Object):
// Defines the JSON structure sent over the network for API requests and responses
// Prevents exposing internal database entities directly to the client
public class OperationalRestrictionDto
{
    public Guid Id { get; set; }
    public RestrictionState State { get; set; } = RestrictionState.Empty;

    public string Title { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public string SituationDescription { get; set; } = string.Empty;
    public string RestrictionDescription { get; set; } = string.Empty;
    public string CancellationDescription { get; set; } = string.Empty;

    public Guid OriginatorUserId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public Guid CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? ArchivedAt { get; set; }

    public Guid? OriginalOperationalRestrictionId { get; set; }
    public SignatureDto? OriginatorSignature { get; set; }
    public List<SignatureDto> Signatures { get; set; } = new();

    public Guid ReviewerId { get; set; }

    public string ReviewDescription { get; set; } = string.Empty;

    public static OperationalRestrictionDto FromOperationalRestrictionToDto(OperationalRestriction operationalRestriction)
    {
        var signatures = operationalRestriction.SignatureSheet?
            .Select(SignatureDto.FromDalEntity)
            .ToList() ?? new List<SignatureDto>();
        var originatorSignature = signatures.FirstOrDefault(s => s.SigneeRole == UserRole.Originator);

        return new()
        {
            Id = operationalRestriction.RestrictionId,
            OriginatorUserId = operationalRestriction.OriginatorUserId,
            Title = operationalRestriction.Title,
            Reference = operationalRestriction.Reference,
            SituationDescription = operationalRestriction.SituationDescription,
            RestrictionDescription = operationalRestriction.RestrictionDescription,
            CancellationDescription = operationalRestriction.CancellationDescription,
            CreatedBy = operationalRestriction.CreatedBy,
            State = operationalRestriction.State,
            StartDate = operationalRestriction.StartDate,
            EndDate = operationalRestriction.EndDate,
            CreatedAt = operationalRestriction.CreatedAt,
            ArchivedAt = operationalRestriction.ArchivedAt,
            OriginalOperationalRestrictionId = operationalRestriction.OriginalOperationalRestrictionId,
            ReviewerId = operationalRestriction.ReviewerId,
            ReviewDescription = operationalRestriction.ReviewDescription,
            Signatures = signatures,
            OriginatorSignature = originatorSignature
        };
    }
}
