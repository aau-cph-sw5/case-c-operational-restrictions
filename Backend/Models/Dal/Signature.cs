using Backend.Models.Enum;

namespace Backend.Models.Entities;

// Immutable once created: a signature can never be changed after it is signed.
public class Signature
{
    public Guid Id { get; init; }
    public Guid RequiresSignatureId { get; init; }
    public Guid OperationalRestrictionId { get; init; }
    public Guid SigneeId { get; init; }
    // The signee's role at the time of signing, so later role changes do not rewrite history.
    public UserRole SigneeRole { get; init; }
    public DateTime SignedAt { get; init; } = DateTime.UtcNow;
    public virtual RequiresSignature RequiresSignature { get; init; }
    public virtual OperationalRestriction OperationalRestriction { get; init; }
    public virtual User Signee { get; init; }

}
