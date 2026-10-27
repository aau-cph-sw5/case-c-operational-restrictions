using Backend.Models.Enum;

namespace Backend.Models.Entities;

// Immutable once created: a signature can never be changed after it is signed.
public sealed class Signature
{
    public required Guid Id { get; init; }
    public required Guid RequiresSignatureId { get; init; }
    public  required Guid OperationalRestrictionId { get; init; }
    public required Guid SigneeId { get; init; }
    // The signee's role at the time of signing, so later role changes do not rewrite history.
    public required UserRole SigneeRole { get; init; }
    public required DateTime SignedAt { get; init; } = DateTime.UtcNow;
}
