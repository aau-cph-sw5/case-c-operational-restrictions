using Backend.Models.Enums;

namespace Backend.Models.Entities;

// MODEL - ENTITY (MVCS):
// Represents database tables managed and mapped by EF Core to PostgreSQL
public class Signature
{
    public Guid SignatureId { get; set; }
    public SignatureType Type { get; set; }
    public bool IsSigned { get; set; }
    public Guid SigneeId { get; set; }
    public UserRole SigneeRole { get; set; }
    public Guid RestrictionId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; } = DateTime.Now;
    
}
