using Backend.Models.Enum;

namespace Backend.Models.Entities;

public class RequiresSignature
{
    public Guid Id { get; set; }
    public Guid OperationalRestrictionId { get; set; }
    public Guid SigneeId { get; set; }
    public DateTime SignedAt { get; set; } = DateTime.UtcNow;
    public RequiresSignatureStatus RequiresSignatureStatus  { get; set; }
    public virtual OperationalRestriction OperationalRestriction { get; set; }
    public virtual User Signee { get; set; }
}
