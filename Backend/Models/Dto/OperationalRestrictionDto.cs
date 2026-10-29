using Backend.Models.Enums;
using Backend.Models.Entities;
namespace Backend.Models.DTOs;


// MODEL - DTO (Data Transfer Object):
// Defines the JSON structure sent over the network for API requests and responses
// Prevents exposing internal database entities directly to the client
public class OperationalRestrictionDto
{
  public Guid RestrictionId { get; set; }
  // before named: status --> now named: state
  public RestrictionState State { get; set; } = RestrictionState.Empty;

  public String RestrictionDescription { get; set; } = string.Empty;

  public Guid OriginatorUserId { get; set; }

  public DateTime StartDate { get; set; } = DateTime.Now;

  public DateTime? EndDate { get; set; } = DateTime.Now;

  public Guid CreatedBy { get; set; }
  
  public DateTime CreatedAt { get; set; }
  
  public DateTime? ArchivedAt { get; set; }
  
  public Guid? OriginalOperationalRestrictionId { get; set; }

  public static OperationalRestrictionDto FromOperationalRestrictionToDto(OperationalRestriction operationalRestriction) =>
    new()
    {
      RestrictionId = operationalRestriction.RestrictionId,
      OriginatorUserId = operationalRestriction.OriginatorUserId,
      RestrictionDescription = operationalRestriction.RestrictionDescription,
      CreatedBy = operationalRestriction.CreatedBy,
      State = operationalRestriction.State,
      StartDate = operationalRestriction.StartDate,
      EndDate = operationalRestriction.EndDate,
      CreatedAt = operationalRestriction.CreatedAt,
      ArchivedAt = operationalRestriction.ArchivedAt,
      OriginalOperationalRestrictionId = operationalRestriction.OriginalOperationalRestrictionId
    };
}
/*
using Backend.Models.Entities;
using Backend.Models.Enums;

namespace Backend.Models.DTOs;

public class OperationalRestrictionDto
{
  public Guid Id { get; set; }*
  public Guid OriginatorUserId { get; set; }*
  public Guid CreatedBy { get; set; }*
  public RestrictionStatus Status { get; set; }*
  public DateTime StartDate { get; set; }*
  public DateTime EndDate { get; set; }*
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
}
*/