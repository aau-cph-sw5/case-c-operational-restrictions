using Backend.Models.Enum;

namespace Backend.Models.Entities;

// Each row is one version of the restriction; edits insert a new row instead of changing this one.
public class OperationalRestriction
{
  public Guid Id { get; set; }
  public Guid OriginatorUserId { get; set; }

  // CreatedBy is the user who made this version (on the first row, the creator; on later rows, the editor).
  // OriginatorUserId is copied unchanged to every version and always points to the restriction's owner.
  public Guid CreatedBy { get; set; }
  public RestrictionState State { get; set; }
  public DateTime StartDate { get; set; }
  public DateTime EndDate { get; set; }
  public DateTime CreatedAt { get; set; }
  public DateTime ArchivedAt { get; set; }
  public Guid? OriginalOperationalRestrictionId { get; set; }
}