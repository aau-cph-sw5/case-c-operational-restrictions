using Backend.Models.Entities;
using Backend.Models.Enums;

namespace Backend.Models.DTOs;

public class SignatureDto
{
    public Guid SignatureId { get; set; }
    public SignatureType Type { get; set; }
    public bool IsSigned { get; set; }
    public Guid SigneeId { get; set; }
    public UserRole SigneeRole { get; set; }
    public Guid RestrictionId { get; set; }
    public DateTime CreatedAt { get; set; }

    public static SignatureDto FromDalEntity(Signature signature) =>
      new()
      {
          SignatureId = signature.SignatureId,
          Type = signature.Type,
          IsSigned = signature.IsSigned,
          SigneeId = signature.SigneeId,
          SigneeRole = signature.SigneeRole,
          RestrictionId = signature.RestrictionId,
          CreatedAt = signature.CreatedAt
      };
}
