namespace Backend.Models.DTOs;

// MODEL - DTO (Data Transfer Object):
// One cell of the signature completeness matrix: has the given operator signed the given restriction?
public class SignatureCellDto
{
    public Guid RestrictionId { get; set; }

    public bool IsSigned { get; set; }

    public DateTime? SignedAt { get; set; }
}
