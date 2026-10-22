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

    public DateTime StartDate { get; set; }= DateTime.Now;

    public DateTime? EndDate { get; set; }= DateTime.Now;

    public DateTime CreatedAt { get; set; }= DateTime.Now;

    public DateTime? ArchivedAt { get; set; }= DateTime.Now;

    public DateTime? RetentionUntil { get; set; }= DateTime.Now;

    public string RestrictionDescription { get; set; } = string.Empty;

    public List<File> Files { get; set; } = new List<File>();

    public int Version { get; set; }

    public Guid ReviewerId { get; set; }

    public Boolean ApprovedForSigning { get; set; }

    public String ReviewDescription { get; set; } = string.Empty;

    public List<Guid> AssignedOperatorsIds { get; set; } = new List<Guid>();

    public DateTime? UpdatedAt { get; set; } = DateTime.MinValue;

    public Guid? OriginalOperationalRestrictionId { get; set; }

}

/* pre version from develop
using Backend.Models.Entities;
using Backend.Models.Enum;
namespace Backend.Models.DTOs;

public class OperationalRestrictionDto
{
  public Guid Id { get; set; }
  public Guid OriginatorUserId { get; set; }
  public Guid CreatedBy { get; set; }
  public RestrictionStatus Status { get; set; }
  public DateTime StartDate { get; set; }
  public DateTime EndDate { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime ArchivedAt { get; set; }
  public Guid? OriginalOperationalRestrictionId { get; set; }
  public static OperationalRestrictionDto FromDalEntity(OperationalRestriction operationalRestriction) =>
    new()
    {
      Id = operationalRestriction.Id,
      OriginatorUserId = operationalRestriction.Originator,
      CreatedBy = operationalRestriction.CreatedBy,
      Status = operationalRestriction.Status,
      StartDate = operationalRestriction.StartDate,
      EndDate = operationalRestriction.EndDate,
      CreatedAt = operationalRestriction.CreatedAt,
      ArchivedAt = operationalRestriction.ArchivedAt,
      OriginalOperationalRestrictionId = operationalRestriction.OriginalOperationalRestrictionId
    }; 
    */