using Backend.Models.Entities;

namespace Backend.Models.DTOs;

// A restriction the logged-in user still has to read and sign.
public class RequiredSignatureDto
{
    public Guid RequiresSignatureId { get; set; }
    public Guid RestrictionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime RequiredSince { get; set; }

    // Expects OperationalRestriction to be loaded (Include) on the required signature.
    public static RequiredSignatureDto FromDalEntity(RequiresSignature requiredSignature) =>
        new()
        {
            RequiresSignatureId = requiredSignature.Id,
            RestrictionId = requiredSignature.OperationalRestrictionId,
            Title = requiredSignature.OperationalRestriction?.Title ?? string.Empty,
            Reference = requiredSignature.OperationalRestriction?.Reference,
            StartDate = requiredSignature.OperationalRestriction?.StartDate ?? default,
            EndDate = requiredSignature.OperationalRestriction?.EndDate,
            RequiredSince = requiredSignature.CreatedAt
        };
}
