using Backend.Models.Entities;
using Backend.Models.Enum;

namespace Backend.Models.DTOs;

public class OperationalRestrictionDto
{
  public Guid Id { get; set; }
  public Guid OriginatorUserId { get; set; }
  public Guid CreatedBy { get; set; }
  public RestrictionState State { get; set; }
  public DateTime StartDate { get; set; }
  public DateTime EndDate { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime ArchivedAt { get; set; }
  public Guid? OriginalOperationalRestrictionId { get; set; }

  public static OperationalRestrictionDto FromDalEntity(OperationalRestriction operationalRestriction) =>
    new()
    {
      Id = operationalRestriction.Id,
      OriginatorUserId = operationalRestriction.OriginatorUserId,
      CreatedBy = operationalRestriction.CreatedBy,
      State = operationalRestriction.State,
      StartDate = operationalRestriction.StartDate,
      EndDate = operationalRestriction.EndDate,
      CreatedAt = operationalRestriction.CreatedAt,
      ArchivedAt = operationalRestriction.ArchivedAt,
      OriginalOperationalRestrictionId = operationalRestriction.OriginalOperationalRestrictionId
    };
}