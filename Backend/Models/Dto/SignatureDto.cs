using Backend.Models.Entities;

namespace Backend.Models.DTOs;

public class SignatureDto
{
    public Guid Id { get; set; }
    public Guid RequiresSignatureId { get; set; }
    public Guid OperationalRestrictionId { get; set; }
    public Guid SigneeId { get; set; }
    public string SigneeRole { get; set; } = string.Empty;
    public DateTime SignedAt { get; set; }
    // Coarse device class (Phone, Tablet, Desktop, Unknown) recorded when the signature was made.
    public string DeviceClass { get; set; } = "Unknown";

    public static SignatureDto FromDalEntity(Signature signature) =>
      new()
      {
          Id = signature.Id,
          RequiresSignatureId = signature.RequiresSignatureId,
          OperationalRestrictionId = signature.OperationalRestrictionId,
          SigneeId = signature.SigneeId,
          SigneeRole = signature.SigneeRole,
          SignedAt = signature.SignedAt,
          DeviceClass = signature.DeviceClass.ToString()
      };
}
