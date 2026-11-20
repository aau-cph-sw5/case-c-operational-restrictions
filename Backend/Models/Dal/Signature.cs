using Backend.Models.Enums;

namespace Backend.Models.Entities;

// Immutable once created: a signature can never be changed after it is signed.
public sealed class Signature
{
    public required Guid Id { get; init; }
    public required Guid RequiresSignatureId { get; init; }
    public required Guid OperationalRestrictionId { get; init; }
    public required Guid SigneeId { get; init; }
    // The signee's role name(s) at the time of signing, so later role changes do not rewrite history.
    public required string SigneeRole { get; init; }
    public required DateTime SignedAt { get; init; } = DateTime.UtcNow;
    // Evidence about the device the signature was made from, written together with the signature.
    // It is not an input to any authorization decision.
    public DeviceClass DeviceClass { get; init; } = DeviceClass.Unknown;
    public string UserAgent { get; init; } = string.Empty;
    // Client-generated key of the signing attempt; lets a retry find the committed signature.
    public Guid? AttemptKey { get; init; }
}
