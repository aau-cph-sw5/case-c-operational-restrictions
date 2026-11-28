using Backend.Models.Enums;

namespace Backend.Models.DTOs;

// MET-C-026: lets the client find out what happened to a signing attempt
// whose response was lost, e.g. after a connection failure.
public class SigningStatusDto
{
    // The version the client asked about (the one it tried to sign).
    public Guid RequestedVersionId { get; set; }

    // False when the restriction has changed since the client loaded it.
    public bool IsLatestVersion { get; set; }

    public Guid LatestVersionId { get; set; }
    public int LatestVersion { get; set; }
    public RestrictionState State { get; set; }

    // The current user's signatures on the latest version. If the signature the
    // client tried to make is here, the attempt was committed.
    public List<SignatureDto> MySignatures { get; set; } = new();
}
