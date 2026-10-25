using Backend.Models.Entities;

namespace Backend.Models.DTOs;

public class SignatureDto
{
  public Guid Id { get; set; }
  public Guid RequiresSignatureId { get; set; }
  public Guid OperationalRestrictionId { get; set; }
  public Guid SigneeId { get; set; }
  public DateTime SignedAt { get; set; }

  public static SignatureDto FromDalEntity(Signature signature) =>
    new()
    {
      Id = signature.Id,
      RequiresSignatureId = signature.RequiresSignatureId,
      OperationalRestrictionId = signature.OperationalRestrictionId,
      SigneeId = signature.SigneeId,
      SignedAt = signature.SignedAt
    };
}
