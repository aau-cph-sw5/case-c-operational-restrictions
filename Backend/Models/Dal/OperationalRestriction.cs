using Backend.Models.Enums;
namespace Backend.Models.Entities;

// MODEL - ENTITY (MVCS):
// Represents database tables managed and mapped by EF Core to PostgreSQL
public class OperationalRestriction
{
    public Guid RestrictionId { get; set; }

    public Guid OriginatorUserId { get; set; }

    public Guid CreatedBy { get; set; }

    public List<Signature> SignatureSheet { get; set; } = new List<Signature>();

    public RestrictionState State { get; set; } = RestrictionState.Empty;

    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; }

    public DateTime? ArchivedAt { get; set; }

    public DateTime? RetentionUntil { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public string SituationDescription { get; set; } = string.Empty;
    public string RestrictionDescription { get; set; } = string.Empty;
    public string CancellationDescription { get; set; } = string.Empty;

    public List<File> Files { get; set; } = new List<File>();

    public int Version { get; set; }

    public Guid ReviewerId { get; set; }

    public Boolean ApprovedForSigning { get; set; }

    public String ReviewDescription { get; set; } = string.Empty;

    public List<Guid> AssignedOperatorsIds { get; set; } = new List<Guid>();

    public DateTime? UpdatedAt { get; set; }

    public Guid? OriginalOperationalRestrictionId { get; set; }
}
