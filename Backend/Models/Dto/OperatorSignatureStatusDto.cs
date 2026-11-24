namespace Backend.Models.DTOs;

// MODEL - DTO (Data Transfer Object):
// An operator on shift, as a row in the signature completeness matrix.
public class OperatorSignatureStatusDto
{
    public Guid OperatorId { get; set; }

    public string Name { get; set; } = string.Empty;

    public List<SignatureCellDto> Cells { get; set; } = new();
}
