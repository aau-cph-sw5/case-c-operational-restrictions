using Backend.Models.Entities;
using Backend.Models.Enum;

namespace Backend.Models.DTOs;

public class SignatureDto
{
    public Guid Id { get; set; }
    public Guid RequiresSignatureId { get; set; }
    public Guid OperationalRestrictionId { get; set; }
    public Guid SigneeId { get; set; }
    public UserRole SigneeRole { get; set; }
    public DateTime SignedAt { get; set; }

    public static SignatureDto FromDalEntity(Signature signature) =>
      new()
      {
          Id = signature.Id,
          RequiresSignatureId = signature.RequiresSignatureId,
          OperationalRestrictionId = signature.OperationalRestrictionId,
          SigneeId = signature.SigneeId,
          SigneeRole = signature.SigneeRole,
          SignedAt = signature.SignedAt
      };
}
