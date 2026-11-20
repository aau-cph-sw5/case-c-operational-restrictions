namespace Backend.Models.DTOs;

// Answer to "what happened to my signing attempt?" after a lost connection.
// A signature either exists (Signed, with the signature) or it does not (NotSigned).
public class SigningAttemptStatusDto
{
    public const string SignedStatus = "Signed";
    public const string NotSignedStatus = "NotSigned";

    public Guid AttemptKey { get; set; }
    public string Status { get; set; } = NotSignedStatus;
    public SignatureDto? Signature { get; set; }
}
